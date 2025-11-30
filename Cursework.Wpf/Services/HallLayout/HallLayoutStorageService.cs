using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Cursework.Wpf.Models.HallLayout;

namespace Cursework.Wpf.Services.HallLayout
{
    public class HallLayoutStorageService : IHallLayoutStorageService
    {
        private readonly string _filePath;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly ActivePresetStore _activePresetStore;

        public HallLayoutStorageService()
            : this(Path.Combine(AppContext.BaseDirectory, "Presets", "hall-layouts.json"))
        {
        }

        public HallLayoutStorageService(string filePath)
        {
            _filePath = filePath;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };

            var baseDir = AppContext.BaseDirectory;
            var presetsDir = Path.Combine(baseDir, "Presets");

            _activePresetStore = new ActivePresetStore(Path.Combine(presetsDir, "active-presets.json"));
        }

        public string? GetActivePresetName(string zoneCode)
    => _activePresetStore.GetActivePreset(zoneCode);

        public void SetActivePresetName(string zoneCode, string presetName)
            => _activePresetStore.SetActivePreset(zoneCode, presetName);


        public List<HallLayoutPresetModel> LoadPresets()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new List<HallLayoutPresetModel>();

                var json = File.ReadAllText(_filePath);
                if (string.IsNullOrWhiteSpace(json))
                    return new List<HallLayoutPresetModel>();

                var model = JsonSerializer.Deserialize<HallLayoutsFileModel>(json, _jsonOptions);
                return model?.Presets ?? new List<HallLayoutPresetModel>();
            }
            catch
            {
                // На защите достаточно молча вернуть пустой список.
                return new List<HallLayoutPresetModel>();
            }
        }

        public void SavePresets(List<HallLayoutPresetModel> presets)
        {
            var dir = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            var model = new HallLayoutsFileModel { Presets = presets };
            var json = JsonSerializer.Serialize(model, _jsonOptions);
            File.WriteAllText(_filePath, json);
        }

        public List<HallLayoutPresetModel> GetPresetsForZone(string zone)
        {
            var all = LoadPresets();
            return all
                .Where(p => string.Equals(p.Zone, zone, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Name)
                .ToList();
        }

        public HallLayoutPresetModel? LoadPreset(string zone, string presetIdOrName)
        {
            var all = LoadPresets();

            return all.FirstOrDefault(p =>
                       string.Equals(p.Zone, zone, StringComparison.OrdinalIgnoreCase) &&
                       (string.Equals(p.Id, presetIdOrName, StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(p.Name, presetIdOrName, StringComparison.OrdinalIgnoreCase)));
        }

        public void SavePreset(HallLayoutPresetModel preset)
        {
            var all = LoadPresets();

            var existing = all.FirstOrDefault(p => p.Id == preset.Id);
            if (existing == null)
            {
                all.Add(preset);
            }
            else
            {
                existing.Name = preset.Name;
                existing.Zone = preset.Zone;
                existing.IsActive = preset.IsActive;
                existing.Tables = preset.Tables;
            }

            SavePresets(all);
        }

        public void DeletePreset(string presetId)
        {
            var all = LoadPresets();
            if (all.RemoveAll(p => p.Id == presetId) > 0)
                SavePresets(all);
        }
    }
}
