using Cursework.Application.Interfaces;
using Cursework.Application.Models;
using Cursework.Application.Realtime;
using Cursework.Domains.Models;
using Cursework.Wpf.Services.HallLayout;
using Cursework.Wpf.Services.Realtime;
using Cursework.Wpf.ViewModels.Admin;
using Cursework.Wpf.ViewModels.Base;
using Cursework.Wpf.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WpfApplication = System.Windows.Application;

namespace Cursework.Wpf.ViewModels.Waiter
{
    public class WaiterWindowViewModel : ViewModelBase, IDisposable
    {
        private readonly ITableService _tableService;
        private readonly IOrderService _orderService;
        private readonly IOrderDetailsService _orderDetailsService;
        private readonly IMenuService _menuService;
        private readonly ICallWaiterService _callWaiterService;
        private readonly IRealtimeService _realtimeService;
        private readonly IHallLayoutStorageService _layoutStorageService;
        private readonly IStaffService _staffService;

        private List<DiningTable> _tables = new();
        private List<Order> _orders = new();
        private List<Staff> _staff = new();
        private OrderDetailsDto? _currentDetails;

        public ObservableCollection<CallWaiter> Notifications { get; } = new();
        public ObservableCollection<WaiterOrderItemViewModel> OrderItems { get; } = new();

        private HallMapViewModel _map;
        public HallMapViewModel Map
        {
            get => _map;
            private set => Set(ref _map, value);
        }

        private Staff? _currentStaff;
        public Staff? CurrentStaff
        {
            get => _currentStaff;
            private set => Set(ref _currentStaff, value);
        }

        private bool _isNotificationsOpen;
        public bool IsNotificationsOpen
        {
            get => _isNotificationsOpen;
            set => Set(ref _isNotificationsOpen, value);
        }

        public int NotificationsCount => Notifications.Count;

        private DiningTable? _selectedTable;
        public DiningTable? SelectedTable
        {
            get => _selectedTable;
            private set
            {
                if (Set(ref _selectedTable, value))
                {
                    Raise(nameof(SelectedTableName));
                    Raise(nameof(SelectedTableSeats));
                    Raise(nameof(SelectedTableStatus));
                    _ = LoadActiveOrderAsync();
                }
            }
        }

        public string SelectedTableName => SelectedTable?.Name ?? "Не выбран";
        public int SelectedTableSeats => SelectedTable?.Seats ?? 0;
        public string SelectedTableStatus => SelectedTable?.Status ?? string.Empty;

        private Order? _activeOrder;
        public Order? ActiveOrder
        {
            get => _activeOrder;
            private set
            {
                if (Set(ref _activeOrder, value))
                {
                    Raise(nameof(IsOrderActive));
                    Raise(nameof(ActiveOrderStatus));
                    Raise(nameof(ActiveOrderTotal));
                }
            }
        }

        public bool IsOrderActive => ActiveOrder != null;
        public string ActiveOrderStatus => ActiveOrder?.Status ?? string.Empty;
        public decimal ActiveOrderTotal { get; private set; }

        public ICommand ToggleNotificationsCommand { get; }
        public ICommand AcceptNotificationCommand { get; }
        public ICommand PrevZoneCommand { get; }
        public ICommand NextZoneCommand { get; }
        public ICommand CreateOrderCommand { get; }
        public ICommand ViewOrderCommand { get; }
        public ICommand CloseOrderCommand { get; }
        public ICommand SaveItemsCommand { get; }

