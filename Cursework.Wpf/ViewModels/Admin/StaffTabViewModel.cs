using Cursework.Application.Interfaces;
using Cursework.Application.Realtime;
using Cursework.Application.Security;
using Cursework.Application.Services;
using Cursework.Domains.Models;
using Cursework.Wpf.Services.Realtime;
using Cursework.Wpf.ViewModels.Base;
using Cursework.Wpf.Views.Dialogs;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace Cursework.Wpf.ViewModels.Admin
{
    public class StaffTabViewModel : ViewModelBase
    {
        private readonly IStaffService _staffService;
        private readonly IPasswordHasher _hasher;
        private readonly IRealtimeService _realtime;

        public ObservableCollection<Staff> StaffList { get; } = new();
        private readonly CancellationTokenSource _cts = new();
        public ICollectionView StaffView { get; } 

        private Staff? _selected;
        public Staff? Selected
        {
            get => _selected;
            set
            {
                if (Set(ref _selected, value))
                {
                    EditItem = value is null ? null : Clone(value);

                    Raise(nameof(CanEdit));
                    Raise(nameof(CanDelete));
                    Raise(nameof(CanSave));
                    Raise(nameof(CanCancel));

                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private Staff? _editItem;
        public Staff? EditItem
        {
            get => _editItem;
            set
            {
                if (Set(ref _editItem, value))
                {
                    Raise(nameof(CanSave));
                    CommandManager.InvalidateRequerySuggested();
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
                    Raise(nameof(CanEdit));
                    Raise(nameof(CanDelete));
                    Raise(nameof(CanSave));
                    Raise(nameof(CanCancel));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        private string _filterRole = "Все";
        public string FilterRole
        {
            get => _filterRole;
            set { if (Set(ref _filterRole, value)) ApplyFilter(); }
        }

        private string _searchText = "";
        public string SearchText
        {
            get => _searchText;
            set { if (Set(ref _searchText, value)) ApplyFilter(); }
        }

        private int _totalCount;
        public int TotalCount { get => _totalCount; private set => Set(ref _totalCount, value); }

        private int _filteredCount;
        public int FilteredCount { get => _filteredCount; private set => Set(ref _filteredCount, value); }

        private int _maxId;
        public int MaxId
        {
            get => _maxId;
            private set => Set(ref _maxId, value);
        }

        public bool CanEdit => Selected != null && !IsEditingGrid;
        public bool CanDelete => Selected != null && !IsEditingGrid;
        public bool CanSave => IsEditingGrid && EditItem != null;
        public bool CanCancel => IsEditingGrid;

        public RelayCommand AddCommand { get; }
        public RelayCommand EditCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand SaveCommand { get; }
        public RelayCommand CancelCommand { get; }
        public RelayCommand ResetPasswordCommand { get; }

        public StaffTabViewModel(IStaffService staffService, IPasswordHasher hasher, IRealtimeService realtime)
        {
            _staffService = staffService;
            _hasher = hasher;
            _realtime = realtime;
            _realtime.StaffChanged += OnStaffChanged;

            StaffView = CollectionViewSource.GetDefaultView(StaffList);
            StaffView.Filter = StaffFilter;

            AddCommand = new RelayCommand(_ => Add());
            EditCommand = new RelayCommand(_ => BeginEdit(), _ => CanEdit);
            DeleteCommand = new RelayCommand(async _ => await DeleteAsync(), _ => CanDelete);
            SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => CanSave);
            CancelCommand = new RelayCommand(_ => Cancel(), _ => CanCancel);
            ResetPasswordCommand = new RelayCommand(
                async _ => await ResetPasswordAsync(),
                _ => Selected != null && Selected.Id != 0
            );

            _ = LoadAsync();
        }

        private async Task LoadAsync()
        {
            StaffList.Clear();
            var data = await _staffService.GetAllAsync();
            foreach (var s in data) StaffList.Add(s);

            TotalCount = StaffList.Count;
            MaxId = StaffList.Count == 0 ? 0 : StaffList.Max(s => s.Id);

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            StaffView.Refresh();
            FilteredCount = StaffList.Where(s => StaffFilter(s)).Count();
        }

        private bool StaffFilter(object? obj)
        {
            if (obj is not Staff s) return false;

            if (!string.IsNullOrWhiteSpace(FilterRole) && FilterRole != "Все")
            {
                var targetRole = FilterRole switch
                {
                    "Администратор" => "Admin",
                    "Официант" => "Waiter",
                    _ => FilterRole
                };

                if (!string.Equals(s.Role, targetRole, StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var q = SearchText.Trim();
                if (int.TryParse(q, out var idVal))
                {
                    if (s.Id == idVal) return true;
                }

                if (!(ContainsCI(s.Name, q) || ContainsCI(s.Login, q)))
                    return false;
            }

            return true;
        }


        private static bool ContainsCI(string? text, string q)
            => (text ?? "").IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0;

        private void Add()
        {
            if (StaffList.Any(s => s.Id == 0))
            {
                MessageBox.Show(
                    "Сначала сохраните или отмените создание текущего нового сотрудника.",
                    "Сотрудники",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var fresh = new Staff { Name = "", Login = "", Role = "Waiter", PasswordHash = "" };
            StaffList.Add(fresh);
            Selected = fresh;
            IsEditingGrid = true;
        }

        private void BeginEdit()
        {
            if (Selected == null)
                return;

            IsEditingGrid = true;
        }


        private async Task DeleteAsync()
        {
            if (Selected == null) return;

            var namePart = string.IsNullOrWhiteSpace(Selected.Name)
                ? "этого сотрудника"
                : $"сотрудника «{Selected.Name}»";

            var result = MessageBox.Show(
                $"Вы действительно хотите удалить {namePart}?",
                "Удаление сотрудника",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            var id = Selected.Id;

            try
            {
                if (id != 0)
                    await _staffService.DeleteAsync(id);

                await LoadAsync();

                Selected = null;
                EditItem = null;
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Сотрудники",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось удалить сотрудника.\n\n{ex.Message}",
                    "Ошибка удаления",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task SaveAsync()
        {
            if (Selected == null || EditItem == null) return;

            try
            {
                Selected.Name = EditItem.Name;
                Selected.Login = EditItem.Login;
                Selected.Role = EditItem.Role;

                var isNew = Selected.Id == 0;

                if (isNew)
                    await _staffService.AddAsync(Selected);
                else
                    await _staffService.UpdateAsync(Selected);

                IsEditingGrid = false;
                EditItem = null;

                if (isNew)
                {
                    var passwordSet = await ResetPasswordForAsync(Selected);
                    if (passwordSet)
                    {
                        MessageBox.Show("Сотрудник успешно добавлен.", "Сотрудники", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }

                await LoadAsync();
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show(ex.Message,
                    "Сотрудники",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении сотрудника: {ex.Message}",
                    "Сотрудники",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void Cancel()
        {
            if (Selected != null && Selected.Id == 0)
            {
                StaffList.Remove(Selected);
                Selected = null;
                EditItem = null;
            }
            else if (Selected != null)
            {
                EditItem = Clone(Selected);
            }

            IsEditingGrid = false;
        }

        private static Staff Clone(Staff s) => new()
        {
            Id = s.Id,
            Name = s.Name,
            Login = s.Login,
            Role = s.Role,
            PasswordHash = s.PasswordHash
        };

        private async Task ResetPasswordAsync()
        {
            if (Selected == null) return;

            var changed = await ResetPasswordForAsync(Selected);
            if (changed)
            {
                MessageBox.Show("Пароль обновлён.",
                    "Сотрудники", MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadAsync();
            }
        }

        private async Task<bool> ResetPasswordForAsync(Staff staff)
        {
            var dlg = new ResetPasswordDialog();
            if (dlg.ShowDialog() == true)
            {
                var hash = _hasher.Hash(dlg.Password!);
                staff.PasswordHash = hash;
                await _staffService.UpdateAsync(staff);
                return true;
            }

            return false;
        }

        private void OnStaffChanged(StaffChangedDto dto)
        {
            if (dto.Staff == null)
                return;

            RunOnUi(() =>
            {
                var existing = StaffList.FirstOrDefault(s => s.Id == dto.Staff.Id);

                switch (dto.Action)
                {
                    case EntityChangeAction.Created:
                        if (existing == null)
                            StaffList.Add(dto.Staff);
                        else
                        {
                            var idx = StaffList.IndexOf(existing);
                            StaffList[idx] = dto.Staff;
                        }
                        break;

                    case EntityChangeAction.Updated:
                        if (existing != null)
                        {
                            var idx = StaffList.IndexOf(existing);
                            StaffList[idx] = dto.Staff;
                        }
                        else
                        {
                            StaffList.Add(dto.Staff);
                        }
                        break;

                    case EntityChangeAction.Deleted:
                        if (existing != null)
                            StaffList.Remove(existing);
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
            _realtime.StaffChanged -= OnStaffChanged;
            _cts.Cancel();
            _cts.Dispose();
        }
    }
}
