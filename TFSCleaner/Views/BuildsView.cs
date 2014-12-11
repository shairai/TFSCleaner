using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.TeamFoundation.Build.Client;
using SR.TFSCleaner.Helpers;
using SR.TFSCleaner.Infrastructure;
using SR.TFSCleaner.Models;

namespace SR.TFSCleaner.Views
{
    public class BuildsView : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<BuildDetail> Builds { get; set; }
        public ObservableCollection<IBuildDefinition> BuildDefinitions { get; set; }
        public ObservableCollection<NodeItem> DeleteOptionsList { get; set; }

        public ICommand SearchCommand { get; private set; }
        public ICommand DestroyCommand { get; private set; }
        public ICommand DeleteBuildsCommand { get; private set; }
        public ICommand DestroyBuildsCommand { get; private set; }

        private IBuildDefinition[] buildDefs;
        public BuildsView()
        {
            Builds = new ObservableCollection<BuildDetail>();
            BuildDefinitions = new ObservableCollection<IBuildDefinition>();
            DeleteOptionsList = new ObservableCollection<NodeItem>
            {
                new NodeItem() {IsSelected = true,  Title = DeleteOptions.All.ToString()},
                new NodeItem() {IsSelected = false, Title = DeleteOptions.Details.ToString()},
                new NodeItem() {IsSelected = false, Title = DeleteOptions.DropLocation.ToString()},
                new NodeItem() {IsSelected = false, Title = DeleteOptions.Label.ToString()},
                new NodeItem() {IsSelected = false, Title = DeleteOptions.None.ToString()},
                new NodeItem() {IsSelected = false, Title = DeleteOptions.Symbols.ToString()},
                new NodeItem() {IsSelected = false, Title = DeleteOptions.TestResults.ToString()}
            };

            SearchCommand = new AwaitableDelegateCommand(Search, () => !Working);
            DestroyCommand = new AwaitableDelegateCommand(Destroy, () => SelectedIBuildDefinition != null && !Working);
            DeleteBuildsCommand = new AwaitableDelegateCommand<object>(DeleteBuilds);
            DestroyBuildsCommand = new AwaitableDelegateCommand<object>(DestroyBuilds);
            GetDefinitions();
        }

