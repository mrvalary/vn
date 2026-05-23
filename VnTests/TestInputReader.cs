using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace VnTests
{
    /// <summary>
    /// Читает наборы входных данных unit-тестов из XML-файла.
    /// </summary>
    public static class TestInputReader
    {
        #region Constants

        // Имя XML-файла с наборами входных данных.
        private const string XmlFileName = "vn-test-input-data.xml";

        #endregion

        #region Public Methods

        /// <summary>
        /// Загружает все наборы входных данных для указанного тест-кейса.
        /// </summary>
        /// <param name="testCaseId">Номер тест-кейса.</param>
        /// <returns>Список наборов входных данных.</returns>
        public static IReadOnlyList<TestInputSet> LoadSets(string testCaseId)
        {
            XDocument document = XDocument.Load(GetXmlPath());
            XElement testCase = document.Root
                .Elements("TestCase")
                .FirstOrDefault(x => string.Equals((string)x.Attribute("id"), testCaseId, StringComparison.OrdinalIgnoreCase));

            if (testCase == null)
            {
                throw new InvalidOperationException("В XML с входными данными не найден тест-кейс " + testCaseId + ".");
            }

            List<TestInputSet> sets = testCase
                .Elements("InputSet")
                .Select(ReadInputSet)
                .ToList();

            if (sets.Count < 2)
            {
                throw new InvalidOperationException("Для тест-кейса " + testCaseId + " должно быть минимум два набора входных данных.");
            }

            return sets;
        }

        /// <summary>
        /// Возвращает XML-наборы TC-AUTH-01 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_AUTH_01()
        {
            return LoadRows("TC-AUTH-01");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-AUTH-02 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_AUTH_02()
        {
            return LoadRows("TC-AUTH-02");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-AUTH-03 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_AUTH_03()
        {
            return LoadRows("TC-AUTH-03");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-AUTH-04 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_AUTH_04()
        {
            return LoadRows("TC-AUTH-04");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-AUTH-05 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_AUTH_05()
        {
            return LoadRows("TC-AUTH-05");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-AUTH-06 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_AUTH_06()
        {
            return LoadRows("TC-AUTH-06");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-DB-01 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_DB_01()
        {
            return LoadRows("TC-DB-01");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-DB-02 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_DB_02()
        {
            return LoadRows("TC-DB-02");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-DB-03 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_DB_03()
        {
            return LoadRows("TC-DB-03");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-DB-04 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_DB_04()
        {
            return LoadRows("TC-DB-04");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-DB-05 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_DB_05()
        {
            return LoadRows("TC-DB-05");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-DB-06 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_DB_06()
        {
            return LoadRows("TC-DB-06");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-NOTE-01 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_NOTE_01()
        {
            return LoadRows("TC-NOTE-01");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-NOTE-02 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_NOTE_02()
        {
            return LoadRows("TC-NOTE-02");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-NOTE-03 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_NOTE_03()
        {
            return LoadRows("TC-NOTE-03");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-NOTE-04 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_NOTE_04()
        {
            return LoadRows("TC-NOTE-04");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-NOTE-05 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_NOTE_05()
        {
            return LoadRows("TC-NOTE-05");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-NOTE-06 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_NOTE_06()
        {
            return LoadRows("TC-NOTE-06");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-NOTE-07 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_NOTE_07()
        {
            return LoadRows("TC-NOTE-07");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-NOTE-08 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_NOTE_08()
        {
            return LoadRows("TC-NOTE-08");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-UPD-01 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_UPD_01()
        {
            return LoadRows("TC-UPD-01");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-UPD-02 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_UPD_02()
        {
            return LoadRows("TC-UPD-02");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-UPD-03 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_UPD_03()
        {
            return LoadRows("TC-UPD-03");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-UPD-04 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_UPD_04()
        {
            return LoadRows("TC-UPD-04");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-UPD-05 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_UPD_05()
        {
            return LoadRows("TC-UPD-05");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-UPD-06 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_UPD_06()
        {
            return LoadRows("TC-UPD-06");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-UPD-07 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_UPD_07()
        {
            return LoadRows("TC-UPD-07");
        }

        /// <summary>
        /// Возвращает XML-наборы TC-UPD-08 как отдельные строки data-driven теста.
        /// </summary>
        public static IEnumerable<object[]> TC_UPD_08()
        {
            return LoadRows("TC-UPD-08");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Читает один набор входных данных из XML.
        /// </summary>
        /// <param name="element">XML-элемент InputSet.</param>
        /// <returns>Набор входных данных.</returns>
        private static TestInputSet ReadInputSet(XElement element)
        {
            Dictionary<string, string> values = element
                .Elements("Input")
                .ToDictionary(
                    x => (string)x.Attribute("name"),
                    x => x.Value);

            return new TestInputSet(
                (string)element.Attribute("name"),
                (string)element.Attribute("expected"),
                values);
        }

        /// <summary>
        /// Преобразует XML-наборы в строки DynamicData.
        /// </summary>
        /// <param name="testCaseId">Номер тест-кейса.</param>
        /// <returns>Строки данных MSTest.</returns>
        private static IEnumerable<object[]> LoadRows(string testCaseId)
        {
            return LoadSets(testCaseId).Select(x => new object[] { x });
        }

        /// <summary>
        /// Возвращает путь к XML-файлу с входными данными.
        /// </summary>
        /// <returns>Абсолютный путь к vn-test-input-data.xml.</returns>
        private static string GetXmlPath()
        {
            string copiedPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", XmlFileName);
            if (File.Exists(copiedPath))
            {
                return copiedPath;
            }

            string sourcePath = Path.GetFullPath(
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "TestData", XmlFileName));
            if (File.Exists(sourcePath))
            {
                return sourcePath;
            }

            throw new FileNotFoundException("XML-файл с входными данными не найден.", copiedPath);
        }

        #endregion
    }
}
