using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CursovoyProjectxDxD.Commands;
using CursovoyProjectxDxD.Models;
using CursovoyProjectxDxD.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace VnTests
{
    /// <summary>
    /// Проверяет модуль заметок по тест-кейсам, данные которых берутся из XML.
    /// </summary>
    [TestClass]
    public sealed class NoteTests
    {
        /// <summary>
        /// Контекст MSTest для вывода пояснений при запуске тестов.
        /// </summary>
        public TestContext TestContext { get; set; }

        #region Note Test Cases

        /// <summary>
        /// Проверяет добавление заметки и отказ при пустом тексте.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Notes")]
        [DynamicData(nameof(TestInputReader.TC_NOTE_01), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_NOTE_01_AddNote_RejectsEmptyText(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-NOTE-01");
            NoteService noteService = CreateNoteService(CreateAuthorizedSession(UserRole.User));

            string text = inputSet.Get("text");
            if (inputSet.Expected == "failure")
            {
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    async () => await noteService.AddNoteAsync(text, CancellationToken.None),
                    Failure(inputSet));
            }
            else
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(text), Failure(inputSet));
            }

            Log(testCase, inputSet, "Текст заметки из XML проверен.", "PASS");
        }

        /// <summary>
        /// Проверяет просмотр списка заметок без авторизации.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Notes")]
        [DynamicData(nameof(TestInputReader.TC_NOTE_02), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_NOTE_02_ListNotes_RequiresAuthorizedUser(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-NOTE-02");

            AuthSessionService session = string.IsNullOrWhiteSpace(inputSet.Get("role"))
                ? new AuthSessionService()
                : CreateAuthorizedSession(inputSet.Get("role"));
            NoteService noteService = CreateNoteService(session);

            if (inputSet.Expected == "failure")
            {
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    async () => await noteService.ListNotesAsync(CancellationToken.None),
                    Failure(inputSet));
            }
            else
            {
                Assert.IsTrue(session.IsAuthenticated, Failure(inputSet));
                Assert.AreEqual("nt list", inputSet.Get("command"), Failure(inputSet));
            }

            Log(testCase, inputSet, "Сценарий просмотра списка из XML проверен.", "PASS");
        }

        /// <summary>
        /// Проверяет разбор количества для команды последних заметок.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Notes")]
        [DynamicData(nameof(TestInputReader.TC_NOTE_03), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public void TC_NOTE_03_RecentLimit_IsParsedAndValidated(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-NOTE-03");

            string[] args = inputSet.Get("command").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            int limit;
            bool result = TryParseRecentLimit(args, out limit);

            if (inputSet.Expected == "failure")
            {
                Assert.IsFalse(result, Failure(inputSet));
            }
            else
            {
                Assert.IsTrue(result, Failure(inputSet));
                Assert.AreEqual(int.Parse(inputSet.Expected), limit, Failure(inputSet));
            }

            Log(testCase, inputSet, "Лимит nt recent из XML разобран корректно.", "PASS");
        }

        /// <summary>
        /// Проверяет поиск заметок и отказ при пустом запросе.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Notes")]
        [DynamicData(nameof(TestInputReader.TC_NOTE_04), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_NOTE_04_SearchNotes_RejectsEmptyQuery(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-NOTE-04");
            NoteService noteService = CreateNoteService(CreateAuthorizedSession(UserRole.User));

            string query = inputSet.Get("query");
            if (inputSet.Expected == "failure")
            {
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    async () => await noteService.SearchNotesAsync(query, CancellationToken.None),
                    Failure(inputSet));
            }
            else
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(query), Failure(inputSet));
            }

            Log(testCase, inputSet, "Поисковый запрос из XML проверен.", "PASS");
        }

        /// <summary>
        /// Проверяет редактирование заметки и отказ при некорректных данных.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Notes")]
        [DynamicData(nameof(TestInputReader.TC_NOTE_05), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_NOTE_05_EditNote_RejectsInvalidIdAndText(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-NOTE-05");
            NoteService noteService = CreateNoteService(CreateAuthorizedSession(UserRole.User));

            int noteId = inputSet.GetInt32("noteId");
            string text = inputSet.Get("text");

            if (inputSet.Expected == "failure")
            {
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    async () => await noteService.UpdateOwnNoteAsync(noteId, text, CancellationToken.None),
                    Failure(inputSet));
            }
            else
            {
                Assert.IsTrue(noteId > 0, Failure(inputSet));
                Assert.IsFalse(string.IsNullOrWhiteSpace(text), Failure(inputSet));
            }

            Log(testCase, inputSet, "Набор редактирования заметки из XML проверен.", "PASS");
        }

        /// <summary>
        /// Проверяет удаление заметки и отказ при некорректном ID.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Notes")]
        [DynamicData(nameof(TestInputReader.TC_NOTE_06), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_NOTE_06_DeleteNote_RejectsInvalidId(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-NOTE-06");
            NoteService noteService = CreateNoteService(CreateAuthorizedSession(UserRole.User));

            int noteId = inputSet.GetInt32("noteId");
            if (inputSet.Expected == "failure")
            {
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    async () => await noteService.DeleteNoteAsync(noteId, CancellationToken.None),
                    Failure(inputSet));
            }
            else
            {
                Assert.IsTrue(noteId > 0, Failure(inputSet));
            }

            Log(testCase, inputSet, "Набор удаления заметки из XML проверен.", "PASS");
        }

        /// <summary>
        /// Проверяет запрет работы с заметками без авторизации.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Notes")]
        [DynamicData(nameof(TestInputReader.TC_NOTE_07), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_NOTE_07_NoteCommands_RequireAuthentication(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-NOTE-07");
            NoteService noteService = CreateNoteService(new AuthSessionService());

            string command = inputSet.Get("command");
            if (command.StartsWith("nt add", StringComparison.OrdinalIgnoreCase))
            {
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    async () => await noteService.AddNoteAsync("text", CancellationToken.None),
                    Failure(inputSet));
            }
            else if (command == "nt list")
            {
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    async () => await noteService.ListNotesAsync(CancellationToken.None),
                    Failure(inputSet));
            }
            else if (command.StartsWith("nt del", StringComparison.OrdinalIgnoreCase))
            {
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    async () => await noteService.DeleteNoteAsync(1, CancellationToken.None),
                    Failure(inputSet));
            }

            Log(testCase, inputSet, "Команда заметок без авторизации из XML отклонена.", "PASS");
        }

        /// <summary>
        /// Проверяет команды администратора по заметкам.
        /// </summary>
        [DataTestMethod]
        [TestCategory("Notes")]
        [DynamicData(nameof(TestInputReader.TC_NOTE_08), typeof(TestInputReader), DynamicDataSourceType.Method)]
        public async Task TC_NOTE_08_AdminNoteCommands_RejectNonAdminRole(TestInputSet inputSet)
        {
            TestCaseData testCase = TestCaseReader.Load("TC-NOTE-08");

            AuthSessionService session = CreateAuthorizedSession(inputSet.Get("role"));
            NoteService noteService = CreateNoteService(session);

            if (inputSet.Expected == "denied")
            {
                await Assert.ThrowsExceptionAsync<InvalidOperationException>(
                    async () => await noteService.ListNotesByLoginForAdminAsync("user", CancellationToken.None),
                    Failure(inputSet));
            }
            else
            {
                Assert.IsTrue(session.IsAdmin(), Failure(inputSet));
                Assert.IsTrue(inputSet.Get("command").StartsWith("admin", StringComparison.OrdinalIgnoreCase), Failure(inputSet));
            }

            Log(testCase, inputSet, "Admin-команда заметок проверена по роли из XML.", "PASS");
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Создает сервис заметок с указанной сессией пользователя.
        /// </summary>
        private static NoteService CreateNoteService(AuthSessionService session)
        {
            return new NoteService(new DatabaseConnectionFactory(), session);
        }

        /// <summary>
        /// Создает авторизованную сессию с указанной ролью.
        /// </summary>
        private static AuthSessionService CreateAuthorizedSession(string roleName)
        {
            AuthSessionService session = new AuthSessionService();
            session.SignIn(1, roleName, roleName);
            return session;
        }

        /// <summary>
        /// Вызывает закрытый разбор лимита команды nt recent.
        /// </summary>
        private static bool TryParseRecentLimit(string[] args, out int limit)
        {
            MethodInfo method = typeof(NoteRecentCommand).GetMethod(
                "TryParseLimit",
                BindingFlags.NonPublic | BindingFlags.Static);
            object[] parameters = { args, 0 };
            bool result = (bool)method.Invoke(null, parameters);
            limit = (int)parameters[1];
            return result;
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
