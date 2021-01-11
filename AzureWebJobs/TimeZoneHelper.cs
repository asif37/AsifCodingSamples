using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TexellCheckInWebJob.Helpers
{
    public static class TimeZoneHelper
    {
        public static DateTime ConvertToServerTimeZone(DateTime date)
        {
            using (var db = new Models.Edmx.TexellCheckInContext())
            {
                var timeZoneIndex = int.Parse(db.MaintenanceSettings.FirstOrDefault(r => r.SettingName == "TimeZone").SettingValue);
                var timeZoneName = db.TimeZones.FirstOrDefault(z => z.Id == timeZoneIndex).TimeZoneName;
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(date, timeZoneName);
            }
        }

        public static string GetServerTimeStamp()
        {
            return ConvertToServerTimeZone(DateTime.Now).ToString("MM/dd/yyyy H:mm tt");
        }
    }
}
