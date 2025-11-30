using System;
using System.Collections.Generic;

namespace Cursework.Wpf.Models.HallLayout
{
    /// <summary>
    /// Положение одного стола в раскладке.
    /// Хранится только локально (JSON), не в БД.
    /// </summary>
    public class HallTableLayoutModel
    {
        public int TableId { get; set; }

        /// <summary>
        /// Код зоны: MainHall / Terrace / Bar / VIP
        /// </summary>
        public string Zone { get; set; } = string.Empty;

        /// <summary>
        /// Координаты в пикселях внутри слоя схемы.
        /// </summary>
        public double X { get; set; }
        public double Y { get; set; }

        /// <summary>
        /// Визуальная модель: "Single" / "Multi".
        /// </summary>
        public string ModelType { get; set; } = "Single";
    }

    /// <summary>
    /// Один пресет раскладки для конкретной зоны.
    /// </summary>
    public class HallLayoutPresetModel
    {
        public string Id { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Отображаемое имя пресета: "Стандартный", "Банкет", "Праздник" и т.д.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Зона, к которой относится пресет: MainHall / Terrace / Bar / VIP.
        /// </summary>
        public string Zone { get; set; } = string.Empty;

        /// <summary>
        /// Флаг "по умолчанию" для зоны.
        /// </summary>
        public bool IsActive { get; set; }

        public List<HallTableLayoutModel> Tables { get; set; } = new();
    }

    /// <summary>
    /// Корневая модель для JSON-файла hall-layouts.json.
    /// </summary>
    public class HallLayoutsFileModel
    {
        public List<HallLayoutPresetModel> Presets { get; set; } = new();
    }
}
