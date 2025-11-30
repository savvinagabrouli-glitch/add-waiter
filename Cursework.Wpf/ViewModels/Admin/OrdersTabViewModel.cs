using Cursework.Application.Interfaces;
using Cursework.Application.Realtime;
using Cursework.Application.Services;
using Cursework.Domains.Models;
using Cursework.Wpf.Services.Realtime;
using Cursework.Wpf.ViewModels.Base;
using Cursework.Wpf.Views.Dialogs;
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
    public class OrdersTabViewModel : ViewModelBase
    {
        private readonly IOrderService _ordersService;
        private readonly ITableService _tableService;
        private readonly IStaffService _staffService;
        private readonly IOrderDetailsService _orderDetailsService;
        private readonly IMenuService _menuService;
        private readonly IRealtimeService _realtime;

        public ObservableCollection<Order> Orders { get; } = new();
        public ObservableCollection<DiningTable> Tables { get; } = new();
        public ObservableCollection<Staff> Waiters { get; } = new();
        
        private CancellationTokenSource _cts = new();

        public ICollectionView OrdersView { get; }

        public class OrderStatusOption
        {
            public string? Code { get; init; }
            public string Display { get; init; } = "";
        }

        public ObservableCollection<OrderStatusOption> StatusOptions { get; } =
            new()
            {
                new OrderStatusOption { Code = "Preorder",  Display = "Предзаказ" },
                new OrderStatusOption { Code = "New",       Display = "Новый" },
                new OrderStatusOption { Code = "Pending",   Display = "В ожидании" },
                new OrderStatusOption { Code = "ReadyToPay",Display = "Готов к оплате" },
                new OrderStatusOption { Code = "Closed",    Display = "Закрыт" },
                new OrderStatusOption { Code = "Cancelled", Display = "Отменён" }
            };

        public ObservableCollection<OrderStatusOption> StatusFilterOptions { get; }

        private string? _selectedStatusFilterCode;
        public string? SelectedStatusFilterCode
        {
            get => _selectedStatusFilterCode;
            set
            {
                if (Set(ref _selectedStatusFilterCode, value))
                    RefreshView();
            }
        }

        private string? _dateRangeFilterText;
        public string? DateRangeFilterText
        {
            get => _dateRangeFilterText;
            set
            {
                if (Set(ref _dateRangeFilterText, value))
                    RefreshView();
            }
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (Set(ref _searchText, value))
                    RefreshView();
            }
        }

        private Order? _selected;
        public Order? Selected
        {
            get => _selected;
            set
            {
                if (Set(ref _selected, value))
                {
                    if (!IsEditingGrid && _selected != null)
                    {
                        EditingOrder = CloneOrder(_selected);
                        SyncDateInputsFromEditing();
                    }

                    InvalidateCommands();
                }
            }
        }

        private Order? _editingOrder;
        public Order? EditingOrder
        {
            get => _editingOrder;
            set => Set(ref _editingOrder, value);
        }

        private string _createdAtInput = "";
        public string CreatedAtInput
        {
            get => _createdAtInput;
            set => Set(ref _createdAtInput, value);
        }

        private string _closedAtInput = "";
        public string ClosedAtInput
        {
            get => _closedAtInput;
            set => Set(ref _closedAtInput, value);
        }

        private bool _isEditingGrid;
        public bool IsEditingGrid
        {
            get => _isEditingGrid;
            set
            {
                if (Set(ref _isEditingGrid, value))
                    InvalidateCommands();
            }
        }

        private int _filteredCount;
        public int FilteredCount
        {
            get => _filteredCount;
            set => Set(ref _filteredCount, value);
        }

        public ICommand AddCommand { get; }
        public ICommand EditCommand { get; }
        public ICommand DeleteCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ShowDetailsCommand { get; }

        public OrdersTabViewModel(
            IOrderService ordersService,
            ITableService tableService,
            IStaffService staffService,
            IMenuService menuService,
            IOrderDetailsService orderDetailsService,
            IRealtimeService realtime)
        {
            _ordersService = ordersService;
            _tableService = tableService;
            _staffService = staffService;
            _menuService = menuService;
            _orderDetailsService = orderDetailsService;
            _realtime = realtime;
            _realtime.OrderChanged += OnOrderChanged;
            _realtime.StaffChanged += OnStaffChanged;

            StatusFilterOptions = new ObservableCollection<OrderStatusOption>(
                new[]
                {
                    new OrderStatusOption { Code = null, Display = "Все" }
                }.Concat(StatusOptions)
            );

            OrdersView = CollectionViewSource.GetDefaultView(Orders);
            OrdersView.SortDescriptions.Clear();
            OrdersView.SortDescriptions.Add(
                new SortDescription(nameof(Order.Id), ListSortDirection.Ascending));
            OrdersView.Filter = FilterOrder;

            AddCommand = new RelayCommand(_ => Add(), _ => !IsEditingGrid);
            EditCommand = new RelayCommand(_ => BeginEdit(), _ => Selected != null && !IsEditingGrid);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => Selected != null && !IsEditingGrid);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => EditingOrder != null && IsEditingGrid);
            CancelCommand = new RelayCommand(_ => CancelEdit(), _ => IsEditingGrid);
            ShowDetailsCommand = new RelayCommand(_ => ShowDetails(), _ => Selected != null);

            _ = InitializeAsync();
        }

        private void RefreshView()
        {
            OrdersView.Refresh();
            FilteredCount = OrdersView.Cast<Order>().Count();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var ct = _cts.Token;
                Orders.Clear();
                var items = await _ordersService.GetAllAsync(ct);
                foreach (var order in items)
                    Orders.Add(order);

                await LoadOrdersAsync();
                await LoadTablesAsync();
                await LoadWaitersAsync();
            }
            catch
            {
            }

        }

        private async Task LoadOrdersAsync(int? selectId = null)
        {
            try
            {
                var items = await _ordersService.GetAllAsync();
                Orders.Clear();
                foreach (var o in items)
                    Orders.Add(o);

                if (selectId.HasValue)
                    Selected = Orders.FirstOrDefault(o => o.Id == selectId.Value);
                else
                    Selected = Orders.FirstOrDefault();

                RefreshView();
                InvalidateCommands();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при загрузке заказов: {ex.Message}",
                    "Заказы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadTablesAsync()
        {
            try
            {
                var tables = await _tableService.GetAllAsync();
                Tables.Clear();
                foreach (var t in tables)
                    Tables.Add(t);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при загрузке столов: {ex.Message}",
                    "Столы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadWaitersAsync()
        {
            try
            {
                var staff = await _staffService.GetAllAsync();
                Waiters.Clear();
                foreach (var w in staff.Where(s => s.Role == "Waiter"))
                    Waiters.Add(w);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при загрузке официантов: {ex.Message}",
                    "Сотрудники",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static Order CloneOrder(Order source)
        {
            return new Order
            {
                Id = source.Id,
                TableId = source.TableId,
                WaiterId = source.WaiterId,
                Status = source.Status,
                CreatedAt = source.CreatedAt,
                ClosedAt = source.ClosedAt
            };
        }

        private void SyncDateInputsFromEditing()
        {
            if (EditingOrder == null)
            {
                CreatedAtInput = "";
                ClosedAtInput = "";
                return;
            }

            CreatedAtInput = EditingOrder.CreatedAt.ToString("dd.MM.yyyy HH:mm");
            ClosedAtInput = EditingOrder.ClosedAt?.ToString("dd.MM.yyyy HH:mm") ?? "";
        }

        private bool TryApplyDateInputsToEditing()
        {
            if (EditingOrder == null)
                return false;

            var culture = new CultureInfo("ru-RU");
            const string format = "dd.MM.yyyy HH:mm";

            if (string.IsNullOrWhiteSpace(CreatedAtInput) ||
                !DateTime.TryParseExact(CreatedAtInput.Trim(), format, culture,
                    DateTimeStyles.None, out var created))
            {
                MessageBox.Show(
                    "Поле \"Создан\" должно быть в формате: дд.мм.гггг чч:мм",
                    "Валидация даты",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            EditingOrder.CreatedAt = created;

            if (string.IsNullOrWhiteSpace(ClosedAtInput))
            {
                EditingOrder.ClosedAt = null;
            }
            else if (DateTime.TryParseExact(ClosedAtInput.Trim(), format, culture,
                         DateTimeStyles.None, out var closed))
            {
                EditingOrder.ClosedAt = closed;
            }
            else
            {
                MessageBox.Show(
                    "Поле \"Закрыт\" должно быть в формате: дд.мм.гггг чч:мм или пустым.",
                    "Валидация даты",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        private void Add()
        {
            EditingOrder = new Order
            {
                Status = "New",
                CreatedAt = DateTime.Now
            };

            SyncDateInputsFromEditing();
            IsEditingGrid = true;
        }

        private void BeginEdit()
        {
            if (Selected == null)
                return;

            EditingOrder = CloneOrder(Selected);
            SyncDateInputsFromEditing();
            IsEditingGrid = true;
        }

        private void CancelEdit()
        {
            IsEditingGrid = false;

            if (Selected != null)
            {
                EditingOrder = CloneOrder(Selected);
                SyncDateInputsFromEditing();
            }
            else
            {
                EditingOrder = null;
                CreatedAtInput = "";
                ClosedAtInput = "";
            }
        }

        private async Task SaveAsync()
        {
            if (EditingOrder == null)
                return;

            if (!TryApplyDateInputsToEditing())
                return;

            if (EditingOrder.TableId <= 0)
            {
                MessageBox.Show(
                    "Выберите стол для заказа.",
                    "Заказы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            if (!EditingOrder.WaiterId.HasValue || EditingOrder.WaiterId.Value <= 0)
            {
                MessageBox.Show(
                    "Выберите официанта для заказа.",
                    "Заказы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }


            try
            {
                int selectedId;

                if (EditingOrder.Id == 0)
                {
                    var created = await _ordersService.AddAsync(EditingOrder);
                    selectedId = created.Id;
                }
                else
                {
                    await _ordersService.UpdateAsync(EditingOrder);
                    selectedId = EditingOrder.Id;
                }

                EditingOrder = null;
                IsEditingGrid = false;

                await LoadOrdersAsync(selectedId);
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Заказы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при сохранении заказа: {ex.Message}",
                    "Заказы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task DeleteAsync()
        {
            if (Selected == null)
                return;

            var result = MessageBox.Show(
                $"Удалить заказ #{Selected.Id}?",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                await _ordersService.DeleteAsync(Selected.Id);
                await LoadOrdersAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при удалении заказа: {ex.Message}",
                    "Заказы",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private bool FilterOrder(object obj)
        {
            if (obj is not Order o)
                return false;

            if (string.Equals(o.Status, "Preorder", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(SelectedStatusFilterCode, "Preorder", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(SelectedStatusFilterCode) &&
                !string.Equals(o.Status, SelectedStatusFilterCode, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(DateRangeFilterText))
            {
                var parts = DateRangeFilterText.Split(':');
                var culture = new CultureInfo("ru-RU");
                DateTime? from = null;
                DateTime? to = null;

                if (parts.Length >= 1 &&
                    DateTime.TryParse(parts[0].Trim(), culture, DateTimeStyles.None, out var f))
                    from = f.Date;

                if (parts.Length >= 2 &&
                    DateTime.TryParse(parts[1].Trim(), culture, DateTimeStyles.None, out var t))
                    to = t.Date;

                var createdDate = o.CreatedAt.Date;

                if (from.HasValue && !to.HasValue)
                {
                    if (createdDate != from.Value)
                        return false;
                }
                else
                {
                    if (from.HasValue && createdDate < from.Value)
                        return false;
                    if (to.HasValue && createdDate > to.Value)
                        return false;
                }
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var text = SearchText.Trim().ToLowerInvariant();

                var idMatch = o.Id.ToString().Contains(text, StringComparison.OrdinalIgnoreCase);
                var tableMatch = (o.Table?.Name ?? "").ToLowerInvariant().Contains(text);
                var waiterMatch = (o.Waiter?.Name ?? "").ToLowerInvariant().Contains(text);

                if (!idMatch && !tableMatch && !waiterMatch)
                    return false;
            }

            return true;
        }

        private async void ShowDetails()
        {
            try
            {
                if (Selected == null)
                {
                    MessageBox.Show(
                        "Сначала выберите заказ в списке.",
                        "Детали заказа",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var orderId = Selected.Id;

                int initialGuestCount = 1;

                try
                {
                    var existingDetails = await _orderDetailsService.GetAsync(orderId);

                    if (existingDetails != null &&
                        existingDetails.Guests != null &&
                        existingDetails.Guests.Count > 0)
                    {
                        initialGuestCount = existingDetails.Guests.Count;
                    }
                }
                catch
                {
                }

                var guestCountDialog = new GuestCountDialog(initialGuestCount);
                var guestCountResult = guestCountDialog.ShowDialog();

                if (guestCountResult != true)
                    return;

                var guestCount = guestCountDialog.GuestCount;

                var tableName = Selected.Table?.Name ?? Selected.TableId.ToString();
                var status = Selected.Status ?? string.Empty;

                var detailsVm = new OrderDetailsDialogViewModel(
                    _orderDetailsService,
                    _menuService,
                    orderId,
                    tableName,
                    status,
                    guestCount);

                await detailsVm.InitializeAsync();

                var detailsDialog = new OrderDetailsDialog(detailsVm);
                var dialogResult = detailsDialog.ShowDialog();

                if (dialogResult == true)
                {
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при открытии деталей заказа:\n{ex}",
                    "Детали заказа",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private static void InvalidateCommands() => CommandManager.InvalidateRequerySuggested();

        private void OnOrderChanged(OrderChangedDto dto)
        {
            if (dto.Order == null)
                return;

            RunOnUi(() =>
            {
                var existing = Orders.FirstOrDefault(o => o.Id == dto.Order.Id);

                switch (dto.Action)
                {
                    case EntityChangeAction.Created:
                        if (existing == null)
                            Orders.Add(dto.Order);
                        else
                        {
                            var index = Orders.IndexOf(existing);
                            Orders[index] = dto.Order;
                        }
                        break;

                    case EntityChangeAction.Updated:
                        if (existing != null)
                        {
                            var index = Orders.IndexOf(existing);
                            Orders[index] = dto.Order;
                        }
                        else
                        {
                            Orders.Add(dto.Order);
                        }
                        break;

                    case EntityChangeAction.Deleted:
                        if (existing != null)
                            Orders.Remove(existing);
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
            _realtime.OrderChanged -= OnOrderChanged;
            _realtime.StaffChanged -= OnStaffChanged;
            _cts.Cancel();
            _cts.Dispose();
        }

        private void OnStaffChanged(StaffChangedDto dto)
        {
            if (dto.Staff == null)
                return;

            RunOnUi(() =>
            {
                var existing = Waiters.FirstOrDefault(w => w.Id == dto.Staff.Id);

                var isWaiter = string.Equals(dto.Staff.Role, "Waiter", StringComparison.OrdinalIgnoreCase);

                switch (dto.Action)
                {
                    case EntityChangeAction.Created:
                    case EntityChangeAction.Updated:
                        if (!isWaiter)
                        {
                            if (existing != null)
                                Waiters.Remove(existing);
                        }
                        else
                        {
                            if (existing == null)
                            {
                                Waiters.Add(dto.Staff);
                            }
                            else
                            {
                                var idx = Waiters.IndexOf(existing);
                                Waiters[idx] = dto.Staff;
                            }
                        }
                        break;

                    case EntityChangeAction.Deleted:
                        if (existing != null)
                            Waiters.Remove(existing);
                        break;
                }
            });
        }
    }
}