        public WaiterWindowViewModel(
            ITableService tableService,
            IOrderService orderService,
            IOrderDetailsService orderDetailsService,
            IMenuService menuService,
            ICallWaiterService callWaiterService,
            IRealtimeService realtimeService,
            IHallLayoutStorageService layoutStorageService,
            IStaffService staffService)
        {
            _tableService = tableService;
            _orderService = orderService;
            _orderDetailsService = orderDetailsService;
            _menuService = menuService;
            _callWaiterService = callWaiterService;
            _realtimeService = realtimeService;
            _layoutStorageService = layoutStorageService;
            _staffService = staffService;

            _map = new HallMapViewModel(layoutStorageService, Enumerable.Empty<DiningTable>(), "MainHall", isAdminMode: false);

            ToggleNotificationsCommand = new RelayCommand(_ => IsNotificationsOpen = !IsNotificationsOpen);
            AcceptNotificationCommand = new RelayCommand(async p => await AcceptNotificationAsync(p as CallWaiter));
            PrevZoneCommand = new RelayCommand(_ => SwitchZone(-1));
            NextZoneCommand = new RelayCommand(_ => SwitchZone(1));
            CreateOrderCommand = new RelayCommand(async _ => await CreateOrderAsync(), _ => SelectedTable != null);
            ViewOrderCommand = new RelayCommand(async _ => await OpenOrderDialogAsync(), _ => ActiveOrder != null);
            CloseOrderCommand = new RelayCommand(async _ => await CloseOrderAsync(), _ => ActiveOrder != null && string.Equals(ActiveOrder.Status, "ReadyToPay", StringComparison.OrdinalIgnoreCase));
            SaveItemsCommand = new RelayCommand(async _ => await SaveItemsAsync(), _ => ActiveOrder != null && OrderItems.Any());

            _realtimeService.CallWaiterChanged += OnCallWaiterChanged;
            _realtimeService.OrderChanged += OnOrderChanged;
            _realtimeService.DiningTableChanged += OnDiningTableChanged;
        }

        public async Task InitAsync(Staff staff)
        {
            CurrentStaff = staff;
            await LoadBaseDataAsync();
        }

        private async Task LoadBaseDataAsync()
        {
            _tables = (await _tableService.GetAllAsync())?.ToList() ?? new List<DiningTable>();
            _orders = (await _orderService.GetAllAsync())?.ToList() ?? new List<Order>();
            _staff = (await _staffService.GetAllAsync())?.ToList() ?? new List<Staff>();

            Map = new HallMapViewModel(_layoutStorageService, _tables, Map.SelectedZone, isAdminMode: false);
            Map.SelectedTableOnMap = Map.TablesOnMap.FirstOrDefault();
            Map.PropertyChanged += Map_PropertyChanged;
            UpdateSelectedTableFromMap();

            await LoadNotificationsAsync();
        }

        private async Task LoadNotificationsAsync()
        {
            Notifications.Clear();
            var all = await _callWaiterService.GetAllAsync();
            foreach (var c in all.Where(cw => cw.IsHandled == false))
                Notifications.Add(c);
            Raise(nameof(NotificationsCount));
        }

