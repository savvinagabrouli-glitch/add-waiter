using System;
using System.Windows;
using Cursework.Wpf.ViewModels.Admin;

namespace Cursework.Wpf.Views.Dialogs
{
    public partial class TableQrWindow : Window
    {
        public TableQrWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is TableQrViewModel vm)
            {
                vm.RequestClose += (_, __) => this.Close();
            }
        }
    }
}
