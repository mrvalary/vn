namespace CursovoyProjectxDxD.Models
{
    // Модель результата проверки обновления.
    public sealed class AppUpdateInfo
    {
        // Текущая локальная версия.
        public string CurrentVersion { get; set; } = string.Empty;
        // Последняя версия из GitHub.
        public string LatestVersion { get; set; } = string.Empty;
        // Флаг доступности новой версии.
        public bool IsAvailable { get; set; }
        // Имя zip-архива релиза.
        public string AssetName { get; set; } = string.Empty;
        // Прямая ссылка на скачивание архива.
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
