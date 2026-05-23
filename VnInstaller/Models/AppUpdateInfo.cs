namespace VnInstaller.Models
{
    /// <summary>
    /// Модель релиза, достаточная для скачивания и установки.
    /// </summary>
    public sealed class AppUpdateInfo
    {
        /// <summary>
        /// Последняя версия релиза.
        /// </summary>
        public string LatestVersion { get; set; }

        /// <summary>
        /// Имя zip-архива релиза.
        /// </summary>
        public string AssetName { get; set; }

        /// <summary>
        /// Прямая ссылка на скачивание архива.
        /// </summary>
        public string DownloadUrl { get; set; }
    }
}