        async Task DestroyBuilds(object selectedBuilds)
        {
            if (selectedBuilds == null) return;
            var buildDetails = (selectedBuilds as IEnumerable<object>).Cast<BuildDetail>().ToList();
            if (!buildDetails.Any())
            {
                MessageBox.Show("Please select builds to destroy.", "Missing Builds", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show(string.Format("You're about to destroy '{0}' builds\n\n** The destroy action cannot be reversed **\n\nPress Yes to continue. ",
                buildDetails.Count()), "Destory Build", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            StartLoading();

            await Task.Run(() =>
            {
                DisplayIsIndeterminate = true;
                var buildsToDelete = buildDetails.Select(b => b.Build).ToArray();
                TfsShared.Instance.BuildServer.DeleteBuilds(buildsToDelete, DeleteOptions.All);
                TfsShared.Instance.BuildServer.DestroyBuilds(buildsToDelete);
                ProgressText = string.Format("Done");
                DisplayIsIndeterminate = false;
            });

            await Search();
        }

        void StartLoading()
        {
            Working = true;
            DisplayIsIndeterminate = true;
            ProgressTotalItems = 0;
            ProgressValue = 0;
        }

        async Task DeleteBuilds(object selectedBuilds)
        {
            if (selectedBuilds == null) return;
            var buildDetails = (selectedBuilds as IEnumerable<object>).Cast<BuildDetail>().ToList();
            if (!buildDetails.Any())
            {
                MessageBox.Show("Please select builds to delete", "Missing Builds", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            var selectedDeleteOptions = DeleteOptionsList.Where(d => d.IsSelected);
            IEnumerable<NodeItem> nodeItems = selectedDeleteOptions as NodeItem[] ?? selectedDeleteOptions.ToArray();
            if (!nodeItems.Any())
            {
                MessageBox.Show("Please select at least one delete option", "Missing Delete Option", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (MessageBox.Show(string.Format("You're about to delete '{0}' builds\n\n** The destroy action cannot be reversed **\n\nPress Yes to continue. ",
                buildDetails.Count()), "Destory Build", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            StartLoading();

            List<DeleteOptions> options = new List<DeleteOptions>();
            foreach (NodeItem node in nodeItems)
            {
                DeleteOptions deleteOptions;
                Enum.TryParse(node.Title, out deleteOptions);
                options.Add(deleteOptions);
            }

            await Task.Run(() =>
            {
                ProgressTotalItems = buildDetails.Count;
                foreach (BuildDetail buildDetail in buildDetails)
                {
                    foreach (DeleteOptions deleteOption in options)
                    {
                        ProgressText = string.Format("Deleting '{1}' from '{0}' build", buildDetail.Build.BuildNumber, deleteOption);
                        IBuildDeletionResult result = buildDetail.Build.Delete(deleteOption);
                    }
                    ProgressValue++;
                }
                ProgressText = string.Format("Done");
            });

            await Search();
        }

        private async Task Destroy()
        {
            if (MessageBox.Show(string.Format("You're about to destory the entire Build Definition '{0}'\nthis operation will delete drop, test results, labels and remove the build from database records.\n\n** The destroy action cannot be reversed **\n\nPress Yes to continue. ", SelectedIBuildDefinition.Name), "Destory Build", MessageBoxButton.YesNo, MessageBoxImage.Question) !=
                MessageBoxResult.Yes) return;

            StartLoading();

            await Task.Run(async () => 
            {
                ProgressText = string.Format("Query Definition Builds...");

                IBuildDetail[] builds = await SearchBuilds(SelectedIBuildDefinition);
                ProgressText = string.Format("Destroying Builds...");
                TfsShared.Instance.BuildServer.DestroyBuilds(builds);
                ProgressText = string.Format("Deleting Build Definition...");
                SelectedIBuildDefinition.Delete();
                ProgressText = string.Format("Destroy Definition Completed.");
            });
        }

        async Task<IBuildDetail[]> SearchBuilds(IBuildDefinition buildDef)
        {
            List<IBuildDetail> builds = new List<IBuildDetail>();
            await Task.Run(() =>
            {
                IBuildDetailSpec buildDetailSpec = TfsShared.Instance.BuildServer.CreateBuildDetailSpec(buildDef);
                buildDetailSpec.QueryDeletedOption = SelectedQueryDeletedOption;
                buildDetailSpec.QueryOptions = SelectedQueryOption;

                IBuildQueryResult result = TfsShared.Instance.BuildServer.QueryBuilds(buildDetailSpec);
                builds = result.Builds.ToList();
            });

            return builds.ToArray();
        }

        async void GetDefinitions()
        {
            Working = true;
            await Task.Run(() =>
            {
                buildDefs = TfsShared.Instance.BuildServer.QueryBuildDefinitions(TfsShared.Instance.ProjectInfo.Name, QueryOptions.All);
            });
            foreach (var def in buildDefs)
            {
                BuildDefinitions.Add(def);
            }
            Working = false;
        }

        private async Task Search()
        {
            StartLoading();
            Builds.Clear();
            DisplayIsIndeterminate = true;

            await Task.Run(async () =>
            {
                ProgressTotalItems = buildDefs.Length;               

                foreach (IBuildDefinition buildDef in buildDefs)
                {
                    IBuildDetail[] builds = await SearchBuilds(buildDef);
                    foreach (IBuildDetail detail in builds)
                    {
                        Builds.AddOnUi(new BuildDetail(buildDef, detail));
                    }
                    ProgressValue++;
                    DisplayIsIndeterminate = false;
                }
            });

            Working = false;
        }


        private bool _working = false;
        public bool Working
        {
            get { return _working; }
            set
            {
                if (value == _working) return;
                _working = value;
                this.OnPropertyChanged("Working");
            }
        }

        private QueryOptions _selectedQueryOption = QueryOptions.All;
        public QueryOptions SelectedQueryOption
        {
            get { return _selectedQueryOption; }
            set
            {
                if (value == _selectedQueryOption) return;
                _selectedQueryOption = value;
                this.OnPropertyChanged("SelectedQueryOption");
            }
        }

        private QueryDeletedOption _selectedQueryDeletedOption = QueryDeletedOption.OnlyDeleted;
        public QueryDeletedOption SelectedQueryDeletedOption
        {
            get { return _selectedQueryDeletedOption; }
            set
            {
                if (value == _selectedQueryDeletedOption) return;
                _selectedQueryDeletedOption = value;
                this.OnPropertyChanged("SelectedQueryDeletedOption");
            }
        }

        private IBuildDefinition _selectedIBuildDefinition;
        public IBuildDefinition SelectedIBuildDefinition
        {
            get { return _selectedIBuildDefinition; }
            set
            {
                if (value == _selectedIBuildDefinition) return;
                _selectedIBuildDefinition = value;
                this.OnPropertyChanged("SelectedIBuildDefinition");
            }
        }

        private int _progressValue = 0;
        public int ProgressValue
        {
            get { return _progressValue; }
            set
            {
                if (value == _progressValue) return;
                _progressValue = value;
                this.OnPropertyChanged("ProgressValue");
            }
        }

        private string _progressText = "0/0";
        public string ProgressText
        {
            get { return _progressText; }
            set
            {
                if (value == _progressText) return;
                _progressText = value;
                this.OnPropertyChanged("ProgressText");
            }
        }


        private int _progressTotalItems = 0;
        public int ProgressTotalItems
        {
            get { return _progressTotalItems; }
            set
            {
                if (value == _progressTotalItems) return;
                _progressTotalItems = value;
                this.OnPropertyChanged("ProgressTotalItems");
            }
        }

        private bool _displayIsIndeterminate = false;
        public bool DisplayIsIndeterminate
        {
            get { return _displayIsIndeterminate; }
            set
            {
                if (value != _displayIsIndeterminate)
                {
                    _displayIsIndeterminate = value;
                    this.OnPropertyChanged("DisplayIsIndeterminate");
                }
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
