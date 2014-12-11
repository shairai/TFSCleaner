using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using SR.TFSCleaner.Helpers;
using SR.TFSCleaner.Models;

namespace SR.TFSCleaner.Controls
{
    /// <summary>
    /// Interaction logic for CustomComboBox.xaml
    /// </summary>
    public partial class CustomComboBox : UserControl
    {
        public CustomComboBox()
        {
            InitializeComponent();
            _itemsSourceOriginal = new List<NodeItem>();
        }

        #region Dependency Properties

        private List<NodeItem> _itemsSourceOriginal;
        /// <summary>
        ///Gets or sets a collection used to generate the content of the ComboBox
        /// </summary>
        public IEnumerable<NodeItem> ItemsSource
        {
            get { return (IEnumerable<NodeItem>)GetValue(ItemsSourceProperty); }
            set
            {
                SetValue(ItemsSourceProperty, value);
                SetText();
            }
        }

        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IEnumerable<NodeItem>), typeof(CustomComboBox), new UIPropertyMetadata(null));
        /// <summary>
        ///Gets or sets the text displayed in the ComboBox
        /// </summary>
        public string Text
        {
            get { return (string)GetValue(TextProperty); }
            set { SetValue(TextProperty, value); }
        }

        public static readonly DependencyProperty TextProperty = DependencyProperty.Register("Text", typeof(string), typeof(CustomComboBox), new UIPropertyMetadata(string.Empty));
        /// <summary>
        ///Gets or sets the text displayed in the ComboBox if there are no selected items
        /// </summary>
        public string DefaultText
        {
            get { return (string)GetValue(DefaultTextProperty); }
            set { SetValue(DefaultTextProperty, value); }
        }

        // Using a DependencyProperty as the backing store for DefaultText.  This enables animation, styling, binding, etc…
        public static readonly DependencyProperty DefaultTextProperty = DependencyProperty.Register("DefaultText", typeof(string), typeof(CustomComboBox), new UIPropertyMetadata(string.Empty));
        #endregion

        /// <summary>
        ///Whenever a CheckBox is checked, change the text displayed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            SetText();
        }

        private List<NodeItem> GetDifferences()
        {
            var listA = ItemsSource.ToList();
            var listB = _itemsSourceOriginal.ToList();
            List<NodeItem> listC = new List<NodeItem>();
            if (listA.Count != listB.Count) return listC;
            for (int i = 0; i < listA.Count; i++)
            {
                var itemA = listA[i];
                var itemB = listB[i];

                if(itemA.IsSelected != itemB.IsSelected)
                    listC.Add(itemA);
            }
            return listC;
        }

        void InitOriginalList()
        {
            _itemsSourceOriginal.Clear();
            foreach (var ite in ItemsSource)
                _itemsSourceOriginal.Add(new NodeItem() { IsSelected = ite.IsSelected, Title = ite.Title });
        }

        /// <summary>
        ///Set the text property of this control (bound to the ContentPresenter of the ComboBox)
        /// </summary>
        private void SetText()
        {
            if (ItemsSource == null) return;
            if (_itemsSourceOriginal.Count == 0)
            {
                InitOriginalList();
            }

            var differences = GetDifferences();

            if (differences.Any(i => i.Title.Equals("Any") || i.Title.Equals("All")  && i.IsSelected))
            {
                foreach (NodeItem item in ItemsSource.Where(item => !item.Title.Equals("Any") && !item.Title.Equals("All")))
                {
                    item.IsSelected = false;
                }
            }
            else if (differences.Any(i => !i.Title.Equals("Any") || i.Title.Equals("All") && i.IsSelected))
            {
                foreach (NodeItem item in ItemsSource.Where(item => item.Title.Equals("Any") || item.Title.Equals("All")))
                {
                    item.IsSelected = false;
                }
            }

            InitOriginalList();

            StringBuilder sb = new StringBuilder();
            foreach (NodeItem item in this.ItemsSource.Where(i => i.IsSelected))
            {
                sb.Append(item.Title);
                sb.Append(",");
            }
            this.Text = sb.ToString().TrimEnd(new char[] {','});

            if (string.IsNullOrEmpty(this.Text))
            {
                this.Text = this.DefaultText;
            }
        }

        private void CustomComboBox_OnLoaded(object sender, RoutedEventArgs e)
        {
            SetText();
        }
    }
}
