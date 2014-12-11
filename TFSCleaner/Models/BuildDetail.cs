using System.ComponentModel;
using Microsoft.TeamFoundation.Build.Client;

namespace SR.TFSCleaner.Models
{
    public class BuildDetail : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Id { get; set; }
        public ContinuousIntegrationType ContinuousIntegrationType { get; set; }
        public BuildDetail(IBuildDefinition buildDef,  IBuildDetail buildDetail)
        {
            Id = buildDef.Id;
            ContinuousIntegrationType = buildDef.ContinuousIntegrationType;
            BuildDefinitionName = buildDef.Name;

            Build = buildDetail;           
        }

        private IBuildDetail _build;
        public IBuildDetail Build
        {
            get { return _build; }
            set
            {
                if (value == _build) return;
                _build = value;
                this.OnPropertyChanged("Build");
            }
        }

        private string _buildDefinitionName = "N/A";
        public string BuildDefinitionName
        {
            get { return _buildDefinitionName; }
            set
            {
                if (value == _buildDefinitionName) return;
                _buildDefinitionName = value;
                this.OnPropertyChanged("BuildDefinitionName");
            }
        }
    }
}
