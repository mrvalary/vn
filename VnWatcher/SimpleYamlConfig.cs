using System;
using System.Collections.Generic;
using System.IO;

namespace VnWatcher
{
    /// <summary>
    /// Минимальный YAML-парсер для безопасных настроек watcher.yml.
    /// </summary>
    internal sealed class SimpleYamlConfig
    {
        #region Fields

        // Секции YAML хранятся как словарь "секция -> ключ -> значение".
        private readonly Dictionary<string, Dictionary<string, string>> _sections =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        #endregion

        #region Load

        /// <summary>
        /// Загружает YAML-файл из папки приложения или текущей рабочей папки.
        /// </summary>
        /// <param name="fileName">Имя YAML-файла.</param>
        /// <returns>Прочитанная конфигурация или пустая конфигурация, если файл отсутствует.</returns>
        public static SimpleYamlConfig Load(string fileName)
        {
            SimpleYamlConfig config = new SimpleYamlConfig();
            string path = ResolvePath(fileName);

            if (!File.Exists(path))
            {
                return config;
            }

            string currentSection = string.Empty;
            foreach (string rawLine in File.ReadAllLines(path))
            {
                string line = StripComment(rawLine).TrimEnd();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (!char.IsWhiteSpace(rawLine, 0) && line.EndsWith(":", StringComparison.Ordinal))
                {
                    currentSection = line.Substring(0, line.Length - 1).Trim();
                    if (!config._sections.ContainsKey(currentSection))
                    {
                        config._sections[currentSection] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    }

                    continue;
                }

                int delimiterIndex = line.IndexOf(':');
                if (delimiterIndex <= 0 || string.IsNullOrWhiteSpace(currentSection))
                {
                    continue;
                }

                string key = line.Substring(0, delimiterIndex).Trim();
                string value = line.Substring(delimiterIndex + 1).Trim();
                config._sections[currentSection][key] = Unquote(value);
            }

            return config;
        }

        #endregion

        #region Accessors

        /// <summary>
        /// Возвращает строковое значение настройки из указанной секции.
        /// </summary>
        /// <param name="section">Название секции YAML.</param>
        /// <param name="key">Название ключа внутри секции.</param>
        /// <param name="fallback">Значение по умолчанию.</param>
        /// <returns>Значение из YAML или fallback, если ключ не найден.</returns>
        public string GetValue(string section, string key, string fallback)
        {
            Dictionary<string, string> values;
            string value;
            if (_sections.TryGetValue(section, out values) &&
                values.TryGetValue(key, out value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }

            return fallback;
        }

        /// <summary>
        /// Возвращает положительное целое значение настройки.
        /// </summary>
        /// <param name="section">Название секции YAML.</param>
        /// <param name="key">Название ключа внутри секции.</param>
        /// <param name="fallback">Значение по умолчанию.</param>
        /// <returns>Положительное число из YAML или fallback.</returns>
        public int GetPositiveInt(string section, string key, int fallback)
        {
            int value;
            return int.TryParse(GetValue(section, key, fallback.ToString()), out value) && value > 0
                ? value
                : fallback;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Определяет путь к YAML-файлу рядом с exe или в текущей рабочей папке.
        /// </summary>
        /// <param name="fileName">Имя YAML-файла.</param>
        /// <returns>Полный путь к файлу конфигурации.</returns>
        private static string ResolvePath(string fileName)
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string basePath = Path.Combine(baseDirectory, fileName);
            return File.Exists(basePath) ? basePath : Path.Combine(Environment.CurrentDirectory, fileName);
        }

        /// <summary>
        /// Удаляет YAML-комментарий из строки.
        /// </summary>
        /// <param name="line">Исходная строка.</param>
        /// <returns>Строка без части после символа #.</returns>
        private static string StripComment(string line)
        {
            int commentIndex = line.IndexOf('#');
            return commentIndex < 0 ? line : line.Substring(0, commentIndex);
        }

        /// <summary>
        /// Убирает одинарные или двойные кавычки вокруг значения.
        /// </summary>
        /// <param name="value">Значение из YAML.</param>
        /// <returns>Значение без внешних кавычек.</returns>
        private static string Unquote(string value)
        {
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[value.Length - 1] == '"') ||
                 (value[0] == '\'' && value[value.Length - 1] == '\'')))
            {
                return value.Substring(1, value.Length - 2);
            }

            return value;
        }

        #endregion
    }
}
