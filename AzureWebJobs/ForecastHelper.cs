using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TexellCheckInWebJob.Models.Edmx;

namespace TexellCheckInWebJob.Helpers
{
    public static class ForecastHelper
    {
        public static void GetLocationOpenHours(Location loc, int dayOfWeek, out int start, out int end)
        {
            if (loc == null)
            {
                start = 0;
                end = 23;
                return;
            }
            switch (dayOfWeek)
            {
                case 0:
                    start = loc.SundayStart.GetValueOrDefault().Hours;
                    end = loc.SundayEnd.GetValueOrDefault().Hours;
                    break;
                case 1:
                    start = loc.MondayStart.GetValueOrDefault().Hours;
                    end = loc.MondayEnd.GetValueOrDefault().Hours;
                    break;
                case 2:
                    start = loc.TuesdayStart.GetValueOrDefault().Hours;
                    end = loc.TuesdayEnd.GetValueOrDefault().Hours;
                    break;
                case 3:
                    start = loc.WednesdayStart.GetValueOrDefault().Hours;
                    end = loc.WednesdayEnd.GetValueOrDefault().Hours;
                    break;
                case 4:
                    start = loc.ThursdayStart.GetValueOrDefault().Hours;
                    end = loc.ThursdayEnd.GetValueOrDefault().Hours;
                    break;
                case 5:
                    start = loc.FridayStart.GetValueOrDefault().Hours;
                    end = loc.FridayEnd.GetValueOrDefault().Hours;
                    break;
                case 6:
                    start = loc.SaturdayStart.GetValueOrDefault().Hours;
                    end = loc.SaturdayEnd.GetValueOrDefault().Hours;
                    break;
                default:
                    start = 0;
                    end = 23;
                    break;
            }
        }

        // dayofweek count
        public static int CountDays(int? day, DateTime start, DateTime end)
        {
            TimeSpan ts = end - start;                       // Total duration
            int count = (int)Math.Floor(ts.TotalDays / 7);   // Number of whole weeks
            int remainder = (int)(ts.TotalDays % 7);         // Number of remaining days
            int sinceLastDay = (int)(end.DayOfWeek - day);   // Number of days since last [day]
            if (sinceLastDay < 0) sinceLastDay += 7;         // Adjust for negative days since last [day]

            // If the days in excess of an even week are greater than or equal to the number days since the last [day], then count this one, too.
            if (remainder >= sinceLastDay) count++;

            return count;
        }
        
        public static int RecurrenceCount(DayOfWeek day, DateTime end)
        {
            DateTime start = new DateTime(end.Year, end.Month, 1);
            TimeSpan ts = end - start;                       // Total duration
            int count = (int)Math.Floor(ts.TotalDays / 7);   // Number of whole weeks
            int remainder = (int)(ts.TotalDays % 7);         // Number of remaining days
            int sinceLastDay = (int)(end.DayOfWeek - day);   // Number of days since last [day]
            if (sinceLastDay < 0) sinceLastDay += 7;         // Adjust for negative days since last [day]

            // If the days in excess of an even week are greater than or equal to the number days since the last [day], then count this one, too.
            if (remainder >= sinceLastDay) count++;

            return count;
        }

        public enum Frequency
        {
            Weekly = 1,
            Monthly = 2,
            Yearly = 3,
            YearlyVariable = 4
        }
    }
}
