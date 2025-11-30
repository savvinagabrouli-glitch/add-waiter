using System;
using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Cursework.Wpf.Views.Controls
{
    public partial class HallMapZoneControl : UserControl
    {
        public HallMapZoneControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty BackgroundSourceProperty =
            DependencyProperty.Register(nameof(BackgroundSource), typeof(object), typeof(HallMapZoneControl), new PropertyMetadata(null));

        public object? BackgroundSource
        {
            get => GetValue(BackgroundSourceProperty);
            set => SetValue(BackgroundSourceProperty, value);
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(HallMapZoneControl), new PropertyMetadata(null));

        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(HallMapZoneControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty ZoomProperty =
            DependencyProperty.Register(nameof(Zoom), typeof(double), typeof(HallMapZoneControl), new PropertyMetadata(1.0));

        public double Zoom
        {
            get => (double)GetValue(ZoomProperty);
            set => SetValue(ZoomProperty, value);
        }

        public static readonly DependencyProperty IsDragEnabledProperty =
            DependencyProperty.Register(nameof(IsDragEnabled), typeof(bool), typeof(HallMapZoneControl), new PropertyMetadata(true));

        public bool IsDragEnabled
        {
            get => (bool)GetValue(IsDragEnabledProperty);
            set => SetValue(IsDragEnabledProperty, value);
        }

        public static readonly DependencyProperty IsContextMenuEnabledProperty =
            DependencyProperty.Register(nameof(IsContextMenuEnabled), typeof(bool), typeof(HallMapZoneControl), new PropertyMetadata(true));

        public bool IsContextMenuEnabled
        {
            get => (bool)GetValue(IsContextMenuEnabledProperty);
            set => SetValue(IsContextMenuEnabledProperty, value);
        }

        private void TableThumb_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Thumb t)
                SelectedItem = t.DataContext;
            // НЕ e.Handled=true — иначе Thumb может не начать drag.
        }

        private void TableThumb_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Thumb t)
                SelectedItem = t.DataContext;
            // НЕ e.Handled=true — иначе ContextMenu может не открыться.
        }

        private void TableThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            if (!IsDragEnabled) return;
            if (sender is not Thumb thumb) return;

            var item = thumb.DataContext;
            if (item == null) return;

            var z = Zoom <= 0 ? 1.0 : Zoom;

            if (TryGetDouble(item, "X", out var x) && TryGetDouble(item, "Y", out var y))
            {
                TrySetDouble(item, "X", x + (e.HorizontalChange / z));
                TrySetDouble(item, "Y", y + (e.VerticalChange / z));
            }
        }

        private static bool TryGetDouble(object obj, string propName, out double value)
        {
            value = 0;
            var p = obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
            if (p == null || !p.CanRead) return false;

            var raw = p.GetValue(obj);
            if (raw == null) return false;

            try
            {
                value = Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                return true;
            }
            catch { return false; }
        }

        private static bool TrySetDouble(object obj, string propName, double value)
        {
            var p = obj.GetType().GetProperty(propName, BindingFlags.Instance | BindingFlags.Public);
            if (p == null || !p.CanWrite) return false;

            try
            {
                if (p.PropertyType == typeof(double))
                {
                    p.SetValue(obj, value);
                    return true;
                }

                var converted = Convert.ChangeType(value, p.PropertyType, CultureInfo.InvariantCulture);
                p.SetValue(obj, converted);
                return true;
            }
            catch { return false; }
        }

        private void SetModelSingle_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is object item)
            {
                var prop = item.GetType().GetProperty("Model");
                if (prop != null && prop.CanWrite)
                    prop.SetValue(item, "Single");
            }
        }

        private void SetModelMulti_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.DataContext is object item)
            {
                var prop = item.GetType().GetProperty("Model");
                if (prop != null && prop.CanWrite)
                    prop.SetValue(item, "Multi");
            }
        }
    }
}
