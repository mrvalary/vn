namespace VnInstaller.Models
{
    // Модель релиза, достаточная для скачивания и установки.
    public sealed class AppUpdateInfo
    {
        // Последняя версия релиза.
        public string LatestVersion { get; set; }
        // Имя zip-архива релиза.
        public string AssetName { get; set; }
        // Прямая ссылка на скачивание архива.
        public string DownloadUrl { get; set; }
    }
}
