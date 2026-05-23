using System.Collections.Generic;

namespace VnTests
{
    /// <summary>
    /// Хранит один набор входных данных для unit-теста.
    /// </summary>
    public sealed class TestInputSet
    {
        #region Constructor

        /// <summary>
        /// Создает набор входных данных.
        /// </summary>
        /// <param name="name">Название набора данных.</param>
        /// <param name="expected">Ожидаемый результат набора.</param>
        /// <param name="values">Поля входных данных.</param>
        public TestInputSet(string name, string expected, Dictionary<string, string> values)
        {
            Name = name;
            Expected = expected;
            Values = values;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Название набора данных внутри XML.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Ожидаемый результат для набора данных.
        /// </summary>
        public string Expected { get; private set; }

        /// <summary>
        /// Значения входных параметров.
        /// </summary>
        public Dictionary<string, string> Values { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// Возвращает значение входного параметра по имени.
        /// </summary>
        /// <param name="name">Название параметра.</param>
        /// <returns>Значение параметра или пустая строка.</returns>
        public string Get(string name)
        {
            string value;
            return Values.TryGetValue(name, out value) ? value : string.Empty;
        }

        /// <summary>
        /// Возвращает входной параметр как число.
        /// </summary>
        /// <param name="name">Название параметра.</param>
        /// <returns>Числовое значение параметра.</returns>
        public int GetInt32(string name)
        {
            return int.Parse(Get(name));
        }

        /// <summary>
        /// Возвращает название набора для отображения в обозревателе тестов.
        /// </summary>
        /// <returns>Название XML-набора.</returns>
        public override string ToString()
        {
            return Name;
        }

        #endregion
    }
}
