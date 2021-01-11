using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TexellCheckInMobile.Models;

namespace TexellCheckInMobile.Helpers
{
    public static class MaintenanceHelper
    {
        public static MaintenanceSettings GetTimeZone()
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                return db.MaintenanceSettings.DefaultIfEmpty(null).FirstOrDefault(s => s.SettingName == "TimeZone");
            }
        }

        public static List<TimeZones> GetTimeZones()
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                return db.TimeZones.ToList();
            }
        }
    }
}
