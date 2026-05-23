using System;
using System.Collections.Generic;
using System.IO;

namespace CursovoyProjectxDxD.Services
{
    /// <summary>
    /// Читает безопасные настройки приложения из vn.yml без хранения паролей и строк подключения.
    /// </summary>
    public sealed class YamlAppSettings
    {
        #region Defaults

        private const string ConfigFileName = "vn.yml";
        private const string DefaultUpdateOwner = "mrvalary";
        private const string DefaultUpdateRepo = "vn";
        private const string DefaultUpdateAssetExtension = ".zip";
        private const int DefaultUpdateHttpTimeoutSeconds = 15;

        #endregion

        #region Properties

        /// <summary>
        /// Владелец GitHub-репозитория, из которого проверяются обновления.
        /// </summary>
        public string UpdateOwner { get; private set; }

        /// <summary>
        /// Имя GitHub-репозитория, из которого проверяются обновления.
        /// </summary>
        public string UpdateRepo { get; private set; }

        /// <summary>
        /// Расширение архива обновления, который нужно искать среди assets релиза.
        /// </summary>
        public string UpdateAssetExtension { get; private set; }

        /// <summary>
        /// Таймаут HTTP-запроса к GitHub Releases в секундах.
        /// </summary>
        public int UpdateHttpTimeoutSeconds { get; private set; }

        #endregion

        #region Load

        /// <summary>
        /// Загружает настройки клиента из vn.yml или возвращает значения по умолчанию.
        /// </summary>
        /// <returns>Безопасные настройки приложения.</returns>
        public static YamlAppSettings Load()
        {
            // vn.yml лежит рядом с exe и содержит только неопасные параметры.
            SimpleYamlConfig yaml = SimpleYamlConfig.Load(ConfigFileName);

            return new YamlAppSettings
            {
                UpdateOwner = yaml.GetValue("updates", "owner", DefaultUpdateOwner),
                UpdateRepo = yaml.GetValue("updates", "repo", DefaultUpdateRepo),
                UpdateAssetExtension = yaml.GetValue("updates", "assetExtension", DefaultUpdateAssetExtension),
                UpdateHttpTimeoutSeconds = yaml.GetPositiveInt("updates", "httpTimeoutSeconds", DefaultUpdateHttpTimeoutSeconds)
            };
        }

        #endregion

        #region Simple YAML

        /// <summary>
        /// Минимальный парсер для простого YAML-формата "section: key: value".
        /// </summary>
        private sealed class SimpleYamlConfig
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

                // Если vn.yml не найден, приложение спокойно работает с дефолтными значениями.
                if (!File.Exists(path))
                {
                    return config;
                }

                string currentSection = string.Empty;
                foreach (string rawLine in File.ReadAllLines(path))
                {
                    // Поддерживаем комментарии через # и пустые строки.
                    string line = StripComment(rawLine).TrimEnd();
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    // Строка без отступа и с двоеточием в конце открывает новую секцию.
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

                    // Внутри секции ожидается простой формат key: value.
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

        #endregion
    }
}
