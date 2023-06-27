using Sitecore;
using Sitecore.Abstractions;
using Sitecore.Analytics.Pipelines.ExcludeRobots;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Sitecore.Framework.Conditions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.ServiceModel.Configuration;
using System.Text;
using System.Web;

namespace SitecoreFundamentals.ExcludeRobotsByAgent.Pipelines.ExcludeRobots
{
    public class CheckUserAgentUsingWildcard : ExcludeRobotsProcessor
    {
        private readonly BaseLog _log;
        private static List<Models.BlockedUserAgent> _blockedUserAgents { get; set; }
        private static List<string> _blockedIps { get; set; } = new List<string>();
        private static bool _missingConfigValues { get; set; } = false;
        private static DateTime _hitsLoggingTimer { get; set; }
        
        private static string _logPrefix { get; set; }
        private static string LogPrefix {
            get
            {
                if (string.IsNullOrWhiteSpace(_logPrefix))
                {
                    _logPrefix = Settings.GetSetting("SitecoreFundamentals.ExcludeRobotsByAgent.LogPrefix");
                }
                return _logPrefix;
            }
        }

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

            var valueSettingName = "SitecoreFundamentals.ExcludeRobotsByAgent.ExclusionValues";
            var exclusionValuesSetting = Settings.GetSetting(valueSettingName);

            if (string.IsNullOrWhiteSpace(exclusionValuesSetting) && !_missingConfigValues)
            {
                Log.Warn($"{LogPrefix} No config value found in {valueSettingName}", this);
                _missingConfigValues = true;
                return;
            }

            CheckBlockedUserAgentList();

            var exclusionValues = exclusionValuesSetting.ToLower().Split(',').Select(x => x.Trim()).ToList();
            var ignoreValues = Settings.GetSetting("SitecoreFundamentals.ExcludeRobotsByAgent.IgnoreValues").ToLower().Split(',').Select(x => x.Trim()).ToList();

            var context = HttpContext.Current;

            if (string.IsNullOrWhiteSpace(context?.Request?.UserAgent))
                return;

            var userAgent = context.Request.UserAgent.ToLower();
            var ip = GetIP(context);

            var ignoreValue = ignoreValues.FirstOrDefault(x => userAgent.Contains(x) && !string.IsNullOrWhiteSpace(x));
            if (ignoreValue != null)
            {
                Log.Debug($"{LogPrefix} User Agent contains the ignore value and will exit: {ignoreValue.Trim()}", this);
                return;
            }

            var exclusionValue = exclusionValues.FirstOrDefault(x => userAgent.Contains(x));
            if (exclusionValue != null)
            {
                args.IsInExcludeList = true;

                Log.Debug($"{LogPrefix} User Agent contains the value {exclusionValue.Trim()}", this);

                StoreHitForLogging(context, ip, false);

                if (!_blockedIps.Any(x => x.Equals(ip)))
                    _blockedIps.Add(ip);

                return;
            }
            else if (Settings.GetBoolSetting("SitecoreFundamentals.ExcludeRobotsByAgent.BlockWithIp", true) && _blockedIps.Any(x => x.Equals(ip)))
            {
                args.IsInExcludeList = true;

                Log.Debug($"{LogPrefix} IP address {ip} was previously blocked due to its User Agent", this);

                StoreHitForLogging(context, ip, true);

                return;
            }
        }

        private void StoreHitForLogging(HttpContext context, string ip, bool ipBlocked)
        {
            if (_blockedUserAgents.Count() < Settings.GetIntSetting("SitecoreFundamentals.ExcludeRobotsByAgent.SampleRecordsPerLogDump", 60))
            {
                var userAgent = context.Request.UserAgent;
                if (ipBlocked)
                    userAgent = $"{context.Request.UserAgent} - iP address {ip} was previously blocked due to its User Agent";

                _blockedUserAgents.Add(new Models.BlockedUserAgent()
                {
                    Ip = ip,
                    UserAgent = userAgent,
                    Url = context.Request.Url.AbsoluteUri,
                    DateTime = DateTime.Now
                });
            }

            if (_hitsLoggingTimer == DateTime.MinValue)
                _hitsLoggingTimer = DateTime.Now;
        }

        internal static void CheckIfEmailShouldBeSentAndResetList()
        {
            CheckBlockedUserAgentList();

            if (_blockedUserAgents.Any())
            {
                Log.Info($"{LogPrefix} {_blockedUserAgents.Count()} requests have been blocked since {_hitsLoggingTimer.ToString(Settings.GetSetting("SitecoreFundamentals.ExcludeRobotsByAgent.DateTimeFormat"))}", "CheckIfEmailShouldBeSentAndListReset");

                SendEmailNotification();

                _hitsLoggingTimer = DateTime.Now;
                _blockedUserAgents.Clear();
            }
        }

