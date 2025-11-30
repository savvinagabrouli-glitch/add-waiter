using System.Collections.Generic;
using Cursework.Wpf.Models.HallLayout;

namespace Cursework.Wpf.Services.HallLayout
{
    /// <summary>
    /// Сервис локального хранения пресетов раскладок залов.
    /// Работает только в WPF-проекте, без EF и без API.
    /// </summary>
    public interface IHallLayoutStorageService
    {
        /// <summary>
        /// Загружает все пресеты из JSON-файла.
        /// Если файла нет, возвращает пустой список.
        /// </summary>
        List<HallLayoutPresetModel> LoadPresets();

        /// <summary>
        /// Перезаписывает JSON-файл полным списком пресетов.
        /// </summary>
        void SavePresets(List<HallLayoutPresetModel> presets);

        /// <summary>
        /// Возвращает пресеты только для указанной зоны.
        /// </summary>
        List<HallLayoutPresetModel> GetPresetsForZone(string zone);

        /// <summary>
        /// Загружает один пресет по зоне и Id или имени.
        /// </summary>
        HallLayoutPresetModel? LoadPreset(string zone, string presetIdOrName);

        /// <summary>
        /// Добавляет/обновляет один пресет в файле.
        /// </summary>
        void SavePreset(HallLayoutPresetModel preset);

        /// <summary>
        /// Удаляет пресет по Id.
        /// </summary>
        void DeletePreset(string presetId);
        string? GetActivePresetName(string zoneCode);
        void SetActivePresetName(string zoneCode, string presetIdOrName);
    }
}
