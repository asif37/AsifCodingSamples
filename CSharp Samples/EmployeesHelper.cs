using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using TexellCheckInKiosk.Models;
using TexellCheckInKiosk.Models.Custom;

namespace TexellCheckInKiosk.Helpers
{
    public static class EmployeesHelper
    {
        public static int GetEmployeeLocation(string email)
        {
            try
            {
                using (TexellCheckInContext db = new TexellCheckInContext())
                {
                    //Find the user based on the email address provided
                    var user = db.Users.DefaultIfEmpty(null).FirstOrDefault(r => r.Email.ToUpper().Trim().Equals(email.ToUpper().Trim()));
                    if (user != null)
                    {
                        //Find the employee based on the user id
                        var employee = db.Employee.DefaultIfEmpty(null).FirstOrDefault(r => r.UserId.Equals(user.Id));
                        if (employee != null)
                        {
                            return employee.IdAssignedLocation;
                        }
                    }
                }
                return -1;
            }
            catch (Exception ex)
            {
                ExceptionHelper.Throw(ex);
                return -1;
            }
        }

        public static int GetEmployeeQueue(string email)
        {
            try
            {
                using (TexellCheckInContext db = new TexellCheckInContext())
                {
                    //Find the user based on the email address provided
                    var user = db.Users.DefaultIfEmpty(null).FirstOrDefault(r => r.Email.ToUpper().Trim().Equals(email.ToUpper().Trim()));
                    if (user != null)
                    {
                        //Find the employee based on the user id
                        var employee = db.Employee.DefaultIfEmpty(null).FirstOrDefault(r => r.UserId.Equals(user.Id));
                        if (employee != null)
                        {
                            return employee.IdQueue;
                        }
                    }
                }
                return -1;
            }
            catch (Exception ex)
            {
                ExceptionHelper.Throw(ex);
                return -1;
            }
        }
    }
}
