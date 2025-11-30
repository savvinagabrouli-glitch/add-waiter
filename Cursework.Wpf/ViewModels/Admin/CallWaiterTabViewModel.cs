using Cursework.Application.Interfaces;
using Cursework.Application.Realtime;
using Cursework.Application.Services;
using Cursework.Domains.Models;
using Cursework.Wpf.Services.Realtime;
using Cursework.Wpf.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Cursework.Wpf.ViewModels.Admin
{
    public class CallWaiterTabViewModel : ViewModelBase
    {
        private readonly ICallWaiterService _callWaiterService;
        private readonly ITableService _tableService;
        private readonly IRealtimeService _realtime;
        public ICommand ClearTableFilterCommand { get; }

        public ObservableCollection<CallWaiter> CallWaiterList { get; }
        private readonly CancellationTokenSource _cts = new();
        public ICollectionView CallWaitersView { get; }
        public ObservableCollection<DiningTable> Tables { get; } = new();

        public CallWaiterTabViewModel(ICallWaiterService callWaiterService, ITableService tableService, IRealtimeService realtime)
        {
            _callWaiterService = callWaiterService;
            _tableService = tableService;
            _realtime = realtime;

            CallWaiterList = new ObservableCollection<CallWaiter>();
            CallWaitersView = CollectionViewSource.GetDefaultView(CallWaiterList);
            CallWaitersView.SortDescriptions.Add(
                new SortDescription(nameof(CallWaiter.Id), ListSortDirection.Ascending));
            CallWaitersView.Filter = CallWaiterFilter;

            AddCommand = new RelayCommand(_ => BeginAdd(), _ => !IsEditingGrid);
            EditCommand = new RelayCommand(_ => BeginEdit(), _ => Selected != null && !IsEditingGrid);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => Selected != null && !IsEditingGrid);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => IsEditingGrid && EditItem != null);
            CancelCommand = new RelayCommand(_ => CancelEdit(), _ => IsEditingGrid);
            ClearTableFilterCommand = new RelayCommand(_ =>
            {
                FilterTableId = null;
            });

            _realtime.CallWaiterChanged += OnCallWaiterChanged;
            _ = LoadAsync();
            _ = LoadTablesAsync();
        }

        private CallWaiter? _selected;
        public CallWaiter? Selected
        {
            get => _selected;
            set
            {
                if (Set(ref _selected, value))
                {
                    Raise(nameof(IsItemSelected));

                    if (IsEditingGrid)
                        return;

                    if (value is null)
                    {
                        EditItem = null;
                        CreatedAtInput = string.Empty;
                        HandledAtInput = string.Empty;
                        EditingType = null;
                        EditingHandledStatus = null;
                    }
                    else
                    {
                        var clone = Clone(value);
                        EditItem = clone;

                        CreatedAtInput = clone.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                        HandledAtInput = clone.HandledAt?.ToString("dd.MM.yyyy HH:mm") ?? string.Empty;
                        EditingType = MapCodeTypeToUi(clone.Type);
                        EditingHandledStatus = clone.IsHandled ? "Обработан" : "Не обработан";
                    }
                    InvalidateCommands();
                }
            }
        }

        private CallWaiter? _editItem;
        public CallWaiter? EditItem
        {
            get => _editItem;
            set => Set(ref _editItem, value);
        }

        private bool _isEditingGrid;
        public bool IsEditingGrid
        {
            get => _isEditingGrid;
            set
            {
                if (Set(ref _isEditingGrid, value))
                {
                    InvalidateCommands();
                }
            }
        }

        public bool IsItemSelected => Selected != null;

        private int _filteredCount;
        public int FilteredCount
        {
            get => _filteredCount;
            set => Set(ref _filteredCount, value);
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set => Set(ref _totalCount, value);
        }

        private int? _filterTableId;
        public int? FilterTableId
        {
            get => _filterTableId;
            set
            {
                if (Set(ref _filterTableId, value))
                    ApplyFilter();
            }
        }

        private string _filterType = "Все";
        public string FilterType
        {
            get => _filterType;
            set
            {
                if (Set(ref _filterType, value))
                    ApplyFilter();
            }
        }

        private string _filterHandled = "Все";
        public string FilterHandled
        {
            get => _filterHandled;
            set
            {
                if (Set(ref _filterHandled, value))
                    ApplyFilter();
            }
        }

        private string? _createdAtFilterText;
        public string? CreatedAtFilterText
        {
            get => _createdAtFilterText;
            set
            {
                if (Set(ref _createdAtFilterText, value))
                    ApplyFilter();
            }
        }

        private string? _handledAtFilterText;
        public string? HandledAtFilterText
        {
            get => _handledAtFilterText;
            set
            {
                if (Set(ref _handledAtFilterText, value))
                    ApplyFilter();
            }
        }

        private string? _searchOrderIdText;
        public string? SearchOrderIdText
        {
            get => _searchOrderIdText;
            set
            {
                if (Set(ref _searchOrderIdText, value))
                    ApplyFilter();
            }
        }

        private string? _createdAtInput;
        public string? CreatedAtInput
        {
            get => _createdAtInput;
            set
            {
                if (Set(ref _createdAtInput, value) && EditItem != null &&
                    TryParseDateTime(value, out var dt))
                {
                    EditItem.CreatedAt = dt;
                }
            }
        }

        private string? _handledAtInput;
        public string? HandledAtInput
        {
            get => _handledAtInput;
            set
            {
                if (Set(ref _handledAtInput, value) && EditItem != null &&
                    TryParseDateTime(value, out var dt))
                {
                    EditItem.HandledAt = dt;
                }
            }
        }

        private string? _editingType;
        public string? EditingType
        {
            get => _editingType;
            set
            {
                if (Set(ref _editingType, value) && EditItem != null)
                {
                    EditItem.Type = MapUiTypeToCode(value);
                }
            }
        }

        private string? _editingHandledStatus;
        public string? EditingHandledStatus
        {
            get => _editingHandledStatus;
            set
            {
                if (Set(ref _editingHandledStatus, value) && EditItem != null)
                {
                    EditItem.IsHandled = value == "Обработан";
                }
            }
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        
        private async Task LoadAsync()
        {
            try
            {
                var ct = _cts.Token;
                var items = await _callWaiterService.GetAllAsync();
                CallWaiterList.Clear();

                foreach (var cw in items.OrderBy(c => c.Id))
                    CallWaiterList.Add(cw);

                TotalCount = CallWaiterList.Count;
                ApplyFilter();
                InvalidateCommands();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить вызовы официанта:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            CallWaitersView.Refresh();
            FilteredCount = CallWaiterList.Count(cw => CallWaiterFilter(cw));
        }

        private bool CallWaiterFilter(object? obj)
        {
            if (obj is not CallWaiter cw)
                return false;

            if (FilterTableId.HasValue && cw.TableId != FilterTableId.Value)
                return false;

            if (!string.IsNullOrWhiteSpace(FilterType) && FilterType != "Все")
            {
                var codeType = MapUiTypeToCode(FilterType);
                if (!string.Equals(cw.Type, codeType, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(FilterHandled) && FilterHandled != "Все")
            {
                if (FilterHandled == "Только обработанные" && !cw.IsHandled) return false;
                if (FilterHandled == "Только необработанные" && cw.IsHandled) return false;
            }

            if (!string.IsNullOrWhiteSpace(CreatedAtFilterText) &&
                TryParseDateTime(CreatedAtFilterText, out var createdMin))
            {
                if (cw.CreatedAt < createdMin)
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(HandledAtFilterText) &&
                TryParseDateTime(HandledAtFilterText, out var handledMin))
            {
                if (!cw.HandledAt.HasValue || cw.HandledAt.Value < handledMin)
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(SearchOrderIdText))
            {
                if (int.TryParse(SearchOrderIdText, out var oid))
                {
                    if (cw.OrderId != oid)
                        return false;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private static bool TryParseDateTime(string? text, out DateTime result)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                result = default;
                return false;
            }

            return DateTime.TryParseExact(
                text.Trim(),
                "dd.MM.yyyy HH:mm",
                CultureInfo.GetCultureInfo("ru-RU"),
                DateTimeStyles.AssumeLocal,
                out result);
        }

        private void BeginAdd()
        {
            var now = DateTime.Now;

            EditItem = new CallWaiter
            {
                CreatedAt = now,
                IsHandled = false,
                Type = "Call" 
            };

            CreatedAtInput = now.ToString("dd.MM.yyyy HH:mm");
            HandledAtInput = string.Empty;
            EditingType = "Вызов";
            EditingHandledStatus = "Не обработан";

            IsEditingGrid = true;
        }

        private void BeginEdit()
        {
            if (Selected is null)
                return;

            EditItem = Clone(Selected);

            CreatedAtInput = EditItem.CreatedAt.ToString("dd.MM.yyyy HH:mm");
            HandledAtInput = EditItem.HandledAt?.ToString("dd.MM.yyyy HH:mm") ?? string.Empty;
            EditingType = MapCodeTypeToUi(EditItem.Type);
            EditingHandledStatus = EditItem.IsHandled ? "Обработан" : "Не обработан";

            IsEditingGrid = true;
        }

        private async Task SaveAsync()
        {
            if (EditItem == null)
                return;

            if (EditItem.TableId <= 0)
            {
                MessageBox.Show(
                    "Выберите стол для вызова.",
                    "Вызовы официанта",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!string.IsNullOrWhiteSpace(EditItem.OrderId?.ToString()))
            {
                if (EditItem.OrderId <= 0)
                {
                    MessageBox.Show(
                        "Номер заказа должен быть положительным числом или пустым.",
                        "Вызовы официанта",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
            }

            if (!string.IsNullOrWhiteSpace(HandledAtInput))
            {
                if (!TryParseDateTime(HandledAtInput, out var handled))
                {
                    MessageBox.Show(
                        "Поле \"Взят\" должно быть в формате: дд.мм.гггг чч:мм или пустым.",
                        "Вызовы официанта",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                    return;
                }
                EditItem.HandledAt = handled;
            }
            else
            {
                EditItem.HandledAt = null;
            }

            if (string.IsNullOrWhiteSpace(EditingType))
            {
                MessageBox.Show(
                    "Выберите тип вызова.",
                    "Вызовы официанта",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(EditingHandledStatus))
            {
                MessageBox.Show(
                    "Выберите статус обработки.",
                    "Вызовы официанта",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                if (EditItem.Id == 0)
                {
                    var created = await _callWaiterService.AddAsync(EditItem);
                    created.Table ??= Tables.FirstOrDefault(t => t.Id == created.TableId);
                    CallWaiterList.Add(created);
                    Selected = created;
                }
                else
                {
                    await _callWaiterService.UpdateAsync(EditItem);

                    var existing = CallWaiterList.FirstOrDefault(c => c.Id == EditItem.Id);
                    if (existing != null)
                    {
                        Copy(EditItem, existing);
                        var table = Tables.FirstOrDefault(t => t.Id == existing.TableId);
                        if (table != null)
                            existing.Table = table;
                    }
                }

                TotalCount = CallWaiterList.Count;
                IsEditingGrid = false;
                ApplyFilter();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Вызовы официанта",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при сохранении вызова официанта:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        private async Task DeleteAsync()
        {
            if (Selected == null)
                return;

            var confirm = MessageBox.Show(
                "Удалить выбранный вызов официанта?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                await _callWaiterService.DeleteAsync(Selected.Id);

                CallWaiterList.Remove(Selected);
                Selected = null;

                TotalCount = CallWaiterList.Count;
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при удалении вызова официанта:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void CancelEdit()
        {
            if (Selected != null)
            {
                EditItem = Clone(Selected);

                CreatedAtInput = EditItem.CreatedAt.ToString("dd.MM.yyyy HH:mm");
                HandledAtInput = EditItem.HandledAt?.ToString("dd.MM.yyyy HH:mm") ?? string.Empty;
                EditingType = MapCodeTypeToUi(EditItem.Type);
                EditingHandledStatus = EditItem.IsHandled ? "Обработан" : "Не обработан";
            }
            else
            {
                EditItem = null;
                CreatedAtInput = string.Empty;
                HandledAtInput = string.Empty;
                EditingType = null;
                EditingHandledStatus = null;
            }

            IsEditingGrid = false;
        }

        private static CallWaiter Clone(CallWaiter src)
        {
            return new CallWaiter
            {
                Id = src.Id,
                TableId = src.TableId,
                OrderId = src.OrderId,
                CreatedAt = src.CreatedAt,
                HandledAt = src.HandledAt,
                Type = src.Type,
                IsHandled = src.IsHandled,
                Table = src.Table
            };
        }

        private static void Copy(CallWaiter from, CallWaiter to)
        {
            to.TableId = from.TableId;
            to.OrderId = from.OrderId;
            to.CreatedAt = from.CreatedAt;
            to.HandledAt = from.HandledAt;
            to.Type = from.Type;
            to.IsHandled = from.IsHandled;
            to.Table = from.Table;
        }

        private static string MapUiTypeToCode(string? uiType) =>
            uiType switch
            {
                "Вызов" => "Call",
                "Принять предзаказ" => "AcceptPreorder",
                _ => "Call"
            };

        private static string MapCodeTypeToUi(string? code) =>
            code switch
            {
                "Call" => "Вызов",
                "AcceptPreorder" => "Принять предзаказ",
                _ => "Вызов"
            };

        private async Task LoadTablesAsync()
        {
            try
            {
                var tables = await _tableService.GetAllAsync();

                Tables.Clear();
                foreach (var t in tables.OrderBy(t => t.Name))
                    Tables.Add(t);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить список столов:\n{ex.Message}",
                    "Ошибка",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void InvalidateCommands() => CommandManager.InvalidateRequerySuggested();

        private void OnCallWaiterChanged(CallWaiterChangedDto dto)
        {
            if (dto.CallWaiter == null)
                return;

            RunOnUi(() =>
            {
                var existing = CallWaiterList.FirstOrDefault(c => c.Id == dto.CallWaiter.Id);

                switch (dto.Action)
                {
                    case EntityChangeAction.Created:
                        if (existing == null)
                            CallWaiterList.Add(dto.CallWaiter);
                        else
                        {
                            var idx = CallWaiterList.IndexOf(existing);
                            CallWaiterList[idx] = dto.CallWaiter;
                        }
                        break;

                    case EntityChangeAction.Updated:
                        if (existing != null)
                        {
                            var idx = CallWaiterList.IndexOf(existing);
                            CallWaiterList[idx] = dto.CallWaiter;
                        }
                        else
                        {
                            CallWaiterList.Add(dto.CallWaiter);
                        }
                        break;

                    case EntityChangeAction.Deleted:
                        if (existing != null)
                            CallWaiterList.Remove(existing);
                        break;
                }
            });
        }

        private void RunOnUi(Action action)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }

        public void Dispose()
        {
            _realtime.CallWaiterChanged -= OnCallWaiterChanged;
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