        private static void CheckBlockedUserAgentList()
        {
            if (_blockedUserAgents == null)
            {
                _blockedUserAgents = new List<Models.BlockedUserAgent>();
                Log.Info($"{LogPrefix} has started monitoring for invalid User Agent data.", "CheckBlockedUserAgentList");
            }
        }

        private static void SendEmailNotification()
        {
            var contextDb = Factory.GetDatabase("web");

            var settingsItem = contextDb.GetItem(Constants.ItemIDs.Settings);

            if (settingsItem == null || settingsItem.Fields[Constants.Templates.Settings.Fields.SendEmailNotifications].Value != "1")
                return;

            var emailFrom = settingsItem.Fields[Constants.Templates.Settings.Fields.EmailFrom].Value;
            var emailTo = settingsItem.Fields[Constants.Templates.Settings.Fields.EmailTo].Value;
            var emailBcc = settingsItem.Fields[Constants.Templates.Settings.Fields.EmailBcc].Value;
            var subject = settingsItem.Fields[Constants.Templates.Settings.Fields.Subject].Value;
            var beginningOfEmail = settingsItem.Fields[Constants.Templates.Settings.Fields.BeginningOfEmail].Value;
            var endOfEmail = settingsItem.Fields[Constants.Templates.Settings.Fields.EndOfEmail].Value;

            if (string.IsNullOrWhiteSpace(emailFrom) || string.IsNullOrWhiteSpace(emailFrom))
            {
                Log.Warn($"{LogPrefix} Emails are enabled but the From or To address is empty.", "SendEmailNotification");
                return;
            }

            var sb = new StringBuilder();
            var sampleFormat = Settings.GetSetting("SitecoreFundamentals.ExcludeRobotsByAgent.SampleRecordFormat");
            var dateTimeFormat = Settings.GetSetting("SitecoreFundamentals.ExcludeRobotsByAgent.DateTimeFormat");

            foreach (var blockedUserAgent in _blockedUserAgents)
            {
                var record = string.Copy(sampleFormat);

                record = record.Replace("{DateTime}", blockedUserAgent.DateTime.ToString(dateTimeFormat));
                record = record.Replace("{IP}", blockedUserAgent.Ip);
                record = record.Replace("{URL}", blockedUserAgent.Url);
                record = record.Replace("{UserAgent}", blockedUserAgent.UserAgent);
                record = record.Replace("{BREAK}", "<br />");

                sb.Append($"{record}<br />");
            }

            var message = new MailMessage();

            message.From = new MailAddress(emailFrom);

            if (!emailTo.Contains(","))
            {
                message.To.Add(new MailAddress(emailTo));
            }
            else
            {
                foreach (string email in emailTo.Split(','))
                {
                    if (!string.IsNullOrWhiteSpace(email))
                        message.To.Add(new MailAddress(email.Trim()));
                }
            }

            var bccLog = "";

            if (!string.IsNullOrWhiteSpace(emailBcc))
            {
                bccLog = $" (bcc to: {emailBcc})";

                if (!emailBcc.Contains(","))
                {
                    message.To.Add(new MailAddress(emailBcc));
                }
                else
                {
                    foreach (string email in emailBcc.Split(','))
                    {
                        if (!string.IsNullOrWhiteSpace(email))
                            message.To.Add(new MailAddress(email.Trim()));
                    }
                }
            }

            message.Subject = subject;

            message.Body = $"{beginningOfEmail}{sb}{endOfEmail}"
                .Replace("{LogDumpTimerStarted}", _hitsLoggingTimer.ToString(Settings.GetSetting("SitecoreFundamentals.ExcludeRobotsByAgent.DateTimeFormat")))
                .Replace("{SampleRecordsPerLogDump}", Settings.GetIntSetting("SitecoreFundamentals.ExcludeRobotsByAgent.SampleRecordsPerLogDump", 200).ToString());

            message.IsBodyHtml = true;

            try
            {
                MainUtil.SendMail(message);
                
                Log.Info($"{LogPrefix} An email has been sent to {emailTo}{bccLog} with a report of these User Agent blocks.", "SendEmailNotification");
            }
            catch (Exception ex)
            {
                Log.Error($"{LogPrefix} {ex}", "SendEmailNotification");
            }
        }

        private static string GetIP(HttpContext context)
        {
            var ip = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            if (string.IsNullOrWhiteSpace(ip))
                ip = context.Request.ServerVariables["REMOTE_ADDR"];

            return ip;
        }
    }
}