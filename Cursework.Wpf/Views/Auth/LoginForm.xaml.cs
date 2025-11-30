using Cursework.Application.Interfaces;
using Cursework.Application.Security;
using Cursework.Domains.Models;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Cursework.Wpf.Views.Auth
{
    public partial class LoginForm : Window
    {
        private readonly IStaffService _staffService;
        private readonly IPasswordHasher _hasher;

        public Staff? SelectedStaff { get; private set; }

        public LoginForm(IStaffService staffService, IPasswordHasher hasher)
        {
            InitializeComponent();
            _staffService = staffService;
            _hasher = hasher;
        }

        // Фоллбек-конструктор (если вдруг вызывается без DI/StartupUri в XAML)
        public LoginForm() : this(
            App.Services.GetRequiredService<IStaffService>(),
            App.Services.GetRequiredService<IPasswordHasher>())
        {
        }

        public string Login => txtLogin.Text.Trim();
        public string Password => pwdPassword.Password;


        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(Login))
            {
                MessageBox.Show(this, "Введите логин.", "Аутентификация", MessageBoxButton.OK, MessageBoxImage.Information);
                txtLogin.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                MessageBox.Show(this, "Введите пароль.", "Аутентификация", MessageBoxButton.OK, MessageBoxImage.Information);
                pwdPassword.Focus();
                return;
            }

            btnLogin.IsEnabled = false;
            try
            {
                var ok = await SignInAsync(Login, Password);
                if (!ok) return;

                Close();
            }
            finally
            {
                btnLogin.IsEnabled = true;
            }
        }

        private async Task<bool> SignInAsync(string login, string plainPassword)
        {
            try
            {
                var staffList = await _staffService.GetAllAsync();
                var staff = staffList.FirstOrDefault(s => s.Login == login);

                if (staff == null)
                {
                    MessageBox.Show(this, "Неверный логин.", "Аутентификация", MessageBoxButton.OK, MessageBoxImage.Information);
                    txtLogin.Focus();
                    return false;
                }

                if (!_hasher.Verify(plainPassword, staff.PasswordHash))
                {
                    MessageBox.Show(this, "Неверный пароль.", "Аутентификация", MessageBoxButton.OK, MessageBoxImage.Information);
                    pwdPassword.Focus();
                    return false;
                }

                SelectedStaff = staff;
                DialogResult = true;
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Ошибка подключения к базе:\n{ex.Message}",
                "Аутентификация", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
