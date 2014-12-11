using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.TeamFoundation.VersionControl.Common;
using SR.TFSCleaner.Helpers;
using SR.TFSCleaner.Infrastructure;
using Microsoft.TeamFoundation.VersionControl.Client;
using SR.TFSCleaner.Models;

namespace SR.TFSCleaner.Views
{

    public class SourceControlView : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<SourceControlItem> SourceControlItems { get; set; }
        public ObservableCollection<Changeset> SelectedItemHistory { get; set; }

        public ICommand DestroyCommand { get; private set; }
        public ICommand CopyDetailsCommand { get; private set; }

        public SourceControlView()
        {
            SourceControlItems = new ObservableCollection<SourceControlItem>();
            SelectedItemHistory = new ObservableCollection<Changeset>();
            GetSourceControlFirstLevel();
            DestroyCommand = new DelegateCommand(Destroy, CanDelete);
            CopyDetailsCommand = new DelegateCommand(CopyDetails, () => SelectedSourceControlItem != null);
        }

        void CopyDetails()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("Item: {0}", SelectedSourceControlItem.ServerItem));
            TableBuilder table = new TableBuilder();
            table.AddHeaders(new Header("ID"), new Header("Change"), new Header("Committer"), new Header("Date", 10), new Header("Comment", 35));

            foreach (Changeset change in SelectedItemHistory)
            {
                string changesStr = string.Empty;
                table.AddValues(change.ChangesetId.ToString(), change.Changes.Aggregate(changesStr,
                     (current, changeStr) => current + (changeStr.ChangeType.ToString() + ", ")), change.Committer, change.CreationDate.ToString(), change.Comment);
            }

            Clipboard.SetText(table.ToString());
        }

        async void Destroy()
        {
            if (
                MessageBox.Show(string.Format("You're about to delete {0} item.\nThe destroy action cannot be reversed. You must not destroy files that are still needed\n\nClick OK to continue.", SelectedSourceControlItem.ServerItem), "Delete Confirmation",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel) return;

            Working = true;

            if (SelectedSourceControlItem.Item.DeletionId > 0)
            {
                Item[] results = null;
                await Task.Run(() =>
                {
                    results =
                        TfsShared.Instance.Vcs.Destroy(
                            new ItemSpec(SelectedSourceControlItem.ServerItem, RecursionType.Full,
                                SelectedSourceControlItem.Item.DeletionId),
                            VersionSpec.Latest, null,
                            KeepHistory
                                ? DestroyFlags.KeepHistory | DestroyFlags.StartCleanup
                                : DestroyFlags.StartCleanup);
                });

                if (results != null && results.Length > 0)
                {
                    MessageBox.Show(string.Format("Total Destroyed Items - {0}", results.Length), "Destroy Completed",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    GetSourceControlFirstLevel();
                }
                else
                {
                    MessageBox.Show(string.Format("There were no items to destroy."), "No Items To Destory",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }

            Working = false;
        }

        private async void GetHistory()
        {
            if (SelectedSourceControlItem == null) return;
            Working = true;
            SelectedItemHistory.Clear();
            await Task.Run(() =>
            {
                string serverItem = SelectedSourceControlItem.ServerItem;
                List<Changeset> changesets = new List<Changeset>();
                ItemSpec spec = new ItemSpec(serverItem, RecursionType.None);

                ChangesetVersionSpec version = new ChangesetVersionSpec(SelectedSourceControlItem.Item.ChangesetId);
                ChangesetVersionSpec versionFrom = new ChangesetVersionSpec(1);

                changesets = TfsShared.Instance.Vcs.QueryHistory(serverItem,
                version, 0, RecursionType.None, null,
                versionFrom, VersionSpec.Latest, int.MaxValue, true, false).Cast<Changeset>().OrderByDescending(d => d.CreationDate).ToList();

                foreach (var change in changesets)
                    SelectedItemHistory.AddOnUi(change);
            });
            Working = false;
        }

        bool CanDelete()
        {
            return SelectedSourceControlItem != null && !Working;
        }

        private async void GetSourceControlFirstLevel(RecursionType recursionType = RecursionType.OneLevel)
        {
            SourceControlItems.Clear();
            string serverItem = "$/" + TfsShared.Instance.ProjectInfo.Name;
            ItemSet itemSet = null;
            Working = true;
            await Task.Run(() =>
            {
                ItemSpec spec = new ItemSpec(serverItem, recursionType);
                itemSet = TfsShared.Instance.Vcs.GetItems(spec, VersionSpec.Latest, DeletedState, ItemType, false);
            });

            foreach (var item in itemSet.Items.Where(i => !i.ServerItem.Equals(serverItem)).OrderByDescending(t => t.ItemType))
                SourceControlItems.Add(new SourceControlItem(item));

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

        private bool _keepHistory = true;
        public bool KeepHistory
        {
            get { return _keepHistory; }
            set
            {
                if (value == _keepHistory) return;
                _keepHistory = value;
                this.OnPropertyChanged("KeepHistory");
            }
        }

        private DeletedState _deletedState = DeletedState.Any;
        public DeletedState DeletedState
        {
            get { return _deletedState; }
            set
            {
                if (value == _deletedState) return;
                _deletedState = value;
                GetSourceControlFirstLevel();
                this.OnPropertyChanged("DeletedState");
            }
        }

        private Changeset _selectedChange;
        public Changeset SelectedChange
        {
            get { return _selectedChange; }
            set
            {
                if (value == _selectedChange) return;
                _selectedChange = value;
                this.OnPropertyChanged("SelectedChange");
            }
        }

        private SourceControlItem _selectedSourceControlItem;
        public SourceControlItem SelectedSourceControlItem
        {
            get { return _selectedSourceControlItem; }
            set
            {
                if (value == _selectedSourceControlItem) return;
                _selectedSourceControlItem = value;
                ServerItem = _selectedSourceControlItem.ServerItem;
                GetHistory();
                this.OnPropertyChanged("SelectedSourceControlItem");
            }
        }

        private string _serverItem;
        public string ServerItem
        {
            get { return _serverItem; }
            set
            {
                if (value == _serverItem) return;
                _serverItem = value;
                this.OnPropertyChanged("ServerItem");
            }
        }

        private ItemType _itemType = ItemType.Any;
        public ItemType ItemType
        {
            get { return _itemType; }
            set
            {
                if (value == _itemType) return;
                _itemType = value;
                GetSourceControlFirstLevel();
                this.OnPropertyChanged("ItemType");
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
