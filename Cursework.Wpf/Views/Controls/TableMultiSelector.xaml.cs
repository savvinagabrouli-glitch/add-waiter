using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Cursework.Domains.Models;

namespace Cursework.Wpf.Views.Controls
{
    public partial class TableMultiSelector : UserControl
    {
        public TableMultiSelector()
        {
            InitializeComponent();

            // Когда окно теряет фокус — закрываем попап, чтобы он не висел над другими программами
            Loaded += (_, _) =>
            {
                var window = Window.GetWindow(this);
                if (window != null)
                {
                    window.Deactivated += (_, _) => Popup.IsOpen = false;
                }
            };
        }

        // ===== Dependency Properties =====

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(
                nameof(ItemsSource),
                typeof(IEnumerable<DiningTable>),
                typeof(TableMultiSelector),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable<DiningTable>? ItemsSource
        {
            get => (IEnumerable<DiningTable>?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public static readonly DependencyProperty SelectedTableIdProperty =
            DependencyProperty.Register(
                nameof(SelectedTableId),
                typeof(int?),
                typeof(TableMultiSelector),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnSelectedTableIdChanged));

        public int? SelectedTableId
        {
            get => (int?)GetValue(SelectedTableIdProperty);
            set => SetValue(SelectedTableIdProperty, value);
        }

        private INotifyCollectionChanged? _collectionSubscription;

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TableMultiSelector control)
                return;

            // Отписываемся от старой коллекции
            if (e.OldValue is INotifyCollectionChanged oldCollection)
            {
                oldCollection.CollectionChanged -= control.ItemsSource_CollectionChanged;
            }

            // Подписываемся на новую
            if (e.NewValue is INotifyCollectionChanged newCollection)
            {
                newCollection.CollectionChanged += control.ItemsSource_CollectionChanged;
            }

            control.RefreshZones();
            control.UpdateButtonText();
        }

        private void ItemsSource_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshZones();
            UpdateButtonText();
        }

        private static void OnSelectedTableIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TableMultiSelector control)
                return;

            control.UpdateSelectedFromId();
            control.UpdateButtonText();
        }

        // ===== Логика =====

        private void RefreshZones()
        {
            ZonesList.ItemsSource = null;
            TablesList.ItemsSource = null;

            if (ItemsSource == null)
                return;

            var zones = ItemsSource
                .Select(t => t.Zone)
                .Where(z => !string.IsNullOrWhiteSpace(z))
                .Distinct()
                .OrderBy(z => z)
                .ToList();

            ZonesList.ItemsSource = zones;
        }

        private void UpdateSelectedFromId()
        {
            if (ItemsSource == null || SelectedTableId == null)
            {
                ZonesList.SelectedItem = null;
                TablesList.ItemsSource = null;
                return;
            }

            var table = ItemsSource.FirstOrDefault(t => t.Id == SelectedTableId.Value);
            if (table == null)
                return;

            ZonesList.SelectedItem = table.Zone;

            var tablesInZone = ItemsSource
                .Where(t => t.Zone == table.Zone)
                .OrderBy(t => t.Name)
                .ToList();

            TablesList.ItemsSource = tablesInZone;
            TablesList.SelectedItem = tablesInZone.FirstOrDefault(t => t.Id == table.Id);
        }

        private void UpdateButtonText()
        {
            if (ItemsSource == null || SelectedTableId == null)
            {
                ButtonText.Text = "Выберите стол";
                return;
            }

            var table = ItemsSource.FirstOrDefault(t => t.Id == SelectedTableId.Value);
            if (table == null)
            {
                ButtonText.Text = "Выберите стол";
            }
            else
            {
                // Показываем только название стола
                ButtonText.Text = table.Name;
            }
        }

        private void MainButton_Click(object sender, RoutedEventArgs e)
        {
            Popup.IsOpen = !Popup.IsOpen;
        }

        private void ZonesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ItemsSource == null)
                return;

            var zone = ZonesList.SelectedItem as string;
            if (zone == null)
            {
                TablesList.ItemsSource = null;
                return;
            }

            var tables = ItemsSource
                .Where(t => t.Zone == zone)
                .OrderBy(t => t.Name)
                .ToList();

            TablesList.ItemsSource = tables;
        }

        private void TablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TablesList.SelectedItem is DiningTable table)
            {
                SelectedTableId = table.Id;
                UpdateButtonText();
            }
        }

        private void TablesList_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (TablesList.SelectedItem is DiningTable)
            {
                Popup.IsOpen = false;
            }
        }
    }
}
