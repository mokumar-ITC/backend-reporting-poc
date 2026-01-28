using Microsoft.AspNetCore.Mvc;

namespace BIEmbedSystem.API.Jobs
{
    public static class TimeHelper
    {
        private static readonly TimeZoneInfo IstTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
        // Linux alternative: "Asia/Kolkata"

        public static DateTime IstNow()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, IstTimeZone);
        }
    }
}
