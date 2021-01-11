using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using TexellCheckInMobile.Models;
using System.Device;
using System.Device.Location;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using TexellCheckInMobile.Models.MobileViewModel;
using TexellCheckInMobile.Utils;
using System.Globalization;

namespace TexellCheckInMobile.Helpers
{
    public static class LocationListHelper
    {
        public static List<LocationListCustom> GetAllLocations(string latitude, string longitude, DateTime currentDateTime)
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                List<LocationListCustom> NearestLocations = new List<LocationListCustom>();

                var coordinates = new GeoCoordinate(Convert.ToDouble(latitude, CultureInfo.InvariantCulture), Convert.ToDouble(longitude, CultureInfo.InvariantCulture));
                var locationslist = db.Location.Where(lid => lid.ShowInMobile == true).ToList();

                using (SqlConnection connection = new SqlConnection(GetConnectionString()))
                {
                    var dateTimeFormats = new CultureInfo("en-US").DateTimeFormat;
                    string dayName = (currentDateTime.ToString("dddd", dateTimeFormats));
                    connection.Open();
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        using (SqlCommand command = new SqlCommand())
                        {
                            command.Connection = connection;
                            command.CommandText = "sp_getallnearestlocations";
                            command.CommandType = System.Data.CommandType.StoredProcedure;
                            command.Parameters.Add(new SqlParameter("@latitude", latitude));
                            command.Parameters.Add(new SqlParameter("@longitude", longitude));
                            command.Parameters.Add(new SqlParameter("@currentTime", currentDateTime.TimeOfDay)); // comment: It will be changed with usertime param which is coming from function
                            command.Parameters.Add(new SqlParameter("@currentDay", dayName));
                            command.Parameters.Add(new SqlParameter("@currentDate", currentDateTime.Date));
                            using (SqlDataReader reader = command.ExecuteReader())
                            {
                                NearestLocations.Clear();
                                while (reader.Read())
                                {
                                    LocationListCustom nearestLocation = new LocationListCustom()
                                    {
                                        IdLocation = reader.GetInt32(0),
                                        Description = reader.GetString(1),
                                        ClosingTime = reader.GetInt32(2),
                                        OpenClosed = reader.GetString(3),
                                        DistanceInMiles = reader.GetInt32(4),

                                    };
                                    NearestLocations.Add(nearestLocation);
                                }

                            }
                        }
                    }
                    connection.Close();
                }
                //Only for the open locations calculate the estimated wait time
                if (NearestLocations.Any())
                {
                    foreach (var loc in NearestLocations)
                    {
                        if (loc.OpenClosed == "Opened")
                        {
                            loc.EstimatedWaitTime = (int)LocationListHelper.GetEstimatedWaitTime(loc.IdLocation).TotalMinutes;
                        }
                    }
                }

                return NearestLocations.OrderBy(s => s.DistanceInMiles).ToList();
            }
        }

        public static string GetLocationDescription(int id)
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                var location = db.Location.DefaultIfEmpty(null).FirstOrDefault(r => r.Id == id);
                return location != null ? location.Description : "All";
            }
        }

        public static TimeSpan GetEstimatedWaitTime(int idLocation)
        {
            TimeSpan estimatedWaitTime;
            try
            {
                using (TexellCheckInContext db = new TexellCheckInContext())
                {
                    // assigned location = permanent, idlocation = temporary location      
                    var location = db.Location.FirstOrDefault(l => l.Id == idLocation);
                    //Identify the location Close Time
                    DateTime currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
                    TimeSpan? locationCloseTime = MobileHelper.GetLocationCloseTime(idLocation, currentDateTime);
                    List<MobileEmployeeCustom> scheduleEmployees = MobileHelper.CalculateNextAvailableTimes(db, idLocation, currentDateTime, locationCloseTime);
                    MobileEmployeeCustom nextEmp = scheduleEmployees.OrderBy(r => r.NextAvailableTime).FirstOrDefault();
                    if (nextEmp != null)
                    {
                        estimatedWaitTime = nextEmp.NextAvailableTime - currentDateTime.TimeOfDay;
                    }
                    else
                    {
                        estimatedWaitTime = TimeSpan.MaxValue;
                    }

                    //If the estimated wait time exceeds the MaxWaitTime maintaince setting, then the MaxWaitTime is displayed to the user instead of the actual WaitTime
                    var MaxWaitTimeMaintainceSetting = db.MaintenanceRanges.FirstOrDefault(s => s.Description == "MaxWaitTime");
                    var MinimumWaitTimeSelfServeAdmin = db.SelfServeAdmin.FirstOrDefault().MinimumWaitTime;
                    if (MaxWaitTimeMaintainceSetting != null)
                    {
                        if (estimatedWaitTime.TotalMinutes > MaxWaitTimeMaintainceSetting.RangeEnd)
                        {
                            estimatedWaitTime = TimeSpan.FromMinutes(MaxWaitTimeMaintainceSetting.RangeEnd);
                        }
                    }
                    if (estimatedWaitTime.TotalMinutes < MinimumWaitTimeSelfServeAdmin)
                    {
                        estimatedWaitTime = TimeSpan.FromMinutes(MinimumWaitTimeSelfServeAdmin);
                    }
                }
            }
            catch (Exception)
            {

            }
            return estimatedWaitTime;
        }

        private static string GetConnectionString()
        {
            return Startup.ConfigurationInstance.GetConnectionString("DefaultConnection");
        }
    }
}
