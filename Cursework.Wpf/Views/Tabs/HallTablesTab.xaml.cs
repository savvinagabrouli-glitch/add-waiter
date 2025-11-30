using System.Windows;
using System.Windows.Controls;
using Cursework.Wpf.ViewModels.Admin;

namespace Cursework.Wpf.Views.Tabs
{
    public partial class HallTablesTab : UserControl
    {
        public HallTablesTab()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is HallTablesTabViewModel oldVm)
                oldVm.ScrollToSelectedRequested -= OnScrollToSelectedRequested;

            if (e.NewValue is HallTablesTabViewModel newVm)
                newVm.ScrollToSelectedRequested += OnScrollToSelectedRequested;
        }

        private void OnScrollToSelectedRequested()
        {
            if (TileList == null) return;

            if (DataContext is HallTablesTabViewModel vm && vm.Selected != null)
                TileList.ScrollIntoView(vm.Selected);
        }
    }
}
