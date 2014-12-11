using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Microsoft.TeamFoundation.Build.Client;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.Server;
using Microsoft.TeamFoundation.TestManagement.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using SR.TFSCleaner.Models;

namespace SR.TFSCleaner.Helpers
{
    public class TfsShared
    {
        private static TfsShared _instance;

        public TfsTeamProjectCollection Tfs { get; set; }
        public VersionControlServer Vcs { get; set; }
        public ProjectInfo ProjectInfo { get; set; }
        public ICommonStructureService4 Css4 { get; set; }
        public WorkItemStore WorkItemStore { get; set; }
        public ITestManagementService TestService { get; set; }
        public ITestManagementTeamProject TestProject { get; set; }
        public IGroupSecurityService GSS { get; set; }
        public IEnumerable<TeamFoundationTeam> AllTeams { get; set; }
        public List<Transition> Transitions { get; set; }
        public IBuildServer BuildServer { get; set; }
        public static TfsShared Instance
        {
            get { return _instance ?? (_instance = new TfsShared()); }
        }

        public bool Connect()
        {
            TeamProjectPicker tpp = new TeamProjectPicker(TeamProjectPickerMode.SingleProject, false);
            if (tpp.ShowDialog() == DialogResult.OK && tpp.SelectedProjects.Length > 0)
            {
                Tfs = tpp.SelectedTeamProjectCollection;
                ProjectInfo = tpp.SelectedProjects[0];
                Vcs = Tfs.GetService<VersionControlServer>();
                Css4 = Tfs.GetService<ICommonStructureService4>();
                TestService = Tfs.GetService<ITestManagementService>();
                TestProject = TestService.GetTeamProject(ProjectInfo.Name);
                WorkItemStore = Tfs.GetService<WorkItemStore>();
                GSS = (IGroupSecurityService)Tfs.GetService(typeof(IGroupSecurityService));
                BuildServer = (IBuildServer)Tfs.GetService(typeof(IBuildServer));

                //TfsTeamService teamService = Tfs.GetService<TfsTeamService>();
                //AllTeams = teamService.QueryTeams(ProjectInfo.Uri);


                CollectAllBugTransitions();

                return true;
            }
            else
                return false;
        }

        public List<User> GetProjectUsers()
        {
            try
            {
                var allSids = GSS.ReadIdentity(SearchFactor.AccountName,
                    string.Format(@"[{0}]\Project Valid Users", ProjectInfo.Name), QueryMembership.Expanded);
                IEnumerable<Identity> members =
                    GSS.ReadIdentities(SearchFactor.Sid, allSids.Members, QueryMembership.None)
                        .Where(a => a.Type == IdentityType.WindowsUser);
                return
                    members.Select(
                        member => new User() {UserName = member.AccountName, DisplayName = member.DisplayName})
                        .OrderBy(n => n.DisplayName)
                        .ToList();
            }
            catch (Exception ex)
            {
                return new List<User>();
            }
        }


        private void CollectAllBugTransitions()
        {
            Transitions = new List<Transition>();
            var categories = TestProject.WitProject.Categories["Microsoft.BugCategory"];
            foreach (var witd in categories.WorkItemTypes)
            {
                Transitions.AddRange(witd.GetTransistions());
            }
        }
    }
}
