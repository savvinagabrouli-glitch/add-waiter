using Cursework.Domains.Models;
using Cursework.Wpf.Models.HallLayout;
using Cursework.Wpf.Services.HallLayout;
using Cursework.Wpf.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Cursework.Wpf.ViewModels.Admin
{
    public class HallMapViewModel : ViewModelBase
    {
        private readonly Services.Dialogs.ITextPromptService? _prompt;
        public RelayCommand SetActivePresetCommand { get; }

        public class ZoneOption
        {
            public string Code { get; init; } = "";
            public string Display { get; init; } = "";
        }

        /// <summary>
        /// Элемент на карте зала (текущее состояние для выбранной зоны).
        /// </summary>
        public class TableOnMapItem : ViewModelBase
        {
            public int TableId { get; set; }
            public string TableName { get; set; } = "";
            public string ZoneCode { get; set; } = "";

            private double _x;
            public double X
            {
                get => _x;
                set => Set(ref _x, value);
            }

            private double _y;
            public double Y
            {
                get => _y;
                set => Set(ref _y, value);
            }

            private string _model = "Single"; // Single / Multi
            public string Model
            {
                get => _model;
                set => Set(ref _model, value);
            }

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set => Set(ref _isSelected, value);
            }

            public int Seats { get; set; }
            public string Status { get; set; } = "";
        }


        private readonly IHallLayoutStorageService _layoutStorage;
        private readonly List<DiningTable> _allTables;

        /// <summary>
        /// Все пресеты из JSON (для всех зон).
        /// </summary>
        private List<HallLayoutPresetModel> _allPresets = new();

        public ObservableCollection<ZoneOption> Zones { get; }

        private string _selectedZone = "MainHall";
        public string SelectedZone
        {
            get => _selectedZone;
            set
            {
                if (Set(ref _selectedZone, value))
                {
                    Raise(nameof(SelectedZoneTitle));
                    ReloadPresetsForZone();
                }
            }
        }

        public string SelectedZoneTitle => SelectedZone switch
        {
            "MainHall" => "Основной зал",
            "Terrace" => "Терраса",
            "Bar" => "Бар",
            "VIP" => "VIP-зал",
            _ => "Зал"
        };

        private bool _isAdminMode;
        public bool IsAdminMode => _isAdminMode;
        public bool IsWaiterMode => !_isAdminMode;

        /// <summary>
        /// Пресеты только для текущей зоны.
        /// </summary>
        public ObservableCollection<HallLayoutPresetModel> Presets { get; } = new();

        private HallLayoutPresetModel? _selectedPreset;
        public HallLayoutPresetModel? SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                if (Set(ref _selectedPreset, value))
                    LoadPresetToTablesOnMap();
            }
        }

        private double _zoom = 1.0;
        public double Zoom
        {
            get => _zoom;
            set
            {
                var v = Math.Max(0.5, Math.Min(3.0, value));
                Set(ref _zoom, v);
            }
        }

        /// <summary>
        /// Столы в текущем пресете/зоне.
        /// </summary>
        public ObservableCollection<TableOnMapItem> TablesOnMap { get; } = new();

        private TableOnMapItem? _selectedTableOnMap;
        public TableOnMapItem? SelectedTableOnMap
        {
            get => _selectedTableOnMap;
            set
            {
                if (_selectedTableOnMap != null)
                    _selectedTableOnMap.IsSelected = false;

                if (Set(ref _selectedTableOnMap, value))
                {
                    if (_selectedTableOnMap != null)
                        _selectedTableOnMap.IsSelected = true;

                    Raise(nameof(SelectedTableOnMap)); // на всякий случай
                }
            }
        }

        /// <summary>
        /// Все доступные столы из БД, которых ещё нет в текущем пресете.
        /// </summary>
        public ObservableCollection<DiningTable> AvailableTables { get; } = new();

        private DiningTable? _selectedAvailableTable;
        public DiningTable? SelectedAvailableTable
        {
            get => _selectedAvailableTable;
            set
            {
                if (Set(ref _selectedAvailableTable, value))
                    InvalidateCommands();
            }
        }

        /// <summary>
        /// Доступные модели визуализации для ComboBox.
        /// </summary>
        public string[] TableModels { get; } = { "Single", "Multi" };


        public ICommand LoadPresetCommand { get; }
        public ICommand SavePresetCommand { get; }
        public ICommand AddTableToMapCommand { get; }
        public ICommand CreatePresetCommand { get; }
        public ICommand DeletePresetCommand { get; }
        public ICommand RemoveTableFromMapCommand { get; }


        /// <summary>
        /// Пустой конструктор — для дизайнера и на всякий случай.
        /// В реальном коде лучше использовать перегрузку с таблицами.
        /// </summary>
        public HallMapViewModel()
            : this(new HallLayoutStorageService(), Enumerable.Empty<DiningTable>(), "MainHall", isAdminMode: true)
        {
        }

        public HallMapViewModel(
            IHallLayoutStorageService layoutStorage,
            IEnumerable<DiningTable> tables,
            string initialZone,
            bool isAdminMode,
            Services.Dialogs.ITextPromptService? prompt = null)
        {
            _layoutStorage = layoutStorage;
            _allTables = tables?.ToList() ?? new List<DiningTable>();
            _isAdminMode = isAdminMode;
            _prompt = prompt;

            Zones = new ObservableCollection<ZoneOption>
            {
                new ZoneOption { Code = "MainHall", Display = "Основной зал" },
                new ZoneOption { Code = "Terrace",  Display = "Терраса" },
                new ZoneOption { Code = "Bar",      Display = "Бар" },
                new ZoneOption { Code = "VIP",      Display = "VIP-зал" },
            };

            _selectedZone = Zones.First().Code;
            if (!string.IsNullOrWhiteSpace(initialZone) &&
                Zones.Any(z => z.Code == initialZone))
            {
                _selectedZone = initialZone;
            }

            // Загружаем все пресеты из JSON
            _allPresets = _layoutStorage.LoadPresets();

            // Если совсем ничего нет — создаём "Стандартный" пресет для текущей зоны
            if (!_allPresets.Any())
            {
                var preset = new HallLayoutPresetModel
                {
                    Id = $"{_selectedZone}_Default",
                    Name = "Стандартный",
                    Zone = _selectedZone,
                    IsActive = true
                };

                _allPresets.Add(preset);
                _layoutStorage.SavePresets(_allPresets);
            }

            ReloadPresetsForZone();

            LoadPresetCommand = new RelayCommand(_ => LoadPresetToTablesOnMap(), _ => SelectedPreset != null);
            SavePresetCommand = new RelayCommand(_ => SaveCurrentPreset(), _ => SelectedPreset != null);
            AddTableToMapCommand = new RelayCommand(
                                                    p => AddSelectedTableToMap(p),
                                                    _ => CanAddSelectedTable());
            CreatePresetCommand = new RelayCommand(_ => CreateNewPreset());
            DeletePresetCommand = new RelayCommand(_ => DeleteCurrentPreset(), _ => SelectedPreset != null);
            RemoveTableFromMapCommand = new RelayCommand(
                                                        p => RemoveTableFromMap(p as TableOnMapItem),
                                                        p => p is TableOnMapItem);

            SetActivePresetCommand = new RelayCommand(_ => SetActivePreset(), _ => SelectedPreset != null);
        }

        private void InvalidateCommands()
        {
            System.Windows.Input.CommandManager.InvalidateRequerySuggested();
        }

        private void SetActivePreset()
        {
            if (SelectedPreset == null) return;
            if (string.IsNullOrWhiteSpace(SelectedZone)) return;

            // 1) Снять активность у всех пресетов этой зоны
            foreach (var p in _allPresets.Where(p => string.Equals(p.Zone, SelectedZone, StringComparison.OrdinalIgnoreCase)))
                p.IsActive = false;

            // 2) Поставить активность выбранному
            SelectedPreset.IsActive = true;

            // 3) Сохранить обновлённые пресеты (это важно!)
            _layoutStorage.SavePresets(_allPresets);

            // 4) Для официантов сохраняем активный пресет по Id
            _layoutStorage.SetActivePresetName(SelectedZone, SelectedPreset.Id);
            MessageBox.Show($"Активный пресет для зоны {SelectedZone}: {SelectedPreset.Name}");


            // 5) Обновить список в UI
            ReloadPresetsForZone();

        }


        private void ReloadPresetsForZone()
        {
            Presets.Clear();

            var byZone = _layoutStorage.GetPresetsForZone(SelectedZone);
            foreach (var preset in byZone)
                Presets.Add(preset);

            if (Presets.Count == 0)
            {
                var preset = new HallLayoutPresetModel
                {
                    Id = $"{SelectedZone}_Default",
                    Name = "Стандартный",
                    Zone = SelectedZone,
                    IsActive = true
                };

                _allPresets.Add(preset);
                _layoutStorage.SavePresets(_allPresets);

                Presets.Add(preset);
            }

            SelectedPreset = Presets.FirstOrDefault(p => p.IsActive) ?? Presets.FirstOrDefault();
            UpdateAvailableTables();
        }

        private void LoadPresetToTablesOnMap()
        {
            TablesOnMap.Clear();

            if (SelectedPreset == null)
            {
                UpdateAvailableTables();
                return;
            }

            // страхуемся, что у пресета таблицы не null
            SelectedPreset.Tables ??= new List<HallTableLayoutModel>();

            var tablesById = _allTables.ToDictionary(t => t.Id);

            foreach (var layout in SelectedPreset.Tables.Where(t => t.Zone == SelectedZone))
            {
                if (!tablesById.TryGetValue(layout.TableId, out var table))
                    continue;

                TablesOnMap.Add(new TableOnMapItem
                {
                    TableId = table.Id,
                    TableName = table.Name,
                    Seats = table.Seats,
                    Status = table.Status,
                    ZoneCode = SelectedZone,
                    X = layout.X,
                    Y = layout.Y,
                    Model = layout.ModelType
                });
            }

            SelectedTableOnMap = TablesOnMap.FirstOrDefault();
            UpdateAvailableTables();
        }

        private void UpdateAvailableTables()
        {
            AvailableTables.Clear();

            var usedIds = new HashSet<int>(TablesOnMap.Select(t => t.TableId));

            foreach (var table in _allTables
                         .Where(t => t.Zone == SelectedZone && !usedIds.Contains(t.Id))
                         .OrderBy(t => t.Name))
            {
                AvailableTables.Add(table);
            }
        }

        private bool CanAddSelectedTable() =>
            SelectedPreset != null && SelectedAvailableTable != null;

        private void AddSelectedTableToMap(object? parameter)
        {
            if (SelectedPreset == null || SelectedAvailableTable == null)
                return;

            var existing = TablesOnMap.FirstOrDefault(t => t.TableId == SelectedAvailableTable.Id);
            if (existing != null)
            {
                SelectedTableOnMap = existing;
                return;
            }

            var x = 60.0;
            var y = 60.0;

            if (parameter is System.Windows.Size size && size.Width > 0 && size.Height > 0)
            {
                // добавляем в центр карты (логические координаты)
                // ориентируемся на “Single” размер иконки ~50 (см. XAML)
                x = (size.Width - 50) / 2;
                y = (size.Height - 50) / 2;
            }

            var newItem = new TableOnMapItem
            {
                TableId = SelectedAvailableTable.Id,
                TableName = SelectedAvailableTable.Name,
                Seats = SelectedAvailableTable.Seats,
                Status = SelectedAvailableTable.Status,
                ZoneCode = SelectedZone,
                X = x,
                Y = y,
                Model = "Single"
            };

            TablesOnMap.Add(newItem);
            SelectedTableOnMap = newItem;

            UpdateAvailableTables();
        }

        public void MoveTable(TableOnMapItem table, double deltaX, double deltaY)
        {
            if (table == null) return;

            table.X += deltaX;
            table.Y += deltaY;
        }

        private void SaveCurrentPreset()
        {
            if (SelectedPreset == null)
                return;

            SelectedPreset.Zone = SelectedZone;

            SelectedPreset.Tables = TablesOnMap
                .Select(t => new HallTableLayoutModel
                {
                    TableId = t.TableId,
                    Zone = SelectedZone,
                    X = t.X,
                    Y = t.Y,
                    ModelType = t.Model
                })
                .ToList();

            _layoutStorage.SavePreset(SelectedPreset);

            // Обновляем локальный список
            var inAll = _allPresets.FirstOrDefault(p => p.Id == SelectedPreset.Id);
            if (inAll == null)
                _allPresets.Add(SelectedPreset);
            else
                inAll.Tables = SelectedPreset.Tables;

            // Обновляем коллекцию Presets (чтобы не потерять IsActive / Name)
            ReloadPresetsForZone();
        }

        private void CreateNewPreset()
        {
            var name = _prompt?.Ask("Новый пресет", "Введите имя пресета:", $"Пресет ({SelectedZone})");
            if (string.IsNullOrWhiteSpace(name))
                return;

            var preset = new HallLayoutPresetModel
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = name,
                Zone = SelectedZone,
                IsActive = false,
                Tables = new List<HallTableLayoutModel>()
            };

            _layoutStorage.SavePreset(preset);
            _allPresets.Add(preset);

            ReloadPresetsForZone();
            SelectedPreset = Presets.FirstOrDefault(p => p.Id == preset.Id);
        }


        private void DeleteCurrentPreset()
        {
            if (SelectedPreset == null)
                return;

            var id = SelectedPreset.Id;
            _layoutStorage.DeletePreset(id);
            _allPresets.RemoveAll(p => p.Id == id);

            ReloadPresetsForZone();
        }

        private void RemoveTableFromMap(TableOnMapItem? item)
        {
            if (item == null) return;

            TablesOnMap.Remove(item);

            if (SelectedTableOnMap == item)
                SelectedTableOnMap = TablesOnMap.FirstOrDefault();

            UpdateAvailableTables();
        }
    }
}
