using Cursework.Wpf.ViewModels.Admin;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Cursework.Wpf.Views.Tabs
{
    public partial class CallWaiterTab : UserControl
    {
        public CallWaiterTab()
        {
            InitializeComponent();
            DataContext = App.Services.GetRequiredService<CallWaiterTabViewModel>();
        }

        private void CallWaitersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not CallWaiterTabViewModel vm)
                return;

            if (vm.Selected == null)
                return;

            var grid = (DataGrid)sender;

            // как только VM выбрал новый вызов (в т.ч. только что добавленный),
            // прокручиваем к нему и отдаём фокус гриду
            grid.Dispatcher.BeginInvoke(new Action(() =>
            {
                grid.ScrollIntoView(vm.Selected);
                grid.Focus();
            }), DispatcherPriority.Background);
        }
    }
}
