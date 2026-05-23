using System;

namespace VnTests
{
    /// <summary>
    /// Хранит данные одного тест-кейса, прочитанного из XML-файла.
    /// </summary>
    internal sealed class TestCaseData
    {
        #region Properties

        /// <summary>
        /// Номер тест-кейса из таблицы ручных тест-кейсов.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Группа проверяемого модуля.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Название тест-кейса.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Что именно проверяется в тест-кейсе.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Набор входных данных из XML.
        /// </summary>
        public string Inputs { get; set; }

        /// <summary>
        /// Ожидаемый результат из XML.
        /// </summary>
        public string Expected { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Проверяет, что тест-кейс содержит обязательные поля.
        /// </summary>
        public void EnsureValid()
        {
            if (string.IsNullOrWhiteSpace(Id))
            {
                throw new InvalidOperationException("В XML найден тест-кейс без номера.");
            }

            if (string.IsNullOrWhiteSpace(Name))
            {
                throw new InvalidOperationException("В XML тест-кейс " + Id + " не содержит название.");
            }

            if (string.IsNullOrWhiteSpace(Inputs))
            {
                throw new InvalidOperationException("В XML тест-кейс " + Id + " не содержит входные данные.");
            }

            if (string.IsNullOrWhiteSpace(Expected))
            {
                throw new InvalidOperationException("В XML тест-кейс " + Id + " не содержит ожидаемый результат.");
            }
        }

        #endregion
    }
}
