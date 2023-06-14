using Sitecore.Data;

namespace SitecoreFundamentals.ExcludeRobotsByAgent
{
    internal struct Constants
    {
        internal static class ItemIDs
        {
            internal static readonly ID Settings = new ID("{353A1C6B-5833-4848-945E-F3BECC529385}");
        }
        internal static class Templates
        {
            internal static class Settings
            {
                internal static readonly ID ID = new ID("{88D18882-FC22-412C-82D0-3DBA7AFC6209}");
                internal static class Fields
                {
                    internal const string SendEmailNotifications = "{9275F1C0-A874-4BA3-9776-4F6B61FAE34A}";
                    internal const string EmailFrom = "{95B8519C-B33A-49D6-A3AB-6A2A3EF3806C}";
                    internal const string EmailTo = "{12E6D691-4A1E-47C9-BC70-E71E123F2034}";
                    internal const string EmailBcc = "{644D0506-D82A-4679-8557-CC6359797ED9}";
                    internal const string Subject = "{DFDD008A-2C1B-4208-B596-35C7F3C4AE86}";
                    internal const string BeginningOfEmail = "{27BF67B4-DD07-4932-B1E1-18604A959B98}";
                    internal const string EndOfEmail = "{4A92D442-E244-4E52-9DE9-B5A21A9E7C09}";
                }
            }
        }
    }
}