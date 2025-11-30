using Cursework.Wpf.ViewModels.Waiter;
using System.Windows;

namespace Cursework.Wpf.Views.Dialogs
{
    public partial class WaiterOrderDetailsDialog : Window
    {
        public WaiterOrderDetailsDialog(WaiterOrderDetailsDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            viewModel.RequestClose += (_, result) =>
            {
                DialogResult = result;
                Close();
            };
        }
    }
}
