using Pulsar.Server.Models;
using Pulsar.Server.Networking;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;

#nullable enable

namespace Pulsar.Server.Controls.Wpf
{
    public partial class ClientsListView : UserControl
    {
        private readonly ObservableCollection<ClientListEntry> _entries = new();
        private readonly CollectionViewSource _collectionViewSource;
        private bool _groupByCountry;
        private Predicate<object>? _filter;
        private bool _suppressSelectionNotifications;
    private bool _isDragSelecting;
    private ClientListEntry? _dragAnchorEntry;
    private Point _dragStartPoint;
    private SelectionAdorner? _selectionAdorner;

        public ClientsListView()
        {
            InitializeComponent();

            _collectionViewSource = new CollectionViewSource { Source = _entries };
            _collectionViewSource.Filter += OnCollectionFilter;
            ClientsView = _collectionViewSource.View;
            ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.Country), ListSortDirection.Ascending));
            ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.IsFavorite), ListSortDirection.Descending));
            ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.Nickname), ListSortDirection.Ascending));

            ToggleFavoriteCommand = new RelayCommand<ClientListEntry>(OnToggleFavorite);
            DataContext = this;

            ApplyTheme(Settings.DarkMode);

            ClientsGrid.PreviewMouseLeftButtonDown += ClientsGrid_OnPreviewMouseLeftButtonDown;
            ClientsGrid.PreviewMouseMove += ClientsGrid_OnPreviewMouseMove;
            ClientsGrid.PreviewMouseLeftButtonUp += ClientsGrid_OnPreviewMouseLeftButtonUp;
            ClientsGrid.MouseLeave += ClientsGrid_OnMouseLeave;
        }

        public ICollectionView ClientsView { get; }

        public ICommand ToggleFavoriteCommand { get; }

        public event EventHandler<IReadOnlyList<ClientListEntry>>? SelectionChanged;
        public event EventHandler<ClientListEntry>? ItemDoubleClicked;
        public event EventHandler<ClientListEntry>? FavoriteToggled;

        public IReadOnlyList<ClientListEntry> SelectedEntries => ClientsGrid.SelectedItems.Cast<ClientListEntry>().ToList();

        public ClientListEntry? GetEntryByClient(Client client)
        {
            if (client == null)
            {
                return null;
            }

            return Dispatcher.Invoke(() => _entries.FirstOrDefault(e => ReferenceEquals(e.Client, client)));
        }

        public ClientListEntry AddOrUpdate(Client client, Action<ClientListEntry> updater)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (updater == null)
            {
                throw new ArgumentNullException(nameof(updater));
            }

            return Dispatcher.Invoke(() =>
            {
                var entry = _entries.FirstOrDefault(e => ReferenceEquals(e.Client, client));
                if (entry == null)
                {
                    entry = new ClientListEntry(client);
                    _entries.Add(entry);
                }

                updater(entry);
                return entry;
            });
        }

        public void Remove(Client client)
        {
            if (client == null)
            {
                return;
            }

            Dispatcher.Invoke(() =>
            {
                var target = _entries.FirstOrDefault(e => ReferenceEquals(e.Client, client));
                if (target != null)
                {
                    _entries.Remove(target);
                }
            });
        }

        public void Clear()
        {
            Dispatcher.Invoke(() => _entries.Clear());
        }

        public void ApplyFilter(Predicate<ClientListEntry>? filter)
        {
            _filter = filter != null ? new Predicate<object>(o => filter((ClientListEntry)o)) : null;
            Dispatcher.Invoke(() => ClientsView.Refresh());
        }

        public void SetGroupByCountry(bool enabled)
        {
            _groupByCountry = enabled;
            Dispatcher.Invoke(UpdateGrouping);
        }

        public void SetSelectedClients(IEnumerable<Client> clients)
        {
            if (clients == null)
            {
                return;
            }

            var target = new HashSet<Client>(clients);

            Dispatcher.Invoke(() =>
            {
                _suppressSelectionNotifications = true;
                try
                {
                    ClientsGrid.SelectedItems.Clear();
                    foreach (var entry in _entries)
                    {
                        if (target.Contains(entry.Client))
                        {
                            ClientsGrid.SelectedItems.Add(entry);
                        }
                    }
                }
                finally
                {
                    _suppressSelectionNotifications = false;
                }
            });
        }

        public void RefreshSort()
        {
            Dispatcher.Invoke(() =>
            {
                using (ClientsView.DeferRefresh())
                {
                    ClientsView.SortDescriptions.Clear();
                    ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.Country), ListSortDirection.Ascending));
                    ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.IsFavorite), ListSortDirection.Descending));
                    ClientsView.SortDescriptions.Add(new SortDescription(nameof(ClientListEntry.Nickname), ListSortDirection.Ascending));
                }
            });
        }

        public void RefreshItem(ClientListEntry entry)
        {
            Dispatcher.Invoke(() =>
            {
                entry.UpdateStatusBrush();
                ClientsView.Refresh();
            });
        }

        public void ApplyTheme(bool isDarkMode)
        {
            Dispatcher.Invoke(() =>
            {
                Resources["RowBackgroundBrush"] = CreateBrush(isDarkMode ? "#1E1E1E" : "#FFFFFF");
                Resources["RowAlternateBackgroundBrush"] = CreateBrush(isDarkMode ? "#232323" : "#F7F7F7");
                Resources["RowHoverBrush"] = CreateBrush(isDarkMode ? "#2E2E2E" : "#ECECEC");
                Resources["RowSelectedBrush"] = CreateBrush(isDarkMode ? "#162B4C" : "#D8E6FF");
                Resources["RowSelectedInactiveBrush"] = CreateBrush(isDarkMode ? "#11213C" : "#E5EFFE");
                Resources["RowForegroundBrush"] = CreateBrush(isDarkMode ? "#FFFFFF" : "#1A1A1A");
                Resources["RowSelectedForegroundBrush"] = CreateBrush(isDarkMode ? "#67B0FF" : "#0F3B8C");
                Resources["HeaderBackgroundBrush"] = CreateBrush(isDarkMode ? "#2A2A2A" : "#FFFFFF");
                Resources["HeaderForegroundBrush"] = CreateBrush(isDarkMode ? "#FFFFFF" : "#1A1A1A");
                Resources["GridBackgroundBrush"] = CreateBrush(isDarkMode ? "#141414" : "#FFFFFF");
                Resources["ScrollBarTrackBrush"] = CreateBrush(isDarkMode ? "#1E1E1E" : "#E5E5E5");
                Resources["ScrollBarThumbBrush"] = CreateBrush(isDarkMode ? "#444444" : "#B5B5B5");
                Resources["ScrollBarThumbHoverBrush"] = CreateBrush(isDarkMode ? "#5A5A5A" : "#9E9E9E");
                Resources["ScrollBarThumbPressedBrush"] = CreateBrush(isDarkMode ? "#737373" : "#7C7C7C");

                ClientsGrid.Background = (Brush)Resources["GridBackgroundBrush"];
                ClientsGrid.RowBackground = (Brush)Resources["RowBackgroundBrush"];
                ClientsGrid.AlternatingRowBackground = (Brush)Resources["RowAlternateBackgroundBrush"];
                ClientsGrid.Foreground = (Brush)Resources["RowForegroundBrush"];
            });
        }

        private void UpdateGrouping()
        {
            using (ClientsView.DeferRefresh())
            {
                ClientsView.GroupDescriptions.Clear();
                if (_groupByCountry)
                {
                    ClientsView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(ClientListEntry.Country)));
                }
            }
        }

        private void OnCollectionFilter(object sender, FilterEventArgs e)
        {
            if (_filter == null)
            {
                e.Accepted = true;
                return;
            }

            e.Accepted = _filter(e.Item);
        }

        private void OnToggleFavorite(ClientListEntry? entry)
        {
            if (entry == null)
            {
                return;
            }

            System.Diagnostics.Debug.WriteLine($"[ClientsListView] Toggling favorite for {entry.Nickname} ({entry.Client?.Value?.UserAtPc ?? "unknown"})");
            System.Diagnostics.Debug.WriteLine($"[ClientsListView] Before toggle: IsFavorite={entry.IsFavorite}");

            RefreshSort();
            FavoriteToggled?.Invoke(this, entry);

            System.Diagnostics.Debug.WriteLine($"[ClientsListView] After toggle: IsFavorite={entry.IsFavorite}");
        }

        private void ClientsGrid_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressSelectionNotifications)
            {
                return;
            }

            var selection = SelectedEntries;
            SelectionChanged?.Invoke(this, selection);
        }

        private void ClientsGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (ClientsGrid.SelectedItem is ClientListEntry entry)
            {
                ItemDoubleClicked?.Invoke(this, entry);
            }
        }

        private void ClientsGrid_OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not DependencyObject source)
            {
                return;
            }

            if (FindVisualParent<DataGridColumnHeader>(source) != null || FindVisualParent<ScrollBar>(source) != null)
            {
                return;
            }

            if (FindVisualParent<ButtonBase>(source) != null)
            {
                return;
            }

            _dragStartPoint = e.GetPosition(ClientsGrid);
            ClientsGrid.Focus();

            if (Keyboard.Modifiers != ModifierKeys.None)
            {
                return;
            }

            var row = FindVisualParent<DataGridRow>(source);
            _dragAnchorEntry = row?.Item as ClientListEntry;

            BeginDragSelection();

            if (_dragAnchorEntry != null)
            {
                SelectEntries(new[] { _dragAnchorEntry });
            }
            else
            {
                ClearSelectionInternal(false);
            }

            e.Handled = true;
        }

        private void ClientsGrid_OnPreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragSelecting || e.LeftButton != MouseButtonState.Pressed || Keyboard.Modifiers != ModifierKeys.None)
            {
                return;
            }

            var point = e.GetPosition(ClientsGrid);
            UpdateDragSelection(point);
            e.Handled = true;
        }

        private void ClientsGrid_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDragSelecting)
            {
                EndDragSelection();
                e.Handled = true;
            }
        }

        private void ClientsGrid_OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed)
            {
                EndDragSelection();
            }
        }

        private void DataGridRow_OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row)
            {
                if (!ClientsGrid.SelectedItems.Contains(row.Item))
                {
                    ClientsGrid.SelectedItem = row.Item;
                }

                ClientsGrid.Focus();
            }
        }

        private static SolidColorBrush CreateBrush(string hex)
        {
            var color = (Color)ColorConverter.ConvertFromString(hex)!;
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        public void ClearSelection()
        {
            ClearSelectionInternal(true);
        }

        private void EndDragSelection()
        {
            _isDragSelecting = false;
            _dragAnchorEntry = null;

            if (ClientsGrid.IsMouseCaptured)
            {
                ClientsGrid.ReleaseMouseCapture();
            }

            if (_selectionAdorner != null)
            {
                var layer = AdornerLayer.GetAdornerLayer(ClientsGrid);
                layer?.Remove(_selectionAdorner);
                _selectionAdorner = null;
            }
        }

        private DataGridRow? GetRowFromPoint(Point point)
        {
            var element = ClientsGrid.InputHitTest(point) as DependencyObject;
            return FindVisualParent<DataGridRow>(element);
        }

        private static T? FindVisualParent<T>(DependencyObject? current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T target)
                {
                    return target;
                }

                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        private void BeginDragSelection()
        {
            _isDragSelecting = true;
            ClientsGrid.Focus();
            ClientsGrid.CaptureMouse();

            var layer = AdornerLayer.GetAdornerLayer(ClientsGrid);
            if (layer != null)
            {
                _selectionAdorner = new SelectionAdorner(ClientsGrid, _dragStartPoint);
                layer.Add(_selectionAdorner);
            }
        }

        private void UpdateDragSelection(Point currentPoint)
        {
            _selectionAdorner?.Update(currentPoint);

            var rect = new Rect(_dragStartPoint, currentPoint);
            var selected = new List<ClientListEntry>();

            if (_dragAnchorEntry != null)
            {
                selected.Add(_dragAnchorEntry);
            }

            var itemCount = ClientsGrid.Items.Count;
            for (var i = 0; i < itemCount; i++)
            {
                if (ClientsGrid.Items[i] is not ClientListEntry entry)
                {
                    continue;
                }

                if (ReferenceEquals(entry, _dragAnchorEntry))
                {
                    continue;
                }

                if (ClientsGrid.ItemContainerGenerator.ContainerFromIndex(i) is not DataGridRow row)
                {
                    continue;
                }

                var bounds = VisualTreeHelper.GetDescendantBounds(row);
                var topLeft = row.TransformToAncestor(ClientsGrid).Transform(new Point(bounds.X, bounds.Y));
                var rowRect = new Rect(topLeft, bounds.Size);

                if (rowRect.IntersectsWith(rect))
                {
                    selected.Add(entry);
                }
            }

            SelectEntries(selected);
        }

        private void SelectEntries(IReadOnlyList<ClientListEntry> entries)
        {
            _suppressSelectionNotifications = true;
            try
            {
                ClientsGrid.SelectedItems.Clear();
                foreach (var entry in entries)
                {
                    ClientsGrid.SelectedItems.Add(entry);
                }
            }
            finally
            {
                _suppressSelectionNotifications = false;
            }

            SelectionChanged?.Invoke(this, entries);
        }

        private void ClearSelectionInternal(bool raiseEvent)
        {
            if (ClientsGrid.SelectedItems.Count == 0)
            {
                if (raiseEvent)
                {
                    SelectionChanged?.Invoke(this, Array.Empty<ClientListEntry>());
                }
                return;
            }

            _suppressSelectionNotifications = true;
            try
            {
                ClientsGrid.SelectedItems.Clear();
            }
            finally
            {
                _suppressSelectionNotifications = false;
            }

            if (raiseEvent)
            {
                SelectionChanged?.Invoke(this, Array.Empty<ClientListEntry>());
            }
        }

        private sealed class SelectionAdorner : Adorner
        {
            private static readonly Brush FillBrush;
            private static readonly Pen BorderPen;

            private Point _start;
            private Point _end;

            static SelectionAdorner()
            {
                FillBrush = new SolidColorBrush(Color.FromArgb(40, 51, 153, 255));
                FillBrush.Freeze();
                BorderPen = new Pen(new SolidColorBrush(Color.FromArgb(200, 51, 153, 255)), 1)
                {
                    DashStyle = DashStyles.Dash
                };
                BorderPen.Brush.Freeze();
                BorderPen.Freeze();
            }

            public SelectionAdorner(UIElement adornedElement, Point start)
                : base(adornedElement)
            {
                IsHitTestVisible = false;
                _start = start;
                _end = start;
            }

            public void Update(Point current)
            {
                _end = current;
                InvalidateVisual();
            }

            protected override void OnRender(DrawingContext drawingContext)
            {
                var rect = new Rect(_start, _end);
                drawingContext.DrawRectangle(FillBrush, BorderPen, rect);
            }
        }
    }
}
