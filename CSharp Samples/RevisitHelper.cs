using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Device.Location;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TexellCheckInMobile.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace TexellCheckInMobile.Helpers
{
    public static class RevisitHelper
    {
      
        public static void CancelActiveInteraction(int idActiveInteraction)
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                var activeInteraction = db.ActiveInteraction.SingleOrDefault(a => a.Id == idActiveInteraction);
                //For Cancel Interaction Update the service Start,Service End Time
                if (activeInteraction != null)
                {
                    try
                    {
                        activeInteraction.ServiceStart = DateTime.Now.TimeOfDay;
                        activeInteraction.ServiceEnd = TimeSpan.Zero;
                        db.SaveChanges();
                    }
                    catch (Exception ex)
                    {
                        ExceptionHelper.Throw(ex);
                    }
                }
            }
        }
    }
}
