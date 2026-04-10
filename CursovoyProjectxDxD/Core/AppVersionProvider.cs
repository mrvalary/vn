using System;
using System.Reflection;

namespace CursovoyProjectxDxD.Core
{
    // Единая точка получения версии основного приложения.
    public static class AppVersionProvider
    {
        // Возвращает версию текущей сборки в формате major.minor.build.
        public static string GetCurrentVersion()
        {
            // Берём объект Version из исполняемой сборки приложения.
            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            // Если версия доступна, сокращаем её до трёх компонентов.
            // Если по какой-то причине версия отсутствует, используем безопасное значение по умолчанию.
            return version != null ? version.ToString(3) : "1.0.0";
        }
    }
}
