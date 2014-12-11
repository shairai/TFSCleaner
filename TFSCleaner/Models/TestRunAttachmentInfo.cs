using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using SR.TFSCleaner.Helpers;

namespace SR.TFSCleaner.Models
{
    public class TestRunAttachmentInfo : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
        public ITestRunBase Run { get; set; }
        public TestObjectType Type { get; set; }
        public List<ITestAttachment> MatchedAttachments { get; set; }
        public List<WorkItem> RelatedWorkitems { get; set; }

        public TestRunAttachmentInfo(ITestRunBase run, TestObjectType type, List<ITestAttachment> attachments, List<WorkItem> relatedWorkitems)
        {
            Run = run;
            Type = type;
            MatchedAttachments = attachments;
            RelatedWorkitems = relatedWorkitems;
            
            Title = Run.Title;
            Owner = run.OwnerName;
            DateStarted = Run.DateStarted;

            FormatLength = string.Format(new FileSizeFormatProvider(), "{0:fs}", attachments.Sum(attachment => attachment.Length));
        }

        private string _formatLength;
        public string FormatLength
        {
            get { return _formatLength; }
            set
            {
                if (value == _formatLength) return;
                _formatLength = value;
                this.OnPropertyChanged("FormatLength");
            }
        }

        private DateTime _dateStarted;
        public DateTime DateStarted
        {
            get { return _dateStarted; }
            set
            {
                if (value != _dateStarted)
                {
                    _dateStarted = value;
                    this.OnPropertyChanged("DateStarted");
                }
            }
        }

        private string _owner;
        public string Owner
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

        private string _title;
        public string Title
        {
            get { return _title; }
            set
            {
                if (value != _title)
                {
                    _title = value;
                    this.OnPropertyChanged("Title");
                }
            }
        }
    }
}
