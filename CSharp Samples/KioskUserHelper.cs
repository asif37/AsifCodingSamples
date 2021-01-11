using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TexellCheckInKiosk.Models;

namespace TexellCheckInKiosk.Helpers
{
    public static class KioskUserHelper
    {
        /// <summary>
        /// Retrieves the id of the location of the kiosk user whose user email matches the passed parameter
        /// </summary>
        /// <param name="email"></param>
        /// <returns>The id of the kiosk user's location</returns>
        public static int GetKioskLocation(string email)
        {
            try
            {
                using (TexellCheckInContext db = new TexellCheckInContext())
                {
                    //Find the user based on the email address provided
                    var user = db.Users.FirstOrDefault(r => r != null && r.Email.ToUpper().Trim().Equals(email.ToUpper().Trim()));
                    if (user != null)
                    {
                        //Find the kiosk based on the user id
                        var kiosk = db.KioskUser.FirstOrDefault(r => r != null && r.UserId.Equals(user.Id) && !r.IsArchived);
                        if (kiosk != null)
                        {
                            // Return the kiosk's IdLocation
                            return kiosk.IdLocation;
                        }
                    }
                    // If no matching user or kiosk found, return an error value
                    return -1;
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.Throw(ex);
                return -1;
            }
        }

        /// <summary>
        /// Checks if the AspNetUser with the passed email has confirmed their email or not
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsEmailConfirmed(string email)
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                // Find the matching AspNetUser based on email address
                var user = db.Users.DefaultIfEmpty(null).FirstOrDefault(r => r != null && r.Email.Trim().ToLower().Equals(email.Trim().ToLower()));
                if (user != null)
                {
                    // Return whether user email is confirmed
                    return user.EmailConfirmed;
                }
                // If no matching user found, returns false
                return false;
            }
        }

        /// <summary>
        /// Checks if the user associated with the passed email is a valid Kiosk User or not
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsKioskUser(string email)
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                // Find the matching AspNetUser based on email address
                var user = db.Users.Include(s => s.Roles).DefaultIfEmpty(null).FirstOrDefault(r => r != null && r.Email.Trim().ToLower().Equals(email.Trim().ToLower()));
                if (user != null)
                {
                    // Return true if there is a KioskUser record matching the found AspNetUser and the record is not archived
                    return db.KioskUser.Any(r => r.UserId == user.Id && !r.IsArchived);
                }
                // If no match found in either table, returns false
                return false;
            }
        }
    }
}
