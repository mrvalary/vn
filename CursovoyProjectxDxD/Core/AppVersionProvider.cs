using System;
using System.Reflection;

namespace CursovoyProjectxDxD.Core
{
    /// <summary>
    /// Единая точка получения версии основного приложения.
    /// </summary>
    public static class AppVersionProvider
    {
        /// <summary>
        /// Возвращает версию текущей сборки в формате major.minor.build.
        /// 1.0.1 — Исправлена ошибка (Patch)
        /// 1.1.0 — Добавлена новая функция(Minor)
        /// 2.0.0 — Крупное обновление, API изменено(Major)
        /// </summary>
        public static string GetCurrentVersion()
        {
            // Берём объект Version из исполняемой сборки приложения.
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            // Если по какой-то причине версия отсутствует, используем безопасное значение по умолчанию.
            return version != null ? version.ToString(3) : "1.0.0";
        }
    }
}
