using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Cursework.Wpf.Services.HallLayout
{
    public sealed class ActivePresetStore
    {
        private readonly string _filePath;
        private readonly object _lock = new();

        public ActivePresetStore(string filePath)
        {
            _filePath = filePath;
        }

        public string? GetActivePreset(string zoneCode)
        {
            if (string.IsNullOrWhiteSpace(zoneCode)) return null;

            lock (_lock)
            {
                var map = ReadMap();
                return map.TryGetValue(zoneCode, out var preset) ? preset : null;
            }
        }

        public void SetActivePreset(string zoneCode, string presetName)
        {
            if (string.IsNullOrWhiteSpace(zoneCode)) return;
            if (string.IsNullOrWhiteSpace(presetName)) return;

            lock (_lock)
            {
                var map = ReadMap();
                map[zoneCode] = presetName;
                WriteMap(map);
            }
        }

        private Dictionary<string, string> ReadMap()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                var json = File.ReadAllText(_filePath);
                var obj = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                return obj ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
            catch
            {
                return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            }
        }

        private void WriteMap(Dictionary<string, string> map)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);

            var json = JsonSerializer.Serialize(map, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_filePath, json);
        }
    }
}
