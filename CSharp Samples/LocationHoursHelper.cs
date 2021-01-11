using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Kendo.Mvc.Extensions;
using TexellCheckInMobile.Models;
using TexellCheckInMobile.Models.MobileViewModel;

namespace TexellCheckInMobile.Helpers
{
    public static class LocationHoursHelper
    {
        public static LocationHoursViewModel GetLocationHours(int locationId)
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                LocationHoursViewModel viewModel = new LocationHoursViewModel();
                var location = db.Location.FirstOrDefault(r => r.Id == locationId);
                if (location != null)
                {
                    viewModel.LocationName = location.Description;

                    //Sunday
                    if (location.SundayStart != null && location.SundayEnd != null)
                    {
                        viewModel.SundayStart = DateTime.Today.Add(location.SundayStart.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                        viewModel.SundayEnd = DateTime.Today.Add(location.SundayEnd.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                    }
                    else
                    {
                        viewModel.SundayStart = "Closed";
                    }

                    //Monday
                    if (location.MondayStart != null && location.MondayEnd != null)
                    {
                        viewModel.MondayStart = DateTime.Today.Add(location.MondayStart.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                        viewModel.MondayEnd = DateTime.Today.Add(location.MondayEnd.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                    }
                    else
                    {
                        viewModel.MondayStart = "Closed";
                    }

                    //Tuesday
                    if (location.TuesdayStart != null && location.TuesdayEnd != null)
                    {
                        viewModel.TuesdayStart = DateTime.Today.Add(location.TuesdayStart.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                        viewModel.TuesdayEnd = DateTime.Today.Add(location.TuesdayEnd.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                    }
                    else
                    {
                        viewModel.TuesdayStart = "Closed";
                    }

                    //Wednesday
                    if (location.WednesdayStart != null && location.WednesdayEnd != null)
                    {
                        viewModel.WednesdayStart = DateTime.Today.Add(location.WednesdayStart.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                        viewModel.WednesdayEnd = DateTime.Today.Add(location.WednesdayEnd.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                    }
                    else
                    {
                        viewModel.WednesdayStart = "Closed";
                    }

                    //Thursday
                    if (location.ThursdayStart != null && location.ThursdayEnd != null)
                    {
                        viewModel.ThursdayStart = DateTime.Today.Add(location.ThursdayStart.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                        viewModel.ThursdayEnd = DateTime.Today.Add(location.ThursdayEnd.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                    }
                    else
                    {
                        viewModel.ThursdayStart = "Closed";
                    }

                    //Friday
                    if (location.FridayStart != null && location.FridayEnd != null)
                    {
                        viewModel.FridayStart = DateTime.Today.Add(location.FridayStart.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                        viewModel.FridayEnd = DateTime.Today.Add(location.FridayEnd.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                    }
                    else
                    {
                        viewModel.FridayStart = "Closed";
                    }

                    //Saturday
                    if (location.SaturdayStart != null && location.SaturdayEnd != null)
                    {
                        viewModel.SaturdayStart = DateTime.Today.Add(location.SaturdayStart.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                        viewModel.SaturdayEnd = DateTime.Today.Add(location.SaturdayEnd.Value).ToString(@"hh:mm tt", new CultureInfo("en-US"));
                    }
                    else
                    {
                        viewModel.SaturdayStart = "Closed";
                    }
                }

                return viewModel;
            }
        }
    }
}
