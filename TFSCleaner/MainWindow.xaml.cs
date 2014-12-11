using System.Diagnostics;
using System.Windows;
using SR.TFSCleaner.Helpers;
using SR.TFSCleaner.Views;

namespace SR.TFSCleaner
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Connect();
        }

        void Connect()
        {
            if (TfsShared.Instance.Connect())
            {
                WorkspaceAndShelves.DataContext = new WorkspaceShelvesView();
                SourceControl.DataContext = new SourceControlView();
                TestAttachmentCleaner.DataContext = new TestAttachmentCleanup();
                BuildDetails.DataContext = new BuildsView();

                tfsConnection.Content = string.Format("{0}\\{1}", TfsShared.Instance.Tfs.Name , TfsShared.Instance.ProjectInfo.Name);

                return;
            }

            if (
                MessageBox.Show(
                    "TFS Cleaner requires TFS and Team Project before starting.\nWould you like to try again?",
                    "Missing TFS & Team Project", MessageBoxButton.YesNo, MessageBoxImage.Error) ==
                MessageBoxResult.Yes)
            {
                Connect();
            }
            else
            {
                this.Close();
            }
        }

        private void tfsConnection_MouseLeftButtonDown_1(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Connect();
        }

        private void Label_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Process.Start("http://blogs.microsoft.co.il/shair");
        }
    }
}
