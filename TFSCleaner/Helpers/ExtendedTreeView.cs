using System.Windows;
using System.Windows.Controls;

namespace SR.TFSCleaner.Helpers
{
    class ExtendedTreeView : TreeView
    {
        public ExtendedTreeView()
            : base()
        {
            this.SelectedItemChanged += new RoutedPropertyChangedEventHandler<object>(TreeSelectedItemChanged);
        }

        void TreeSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (SelectedItem != null)
            {
                SetValue(SelectedTreeItemProperty, SelectedItem);
            }
        }

        public object SelectedTreeItem
        {
            get { return (object)GetValue(SelectedTreeItemProperty); }
            set { SetValue(SelectedTreeItemProperty, value); }
        }
        public static readonly DependencyProperty SelectedTreeItemProperty = DependencyProperty.Register("SelectedTreeItem", typeof(object),
            typeof(ExtendedTreeView), new UIPropertyMetadata(null));
    }
}
