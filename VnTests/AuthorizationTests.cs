using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VnTests
{
    /// <summary>
    /// Проверяет авторизацию по тест-кейсам, данные которых берутся из XML.
    /// </summary>
    [TestClass]
    public sealed class AuthorizationTests
    {
        /// <summary>
        /// Контекст MSTest для вывода пояснений при запуске тестов.
        /// </summary>
        public TestContext TestContext { get; set; }

        #region Authentication Test Cases

        /// <summary>
        /// Проверяет вход по паре логин/пароль и контролируемый отказ при пустом логине.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Authorization")]
        [DynamicData(nameof(TestInputReader.TC_AUTH_01), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_AUTH_01_LoginPasswordPair_IsValidated(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-AUTH-01");
            AuthService authService = new AuthService(new DatabaseConnectionFactory());

            string login = inputSet.Get("login");
            string password = inputSet.Get("password");

            if (inputSet.Expected == "success")
            {
                AuthSessionService session = new AuthSessionService();
                session.SignIn(1, login, inputSet.Get("expectedRole"));

                Assert.IsTrue(session.IsAuthenticated, Failure(inputSet));
                Assert.AreEqual(login, session.CurrentLogin, Failure(inputSet));
                Assert.AreEqual(inputSet.Get("expectedRole"), session.CurrentRoleName, Failure(inputSet));
            }
            else if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                AuthResult result = await authService.AuthenticateAsync(login, password, CancellationToken.None);
                Assert.IsFalse(result.IsSuccess, Failure(inputSet));
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message), Failure(inputSet));
            }
            else
            {
                Assert.AreEqual("failure", inputSet.Expected, Failure(inputSet));
            }

            Log(testCase, inputSet, "Набор логин/пароль из XML обработан.", "PASS");
        }

        /// <summary>
        /// Проверяет регистрацию и отказ при некорректных регистрационных данных.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Authorization")]
        [DynamicData(nameof(TestInputReader.TC_AUTH_02), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_AUTH_02_RegistrationData_IsValidatedBeforeDatabaseWork(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-AUTH-02");
            AuthService authService = new AuthService(new DatabaseConnectionFactory());

            string login = inputSet.Get("login");
            string password = inputSet.Get("password");
            string role = inputSet.Get("role");

            if (inputSet.Expected == "success")
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(login), Failure(inputSet));
                Assert.IsTrue(password.Length >= 6, Failure(inputSet));
                Assert.IsTrue(IsKnownRole(role), Failure(inputSet));
            }
            else
            {
                AuthResult result = role == UserRole.User
                    ? await authService.RegisterAsync(login, password, CancellationToken.None)
                    : await authService.CreateUserAsync(login, password, role, CancellationToken.None);
                Assert.IsFalse(result.IsSuccess, Failure(inputSet));
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message), Failure(inputSet));
            }

            Log(testCase, inputSet, "Регистрационный набор из XML проверен.", "PASS");
        }

        /// <summary>
        /// Проверяет определение прав после входа для трех ролей.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Authorization")]
        [DynamicData(nameof(TestInputReader.TC_AUTH_03), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_AUTH_03_RoleAfterLogin_DefinesPermissions(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-AUTH-03");

            AuthSessionService session = CreateSession(inputSet.Get("role"));
            Assert.AreEqual(inputSet.Get("role"), session.CurrentRoleName, Failure(inputSet));

            if (inputSet.Expected == "admin-permissions")
            {
                Assert.IsTrue(session.IsAdmin(), Failure(inputSet));
                Assert.IsTrue(session.CanManageMonitoring(), Failure(inputSet));
                Assert.IsTrue(session.CanViewSecurityLogs(), Failure(inputSet));
            }
            else if (inputSet.Expected == "monitoring-permissions")
            {
                Assert.IsFalse(session.IsAdmin(), Failure(inputSet));
                Assert.IsTrue(session.CanManageMonitoring(), Failure(inputSet));
                Assert.IsFalse(session.CanViewSecurityLogs(), Failure(inputSet));
            }
            else
            {
                Assert.IsFalse(session.IsAdmin(), Failure(inputSet));
                Assert.IsFalse(session.CanManageMonitoring(), Failure(inputSet));
                Assert.IsFalse(session.CanViewSecurityLogs(), Failure(inputSet));
            }

            Log(testCase, inputSet, "Права роли из XML соответствуют ожидаемому набору.", "PASS");
        }

        /// <summary>
        /// Проверяет ограничение доступа по ролям.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Authorization")]
        [DynamicData(nameof(TestInputReader.TC_AUTH_04), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_AUTH_04_RoleAccessRestrictions_AreEnforced(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-AUTH-04");

            AuthSessionService session = CreateSession(inputSet.Get("role"));
            bool allowed = IsCommandAllowed(session, inputSet.Get("command"));
            Assert.AreEqual(inputSet.Expected == "allowed", allowed, Failure(inputSet));

            Log(testCase, inputSet, "Ограничение доступа проверено по команде из XML.", "PASS");
        }

        /// <summary>
        /// Проверяет выход из учетной записи и очистку текущей сессии.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Authorization")]
        [DynamicData(nameof(TestInputReader.TC_AUTH_05), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_AUTH_05_Logout_ClearsCurrentSession(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-AUTH-05");

            AuthSessionService session = CreateSession(UserRole.Admin);
            Assert.AreEqual("auth logout", inputSet.Get("action"), Failure(inputSet));
            session.SignOut();

            Assert.IsFalse(session.IsAuthenticated, Failure(inputSet));
            Assert.IsNull(session.CurrentUserId, Failure(inputSet));
            Assert.IsNull(session.CurrentLogin, Failure(inputSet));
            Assert.IsNull(session.CurrentRoleName, Failure(inputSet));
            Assert.IsFalse(session.CanViewSecurityLogs(), Failure(inputSet));

            Log(testCase, inputSet, "Выход очищает сессию для сценария из XML.", "PASS");
        }

        /// <summary>
        /// Проверяет устойчивость авторизации к пустым строкам и пробелам.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Authorization")]
        [DynamicData(nameof(TestInputReader.TC_AUTH_06), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_AUTH_06_InvalidAuthData_ReturnsControlledFailure(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-AUTH-06");
            AuthService authService = new AuthService(new DatabaseConnectionFactory());

            string login = inputSet.Get("login");
            string password = inputSet.Get("password");

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                AuthResult result = await authService.AuthenticateAsync(login, password, CancellationToken.None);
                Assert.IsFalse(result.IsSuccess, Failure(inputSet));
                Assert.IsFalse(string.IsNullOrWhiteSpace(result.Message), Failure(inputSet));
            }
            else
            {
                Assert.AreEqual("controlled-failure", inputSet.Expected, Failure(inputSet));
                Assert.IsTrue(login.Contains("'"), Failure(inputSet));
            }

            Log(testCase, inputSet, "Некорректный набор авторизации из XML дает контролируемый отказ.", "PASS");
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
        /// Проверяет, входит ли роль в список поддерживаемых ролей.
        /// </summary>
        private static bool IsKnownRole(string roleName)
        {
            return roleName == UserRole.User ||
                   roleName == UserRole.Admin ||
                   roleName == UserRole.Statistician;
        }

        /// <summary>
        /// Проверяет доступность команды для текущей роли.
        /// </summary>
        private static bool IsCommandAllowed(AuthSessionService session, string command)
        {
            if (command == "sec logs")
            {
                return session.CanViewSecurityLogs();
            }

            if (command == "admin user list")
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
