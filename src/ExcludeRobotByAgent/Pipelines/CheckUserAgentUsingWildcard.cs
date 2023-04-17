using Sitecore.Abstractions;
using Sitecore.Analytics.Pipelines.ExcludeRobots;
using Sitecore.Diagnostics;
using Sitecore.Framework.Conditions;
using System;
using System.Linq;

namespace SitecoreFundamentals.ExludeRobotsByAgent.Pipelines.ExcludeRobots
{
    public class CheckUserAgentUsingWildcard : ExcludeRobotsProcessor
    {
        private readonly BaseLog _log;
        private static bool _loggedMissingConfig { get; set; } = false;

        public CheckUserAgentUsingWildcard(BaseLog log)
        {
            ValidatorExtensions.IsNotNull<BaseLog>(Condition.Requires<BaseLog>(log, nameof(log)));
            this._log = log;
        }

        public override void Process(ExcludeRobotsArgs args)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (args.IsInExcludeList)
                return;

            var valueSettingName = "SitecoreFundamentals.ExludeRobotsByAgent.ExclusionValues";
            var exclusionValuesSetting = Sitecore.Configuration.Settings.GetSetting(valueSettingName);

            if (string.IsNullOrWhiteSpace(exclusionValuesSetting))
            {
                if (!_loggedMissingConfig)
                {
                    Log.Warn($"{typeof(CheckUserAgent).FullName}.{nameof(CheckUserAgent)} => No config value found in {valueSettingName}", this);
                    _loggedMissingConfig = true;
                }
                return;
            }

            var exclusionValues = exclusionValuesSetting.ToLower().Split(',');

            var httpContext = System.Web.HttpContext.Current;

            if (string.IsNullOrWhiteSpace(httpContext?.Request?.UserAgent))
                return;

            var userAgent = httpContext.Request.UserAgent.ToLower();

            var minimumAgentLength = Sitecore.Configuration.Settings.GetIntSetting("SitecoreFundamentals.ExludeRobotsByAgent.MinimumAgentLength", 255);

            if (userAgent.Length < minimumAgentLength)
            {
                args.IsInExcludeList = true;

                Log.Debug($"{typeof(CheckUserAgent).FullName}.{nameof(CheckUserAgent)} => User Agent is shorter than the configured limit of {minimumAgentLength}", this);

                return;
            }

            var exclusionVaue = exclusionValues.FirstOrDefault(x => userAgent.Contains(x));
            if (exclusionVaue != null)
            {
                args.IsInExcludeList = true;

                Log.Debug($"{typeof(CheckUserAgent).FullName}.{nameof(CheckUserAgent)} => User Agent contains the value  {exclusionVaue}", this);

                return;
            }
        }
    }
}