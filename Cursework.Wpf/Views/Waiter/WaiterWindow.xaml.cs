using Cursework.Domains.Models;
using Cursework.Wpf.ViewModels.Waiter;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Windows;
using System.Windows.Input;

namespace Cursework.Wpf.Views.Waiter
{
    public partial class WaiterWindow : Window
    {
        private double _lastZoom = 1.0;
        public WaiterWindow()
        {
            InitializeComponent();
            DataContext = App.Services.GetRequiredService<WaiterWindowViewModel>();
        }

        public async void Init(Staff staff)
        {
            if (DataContext is not WaiterWindowViewModel vm)
                DataContext = App.Services.GetRequiredService<WaiterWindowViewModel>();

            if (DataContext is WaiterWindowViewModel viewModel)
            {
                await viewModel.InitAsync(staff);
                _lastZoom = viewModel.Map.Zoom;
            }
        }

        private void ZoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (DataContext is not WaiterWindowViewModel vm) return;
            if (MapScroll == null) return;

            AdjustZoomKeepCenter(_lastZoom, vm.Map.Zoom);
            _lastZoom = vm.Map.Zoom;
        }

        private void MapScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (DataContext is not WaiterWindowViewModel vm) return;
            if (MapScroll == null) return;

            var step = e.Delta > 0 ? 0.1 : -0.1;
            var old = vm.Map.Zoom;

            vm.Map.Zoom = Math.Max(0.2, Math.Min(4.0, old + step));
            AdjustZoomKeepCenter(old, vm.Map.Zoom);

            _lastZoom = vm.Map.Zoom;
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
