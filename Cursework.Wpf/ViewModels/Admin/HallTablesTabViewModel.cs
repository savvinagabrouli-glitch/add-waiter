using Cursework.Application.Interfaces;
using Cursework.Domains.Models;
using Cursework.Wpf.Services.HallLayout;
using Cursework.Wpf.Services.QR_Code;
using Cursework.Wpf.ViewModels.Base;
using Cursework.Wpf.Views.Dialogs;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Cursework.Wpf.ViewModels.Admin
{
    public class HallTablesTabViewModel : ViewModelBase
    {
        private readonly ITableService _service;
        private readonly IQrCodeService _qrService;
        private readonly IHallLayoutStorageService _hallLayoutStorage;

        private static readonly string[] AllowedStatuses =
            { "Free", "Occupied", "Reserved", "Cleaning", "OutOfService" };

        private static readonly string[] AllowedZones =
            { "MainHall", "Terrace", "VIP", "Bar" };

        public ObservableCollection<DiningTable> Tables { get; } = new();
        public ICollectionView TablesView { get; }

        private DiningTable? _selected;
        public DiningTable? Selected
        {
            get => _selected;
            set
            {
                if (Set(ref _selected, value))
                {
                    if (!IsEditingGrid && value != null)
                        EditItem = Clone(value);

                    Raise(nameof(CanEdit));
                    Raise(nameof(CanDelete));
                    Raise(nameof(CanSave));
                    Raise(nameof(CanCancel));
                    Raise(nameof(CanAdd));

                    InvalidateCanExec();
                }
            }
        }

        private DiningTable _editItem = new();
        public DiningTable EditItem
        {
            get => _editItem;
            set
            {
                if (Set(ref _editItem, value))
                {
                    Raise(nameof(CanSave));
                    InvalidateCanExec();
                }
            }
        }

        private bool _isEditingGrid;
        public bool IsEditingGrid
        {
            get => _isEditingGrid;
            set
            {
                if (Set(ref _isEditingGrid, value))
                {
                    Raise(nameof(CanAdd));
                    Raise(nameof(CanEdit));
                    Raise(nameof(CanDelete));
                    Raise(nameof(CanSave));
                    Raise(nameof(CanCancel));

                    InvalidateCanExec();
                }
            }
        }

        public int TotalCount => Tables.Count;

        private string? _filterZone = "Все";
        public string? FilterZone
        {
            get => _filterZone;
            set
            {
                if (Set(ref _filterZone, value))
                    ApplyFilter();
            }
        }

        private string? _filterStatus = "Все";
        public string? FilterStatus
        {
            get => _filterStatus;
            set
            {
                if (Set(ref _filterStatus, value))
                    ApplyFilter();
            }
        }

        private string? _filterName;
        public string? FilterName
        {
            get => _filterName;
            set
            {
                if (Set(ref _filterName, value))
                    ApplyFilter();
            }
        }

        private string? _filterSeatsText;
        public string? FilterSeatsText
        {
            get => _filterSeatsText;
            set
            {
                if (Set(ref _filterSeatsText, value))
                    ApplyFilter();
            }
        }

        private int _filteredCount;
        public int FilteredCount
        {
            get => _filteredCount;
            private set => Set(ref _filteredCount, value);
        }

        public event Action? ScrollToSelectedRequested;
        public bool CanAdd => !IsEditingGrid;
        public bool CanEdit => Selected != null && !IsEditingGrid;
        public bool CanDelete => Selected != null && !IsEditingGrid;
        public bool CanSave => IsEditingGrid;
        public bool CanCancel => IsEditingGrid;
        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand OpenHallMapCommand { get; }
        public ICommand OpenModelsCommand { get; }
        public ICommand ShowQrCommand { get; }

        public HallTablesTabViewModel(ITableService service, IQrCodeService qrService)
        {
            _service = service;
            _qrService = qrService;
            _hallLayoutStorage = new HallLayoutStorageService();

            TablesView = CollectionViewSource.GetDefaultView(Tables);
            TablesView.Filter = TableFilter;

            AddCommand = new RelayCommand(_ => Add(), _ => CanAdd);
            EditCommand = new RelayCommand(_ => BeginEdit(), _ => CanEdit);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => CanDelete);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => CanCancel);

            OpenHallMapCommand = new RelayCommand(_ => OpenHallMap());
            OpenModelsCommand = new RelayCommand(_ => ShowNotImplemented());

            ShowQrCommand = new RelayCommand(_ => ShowQr(), _ => Selected != null && !IsEditingGrid);
        }

        public async Task LoadAsync()
        {
            try
            {
                Tables.Clear();
                var all = await _service.GetAllAsync();
                foreach (var t in all.OrderBy(t => t.Id))
                    Tables.Add(t);

                Raise(nameof(TotalCount));
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить список столов.\n\n{ex.Message}",
                    "Ошибка загрузки",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Add()
        {
            var fresh = new DiningTable
            {
                Id = 0,
                Name = "",
                Seats = 2,
                Zone = "MainHall",
                Status = "Free"
            };

            Tables.Add(fresh);
            Selected = fresh;
            EditItem = Clone(fresh);
            IsEditingGrid = true;

            Raise(nameof(TotalCount));
            ScrollToSelectedRequested?.Invoke();
        }

        private void BeginEdit()
        {
            if (Selected == null || IsEditingGrid)
                return;

            EditItem = Clone(Selected);
            IsEditingGrid = true;
        }

        private async Task DeleteAsync()
        {
            if (Selected == null) return;

            var toDelete = Selected;

            if (!Tables.Contains(toDelete))
            {
                MessageBox.Show(
                    "Выбранный стол отсутствует в списке. Обновите список столов.",
                    "Удаление стола",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show(
                    $"Удалить стол \"{toDelete.Name}\"?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                if (toDelete.Id == 0)
                {
                    Tables.Remove(toDelete);
                }
                else
                {
                    await _service.DeleteAsync(toDelete.Id);
                    Tables.Remove(toDelete);
                }

                Selected = null;
                Raise(nameof(TotalCount));
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Столы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось удалить стол.\n\n{ex.Message}",
                    "Ошибка удаления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task SaveAsync()
        {
            if (!ValidateEditItem(out var validationError))
            {
                MessageBox.Show(
                    validationError,
                    "Проверка данных стола",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!CheckUniqueName())
                return;

            try
            {
                if (EditItem.Id == 0)
                {
                    var created = await _service.AddAsync(Clone(EditItem));
                    var idx = Selected != null ? Tables.IndexOf(Selected) : -1;

                    if (idx >= 0)
                        Tables[idx] = created;
                    else
                        Tables.Add(created);

                    Selected = created;
                }
                else
                {
                    await _service.UpdateAsync(Clone(EditItem));

                    if (Selected != null)
                    {
                        Copy(EditItem, Selected);
                        TablesView.Refresh();
                    }
                }

                IsEditingGrid = false;
                ScrollToSelectedRequested?.Invoke();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Проверка данных стола",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось сохранить стол.\n\n{ex.Message}",
                    "Ошибка сохранения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            if (EditItem.Id == 0)
            {
                if (Selected != null && Tables.Contains(Selected))
                    Tables.Remove(Selected);

                Selected = null;
                Raise(nameof(TotalCount));
            }
            else
            {
                if (Selected != null)
                    EditItem = Clone(Selected);
            }

            IsEditingGrid = false;
        }

        private bool ValidateEditItem(out string error)
        {
            if (EditItem == null)
            {
                error = "Нет данных для сохранения.";
                return false;
            }

            var name = EditItem.Name?.Trim();
            if (string.IsNullOrWhiteSpace(name))
            {
                error = "Укажите название стола.";
                return false;
            }

            if (name.Length > 20)
            {
                error = "Длина названия стола не должна превышать 20 символов.";
                return false;
            }

            if (EditItem.Seats <= 0)
            {
                error = "Количество мест должно быть больше 0.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(EditItem.Status) ||
                !AllowedStatuses.Contains(EditItem.Status))
            {
                error = "Выберите корректный статус стола.";
                return false;
            }

            var zone = EditItem.Zone?.Trim();
            if (string.IsNullOrWhiteSpace(zone) || !AllowedZones.Contains(zone))
            {
                error =
                    "Укажите корректную зону стола.\nДопустимые значения: Основной зал, Терраса, VIP, Бар.";
                return false;
            }

            if (zone.Length > 50)
            {
                error = "Длина названия зоны не должна превышать 50 символов.";
                return false;
            }

            error = "";
            return true;
        }

        private bool CheckUniqueName()
        {
            var name = EditItem?.Name?.Trim();
            if (string.IsNullOrEmpty(name))
                return false;

            var exists = Tables.Any(t =>
                t.Id != EditItem.Id &&
                string.Equals(t.Name?.Trim(), name, StringComparison.OrdinalIgnoreCase));

            if (!exists)
                return true;

            MessageBox.Show(
                "Стол с таким названием уже существует.\nВведите другое название.",
                "Дублирующее название стола",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);

            return false;
        }

        private static DiningTable Clone(DiningTable s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Seats = s.Seats,
            Zone = s.Zone,
            Status = s.Status
        };

        private static void Copy(DiningTable from, DiningTable to)
        {
            to.Name = from.Name;
            to.Seats = from.Seats;
            to.Zone = from.Zone;
            to.Status = from.Status;
        }

        private static void InvalidateCanExec() =>
            CommandManager.InvalidateRequerySuggested();

        private static void ShowNotImplemented()
        {
            MessageBox.Show(
                "Функционал пока не реализован.",
                "Информация",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void ApplyFilter()
        {
            TablesView.Refresh();
            UpdateFilteredCount();
        }

        private void UpdateFilteredCount()
        {
            FilteredCount = TablesView.Cast<object>().Count();
        }

        private bool TableFilter(object obj)
        {
            if (obj is not DiningTable t) return false;

            var zoneFilter = FilterZone;
            if (!string.IsNullOrWhiteSpace(zoneFilter) &&
                !string.Equals(zoneFilter, "Все", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(t.Zone, zoneFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            var statusFilter = FilterStatus;
            if (!string.IsNullOrWhiteSpace(statusFilter) &&
                !string.Equals(statusFilter, "Все", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(t.Status, statusFilter, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            var nameFilter = FilterName?.Trim();
            if (!string.IsNullOrWhiteSpace(nameFilter))
            {
                if (t.Name == null ||
                    t.Name.IndexOf(nameFilter, StringComparison.OrdinalIgnoreCase) < 0)
                    return false;
            }

            var seatsText = FilterSeatsText?.Trim();
            if (!string.IsNullOrWhiteSpace(seatsText))
            {
                if (!SeatsMatchesFilter(t.Seats, seatsText))
                    return false;
            }

            return true;
        }

        private static bool SeatsMatchesFilter(int seats, string seatsText)
        {
            var normalized = seatsText.Replace(" ", "");

            var parts = normalized.Split('-', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 1)
            {
                if (int.TryParse(parts[0], out var value) && value > 0)
                    return seats == value;

                return true;
            }

            if (parts.Length == 2)
            {
                if (!int.TryParse(parts[0], out var from) ||
                    !int.TryParse(parts[1], out var to))
                    return true;

                if (from > 0 && to > 0 && from <= to)
                    return seats >= from && seats <= to;

                return true;
            }

            return true;
        }

        private void OpenHallMap()
        {
            try
            {
                var vm = new HallMapViewModel(
                    _hallLayoutStorage,
                    Tables,              // текущий список DiningTable
                    initialZone: "MainHall",
                    isAdminMode: true,
                    prompt: new Cursework.Wpf.Services.Dialogs.TextPromptService());

                var dlg = new HallMapDialog(vm);
                dlg.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при открытии карты зала.\n\n{ex.Message}",
                    "Карта зала",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ShowQr()
        {
            if (Selected == null)
            {
                MessageBox.Show(
                    "Выберите стол, для которого нужно сгенерировать QR-код.",
                    "QR-код стола",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                var vm = new TableQrViewModel(_qrService, Selected);

                var dialog = new TableQrWindow
                {
                    DataContext = vm,
                    Owner = System.Windows.Application.Current.MainWindow
                };

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при показе QR-кода.\n\n{ex.Message}",
                    "QR-код стола",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

    }
}
