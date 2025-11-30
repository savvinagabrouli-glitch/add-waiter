using Cursework.Wpf.ViewModels.Admin;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Cursework.Wpf.Views.Tabs
{
    public partial class StaffTab : UserControl
    {
        public StaffTab()
        {
            InitializeComponent();
            DataContext = App.Services.GetRequiredService<StaffTabViewModel>();
        }

        private void StaffGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is not StaffTabViewModel vm) return;
            if (vm.Selected == null) return;

            var grid = (DataGrid)sender;

            // как только VM выбрал нового сотрудника (в т.ч. fresh при Add),
            // прокручиваем к нему и отдаём фокус гриду
            grid.Dispatcher.BeginInvoke(new Action(() =>
            {
                grid.ScrollIntoView(vm.Selected);
                grid.Focus();              // теперь стрелки/Enter работают в таблице
            }), DispatcherPriority.Background);
        }
    }
}
