using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using SR.TFSCleaner.Helpers;
using SR.TFSCleaner.Infrastructure;
using SR.TFSCleaner.Models;

namespace SR.TFSCleaner.Views
{

    public class TestAttachmentCleanup : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public ObservableCollection<TestRunAttachmentInfo> TestRunAttachmentsInfo { get; set; }

        public ObservableCollection<NodeItem> Extensions { get; set; }
        public ObservableCollection<NodeItem> States { get; set; }

        public ICommand SearchCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand DeleteCommand { get; private set; }

        private const string Any = "Any";

        public TestAttachmentCleanup()
        {
            SearchCommand = new AwaitableDelegateCommand(Search, CanWork);
            DeleteCommand = new AwaitableDelegateCommand(Delete, CanWork);
            StopCommand = new DelegateCommand(Stop, () => Working);

            TestRunAttachmentsInfo = new ObservableCollection<TestRunAttachmentInfo>();
            BuildSearchFilters();
        }

        void BuildSearchFilters()
        {
            Extensions = new ObservableCollection<NodeItem>()
            {
                new NodeItem() {Title = Any,IsSelected = true},
                new NodeItem() {Title = "itrace"},
                new NodeItem() {Title = "wmv"},
                new NodeItem() {Title = "xesc"},
                new NodeItem() {Title = "coverage"},
                new NodeItem() {Title = "trmx"},
                new NodeItem() {Title = "trx"},
                new NodeItem() {Title = "cov"},
                new NodeItem() {Title = "docx"},
                new NodeItem() {Title = "doc"},
                new NodeItem() {Title = "xls"},
                new NodeItem() {Title = "xlsx"},
                new NodeItem() {Title = "png"},
                new NodeItem() {Title = "jpg"},
                new NodeItem() {Title = "gif"},
            };

            States = new ObservableCollection<NodeItem>() { new NodeItem() { Title = Any, IsSelected = true } };
            foreach (var t in TfsShared.Instance.Transitions)
            {
                if (!States.Any(s => s.Title.Equals(t.From)) && !string.IsNullOrEmpty(t.From)) States.Add(new NodeItem() { Title = t.From });
                if (!States.Any(s => s.Title.Equals(t.To)) && !string.IsNullOrEmpty(t.To)) States.Add(new NodeItem() { Title = t.To });
            }
        }

        bool CanWork()
        {
            return !Working;
        }

        private bool _cancelPending = false;
        void Stop()
        {
            _cancelPending = true;
        }

        bool IsCancelPending()
        {
            if (_cancelPending)
            {
                ProgressText = "Cancelled By User.";
                return true;
            }
            return false;
        }

        async Task Delete()
        {
            Working = true;
            _cancelPending = false;
            ProgressText = string.Format("Deleting {0} items...", TestRunAttachmentsInfo.Count);
            ProgressTotalItems = TestRunAttachmentsInfo.Count;
            ProgressValue = 0;

            await Task.Run(() =>
            {
                var itemsToDelete = TestRunAttachmentsInfo.ToList();
                foreach (TestRunAttachmentInfo itemAttachmentInfo in itemsToDelete)
                {
                    if (IsCancelPending()) break;

                    IAttachmentOwner owner = itemAttachmentInfo.Run;
                    for (var i = 0; i < owner.Attachments.Count; i++)
                    {
                        if (IsCancelPending()) break;

                        var attachment = owner.Attachments[i];
                        if (!itemAttachmentInfo.MatchedAttachments.Contains(attachment)) continue;
                        
                        owner.Attachments.RemoveAt(i);
                        itemAttachmentInfo.MatchedAttachments.Remove(attachment);
                    }
                    itemAttachmentInfo.Run.Save();


                    foreach (var wit in itemAttachmentInfo.RelatedWorkitems)
                    {
                        wit.Attachments.Clear();
                        wit.Save();
                    }

                    ProgressValue++;
                    SetTotalSize();
                }
            });

            ProgressText = "Done";
            TestRunAttachmentsInfo.Clear();
            Working = false;
            return;
        }

        async Task Search()
        {
            TestRunAttachmentsInfo.Clear();
            _cancelPending = false;
            SetTotalSize();
            Working = true;
            IEnumerable<ITestRun> runs = TestQueryHelper.QueryRuns(NewerThan, OlderThan);
            IEnumerable<ISession> sessions = TestQueryHelper.QuerySessions(NewerThan, OlderThan);
            int numTotalRunsOrSessions = runs.Count<ITestRun>() + sessions.Count<ISession>();

            if ((runs == null) || (!runs.Any()))
            {
                Trace.WriteLine("No test runs matched the time range specified.");
            }
            else
            {
                await ExecuteAttachmentCleanup(runs, TestObjectType.TestRun);
            }
            if ((sessions == null) || (!sessions.Any()))
            {
                Trace.WriteLine("No sessions matched the time range specified.");
            }
            else
            {
                await ExecuteAttachmentCleanup(sessions, TestObjectType.Session);
            }
            Working = false;
            ProgressText = string.Format("Done");
        }


