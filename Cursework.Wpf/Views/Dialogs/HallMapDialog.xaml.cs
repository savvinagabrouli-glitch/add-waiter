using Cursework.Wpf.ViewModels.Admin;
using System;
using System.Windows;
using System.Windows.Input;

namespace Cursework.Wpf.Views.Dialogs
{
    public partial class HallMapDialog : Window
    {
        private double _lastZoom = 1.0;

        public HallMapDialog(HallMapViewModel viewModel)
        {
            InitializeComponent();
            Owner = System.Windows.Application.Current.MainWindow;

            DataContext = viewModel;

            if (viewModel.Zoom <= 0) viewModel.Zoom = 1.0;
            _lastZoom = viewModel.Zoom;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DataContext is not HallMapViewModel vm) return;
            if (MapScroll == null) return;

            AdjustZoomKeepCenter(_lastZoom, vm.Zoom);
            _lastZoom = vm.Zoom;
        }

        private void MapScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is not HallMapViewModel vm) return;
            if (MapScroll == null) return;

            var step = e.Delta > 0 ? 0.1 : -0.1;
            var old = vm.Zoom;

            vm.Zoom = Math.Max(0.2, Math.Min(4.0, old + step));
            AdjustZoomKeepCenter(old, vm.Zoom);

            _lastZoom = vm.Zoom;
            e.Handled = true;
        }

        private void AdjustZoomKeepCenter(double oldZoom, double newZoom)
        {
            if (MapScroll == null) return;
            if (oldZoom <= 0 || newZoom <= 0) return;

            var centerX = (MapScroll.HorizontalOffset + MapScroll.ViewportWidth / 2) / oldZoom;
            var centerY = (MapScroll.VerticalOffset + MapScroll.ViewportHeight / 2) / oldZoom;

            var newOffsetX = centerX * newZoom - MapScroll.ViewportWidth / 2;
            var newOffsetY = centerY * newZoom - MapScroll.ViewportHeight / 2;

            MapScroll.ScrollToHorizontalOffset(Math.Max(0, newOffsetX));
            MapScroll.ScrollToVerticalOffset(Math.Max(0, newOffsetY));
        }
    }
}
