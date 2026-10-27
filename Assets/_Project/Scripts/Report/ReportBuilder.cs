using System.Text;

namespace CrisisVR
{
    public static class ReportBuilder
    {
        public static string Build(SessionData session)
        {
            var report = new StringBuilder();
            report.AppendLine(ReportTexts.Header);
            report.AppendLine(ReportTexts.SessionCodeLabel + session.sessionId);
            report.AppendLine(ReportTexts.StartedAtLabel + session.startedAt);
            report.AppendLine(ReportTexts.AppVersionLabel + session.appVersion);
            report.AppendLine(ReportTexts.SessionStatusTitle + session.endReason);
            report.AppendLine();
            report.AppendLine(ReportTexts.Act1Title);
            report.AppendLine(ReportTexts.Act1Context);

            if (session.act1Registered)
            {
                report.AppendLine(ReportTexts.OccurredLabel + ReportTexts.Act1Event);
                report.AppendLine(ReportTexts.DecisionLabel + (session.Reintento
                    ? ReportTexts.RetryDecision
                    : ReportTexts.NoActionDecision));
                report.AppendLine(ReportTexts.ReflectionLabel + ReportTexts.ReflectionQuestion);
            }
            else
            {
                report.AppendLine(ReportTexts.Act1NotRegistered);
            }

            report.AppendLine();
            report.AppendLine(ReportTexts.QualityTitle);
            report.AppendLine(ReportTexts.CompletedLabel + (session.act1Completed ? ReportTexts.Yes : ReportTexts.No));
            report.AppendLine();
            report.AppendLine(ReportTexts.LimitsTitle);
            report.AppendLine(ReportTexts.Limits);
            return report.ToString();
        }
    }
}
