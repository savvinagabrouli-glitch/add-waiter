using System;
using System.Windows;
using Cursework.Wpf.ViewModels.Admin;

namespace Cursework.Wpf.Views.Dialogs
{
    public partial class OrderDetailsDialog : Window
    {
        public OrderDetailsDialog(OrderDetailsDialogViewModel viewModel)
        {
            InitializeComponent();
            Owner = System.Windows.Application.Current.MainWindow;

            DataContext = viewModel;

            viewModel.RequestClose += ViewModel_RequestClose;
        }

        private void ViewModel_RequestClose(object? sender, bool e)
        {
            DialogResult = e;
            Close();
        }
    }
}
