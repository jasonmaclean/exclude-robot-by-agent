using System;

namespace SitecoreFundamentals.ExcludeRobotsByAgent.Models
{
    public class BlockedUserAgent
    {
        public DateTime DateTime { get; set; }
        public string Ip { get; set; }
        public string UserAgent { get; set; }
        public string Url { get; set; }
    }
}