using Pulsar.Common.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Pulsar.Server.Controls.Wpf
{
    public partial class ProcessTreeView : UserControl
    {
        private readonly ProcessTreeViewModel _viewModel = new ProcessTreeViewModel();
        private ScrollViewer _scrollViewer;

        public ProcessTreeView()
        {
            InitializeComponent();
            DataContext = _viewModel;
            Loaded += OnLoaded;
        }

        public event EventHandler<SortRequestedEventArgs> SortRequested;

        public event EventHandler SelectedProcessChanged;

        public Process SelectedProcess => (Tree.SelectedItem as ProcessTreeNode)?.Model;

        public IReadOnlyList<Process> SelectedProcesses
        {
            get
            {
                var selected = SelectedProcess;
                return selected != null ? new[] { selected } : Array.Empty<Process>();
            }
        }

        public void UpdateProcesses(IEnumerable<Process> processes, ProcessTreeSortColumn sortColumn, bool ascending, int? ratPid)
        {
            _viewModel.Apply(processes, sortColumn, ascending, ratPid);
            _viewModel.ExpandAll();
        }

        public void ExpandRoots()
        {
            foreach (var node in _viewModel.RootNodes)
            {
                node.IsExpanded = true;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_scrollViewer == null)
            {
                _scrollViewer = FindDescendant<ScrollViewer>(Tree);
            }
        }

        private void OnTreeSelected(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SelectedProcessChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnNameHeaderClick(object sender, MouseButtonEventArgs e)
        {
            SortRequested?.Invoke(this, new SortRequestedEventArgs(ProcessTreeSortColumn.Name));
        }

        private void OnPidHeaderClick(object sender, MouseButtonEventArgs e)
        {
            SortRequested?.Invoke(this, new SortRequestedEventArgs(ProcessTreeSortColumn.Pid));
        }

        private void OnTitleHeaderClick(object sender, MouseButtonEventArgs e)
        {
            SortRequested?.Invoke(this, new SortRequestedEventArgs(ProcessTreeSortColumn.WindowTitle));
        }

        private void OnTreePreviewRightMouse(object sender, MouseButtonEventArgs e)
        {
            if (FindAncestor<TreeViewItem>((DependencyObject)e.OriginalSource) is TreeViewItem tvi)
            {
                tvi.IsSelected = true;
                tvi.Focus();
            }
        }

        private void OnTreePreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (_scrollViewer == null)
            {
                _scrollViewer = FindDescendant<ScrollViewer>(Tree);
            }

            if (_scrollViewer != null && e.Delta != 0)
            {
                var offset = _scrollViewer.VerticalOffset - e.Delta / 3.0;
                _scrollViewer.ScrollToVerticalOffset(Math.Max(0, offset));
                e.Handled = true;
            }
        }

        private static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private static T FindDescendant<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0, count = VisualTreeHelper.GetChildrenCount(parent); i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T match)
                {
                    return match;
                }

                var descendant = FindDescendant<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }
    }

    public enum ProcessTreeSortColumn
    {
        Name = 0,
        Pid = 1,
        WindowTitle = 2
    }

    public sealed class SortRequestedEventArgs : EventArgs
    {
        public SortRequestedEventArgs(ProcessTreeSortColumn column)
        {
            Column = column;
        }

        public ProcessTreeSortColumn Column { get; }
    }

    internal sealed class ProcessTreeViewModel : INotifyPropertyChanged
    {
        public ObservableCollection<ProcessTreeNode> RootNodes { get; } = new ObservableCollection<ProcessTreeNode>();

        private ProcessTreeSortColumn _sortColumn = ProcessTreeSortColumn.Name;
        private bool _sortAscending = true;

        public string HeaderGlyphName => BuildGlyph(ProcessTreeSortColumn.Name);
        public string HeaderGlyphPid => BuildGlyph(ProcessTreeSortColumn.Pid);
        public string HeaderGlyphTitle => BuildGlyph(ProcessTreeSortColumn.WindowTitle);

        public event PropertyChangedEventHandler PropertyChanged;

        public void Apply(IEnumerable<Process> processes, ProcessTreeSortColumn sortColumn, bool ascending, int? ratPid)
        {
            _sortColumn = sortColumn;
            _sortAscending = ascending;
            OnPropertyChanged(nameof(HeaderGlyphName));
            OnPropertyChanged(nameof(HeaderGlyphPid));
            OnPropertyChanged(nameof(HeaderGlyphTitle));

            RootNodes.Clear();

            var items = processes?.ToArray() ?? Array.Empty<Process>();
            if (items.Length == 0)
            {
                return;
            }

            var processById = items.ToDictionary(p => p.Id, p => p);
            var children = new Dictionary<int, List<Process>>();
            var roots = new List<Process>();

            foreach (var process in items)
            {
                if (process.ParentId.HasValue && process.ParentId.Value > 0 && process.ParentId.Value != process.Id && processById.ContainsKey(process.ParentId.Value))
                {
                    if (!children.TryGetValue(process.ParentId.Value, out var list))
                    {
                        list = new List<Process>();
                        children.Add(process.ParentId.Value, list);
                    }
                    list.Add(process);
                }
                else
                {
                    roots.Add(process);
                }
            }

            var comparer = new ProcessComparer(sortColumn, ascending);
            roots.Sort(comparer);

            var visited = new HashSet<int>();

            foreach (var root in roots)
            {
                AddNodeRecursive(root, null);
            }

            foreach (var process in items)
            {
                if (!visited.Contains(process.Id))
                {
                    AddNodeRecursive(process, null);
                }
            }

            void AddNodeRecursive(Process process, ProcessTreeNode parent)
            {
                if (!visited.Add(process.Id))
                {
                    return;
                }

                var node = new ProcessTreeNode(process, ratPid, parent == null);

                if (parent == null)
                {
                    RootNodes.Add(node);
                }
                else
                {
                    parent.Children.Add(node);
                }

                if (children.TryGetValue(process.Id, out var childList))
                {
                    childList.Sort(comparer);
                    foreach (var child in childList)
                    {
                        AddNodeRecursive(child, node);
                    }
                }
            }
        }

        public void ExpandAll()
        {
            foreach (var node in RootNodes)
            {
                node.SetExpandedRecursive(true);
            }
        }

        private string BuildGlyph(ProcessTreeSortColumn forColumn)
        {
            if (_sortColumn != forColumn)
            {
                return string.Empty;
            }
            return _sortAscending ? "▲" : "▼";
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    internal sealed class ProcessTreeNode : INotifyPropertyChanged
    {
        public ProcessTreeNode(Process model, int? ratPid, bool expandByDefault)
        {
            Model = model;
            Children = new ObservableCollection<ProcessTreeNode>();
            _isExpanded = expandByDefault;
            IsRatProcess = ratPid.HasValue && model.Id == ratPid.Value;
            _foreground = IsRatProcess ? new SolidColorBrush(Color.FromRgb(140, 255, 140)) : (Brush)new SolidColorBrush(Color.FromRgb(230, 230, 230));
        }

        public Process Model { get; }

        public ObservableCollection<ProcessTreeNode> Children { get; }

        public bool IsRatProcess { get; }

        public string Name => string.IsNullOrWhiteSpace(Model.Name) ? "(unknown)" : Model.Name;

        public string PidDisplay => Model.Id.ToString();

        public string WindowTitle => string.IsNullOrWhiteSpace(Model.MainWindowTitle) ? string.Empty : Model.MainWindowTitle;

        private bool _isExpanded;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    OnPropertyChanged();
                }
            }
        }

        private readonly Brush _foreground;
        public Brush Foreground => _foreground;

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetExpandedRecursive(bool isExpanded)
        {
            IsExpanded = isExpanded;
            foreach (var child in Children)
            {
                child.SetExpandedRecursive(isExpanded);
            }
        }
    }

    internal sealed class ProcessComparer : IComparer<Process>
    {
        private readonly ProcessTreeSortColumn _column;
        private readonly bool _ascending;

        public ProcessComparer(ProcessTreeSortColumn column, bool ascending)
        {
            _column = column;
            _ascending = ascending;
        }

        public int Compare(Process x, Process y)
        {
            if (ReferenceEquals(x, y))
                return 0;
            if (x is null)
                return _ascending ? -1 : 1;
            if (y is null)
                return _ascending ? 1 : -1;

            int result;

            switch (_column)
            {
                case ProcessTreeSortColumn.Pid:
                    result = x.Id.CompareTo(y.Id);
                    break;
                case ProcessTreeSortColumn.WindowTitle:
                    result = string.Compare(x.MainWindowTitle ?? string.Empty, y.MainWindowTitle ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
                    break;
                default:
                    result = string.Compare(x.Name ?? string.Empty, y.Name ?? string.Empty, StringComparison.CurrentCultureIgnoreCase);
                    break;
            }

            if (result == 0)
            {
                result = x.Id.CompareTo(y.Id);
            }

            return _ascending ? result : -result;
        }
    }
}
