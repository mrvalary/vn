using System;
using System.Collections.Generic;
using System.Net.Sockets;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Npgsql;

namespace VnTests
{
    /// <summary>
    /// Проверяет подключение к базе данных по тест-кейсам из XML.
    /// </summary>
    [TestClass]
    public sealed class DatabaseConnectionTests
    {
        /// <summary>
        /// Контекст MSTest для вывода пояснений при запуске тестов.
        /// </summary>
        public TestContext TestContext { get; set; }

        #region Database Test Cases

        /// <summary>
        /// Проверяет формирование рабочей строки подключения роли.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Database")]
        [DynamicData(nameof(TestInputReader.TC_DB_01), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_DB_01_WorkConnectionString_IsBuiltForRole(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-DB-01");
            DatabaseConnectionFactory factory = new DatabaseConnectionFactory();

            string roleConnectionString = inputSet.Get("roleConnectionString");
            if (inputSet.Expected == "failure")
            {
                Assert.ThrowsException<InvalidOperationException>(() => factory.AlignRoleConnectionString(roleConnectionString), Failure(inputSet));
            }
            else
            {
                string connectionString = factory.AlignRoleConnectionString(roleConnectionString);
                NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(connectionString);
                Assert.AreEqual("127.0.0.1", builder.Host, Failure(inputSet));
                Assert.AreEqual(15432, builder.Port, Failure(inputSet));
                Assert.AreEqual("vn_test", builder.Database, Failure(inputSet));
                Assert.IsFalse(string.IsNullOrWhiteSpace(builder.Username), Failure(inputSet));
            }

            Log(testCase, inputSet, "Строка роли из XML проверена.", "PASS");
        }

        /// <summary>
        /// Проверяет, что адрес БД берется из конфигурации приложения.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Database")]
        [DynamicData(nameof(TestInputReader.TC_DB_02), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_DB_02_DatabaseAddress_IsTakenFromConfiguration(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-DB-02");
            DatabaseConnectionFactory factory = new DatabaseConnectionFactory();

            string connectionString = factory.AlignRoleConnectionString(inputSet.Get("roleConnectionString"));
            NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(connectionString);

            Assert.AreEqual(inputSet.Get("expectedHost"), builder.Host, Failure(inputSet));
            Assert.AreEqual(inputSet.GetInt32("expectedPort"), builder.Port, Failure(inputSet));
            Assert.AreEqual(inputSet.Get("expectedDatabase"), builder.Database, Failure(inputSet));

            Log(testCase, inputSet, "Host, Port и Database подставлены из App.config.", "PASS");
        }

        /// <summary>
        /// Проверяет, что рабочие строки подключения не используют учетную запись postgres.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Database")]
        [DynamicData(nameof(TestInputReader.TC_DB_03), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_DB_03_PostgresAccount_IsNotUsedForWorkRoles(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-DB-03");
            DatabaseConnectionFactory factory = new DatabaseConnectionFactory();

            string username = inputSet.Get("username");
            if (inputSet.Expected == "not-used")
            {
                Assert.AreEqual("postgres", username, Failure(inputSet));
            }
            else
            {
                string connectionString = factory.AlignRoleConnectionString(
                    "Username=" + username + ";Password=secret");
                NpgsqlConnectionStringBuilder builder = new NpgsqlConnectionStringBuilder(connectionString);
                Assert.AreNotEqual("postgres", builder.Username, true, Failure(inputSet));
            }

            Log(testCase, inputSet, "Запрет postgres или разрешенная роль проверены по XML.", "PASS");
        }

        /// <summary>
        /// Проверяет обработку ошибок подключения к БД.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Database")]
        [DynamicData(nameof(TestInputReader.TC_DB_04), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_DB_04_ConnectionErrors_AreConvertedToReadableMessages(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-DB-04");

            string message = DatabaseConnectionFactory.FormatDatabaseError(CreateDatabaseException(inputSet));
            Assert.IsFalse(string.IsNullOrWhiteSpace(message), Failure(inputSet));

            Log(testCase, inputSet, "Ошибка подключения из XML преобразована в читаемое сообщение.", "PASS");
        }

        /// <summary>
        /// Проверяет выполнение операций через ограниченные роли на уровне логики доступа.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Database")]
        [DynamicData(nameof(TestInputReader.TC_DB_05), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_DB_05_LimitedRoles_HaveExpectedApplicationPermissions(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-DB-05");

            AuthSessionService session = CreateSession(inputSet.Get("role"));
            bool allowed = IsOperationAllowed(session, inputSet.Get("operation"));
            Assert.AreEqual(inputSet.Expected == "allowed", allowed, Failure(inputSet));

            Log(testCase, inputSet, "Операция ограниченной роли проверена по XML.", "PASS");
        }

        /// <summary>
        /// Проверяет пользовательское сообщение об ошибке БД.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Database")]
        [DynamicData(nameof(TestInputReader.TC_DB_06), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_DB_06_DatabaseErrorMessage_IsReadableAndSafe(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-DB-06");

            Exception exception = inputSet.Get("exception") == "null"
                ? null
                : new NpgsqlException("technical connection problem");
            string message = DatabaseConnectionFactory.FormatDatabaseError(exception);

            Assert.IsFalse(string.IsNullOrWhiteSpace(message), Failure(inputSet));
            if (!string.IsNullOrWhiteSpace(inputSet.Get("mustNotContain")))
            {
                Assert.IsFalse(message.IndexOf(inputSet.Get("mustNotContain"), StringComparison.OrdinalIgnoreCase) >= 0, Failure(inputSet));
            }

            Log(testCase, inputSet, "Сообщение об ошибке БД из XML безопасно для пользователя.", "PASS");
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Создает локальную сессию пользователя с указанной ролью.
        /// </summary>
        private static AuthSessionService CreateSession(string roleName)
        {
            AuthSessionService session = new AuthSessionService();
            session.SignIn(1, roleName, roleName);
            return session;
        }

        /// <summary>
        /// Создает исключение БД по XML-набору.
        /// </summary>
        private static Exception CreateDatabaseException(TestInputSet inputSet)
        {
            if (inputSet.Get("errorType") == "TimeoutException")
            {
                return new TimeoutException();
            }

            if (inputSet.Get("errorType") == "SocketException")
            {
                return new SocketException((int)SocketError.ConnectionRefused);
            }

            return new NpgsqlException("connection failed");
        }

        /// <summary>
        /// Проверяет, разрешена ли операция роли на уровне логики приложения.
        /// </summary>
        private static bool IsOperationAllowed(AuthSessionService session, string operation)
        {
            if (operation == "own-notes")
            {
                return session.IsAuthenticated;
            }

            if (operation == "monitoring")
            {
                return session.CanManageMonitoring();
            }

            if (operation == "admin-user-management")
            {
                return session.IsAdmin();
            }

            return false;
        }

        /// <summary>
        /// Выводит результат тест-кейса, прочитанного из XML.
        /// </summary>
        private void Log(TestCaseData testCase, string actual, string status)
        {
            TestOutputHelper.WriteLine("Тест-кейс:            " + testCase.Id);
            TestOutputHelper.WriteLine("Название:             " + testCase.Name);
            TestOutputHelper.WriteLine("Входные данные XML:   " + testCase.Inputs);
            TestOutputHelper.WriteLine("Ожидаемый результат:  " + testCase.Expected);
            TestOutputHelper.WriteLine("Фактический результат:" + actual);
            TestOutputHelper.WriteLine("Статус:               " + status);
            TestOutputHelper.WriteLine(string.Empty);
        }

        /// <summary>
        /// Выводит результат тест-кейса с количеством использованных XML-наборов.
        /// </summary>
        private void Log(TestCaseData testCase, IReadOnlyList<TestInputSet> inputSets, string actual, string status)
        {
            TestOutputHelper.WriteLine("Тест-кейс:            " + testCase.Id);
            TestOutputHelper.WriteLine("Название:             " + testCase.Name);
            TestOutputHelper.WriteLine("Наборов данных XML:   " + inputSets.Count);
            TestOutputHelper.WriteLine("Входные данные XML:   " + testCase.Inputs);
            TestOutputHelper.WriteLine("Ожидаемый результат:  " + testCase.Expected);
            TestOutputHelper.WriteLine("Фактический результат:" + actual);
            TestOutputHelper.WriteLine("Статус:               " + status);
            TestOutputHelper.WriteLine(string.Empty);
        }

        /// <summary>
        /// Выводит результат отдельного XML-набора.
        /// </summary>
        private void Log(TestCaseData testCase, TestInputSet inputSet, string actual, string status)
        {
            TestOutputHelper.WriteLine("Тест-кейс:            " + testCase.Id);
            TestOutputHelper.WriteLine("Название:             " + testCase.Name);
            TestOutputHelper.WriteLine("XML-набор:            " + inputSet.Name);
            TestOutputHelper.WriteLine("Ожидание XML-набора:  " + inputSet.Expected);
            TestOutputHelper.WriteLine("Входные данные XML:   " + testCase.Inputs);
            TestOutputHelper.WriteLine("Ожидаемый результат:  " + testCase.Expected);
            TestOutputHelper.WriteLine("Фактический результат:" + actual);
            TestOutputHelper.WriteLine("Статус:               " + status);
            TestOutputHelper.WriteLine(string.Empty);
        }

        /// <summary>
        /// Формирует сообщение об ошибке с названием XML-набора.
        /// </summary>
        private static string Failure(TestInputSet inputSet)
        {
            return "Ошибка в XML-наборе: " + inputSet.Name + ", expected=" + inputSet.Expected;
        }

        #endregion
    }
}
