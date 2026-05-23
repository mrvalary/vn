using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace VnTests
{
    /// <summary>
    /// Читает входные данные unit-тестов из XML-файла с тест-кейсами.
    /// </summary>
    internal static class TestCaseReader
    {
        #region Constants

        // Имя XML-файла, который копируется в папку сборки тестов.
        private const string XmlFileName = "vn-test-cases.xml";

        #endregion

        #region Public Methods

        /// <summary>
        /// Загружает тест-кейс по номеру из XML-файла.
        /// </summary>
        /// <param name="id">Номер тест-кейса, например TC-AUTH-01.</param>
        /// <returns>Данные тест-кейса.</returns>
        public static TestCaseData Load(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("Номер тест-кейса не может быть пустым.", "id");
            }

            string path = GetXmlPath();
            XDocument document = XDocument.Load(path);
            XElement element = document.Root
                .Elements("TestCase")
                .FirstOrDefault(x => string.Equals((string)x.Attribute("id"), id, StringComparison.OrdinalIgnoreCase));

            if (element == null)
            {
                throw new InvalidOperationException("В XML не найден тест-кейс " + id + ".");
            }

            TestCaseData testCase = new TestCaseData
            {
                Id = (string)element.Attribute("id"),
                Group = (string)element.Attribute("group"),
                Name = (string)element.Attribute("name"),
                Description = (string)element.Element("Description"),
                Inputs = (string)element.Element("Inputs"),
                Expected = (string)element.Element("Expected")
            };

            testCase.EnsureValid();
            return testCase;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Возвращает путь к XML-файлу тестовых данных.
        /// </summary>
        /// <returns>Абсолютный путь к vn-test-cases.xml.</returns>
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

            throw new FileNotFoundException("XML-файл с тест-кейсами не найден.", copiedPath);
        }

        #endregion
    }
}
