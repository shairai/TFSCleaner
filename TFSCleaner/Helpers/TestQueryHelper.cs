using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.TeamFoundation.TestManagement.Client;
using SR.TFSCleaner.Models;

namespace SR.TFSCleaner.Helpers
{
    public class TestQueryHelper
    {
        public static IEnumerable<ITestAttachment> CreateAttachmentsQuery(int runId, TestObjectType type)
        {
            string str = (type == TestObjectType.TestRun) ? "TestRunId" : "SessionId";
            StringBuilder sb = new StringBuilder(string.Format(CultureInfo.InvariantCulture, "SELECT * FROM Attachment WHERE {0} = {1} ", str, runId));
            return TfsShared.Instance.TestProject.QueryAttachments(sb.ToString());
        }

        public static IEnumerable<ITestRun> QueryRuns(DateTime newerThan, DateTime olderThan)
        {
            string queryText = "SELECT TestRunId FROM TestRun ";
            queryText = UpdateQueryWithDateCheck(newerThan, olderThan, queryText);
            return TfsShared.Instance.TestProject.TestRuns.Query(queryText);
        }

        public static IEnumerable<ISession> QuerySessions(DateTime newerThan, DateTime olderThan)
        {
            string queryText = "SELECT SessionId FROM Session ";
            queryText = UpdateQueryWithDateCheck(newerThan, olderThan, queryText);
            return TfsShared.Instance.TestProject.Sessions.Query(queryText);
        }
        private static string GetFormattedDate(DateTime date)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd}", new object[] { date });
        }

        private static string Add_AND_WHERE_OperatorsIfRequired(string queryText, bool isFirstClause)
        {
            if (isFirstClause)
            {
                queryText = queryText + "WHERE ";
                return queryText;
            }
            queryText = queryText + "AND ";
            return queryText;
        }

        private static string UpdateQueryWithDateCheck(DateTime NewerThan, DateTime OlderThan, string queryText)
        {
            bool isFirstClause = true;
            DateTime date = (NewerThan == DateTime.MinValue) ? DateTime.MinValue : NewerThan.AddDays(-1.0);
            DateTime time2 = (OlderThan == DateTime.MinValue) ? DateTime.MinValue : OlderThan.AddDays(1.0);
            if (date == DateTime.MinValue)
            {
                if (time2 != DateTime.MinValue)
                {
                    queryText = Add_AND_WHERE_OperatorsIfRequired(queryText, isFirstClause);
                    queryText = queryText + string.Format(CultureInfo.InvariantCulture, "CreationDate < '{0}'", new object[] { GetFormattedDate(time2) });
                    isFirstClause = false;
                }
                return queryText;
            }
            if (time2 == DateTime.MinValue)
            {
                queryText = Add_AND_WHERE_OperatorsIfRequired(queryText, isFirstClause);
                queryText = queryText + string.Format(CultureInfo.InvariantCulture, "CreationDate >= '{0}'", new object[] { GetFormattedDate(date) });
                isFirstClause = false;
                return queryText;
            }
            queryText = Add_AND_WHERE_OperatorsIfRequired(queryText, isFirstClause);
            queryText = queryText + string.Format(CultureInfo.InvariantCulture, "CreationDate >= '{0}' AND CreationDate < '{1}'", new object[] { GetFormattedDate(date), GetFormattedDate(time2) });
            isFirstClause = false;
            return queryText;
        }
    }
}
