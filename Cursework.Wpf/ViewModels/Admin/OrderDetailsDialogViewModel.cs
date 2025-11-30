using Cursework.Application.Interfaces;
using Cursework.Application.Models;
using Cursework.Domains.Models;
using Cursework.Wpf.ViewModels.Base;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Cursework.Wpf.ViewModels.Admin
{
    public class OrderDetailsDialogViewModel : ViewModelBase
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

        public IReadOnlyList<string> StatusCodes { get; } =
            new[] { "Ordered", "Preparing", "Served", "Cancelled" };

        public IReadOnlyList<string> PaymentMethods { get; } =
            new[] { "Cash", "Card", "QR", "Other" };

        private PaymentViewModel _payment = new();
        public PaymentViewModel Payment
        {
            get => _payment;
            private set => Set(ref _payment, value);
        }

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
        public ICommand ShowReceiptCommand { get; }

        public event EventHandler<bool>? RequestClose;

        public OrderDetailsDialogViewModel(
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
            ShowReceiptCommand = new RelayCommand(
                                 async _ => await ShowReceiptAsync(),
                                 _ => Guests.Any());
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

            Payment = new PaymentViewModel
            {
                PaymentId = 0,
                Method = "Cash",
                Amount = 0m,
                PaidAt = DateTime.Now
            };
        }
        private void BuildFromDto(OrderDetailsDto dto)
        {
            Guests.Clear();

            var indexOrderedGuests = dto.Guests
                .OrderBy(g => g.Index)
                .ToList();

            foreach (var g in indexOrderedGuests)
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
                        Status = string.IsNullOrWhiteSpace(itemDto.Status)
                            ? "Ordered"
                            : itemDto.Status
                    };

                    AttachItemHandlers(itemVm);
                    guestVm.Items.Add(itemVm);
                }

                Guests.Add(guestVm);
            }

            if (dto.Payment != null)
            {
                Payment = new PaymentViewModel
                {
                    PaymentId = dto.Payment.PaymentId,
                    Method = string.IsNullOrWhiteSpace(dto.Payment.Method)
                        ? "Cash"
                        : dto.Payment.Method,
                    Amount = dto.Payment.Amount,
                    PaidAt = dto.Payment.PaidAt
                };
            }
            else
            {
                Payment = new PaymentViewModel
                {
                    PaymentId = 0,
                    Method = "Cash",
                    Amount = dto.TotalAmount,
                    PaidAt = DateTime.Now
                };
            }
        }

        private void AttachItemHandlers(OrderItemViewModel item)
        {
            item.PropertyChanged += Item_PropertyChanged;
            item.UpdatePriceFromDish();
        }

        private void Item_PropertyChanged(object? sender, PropertyChangedEventArgs e)
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
            if (parameter is not OrderGuestViewModel guest)
                return;

            var item = new OrderItemViewModel
            {
                GuestId = guest.GuestId,
                GuestIndex = guest.Index,
                Quantity = 1,
                Status = "Ordered",
                UnitPrice = 0m,
                Price = 0m
            };

            AttachItemHandlers(item);
            guest.Items.Add(item);
            RecalculateTotal();
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

            Payment.Amount = TotalAmount;
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
                        UnitPrice = item.UnitPrice > 0m
                            ? item.UnitPrice
                            : item.SelectedDish?.Price ?? 0m,
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

            dto.TotalAmount = dto.Guests
                .SelectMany(g => g.Items)
                .Sum(i => i.Price);

            dto.Payment = new OrderPaymentDto
            {
                PaymentId = Payment.PaymentId,
                Method = Payment.Method,
                Amount = dto.TotalAmount,
                PaidAt = Payment.PaidAt == default ? DateTime.Now : Payment.PaidAt
            };

            return dto;
        }

        private async Task ShowReceiptAsync()
        {
            try
            {
                _isBusy = true;

                var pdfBytes = await _detailsService.GetReceiptPdfAsync(OrderId);

                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "CurseworkReceipts");

                Directory.CreateDirectory(folder);

                var fileName = $"receipt_order_{OrderId}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
                var fullPath = Path.Combine(folder, fileName);

                await File.WriteAllBytesAsync(fullPath, pdfBytes);

                var psi = new ProcessStartInfo(fullPath)
                {
                    UseShellExecute = true
                };

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось сформировать чек:\n{ex.Message}",
                    "Чек",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                _isBusy = false;
            }
        }
    }

    public class OrderGuestViewModel : ViewModelBase
    {
        private int _guestId;
        private int _index;

        public int GuestId
        {
            get => _guestId;
            set => Set(ref _guestId, value);
        }

        public int Index
        {
            get => _index;
            set
            {
                if (Set(ref _index, value))
                    Raise(nameof(Header));
            }
        }

        public string Header => $"Гость {Index}";

        public ObservableCollection<OrderItemViewModel> Items { get; } = new();
    }

    public class OrderItemViewModel : ViewModelBase
    {
        private int _itemId;
        private int _guestId;
        private int _guestIndex;
        private int _dishId;
        private Dish? _selectedDish;
        private int _quantity = 1;
        private decimal _unitPrice;
        private decimal _price;
        private string? _notes;
        private string _status = "Ordered";

        public int ItemId
        {
            get => _itemId;
            set => Set(ref _itemId, value);
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

        public int DishId
        {
            get => _dishId;
            set => Set(ref _dishId, value);
        }

        public Dish? SelectedDish
        {
            get => _selectedDish;
            set
            {
                if (Set(ref _selectedDish, value))
                {
                    DishId = value?.Id ?? 0;
                    UnitPrice = value?.Price ?? 0m;
                    UpdatePriceFromDish();
                }
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (value < 1) value = 1;
                if (Set(ref _quantity, value))
                    UpdatePriceFromDish();
            }
        }

        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (Set(ref _unitPrice, value))
                    UpdatePriceFromDish();
            }
        }

        public decimal Price
        {
            get => _price;
            set => Set(ref _price, value);
        }

        public string? Notes
        {
            get => _notes;
            set => Set(ref _notes, value);
        }

        public string Status
        {
            get => _status;
            set => Set(ref _status, value);
        }

        public void UpdatePriceFromDish()
        {
            var basePrice = UnitPrice > 0m
                ? UnitPrice
                : SelectedDish?.Price ?? 0m;

            Price = basePrice * Quantity;
        }
    }

    public class PaymentViewModel : ViewModelBase
    {
        private int _paymentId;
        private string _method = "Cash";
        private decimal _amount;
        private DateTime _paidAt = DateTime.Now;

        public int PaymentId
        {
            get => _paymentId;
            set => Set(ref _paymentId, value);
        }

        public string Method
        {
            get => _method;
            set => Set(ref _method, value);
        }

        public decimal Amount
        {
            get => _amount;
            set => Set(ref _amount, value);
        }

        public DateTime PaidAt
        {
            get => _paidAt;
            set => Set(ref _paidAt, value);
        }

        public string PaidAtText
        {
            get => PaidAt.ToString("dd.MM.yyyy HH:mm");
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                if (DateTime.TryParseExact(
                        value,
                        "dd.MM.yyyy HH:mm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var dt))
                {
                    PaidAt = dt;
                }
            }
        }
    }
}
