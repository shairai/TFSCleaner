using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using SR.TFSCleaner.Helpers;
using SR.TFSCleaner.Infrastructure;
using Microsoft.TeamFoundation.VersionControl.Client;
using SR.TFSCleaner.Models;

namespace SR.TFSCleaner.Views
{

    public class WorkspaceShelvesView : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<User> UsersCollection { get; set; }
        public ObservableCollection<Workspace> Workspaces { get; set; }
        public ObservableCollection<Shelveset> Shelvesets { get; set; }

        public ICommand SearchCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }
        public ICommand CopyToClipboardCommand { get; private set; }



        public WorkspaceShelvesView()
        {
            UsersCollection = new ObservableCollection<User>();
            Workspaces = new ObservableCollection<Workspace>();
            Shelvesets = new ObservableCollection<Shelveset>();

            UsersCollection.Add(new User() { DisplayName = "All", UserName = "" });

            SearchCommand = new DelegateCommand(Search, CanSearch);
            DeleteCommand = new DelegateCommand(Delete, CanDelete);
            CopyToClipboardCommand = new DelegateCommand(CopyToClipboard, CanDelete);
            GetUsers();
        }

        async void Delete()
        {
            if (
                MessageBox.Show(string.Format("You're about to delete {0} workspaces, and {1} shelvesets\n** Caution: There’s no way to recover a shelveset or workspace once it is deleted. **\nClick OK to continue.", Workspaces.Count, Shelvesets.Count), "Delete Confirmation",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel) return;

            TotalItemToDelete = Workspaces.Count + Shelvesets.Count;
            CompletedItems = 0;

            Working = true;

            await Task.Run(() =>
            {
                foreach (var ws in Workspaces)
                {
                    ws.Delete();
                    CompletedItems++;
                }

                foreach (var sh in Shelvesets)
                {
                    TfsShared.Instance.Vcs.DeleteShelveset(sh);
                    CompletedItems++;
                }
            });

            Workspaces.Clear();
            Shelvesets.Clear();
            Working = false;
        }

        void CopyToClipboard()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Format("List of Workspaces & Shelvesets older than {0}", DateTime.Now.AddDays(-this.MaxDays)));

            if (Workspaces.Count > 0)
            {
                sb.AppendLine("Workspaces:");
                TableBuilder table = new TableBuilder();
                table.AddHeaders(new Header("Name"), new Header("Computer"), new Header("Last Access Date"), new Header("Comment", 35));
                foreach (var ws in Workspaces.OrderBy(o => o.OwnerName))
                {
                    table.AddValues(ws.DisplayName, ws.Computer, ws.LastAccessDate.ToString(), ws.Comment);
                }

                sb.Append(table.ToString());
            }

            if (Shelvesets.Count > 0)
            {
                sb.AppendLine("Shelvesets:");
                TableBuilder table = new TableBuilder();
                table.AddHeaders(new Header("Name"), new Header("Creation Date"), new Header("Comment", 35));
                foreach (var ws in Shelvesets.OrderBy(o => o.OwnerName))
                {
                    table.AddValues(ws.DisplayName, ws.CreationDate.ToString(), ws.Comment);
                }
                sb.Append(table.ToString());
            }

            Clipboard.SetText(sb.ToString());
        }

        async void Search()
        {
            Working = true;
            DisplayIsIndeterminate = true;
            Workspaces.Clear();
            Shelvesets.Clear();

            if (SearchWorkspaces)
            {
                List<Workspace> workspacesFilterByDays = new List<Workspace>();

                await Task.Run(() =>
                {
                    var allWorkspacesForUser = TfsShared.Instance.Vcs.QueryWorkspaces(null, Owner.UserName, null);
                    workspacesFilterByDays.AddRange(allWorkspacesForUser.Where(w => w.LastAccessDate < DateTime.Now.AddDays(-this.MaxDays))
                                                                        .Where(w => w.Folders.Any(s => s.ServerItem.StartsWith(string.Format("$/{0}", TfsShared.Instance.ProjectInfo.Name)))));

                });
                if (workspacesFilterByDays != null)
                {
                    foreach (var ws in workspacesFilterByDays)
                    {
                        Workspaces.AddOnUi(ws);
                    }
                }
            }

            if (SearchShelvesets)
            {
                IEnumerable<Shelveset> shelvsetesFilterByDays = null;
                await Task.Run(() =>
                {
                    var allShelvsetes = TfsShared.Instance.Vcs.QueryShelvesets(null, Owner.UserName);
                    shelvsetesFilterByDays = allShelvsetes.Where(w => w.CreationDate < DateTime.Now.AddDays(-this.MaxDays));
                });
                if (shelvsetesFilterByDays != null)
                {
                    foreach (var sh in shelvsetesFilterByDays)
                    {
                        Shelvesets.AddOnUi(sh);
                    }
                }
            }
            Working = false;
            DisplayIsIndeterminate = false;
        }

        bool CanDelete()
        {
            return (Workspaces.Count > 0 || Shelvesets.Count > 0);
        }

        bool CanSearch()
        {
            return (SearchShelvesets || SearchWorkspaces && (MaxDays > -1));
        }

        private async void GetUsers()
        {
            //List<User> usersList = new List<User>();
            DisplayIsIndeterminate = true;
            //await Task.Run(() =>
            //{
            //    foreach (var user in TfsShared.Instance.AllTeams.Select(
            //        team => team.GetMembers(TfsShared.Instance.Tfs, MembershipQuery.Direct))
            //        .SelectMany(users => users)
            //        .OrderBy(n => n.DisplayName)
            //        .Where(user => !usersList.Any(u => u.UserName.Equals(user.UniqueName))))
            //    {
            //        usersList.Add(new User() { DisplayName = user.DisplayName, UserName = user.UniqueName });
            //    }
            //});

            foreach (var user in TfsShared.Instance.GetProjectUsers())
                UsersCollection.Add(user);

            if (UsersCollection.Count > 0)
                Owner = UsersCollection[0];

            DisplayIsIndeterminate = false;
        }

        private int _maxDays = 30;
        public int MaxDays
        {
            get { return _maxDays; }
            set
            {
                if (value != _maxDays)
                {
                    _maxDays = value;
                    this.OnPropertyChanged("MaxDays");
                }
            }
        }

        private bool _displayIsIndeterminate = true;
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

        private bool _searchShelvesets = true;
        public bool SearchShelvesets
        {
            get { return _searchShelvesets; }
            set
            {
                if (value != _searchShelvesets)
                {
                    _searchShelvesets = value;
                    Shelvesets.Clear();
                    this.OnPropertyChanged("SearchShelvesets");
                }
            }
        }

        private User _owner = null;
        public User Owner
        {
            get { return _owner; }
            set
            {
                if (value != _owner)
                {
                    _owner = value;
                    this.OnPropertyChanged("Owner");
                }
            }
        }

        private bool _searchWorkspaces = true;
        public bool SearchWorkspaces
        {
            get { return _searchWorkspaces; }
            set
            {
                if (value != _searchWorkspaces)
                {
                    _searchWorkspaces = value;
                    Workspaces.Clear();
                    this.OnPropertyChanged("SearchWorkspaces");
                }
            }
        }

        private bool _working = false;
        public bool Working
        {
            get { return _working; }
            set
            {
                if (value != _working)
                {
                    _working = value;
                    this.OnPropertyChanged("Working");
                }
            }
        }

        private int _totalItemToDelete = 0;
        public int TotalItemToDelete
        {
            get { return _totalItemToDelete; }
            set
            {
                if (value != _totalItemToDelete)
                {
                    _totalItemToDelete = value;
                    this.OnPropertyChanged("TotalItemToDelete");
                }
            }
        }

        private int _completedItems = 0;
        public int CompletedItems
        {
            get { return _completedItems; }
            set
            {
                if (value != _completedItems)
                {
                    _completedItems = value;
                    this.OnPropertyChanged("CompletedItems");
                }
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
