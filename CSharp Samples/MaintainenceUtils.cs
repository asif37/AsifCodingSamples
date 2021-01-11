using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TexellCheckInMobile.Helpers;

namespace TexellCheckInMobile.Utils
{
    public static class MaintainenceUtils
    {
        public static DateTime CurrentDateTime(DateTime date)
        {
            var currentTimeZoneValue = Convert.ToInt32(MaintenanceHelper.GetTimeZone().SettingValue);
            var timeZoneName = MaintenanceHelper.GetTimeZones().FirstOrDefault(t => t.Id == currentTimeZoneValue).TimeZoneName;
            return TimeZoneInfo.ConvertTime(date, TimeZoneInfo.FindSystemTimeZoneById(timeZoneName));
        }
    }
}
