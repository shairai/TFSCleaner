using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.TeamFoundation.WorkItemTracking.Client;
using SR.TFSCleaner.Models;

namespace SR.TFSCleaner.Helpers
{
    public static class WorkItemStatesCollector
    {
        private static readonly Dictionary<WorkItemType, List<Transition>> AllTransistions = new Dictionary<WorkItemType, List<Transition>>();

        public static List<Transition> GetTransistions(this WorkItemType workItemType)
        {
            List<Transition> currentTransistions;

            AllTransistions.TryGetValue(workItemType, out currentTransistions);
            if (currentTransistions != null)
                return currentTransistions;

            XmlDocument workItemTypeXml = workItemType.Export(false);
            XmlNodeList transitionsList = workItemTypeXml.GetElementsByTagName("TRANSITIONS");
            XmlNode transitions = transitionsList[0];
            var newTransistions = (from XmlNode transition in transitions
                select new Transition
                {
                    From = transition.Attributes["from"].Value, To = transition.Attributes["to"].Value
                }).ToList();
            AllTransistions.Add(workItemType, newTransistions);

            return newTransistions;
        }
    }
}
