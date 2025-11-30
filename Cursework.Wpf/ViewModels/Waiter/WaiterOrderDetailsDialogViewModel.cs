using Cursework.Application.Interfaces;
using Cursework.Application.Models;
using Cursework.Domains.Models;
using Cursework.Wpf.ViewModels.Admin;
using Cursework.Wpf.ViewModels.Base;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Cursework.Wpf.ViewModels.Waiter
{
    public class WaiterOrderDetailsDialogViewModel : ViewModelBase
    {
        private readonly IOrderDetailsService _detailsService;
        private readonly IMenuService _menuService;
        private readonly int _initialGuestCount;

        private bool _isBusy;

        private int _orderId;
        private string _tableName = string.Empty;
        private string _orderStatus = string.Empty;

        public int OrderId
        {
            get => _orderId;
            set => Set(ref _orderId, value);
        }

        public string TableName
        {
            get => _tableName;
            set => Set(ref _tableName, value);
        }

        public string OrderStatus
        {
            get => _orderStatus;
            set => Set(ref _orderStatus, value);
        }

        public ObservableCollection<OrderGuestViewModel> Guests { get; } = new();
        public ObservableCollection<Dish> Dishes { get; } = new();

        private decimal _totalAmount;
        public decimal TotalAmount
        {
            get => _totalAmount;
            set => Set(ref _totalAmount, value);
        }

        public ICommand AddDishCommand { get; }
        public ICommand RemoveDishCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public event EventHandler<bool>? RequestClose;

        public WaiterOrderDetailsDialogViewModel(
            IOrderDetailsService detailsService,
            IMenuService menuService,
            int orderId,
            string tableName,
            string orderStatus,
            int guestCount)
        {
            _detailsService = detailsService;
            _menuService = menuService;
            OrderId = orderId;
            TableName = tableName;
            OrderStatus = orderStatus;
            _initialGuestCount = guestCount < 1 ? 1 : guestCount;

            AddDishCommand = new RelayCommand(AddDishExecute, _ => !_isBusy);
            RemoveDishCommand = new RelayCommand(RemoveDishExecute, _ => !_isBusy);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !_isBusy);
            CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(this, false), _ => !_isBusy);
        }

        public async Task InitializeAsync()
        {
            _isBusy = true;
            try
            {
                var dishes = await _menuService.GetDishesAsync();
                foreach (var d in dishes.OrderBy(d => d.Name))
                    Dishes.Add(d);

                var existing = await _detailsService.GetAsync(OrderId);

                if (existing != null && existing.Guests.Any())
                {
                    BuildFromDto(existing);
                }
                else
                {
                    BuildEmpty(_initialGuestCount);
                }

                RecalculateTotal();
            }
            finally
            {
                _isBusy = false;
            }
        }

        private void BuildEmpty(int guestCount)
        {
            Guests.Clear();

            for (var i = 1; i <= guestCount; i++)
            {
                var guestVm = new OrderGuestViewModel
                {
                    GuestId = 0,
                    Index = i
                };

                Guests.Add(guestVm);
            }
        }

        private void BuildFromDto(OrderDetailsDto dto)
        {
            Guests.Clear();

            var orderedGuests = dto.Guests.OrderBy(g => g.Index).ToList();
            foreach (var g in orderedGuests)
            {
                var guestVm = new OrderGuestViewModel
                {
                    GuestId = g.GuestId,
                    Index = g.Index
                };

                foreach (var itemDto in g.Items)
                {
                    var dish = Dishes.FirstOrDefault(d => d.Id == itemDto.DishId);
                    var itemVm = new OrderItemViewModel
                    {
                        ItemId = itemDto.ItemId,
                        GuestId = g.GuestId,
                        GuestIndex = g.Index,
                        DishId = itemDto.DishId,
                        SelectedDish = dish,
                        Quantity = itemDto.Quantity <= 0 ? 1 : itemDto.Quantity,
                        UnitPrice = dish?.Price ?? itemDto.UnitPrice,
                        Price = itemDto.Price,
                        Notes = itemDto.Notes,
                        Status = string.IsNullOrWhiteSpace(itemDto.Status) ? "Ordered" : itemDto.Status
                    };

                    AttachItemHandlers(itemVm);
                    guestVm.Items.Add(itemVm);
                }

                Guests.Add(guestVm);
            }
        }

        private void AttachItemHandlers(OrderItemViewModel item)
        {
            item.PropertyChanged += Item_PropertyChanged;
            item.UpdatePriceFromDish();
        }

        private void Item_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (sender is not OrderItemViewModel item)
                return;

            if (e.PropertyName == nameof(OrderItemViewModel.Quantity) ||
                e.PropertyName == nameof(OrderItemViewModel.SelectedDish) ||
                e.PropertyName == nameof(OrderItemViewModel.UnitPrice))
            {
                item.UpdatePriceFromDish();
                RecalculateTotal();
            }
        }

        private void AddDishExecute(object? parameter)
        {
            var guest = Guests.FirstOrDefault();
            if (guest == null)
            {
                guest = new OrderGuestViewModel { GuestId = 0, Index = 1 };
                Guests.Add(guest);
            }

            var item = new OrderItemViewModel
            {
                GuestId = guest.GuestId,
                GuestIndex = guest.Index,
                Quantity = 1,
                Status = "Ordered"
            };

            AttachItemHandlers(item);
            guest.Items.Add(item);
        }

        private void RemoveDishExecute(object? parameter)
        {
            if (parameter is not OrderItemViewModel item)
                return;

            var ownerGuest = Guests.FirstOrDefault(g => g.Items.Contains(item));
            if (ownerGuest == null)
                return;

            ownerGuest.Items.Remove(item);
            RecalculateTotal();
        }

        private void RecalculateTotal()
        {
            TotalAmount = Guests
                .SelectMany(g => g.Items)
                .Where(i => i.Quantity > 0 && i.SelectedDish != null)
                .Sum(i => i.Price);
        }

        private bool CanSave()
        {
            return Guests.SelectMany(g => g.Items)
                         .Any(i => i.SelectedDish != null && i.Quantity > 0);
        }

        private async Task SaveAsync()
        {
            if (!CanSave())
            {
                MessageBox.Show(
                    "Не указано ни одной позиции блюда.",
                    "Детали заказа",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            try
            {
                _isBusy = true;

                var dto = BuildDto();
                var saved = await _detailsService.SaveAsync(dto);

                BuildFromDto(saved);
                RecalculateTotal();

                RequestClose?.Invoke(this, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Ошибка при сохранении деталей заказа: {ex.Message}",
                    "Детали заказа",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }

        private OrderDetailsDto BuildDto()
        {
            var dto = new OrderDetailsDto
            {
                OrderId = OrderId,
                TableId = 0,
                TableName = TableName,
                OrderStatus = OrderStatus
            };

            foreach (var g in Guests.OrderBy(g => g.Index))
            {
                var gDto = new OrderGuestDto
                {
                    GuestId = g.GuestId,
                    Index = g.Index
                };

                foreach (var item in g.Items)
                {
                    if (item.SelectedDish == null || item.Quantity <= 0)
                        continue;

                    var itemDto = new OrderItemDto
                    {
                        ItemId = item.ItemId,
                        OrderId = OrderId,
                        GuestId = g.GuestId,
                        GuestIndex = g.Index,
                        DishId = item.DishId,
                        DishName = item.SelectedDish?.Name ?? string.Empty,
                        UnitPrice = item.UnitPrice > 0m ? item.UnitPrice : item.SelectedDish?.Price ?? 0m,
                        Quantity = item.Quantity,
                        Price = item.Price,
                        Notes = item.Notes,
                        Status = item.Status
                    };

                    gDto.Items.Add(itemDto);
                }

                if (gDto.Items.Count > 0)
                    dto.Guests.Add(gDto);
            }

            dto.TotalAmount = dto.Guests.SelectMany(g => g.Items).Sum(i => i.Price);
            dto.Payment = new OrderPaymentDto
            {
                PaymentId = 0,
                Method = "Cash",
                Amount = dto.TotalAmount,
                PaidAt = DateTime.Now
            };

            return dto;
        }
    }
}