        private async Task ExecuteAttachmentCleanup(IEnumerable<ITestRunBase> runs, TestObjectType type)
        {
            int count = runs.Count<ITestRunBase>();
            ProgressText = string.Format("Found {0} items of type {1}", count, type);
            ProgressTotalItems = count;
            ProgressValue = 0;

            foreach (ITestRunBase run in runs)
            {
                if (IsCancelPending()) break;

                ProgressValue++;
                if (!this.MatchesDateRange(run)) continue;

                List<ITestAttachment> runAttachments = await GetRunAddtionalData(run, type);
                if (runAttachments == null || runAttachments.Count == 0) continue;

                List<WorkItem> relatedWorkitems = await GetRelatedWorkItems(run, runAttachments);

                TestRunAttachmentsInfo.Add(new TestRunAttachmentInfo(run, type, runAttachments, relatedWorkitems));

                SetTotalSize();
            }
        }

        private void SetTotalSize()
        {
            var wits = TestRunAttachmentsInfo.SelectMany(w => w.RelatedWorkitems);
            long witSize = wits.SelectMany(wit => wit.Attachments.Cast<Attachment>()).Sum(attachment => attachment.Length);
            long runsSize = TestRunAttachmentsInfo.SelectMany(a => a.MatchedAttachments).Sum(attachment => attachment.Length);
            TotalSize = string.Format(new FileSizeFormatProvider(), "{0:fs}", witSize + runsSize);
        }


        private async Task<List<ITestAttachment>> GetRunAddtionalData(ITestRunBase run, TestObjectType type)
        {
            try
            {
                ProgressText = string.Format("Collecting attachments for Test Run: {0}", run.Title);
                List<ITestAttachment> runAttachments = new List<ITestAttachment>();
                //IEnumerable<ITestAttachment> attachments = null;
                await Task.Run(() =>
                {
                    //attachments = TestQueryHelper.CreateAttachmentsQuery(run.Id, type);

                    foreach (ITestAttachment attachment in run.Attachments)
                    {
                        if (IsCancelPending()) break;

                        if ((attachment.Length / 1024f) / 1024f >= MinimumSize && Extensions.Where(s=>s.IsSelected).
                                                                                  Any(e => e.Title.Equals(Any) || 
                                                                                  (e.IsSelected && attachment.Name.EndsWith("." + e.Title))))
                            runAttachments.Add(attachment);
                    }
                });

                return runAttachments;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        private async Task<List<WorkItem>> GetRelatedWorkItems(ITestRunBase run, List<ITestAttachment> attachments)
        {
            try
            {
                ProgressText = string.Format("Collecting related work items for Test Run: {0}", run.Title);
                List<WorkItem> relatedWorkItems = new List<WorkItem>();
                await Task.Run(() =>
                {
                    List<string> source = attachments.Select(attachment => attachment.ArtifactUri.ToString()).ToList();

                    Dictionary<string, int[]> workItemIdsForArtifactUris = new Dictionary<string, int[]>();
                    workItemIdsForArtifactUris = TfsShared.Instance.WorkItemStore.GetWorkItemIdsForArtifactUris(source.ToArray(), DateTime.Now);

                    foreach (var wits in workItemIdsForArtifactUris.Values)
                    {
                        if (IsCancelPending()) break;

                        if (wits == null) continue;
                        foreach (var witId in wits)
                        {
                            if (IsCancelPending()) break;

                            WorkItem wit = TfsShared.Instance.WorkItemStore.GetWorkItem(witId);
                            if (States.Where(s => s.IsSelected).Any(s => s.Title.Equals(Any) || s.Title.Equals(wit.State)))
                                relatedWorkItems.Add(wit);
                        }
                    }
                });

                return relatedWorkItems;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        internal bool MatchesDateRange(ITestRunBase run)
        {
            DateTime dateCreated = run.DateCreated;
            if (dateCreated.Kind == DateTimeKind.Utc)
                dateCreated = DateTime.SpecifyKind(dateCreated, DateTimeKind.Local);
            if ((this.OlderThan != DateTime.MinValue) && (dateCreated > this.OlderThan))
                return false;
            return (this.NewerThan == DateTime.MinValue) || (dateCreated >= this.NewerThan);
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

        private string _totalSize = string.Empty;
        public string TotalSize
        {
            get { return _totalSize; }
            set
            {
                if (value == _totalSize) return;
                _totalSize = value;
                this.OnPropertyChanged("TotalSize");
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
                RemainingItems = ProgressTotalItems - _progressValue;
                this.OnPropertyChanged("ProgressValue");
            }
        }

        private int _scannedItems = 0;
        public int RemainingItems
        {
            get { return _scannedItems; }
            set
            {
                if (value == _scannedItems) return;
                _scannedItems = value;
                this.OnPropertyChanged("RemainingItems");
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

        private DateTime _newerThan = DateTime.Now.AddDays(-1);
        public DateTime NewerThan
        {
            get { return _newerThan; }
            set
            {
                if (value == _newerThan) return;
                _newerThan = value;
                this.OnPropertyChanged("NewerThan");
            }
        }
        private double _minimumSize = 1;
        public double MinimumSize
        {
            get { return _minimumSize; }
            set
            {
                if (value == _minimumSize) return;
                _minimumSize = value;
                this.OnPropertyChanged("MinimumSize");
            }
        }


        private DateTime _olderThan = DateTime.Now.AddDays(1);
        public DateTime OlderThan
        {
            get { return _olderThan; }
            set
            {
                if (value == _olderThan) return;
                _olderThan = value;
                this.OnPropertyChanged("OlderThan");
            }
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