        private void Map_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HallMapViewModel.SelectedTableOnMap))
                UpdateSelectedTableFromMap();
        }

        private void UpdateSelectedTableFromMap()
        {
            var id = Map.SelectedTableOnMap?.TableId;
            SelectedTable = id.HasValue ? _tables.FirstOrDefault(t => t.Id == id.Value) : null;
        }

        private void SwitchZone(int delta)
        {
            var codes = Map.Zones.Select(z => z.Code).ToList();
            var idx = codes.IndexOf(Map.SelectedZone);
            if (idx < 0) idx = 0;
            idx = (idx + delta + codes.Count) % codes.Count;
            Map.SelectedZone = codes[idx];
        }

        private async Task AcceptNotificationAsync(CallWaiter? call)
        {
            if (call == null) return;
            try
            {
                call.IsHandled = true;
                call.HandledAt = DateTime.Now;
                await _callWaiterService.UpdateAsync(call);
                Notifications.Remove(call);
                Raise(nameof(NotificationsCount));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось обновить вызов официанта: {ex.Message}");
            }
        }

        private async Task LoadActiveOrderAsync()
        {
            ActiveOrder = null;
            OrderItems.Clear();
            ActiveOrderTotal = 0m;

            if (SelectedTable == null)
                return;

            var tableId = SelectedTable.Id;
            ActiveOrder = _orders.FirstOrDefault(o => o.TableId == tableId && !string.Equals(o.Status, "Closed", StringComparison.OrdinalIgnoreCase) && !string.Equals(o.Status, "Cancelled", StringComparison.OrdinalIgnoreCase));

            if (ActiveOrder != null)
                await LoadOrderDetailsAsync(ActiveOrder);

            Raise(nameof(ActiveOrderTotal));
        }

        private async Task LoadOrderDetailsAsync(Order order)
        {
            try
            {
                OrderItems.Clear();
                _currentDetails = await _orderDetailsService.GetAsync(order.Id);
                if (_currentDetails == null) return;

                foreach (var g in _currentDetails.Guests.OrderBy(g => g.Index))
                {
                    foreach (var item in g.Items)
                    {
                        var vm = new WaiterOrderItemViewModel
                        {
                            ItemId = item.ItemId,
                            DishName = item.DishName,
                            Quantity = item.Quantity,
                            UnitPrice = item.UnitPrice,
                            Price = item.Price,
                            Status = item.Status,
                            GuestId = g.GuestId,
                            GuestIndex = g.Index
                        };
                        vm.StatusChanged += OnItemStatusChanged;
                        OrderItems.Add(vm);
                    }
                }

                ActiveOrderTotal = OrderItems.Sum(i => i.Price);
                Raise(nameof(ActiveOrderTotal));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить детали заказа: {ex.Message}");
            }
        }

        private Task SaveItemsAsync()
        {
            return SaveOrderDetailsInternalAsync();
        }

        private Task OnItemStatusChanged(WaiterOrderItemViewModel item)
        {
            return SaveOrderDetailsInternalAsync();
        }

        private async Task SaveOrderDetailsInternalAsync()
        {
            if (ActiveOrder == null || _currentDetails == null)
                return;

            try
            {
                foreach (var g in _currentDetails.Guests)
                {
                    foreach (var i in g.Items)
                    {
                        var vm = OrderItems.FirstOrDefault(x => x.ItemId == i.ItemId);
                        if (vm != null)
                            i.Status = vm.Status;
                    }
                }

                var saved = await _orderDetailsService.SaveAsync(_currentDetails);
                _currentDetails = saved;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить детали заказа: {ex.Message}");
            }
        }

        private async Task CreateOrderAsync()
        {
            if (SelectedTable == null || CurrentStaff == null)
                return;

            var dialog = new GuestCountDialog(1)
            {
                Owner = WpfApplication.Current.MainWindow
            };

            if (dialog.ShowDialog() != true)
                return;

            try
            {
                var order = new Order
                {
                    TableId = SelectedTable.Id,
                    WaiterId = CurrentStaff.Id,
                    Status = "New",
                    CreatedAt = DateTime.Now
                };

                var created = await _orderService.AddAsync(order);
                if (created != null)
                {
                    _orders.Add(created);
                    ActiveOrder = created;
                    await OpenOrderDialogAsync(created, dialog.GuestCount);
                    created.Status = "Pending";
                    await _orderService.UpdateAsync(created);
                    await LoadOrderDetailsAsync(created);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось создать заказ: {ex.Message}");
            }
        }

        private async Task OpenOrderDialogAsync(Order? order = null, int? guests = null)
        {
            var targetOrder = order ?? ActiveOrder;
            if (targetOrder == null)
                return;

            var guestCount = guests ?? 1;
            if (_currentDetails != null && _currentDetails.Guests.Any())
                guestCount = _currentDetails.Guests.Count;

            var vm = new WaiterOrderDetailsDialogViewModel(
                _orderDetailsService,
                _menuService,
                targetOrder.Id,
                SelectedTableName,
                targetOrder.Status,
                guestCount);

            await vm.InitializeAsync();

            var dialog = new WaiterOrderDetailsDialog(vm)
            {
                Owner = WpfApplication.Current.MainWindow
            };

            vm.RequestClose += (_, result) => dialog.DialogResult = result;
            dialog.ShowDialog();

            await LoadOrderDetailsAsync(targetOrder);
        }

        private async Task CloseOrderAsync()
        {
            if (ActiveOrder == null)
                return;

            if (MessageBox.Show("Закрыть заказ?", "Заказ", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            try
            {
                var pdf = await _orderDetailsService.GetReceiptPdfAsync(ActiveOrder.Id);
                var temp = System.IO.Path.GetTempFileName().Replace(".tmp", ".pdf");
                await System.IO.File.WriteAllBytesAsync(temp, pdf);
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(temp) { UseShellExecute = true });

                ActiveOrder.Status = "Closed";
                await _orderService.UpdateAsync(ActiveOrder);
                _orders = (await _orderService.GetAllAsync())?.ToList() ?? new List<Order>();
                await LoadActiveOrderAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось закрыть заказ: {ex.Message}");
            }
        }

        private void OnCallWaiterChanged(CallWaiterChangedDto dto)
        {
            if (dto?.CallWaiter == null)
                return;

            RunOnUi(() =>
            {
                var existing = Notifications.FirstOrDefault(c => c.Id == dto.CallWaiter.Id);
                switch (dto.Action)
                {
                    case EntityChangeAction.Created:
                    case EntityChangeAction.Updated:
                        if (!dto.CallWaiter.IsHandled)
                        {
                            if (existing == null)
                                Notifications.Add(dto.CallWaiter);
                            else
                            {
                                var idx = Notifications.IndexOf(existing);
                                Notifications[idx] = dto.CallWaiter;
                            }
                        }
                        else if (existing != null)
                        {
                            Notifications.Remove(existing);
                        }
                        break;
                    case EntityChangeAction.Deleted:
                        if (existing != null)
                            Notifications.Remove(existing);
                        break;
                }
                Raise(nameof(NotificationsCount));
            });
        }

        private void OnOrderChanged(OrderChangedDto dto)
        {
            if (dto?.Order == null)
                return;

            RunOnUi(async () =>
            {
                var existing = _orders.FirstOrDefault(o => o.Id == dto.Order.Id);
                switch (dto.Action)
                {
                    case EntityChangeAction.Created:
                        if (existing == null)
                            _orders.Add(dto.Order);
                        break;
                    case EntityChangeAction.Updated:
                        if (existing != null)
                        {
                            _orders.Remove(existing);
                        }
                        _orders.Add(dto.Order);
                        break;
                    case EntityChangeAction.Deleted:
                        if (existing != null)
                            _orders.Remove(existing);
                        break;
                }
                await LoadActiveOrderAsync();
            });
        }

        private void OnDiningTableChanged(DiningTableChangedDto dto)
        {
            if (dto?.Table == null)
                return;

            RunOnUi(async () =>
            {
                var existing = _tables.FirstOrDefault(t => t.Id == dto.Table.Id);
                if (dto.Action == EntityChangeAction.Deleted)
                {
                    if (existing != null)
                        _tables.Remove(existing);
                }
                else
                {
                    if (existing != null)
                        _tables.Remove(existing);
                    _tables.Add(dto.Table);
                }
                await LoadActiveOrderAsync();
            });
        }

        private void RunOnUi(Action action)
        {
            var dispatcher = WpfApplication.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }

        public void Dispose()
        {
            _realtimeService.CallWaiterChanged -= OnCallWaiterChanged;
            _realtimeService.OrderChanged -= OnOrderChanged;
            _realtimeService.DiningTableChanged -= OnDiningTableChanged;
        }
    }

    public class WaiterOrderItemViewModel : ViewModelBase
    {
        private int _itemId;
        private string _dishName = string.Empty;
        private int _quantity;
        private decimal _unitPrice;
        private decimal _price;
        private string _status = "Ordered";
        private int _guestId;
        private int _guestIndex;

        public int ItemId
        {
            get => _itemId;
            set => Set(ref _itemId, value);
        }

        public string DishName
        {
            get => _dishName;
            set => Set(ref _dishName, value);
        }

        public int Quantity
        {
            get => _quantity;
            set => Set(ref _quantity, value);
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set => Set(ref _unitPrice, value);
        }

        public decimal Price
        {
            get => _price;
            set => Set(ref _price, value);
        }

        public string Status
        {
            get => _status;
            set
            {
                if (Set(ref _status, value))
                {
                    _ = StatusChanged?.Invoke(this);
                }
            }
        }

        public int GuestId
        {
            get => _guestId;
            set => Set(ref _guestId, value);
        }

        public int GuestIndex
        {
            get => _guestIndex;
            set => Set(ref _guestIndex, value);
        }

        public event Func<WaiterOrderItemViewModel, Task>? StatusChanged;
    }
}
