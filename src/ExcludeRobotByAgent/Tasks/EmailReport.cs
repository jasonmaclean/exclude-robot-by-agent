using Sitecore.Configuration;
using Sitecore.Diagnostics;
using SitecoreFundamentals.ExludeRobotsByAgent.Pipelines.ExcludeRobots;

namespace SitecoreFundamentals.ExludeRobotsByAgent.Tasks
{
    public class EmailReport
    {
        public void Run()
        {
            Log.Info($"{Settings.GetSetting("SitecoreFundamentals.ExludeRobotsByAgent.LogPrefix")} Checking to see if email should be sent.", this);

            CheckUserAgentUsingWildcard.CheckIfEmailShouldBeSentAndResetList();
        }
    }
}