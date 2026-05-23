namespace CursovoyProjectxDxD.Models
{
    /// <summary>
    /// Модель результата проверки обновления.
    /// </summary>
    public sealed class AppUpdateInfo
    {
        /// <summary>
        /// Текущая локальная версия.
        /// </summary>
        public string CurrentVersion { get; set; } = string.Empty;
        /// <summary>
        /// Последняя версия из GitHub.
        /// </summary>
        public string LatestVersion { get; set; } = string.Empty;

        /// <summary>
        /// Название релиза из GitHub Releases.
        /// </summary>
        public string ReleaseName { get; set; } = string.Empty;

        /// <summary>
        /// Описание релиза из GitHub Releases.
        /// </summary>
        public string ReleaseNotes { get; set; } = string.Empty;

        /// <summary>
        /// Флаг доступности новой версии.
        /// </summary>
        public bool IsAvailable { get; set; }
        /// <summary>
        /// Имя zip-архива релиза.
        /// </summary>
        public string AssetName { get; set; } = string.Empty;
        /// <summary>
        /// Прямая ссылка на скачивание архива.
        /// </summary>
        public string DownloadUrl { get; set; } = string.Empty;
    }
}
