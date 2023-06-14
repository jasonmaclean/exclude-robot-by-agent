using Sitecore.Configuration;
using Sitecore.Diagnostics;
using SitecoreFundamentals.ExcludeRobotsByAgent.Pipelines.ExcludeRobots;

namespace SitecoreFundamentals.ExcludeRobotsByAgent.Tasks
{
    public class EmailReport
    {
        public void Run()
        {
            Log.Info($"{Settings.GetSetting("SitecoreFundamentals.ExcludeRobotsByAgent.LogPrefix")} Checking to see if email should be sent.", this);

            CheckUserAgentUsingWildcard.CheckIfEmailShouldBeSentAndResetList();
        }
    }
}