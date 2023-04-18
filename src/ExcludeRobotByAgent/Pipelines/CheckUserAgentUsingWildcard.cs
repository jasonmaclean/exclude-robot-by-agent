using Sitecore.Abstractions;
using Sitecore.Analytics.Pipelines.ExcludeRobots;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Framework.Conditions;
using System;
using System.Linq;

namespace SitecoreFundamentals.ExludeRobotsByAgent.Pipelines.ExcludeRobots
{
    public class CheckUserAgentUsingWildcard : ExcludeRobotsProcessor
    {
        private readonly BaseLog _log;
        private static bool _missingConfigValues { get; set; } = false;
        private static DateTime _hitsLoggingTimer { get; set; } 
        private static int _hitsLogged { get; set; } = 0;

        public CheckUserAgentUsingWildcard(BaseLog log)
        {
            ValidatorExtensions.IsNotNull<BaseLog>(Condition.Requires<BaseLog>(log, nameof(log)));
            this._log = log;
        }

        public override void Process(ExcludeRobotsArgs args)
        {
            if (_missingConfigValues)
                return;

            if (args == null)
                throw new ArgumentNullException(nameof(args));

            if (args.IsInExcludeList)
                return;

            var valueSettingName = "SitecoreFundamentals.ExludeRobotsByAgent.ExclusionValues";
            var exclusionValuesSetting = Sitecore.Configuration.Settings.GetSetting(valueSettingName);

            if (string.IsNullOrWhiteSpace(exclusionValuesSetting))
            {
                if (!_missingConfigValues)
                {
                    Log.Warn($"{Settings.GetSetting("SitecoreFundamentals.ExludeRobotsByAgent.LogPrefix")} No config value found in {valueSettingName}", this);
                    _missingConfigValues = true;
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

                Log.Debug($"{Settings.GetSetting("SitecoreFundamentals.ExludeRobotsByAgent.LogPrefix")} User Agent is shorter than the configured limit of {minimumAgentLength}", this);

                return;
            }

            var exclusionValue = exclusionValues.FirstOrDefault(x => userAgent.Contains(x));
            if (exclusionValue != null)
            {
                args.IsInExcludeList = true;

                Log.Debug($"{Settings.GetSetting("SitecoreFundamentals.ExludeRobotsByAgent.LogPrefix")} User Agent contains the value {exclusionValue}", this);

                StoreHitForLogging();

                return;
            }
        }

        private void StoreHitForLogging()
        {
            _hitsLogged++;

            if (_hitsLoggingTimer == DateTime.MinValue)
                _hitsLoggingTimer = DateTime.Now;

            var minutesUntilLogDump = Sitecore.Configuration.Settings.GetIntSetting("SitecoreFundamentals.ExludeRobotsByAgent.MinutesUntilLogDump", 30);
        
            if (_hitsLoggingTimer.AddMinutes(minutesUntilLogDump) < DateTime.Now)
            {
                Log.Info($"{Settings.GetSetting("SitecoreFundamentals.ExludeRobotsByAgent.LogPrefix")} {_hitsLogged} requests have been blocked since {_hitsLoggingTimer.ToString("yyyy-MM-dd h:mm tt")}", this);
                _hitsLoggingTimer = DateTime.Now;
                _hitsLogged = 0;
            }
        }
    }
}