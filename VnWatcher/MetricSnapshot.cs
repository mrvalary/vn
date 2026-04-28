namespace VnWatcher
{
    /// <summary>
    /// Снимок текущей нагрузки устройства.
    /// </summary>
    internal sealed class MetricSnapshot
    {
        /// <summary>
        /// Текущая загрузка процессора в процентах.
        /// </summary>
        public decimal CpuPercent { get; set; }

        /// <summary>
        /// Текущая занятость оперативной памяти в процентах.
        /// </summary>
        public decimal RamPercent { get; set; }

        /// <summary>
        /// Текущая занятость локальных дисков в процентах.
        /// </summary>
        public decimal HddPercent { get; set; }
    }
}
