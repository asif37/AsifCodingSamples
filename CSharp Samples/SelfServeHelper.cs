using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using TexellCheckInKiosk.Constant;
using TexellCheckInKiosk.Models;
using TexellCheckInKiosk.Models.Custom;
using TexellCheckInKiosk.Models.KioskViewModels;
using TexellCheckInKiosk.Utils;

namespace TexellCheckInKiosk.Helpers
{
    public static class SelfServeHelper
    {
        /// <summary>
        /// Kiosk category/reasons selection page. THis function will determine which reasons and categories can be displayed
        /// to the person based on the availability of employees with that specific skillset and the rules on each category/reason
        /// </summary>
        /// <param name="culture"></param>
        /// <param name="userId"></param>
        /// <returns>list of available categories and reasons</returns>
        public static List<SelfServeCategoryCustom> GetCategories(string culture, int idLocation, bool onlyActive = false)
        {

            //public static List<SelfServeCategoryCustom> GetCategories(string culture, int idLocation, bool onlyActive = false)
            {
                try
                {
                    using (TexellCheckInContext db = new TexellCheckInContext())
                    {
                        // Retrieve the id of the language with the matching language code, or return 1 (English) if there is no match
                        SelfServeLanguage defaultLanguage = LanguageUtils.GetDefaultLanguage();
                        int idLanguage = db.SelfServeLanguage.DefaultIfEmpty(defaultLanguage).FirstOrDefault(r => r.LanguageCode == culture).Id;

                        //filter categories by the categories setup for this location and convert to correct language
                        var categories =
                                         from selfServeCategory in db.SelfServeCategory
                                         join selfserveCategoryLocation in db.SelfServeCategoryLocation on selfServeCategory.Id equals selfserveCategoryLocation.IdSelfServeCategory
                                         join selfServeCategoryLanguage in db.SelfServeCategoryLanguage on selfServeCategory.Id equals selfServeCategoryLanguage.IdKioskCategory
                                         where selfserveCategoryLocation.IdLocation == idLocation && selfServeCategoryLanguage.IdLanguage == idLanguage
                                         select new SelfServeCategory
                                         {
                                             Description = selfServeCategoryLanguage.Description,
                                             IconUrl = selfServeCategory.IconUrl,
                                             Id = selfServeCategory.Id,
                                             IdQueue = selfServeCategory.IdQueue,
                                             IdQueueNavigation = selfServeCategory.IdQueueNavigation,

                                         };

                        // assigned location = permanent, idlocation = temporary location
                        // get location to identify closing time
                        var currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
                        TimeSpan? locationCloseTime = GetLocationCloseTime(idLocation, currentDateTime);

                        //build a list of employees scheduled for remainder of the day at the current location with their estimated next available time. This will be needed to determine which 
                        //categories and reasons can be serviced
                        var scheduleEmployees = CalculateNextAvailableTimes(db, idLocation, currentDateTime, locationCloseTime);

                        /*At this point we have a list of employees with the NextAvailableTime for each  employee. We can now take the skillset reasons of these employees and create a 
                         * new list of skillset reasons with EstimatedWaitTime for each reason*/
                        List<SelfServeReasonCustom> skillsetReasons = new List<SelfServeReasonCustom>();
                        if (scheduleEmployees.Any())
                        {
                            skillsetReasons = (
                               from employee in scheduleEmployees
                               join reason in db.EmployeeReason on employee.Id equals reason.IdEmployee
                               group employee by reason.IdReason into reasonGroup
                               select new SelfServeReasonCustom
                               {
                                   Id = reasonGroup.Key,
                                   EstimatedWait = reasonGroup.Min(p => p.NextAvailableTime).Subtract(currentDateTime.TimeOfDay),
                               }).ToList();
                        }
                        //Finally we can filter the categories and reasons that will be displayed
                        /*start by cross referencing reasons with skillsets to eliminate reasons that cannot be displayed because there are no matching skillsets to service the reason*/
                        List<SelfServeReasonCustom> reasonResults = new List<SelfServeReasonCustom>();
                        var allSelfServeReason = db.Reason.Where(r => r.IdSelfServeCategory != null);
                        foreach (var reason in allSelfServeReason)
                        {
                            //match the reason with a skillsetReasons record. 
                            var skillset = skillsetReasons.DefaultIfEmpty(null).FirstOrDefault(r => r != null && r.Id == reason.Id);
                            var multiLanguage = db.ReasonLanguage.FirstOrDefault(r => r.IdReason == reason.Id && r.IdLanguage == idLanguage);

                            /*For each reason determine if a skillset match was found meaning that an employee is available (now or in future) to service this reason. If true also check 
                             * if the estimated wait for this reason does not exceed the max wait or the max wait is disabled. If both conditions are true then add the reason to the result 
                             * list that will be displayed on the page*/
                            if (skillset != null && (reason.MaxWait == null || reason.MaxWait == 0 || skillset.EstimatedWait.TotalMinutes <= reason.MaxWait))
                            {
                                reasonResults.Add(new SelfServeReasonCustom
                                {
                                    Id = reason.Id,
                                    Description = multiLanguage != null ? multiLanguage.Description : reason.Description,
                                    EstimatedWait = skillset.EstimatedWait,
                                    CategoryId = reason.IdSelfServeCategory ?? 0,
                                    Active = true,
                                    EnableCallback = reason.EnableCallback.Value
                                });
                            }
                            /*if no skillset was found meaning no employee is available to service this reason or if the estimatedWait exceeds the max wait 
                             * then determine if the reason will be shown as a callback or not shown at all*/
                            else if (reason.EnableCallback != null && reason.EnableCallback == true)
                            {
                                reasonResults.Add(new SelfServeReasonCustom
                                {
                                    Id = reason.Id,
                                    Description = multiLanguage != null ? multiLanguage.Description : reason.Description,
                                    EstimatedWait = TimeSpan.Zero,
                                    CategoryId = reason.IdSelfServeCategory ?? 0,
                                    Active = false,
                                    EnableCallback = reason.EnableCallback.Value
                                });
                            }
                        }

                        /*Now we can filter the categories. Categories must have atleast one reason that will be displayed. If ActiveOnly = True then only return Categories that have active reasons*/
                        List<SelfServeCategoryCustom> categoryResults = new List<SelfServeCategoryCustom>();
                        foreach (var category in categories.ToList())
                        {
                            List<SelfServeReasonCustom> categoryReasons = new List<SelfServeReasonCustom>();

                            //if only categories with active reasons are to be displayed that only pull active reasons
                            if (onlyActive)
                            {
                                categoryReasons = reasonResults.Where(o => o.CategoryId == category.Id && o.Active == true).ToList();
                            }
                            else
                            {
                                categoryReasons = reasonResults.Where(o => o.CategoryId == category.Id).ToList();
                            }

                            if (categoryReasons.Any())
                            {
                                categoryResults.Add(new SelfServeCategoryCustom
                                {
                                    Id = category.Id,
                                    Description = category.Description,
                                    IconUrl = category.IconUrl,
                                    reasons = categoryReasons.ToList()
                                });
                            }
                        }
                        return categoryResults;
                    }
                }
                catch (Exception ex)
                {
                    ExceptionHelper.Throw(ex);
                    return new List<SelfServeCategoryCustom>();
                }
            }
        }

        public static bool ActiveReasonExist(List<SelfServeCategoryCustom> categories)
        {
            bool activeReasonExist = false; 
            if(categories != null && categories.Any())
            {
                activeReasonExist = categories.Any(r => r.reasons.Any(x => x.Active == true));
            }
            return activeReasonExist;
        }
        /// <summary>
        /// This function saves the interaction to the Active Interaction table once the reason has been selected. 
        /// </summary>
        /// <param name="idLocation"></param>
        /// <param name="CheckinDate"></param>
        /// <param name="idSelfServePerson"></param>
        /// <param name="reasonEstimateWait"></param>
        /// <param name="idReason"></param>
        /// <returns></returns>
        public static int SaveKioskInteraction(int idLocation, int idSelfServePerson, string reasonEstimateWait, int idReason)
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                //datetime
                var currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
                var selfServePerson = db.SelfServePerson.FirstOrDefault(r => r.Id == idSelfServePerson);
                // get reason
                var selectedReason = db.Reason.FirstOrDefault(s => s.Id == idReason);
                var idQueue = selectedReason.IdQueue;
                //get reasonCombination. ReasonCombination contains the ActualEstimatedWait time that we need for the selected reason
                var estimatedServiceTime = db.ReasonCombination.FirstOrDefault(r => r.Description == selectedReason.Id.ToString()).AverageServiceTime;


                if (selfServePerson != null)
                {
                    selfServePerson.EstimatedWait = TimeSpan.Parse(reasonEstimateWait);
                    selfServePerson.IdReason = idReason;
                    selfServePerson.IdQueue = idQueue;
                    db.SaveChanges();
                }

                ActiveInteraction activeInteraction = new ActiveInteraction();
                activeInteraction.IdLocation = idLocation;
                activeInteraction.IdCheckinType = Convert.ToInt32(Constant.CheckinType.Lobby);
                activeInteraction.IdSelfServePersonEntryMethod = selfServePerson.EntryMethod;
                activeInteraction.CheckinDate = currentDateTime.Date;
                activeInteraction.CheckinTime = currentDateTime.TimeOfDay;

                activeInteraction.IdQueue = idQueue;
                activeInteraction.EstimateServiceStart = currentDateTime.TimeOfDay + TimeSpan.Parse(reasonEstimateWait);
                activeInteraction.EstimateServiceTime = estimatedServiceTime;
                ///end 

                if (selfServePerson.IdPerson != null && selfServePerson.IdPersonName != null)
                {
                    // Create active interaction for idPerson and IdPersonName not null
                    activeInteraction.IdPerson = selfServePerson.IdPerson ?? 0;
                    activeInteraction.IdPersonName = selfServePerson.IdPersonName ?? 0;
                    //Delete SelfServePerson records from database
                    db.Remove(selfServePerson);
                    db.SaveChanges();
                }
                else
                {
                    // Create active interaction for idPerson and IdPersonName with null
                    activeInteraction.IdSelfServePerson = selfServePerson.Id;   //id from self serve person table                    
                }
                db.Add(activeInteraction);
                db.SaveChanges();
                ///add activeinteraction Reason
                db.ActiveInteractionReason.Add(new ActiveInteractionReason()
                {
                    IdInteraction = activeInteraction.Id,
                    IdReason = idReason
                });
                ///end activeinteraction Reason
                db.SaveChanges();
                return activeInteraction.Id;
            }
        }

        /// <summary>
        /// Function is called on the last screen if the person has requested to see a specific employee
        /// </summary>
        /// <param name="IdActiveInteraction"></param>
        /// <param name="IdLocation"></param>
        /// <returns></returns>
        public static RequestEmployeeViewModel RequestEmployee(int IdActiveInteraction, int IdLocation)
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                //Get the reason selected for the interaction
                var idSelectedReason = db.ActiveInteractionReason.FirstOrDefault(s => s.IdInteraction == IdActiveInteraction).IdReason;

                // get location to identify closing time
                var currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
                TimeSpan? locationCloseTime = GetLocationCloseTime(IdLocation, currentDateTime);

                //determine the employees that are available to service this interaction based on location and determine the next available time for each employee
                var scheduleEmployees = CalculateNextAvailableTimes(db, IdLocation, currentDateTime, locationCloseTime, IdActiveInteraction);

                /* At this point we have a list of employees with the NextAvailableTime for each employee.
                 * We can now build the list of employees that have the skillset for this reason and display with the Estimated Wait Time */
                if (scheduleEmployees.Any())
                {
                    //Prepare the list of employees that will be displayed to the end user
                    var requestEmployees = new List<RequestedEmployeeCustom>();

                    //Find the earliest NextAvailableTime as this will become the First Available option
                    var actualFirstAvailableTime = scheduleEmployees.DefaultIfEmpty(null).Min(s => s.NextAvailableTime);

                    //Retrieve the SelfServeAdmin settings as we'll be using several of its fields here
                    var selfServeAdmin = db.SelfServeAdmin.FirstOrDefault();

                    //Find the MinimumWaitTime. This is the minimum amount of time in minutes that can be shown to the user
                    var minWait = selfServeAdmin.MinimumWaitTime;
                    //Calculate the cutoff time for the minimum wait
                    var minWaitCutoffTime = currentDateTime.TimeOfDay + TimeSpan.FromMinutes(minWait);
                    //Calculate the minimum displayed firstAvailableTime, ensuring it is at least the minimum wait time
                    var displayedFirstAvailableTime = (actualFirstAvailableTime < minWaitCutoffTime) ? minWaitCutoffTime : actualFirstAvailableTime;


                    //Find the WaitTimeBuffer. This is the minimum amount of time allowed between the firstNextAvailableTime and any employee's next available time as shown on the page
                    var waitEstimateBuffer = selfServeAdmin.WaitEstimateBuffer;
                    //If any employee has a next available time less than the waitEstimateBuffer then it will default to minWaitEstimate
                    var minNextWaitEstimateTime = actualFirstAvailableTime + TimeSpan.FromMinutes(waitEstimateBuffer);

                    //This is the max wait that an employee can have in order to be shown on the employee selection list. For example, if max wait is 30 min and for Employee John his wait time is
                    //60 min, because he exceeds the max he will not be shown on the page
                    int requestEmployeeMaxWait = selfServeAdmin.RequestEmployeeMaxWait;
                    //Calculate the actual cutoff time from the current time and the maxWait setting
                    var maxWaitCutoffTime = currentDateTime.TimeOfDay + TimeSpan.FromMinutes(requestEmployeeMaxWait);

                    //Go through all scheduled employees and identify if they have the skillset for this interaction. if false then the employee will not be added to the final list. 
                    foreach (var emp in scheduleEmployees.ToList())
                    {
                        // We only display employees who will be available before the max wait time and who have the skillset for this interaction
                        if (emp.NextAvailableTime <= maxWaitCutoffTime && EmployeeMatchSkillSet(IdActiveInteraction, emp.Id))
                        {
                            // Calculate the available time which will be used for display
                            // This is different from the actual next available time, which is used on the employee-facing side
                            var displayedAvailableTime = emp.NextAvailableTime;

                            // If the next available time is less than the minimum estimated wait, that will be displayed instead
                            if (displayedAvailableTime < minNextWaitEstimateTime)
                            {
                                displayedAvailableTime = minNextWaitEstimateTime;
                            }

                            // Determine the actual estimated wait by calculating the wait time in minutes between now and the actual next available time
                            var actualEstimatedWait = (int)Math.Round((emp.NextAvailableTime - currentDateTime.TimeOfDay).TotalMinutes);

                            // Determine the estimated wait to display to the user by calculating the amount of wait time in minutes between now and the displayed available time
                            var displayedEstimatedWait = (int)Math.Round((displayedAvailableTime - currentDateTime.TimeOfDay).TotalMinutes);

                            // Get the actual employee record in order to get first and last name
                            var employee = db.Employee.FirstOrDefault(r => r.Id == emp.Id);

                            // Add the employee information to the final displayed list
                            requestEmployees.Add(new RequestedEmployeeCustom()
                            {
                                Id = employee.Id,
                                FirstName = employee.FirstName,
                                LastName = employee.LastName,
                                ActualEstimatedWait = actualEstimatedWait,
                                DisplayedEstimatedWait = displayedEstimatedWait
                            });
                        }
                    }

                    // Determine the actual estimated wait by calculating the amount of wait time in minutes between now and the actual first available time
                    var actualFirstEstimatedWait = (int)Math.Round((actualFirstAvailableTime - currentDateTime.TimeOfDay).TotalMinutes);

                    // Determine the estimated wait to display to the user by calculating the amount of wait time in minutes between now and the displayed first available time
                    var displayedFirstEstimatedWait = (int)Math.Round((displayedFirstAvailableTime - currentDateTime.TimeOfDay).TotalMinutes);

                    // Create the view model using the data we've prepared
                    var viewModel = new RequestEmployeeViewModel()
                    {
                        Employees = requestEmployees,
                        ActualFirstWaitTime = actualFirstEstimatedWait,
                        DisplayedFirstWaitTime = displayedFirstEstimatedWait,
                        IdActiveInteraction = IdActiveInteraction,
                    };
                    return viewModel;
                }
                else
                {
                    // There are no employees scheduled; display an empty list

                    //Find the MinimumWaitTime. This is the minimum amount of time in minutes that can be shown to the user
                    var minWait = db.SelfServeAdmin.FirstOrDefault().MinimumWaitTime;

                    // Display no employees and a first available time of the minimum time
                    var viewModel = new RequestEmployeeViewModel()
                    {
                        Employees = new List<RequestedEmployeeCustom>(),
                        ActualFirstWaitTime = minWait,
                        DisplayedFirstWaitTime = minWait,
                        IdActiveInteraction = IdActiveInteraction
                    };
                    return viewModel;
                }
            }
        }

        public static InteractionCustom ConfirmSpecificEmployee(int IdActiveInteraction, int IdEmployee, int estimateWait)
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                //get activeInteraction for update emplyee id
                var getActiveInteraction = db.ActiveInteraction.Include(s => s.IdEmployeeNavigation).FirstOrDefault(f => f.Id == IdActiveInteraction);
                var interactionCustom = new InteractionCustom();
                if (getActiveInteraction != null)
                {
                    interactionCustom.EstimateWaitTime = TimeSpan.FromMinutes(estimateWait);

                    //get employee which selected
                    var getemployee = db.Employee.FirstOrDefault(s => s.Id == IdEmployee);
                    interactionCustom.EmployeeName = getemployee.FirstName + " " + getemployee.LastName;
                    getActiveInteraction.IdEmployee = IdEmployee;
                    getActiveInteraction.IdAssignReason = 1;//1 mean requested
                    //set Estimate sevice start
                    var currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
                    getActiveInteraction.EstimateServiceStart = (TimeSpan)(TimeSpan.FromMinutes(estimateWait) + currentDateTime.TimeOfDay);
                    db.SaveChanges();
                }
                return interactionCustom;
            }
        }

        /// <summary>
        /// Calculate the next available time for each scheduled employee. This is done by calculating the estimated service time 
        /// for each in-service interaction then adding any upcoming schedule event. Next we build a  theoretical queue using Waiting
        /// interactions and assign them to each employee based on skillset. While assigning interactions we add schedule events as they
        /// are scheduled to occur. THe end result is an estimated next available time taking into consideration in-service and waiting interactions
        /// and schedule events. 
        /// </summary>
        /// <param name="db"></param>
        /// <param name="idLocation"></param>
        /// <param name="currentDateTime"></param>
        /// <param name="locationCloseTime"></param>
        /// <param name="activeInteractionId"></param>
        /// <returns></returns>
        public static List<SelfServeEmployeeCustom> CalculateNextAvailableTimes(
            TexellCheckInContext db, int idLocation, DateTime currentDateTime, TimeSpan? locationCloseTime, int activeInteractionId = 0)
        {
            //Employee Schedule Variance is a buffer of time that is used when considering to eliminate an employee from the results of this function. 
            //If an employee exceeds their schedule on the Next Avaialble Time the buffer is added to allow some variance
            var empSchVariance = db.SelfServeAdmin.Select(x => x.EmployeeScheduleVariance).FirstOrDefault();
            List<SelfServeEmployeeCustom> scheduleEmployees = new List<SelfServeEmployeeCustom>();

            //Select the scheduled employees for this location that have not finished their schedule or are closers
            List<Employee> employees = (from employee in db.Employee
                                        join schedule in db.Schedule on employee.Id equals schedule.IdEmployee
                                        where schedule.IdLocation == idLocation &&
                                        schedule.Date == currentDateTime.Date && (schedule.EndTime >= currentDateTime.TimeOfDay || schedule.EndTime >= locationCloseTime)
                                        select employee).ToList();

            //for each scheduled employee determine the next available time 
            foreach (var emp in employees)
            {
                //default NextAvailableTime to zero
                TimeSpan NextAvailableTime = TimeSpan.Zero;
                SelfServeEmployeeCustom employee = new SelfServeEmployeeCustom();
                employee.Id = emp.Id;
                employee.Schedules = db.ActiveSchedule.FirstOrDefault(r => r.IdEmployee == emp.Id && r.Date == currentDateTime.Date);

                /*Set NextAvailableTime equal to Now if the schedule is in progress or the StartTime of the schedule if it is in the future*/
                if (employee.Schedules != null)
                {
                    if (employee.Schedules.StartTime > currentDateTime.TimeOfDay)
                    {
                        NextAvailableTime = employee.Schedules.StartTime;
                    }
                    else
                    {
                        NextAvailableTime = currentDateTime.TimeOfDay;

                        //Only for employees who have already started their schedule check for an active interaction.
                        /*Go to Interaction table and find any Active Interactions 
                        If Employee is currently in service with an Active Interaction:
                        Find the Active Interaction and identify the Interaction Start Time 
                        Add the average Service Time for the Reason(s) to the Interaction Start Time to determine Estimated Service End Time. */

                        var activeInteraction = db.ActiveInteraction.FirstOrDefault(x => x.IdEmployee == employee.Id && x.ServiceStart != null && x.ServiceEnd == null);

                        /*If the active interaction has already exceeded the estimated service end then give the interaction 5 minutes to be completed. Otherwise set NextAvailableTime to the estimated end of the interaction*/

                        if (activeInteraction != null)
                        {
                            if ((activeInteraction.ServiceStart + activeInteraction.EstimateServiceTime) <= currentDateTime.TimeOfDay)
                            {
                                NextAvailableTime = currentDateTime.AddMinutes(5).TimeOfDay;
                            }
                            else
                            {
                                NextAvailableTime = (activeInteraction.ServiceStart.Value + activeInteraction.EstimateServiceTime);
                            }
                        }
                    }
                }

                /*find all unavailable schedule events remaining for the day and save them with the employee. Order by latest to earliest*/
                if (employee.Schedules != null)
                {
                    employee.ScheduleEvents = db.ActiveScheduleEvents.Where(o => o.IdSchedule == employee.Schedules.Id && o.EndTime > NextAvailableTime).OrderByDescending(o => o.StartTime).ToList();
                }

                /*determine if employee has an unavailable event in progress, about to start, or past due. If any is true then offset NextAvailableTime by the duration of the unavailable event. 
                 * Since it is possible that unavailable events are stacked back to back we have to loop through them until there is a break between the last end time and the next start time*/

                if (employee.ScheduleEvents.Any())
                {
                    //in-progress/future schedule events were previously organized in latest to earliest order. Now we loop through them in reverse order which means we access them earliest to latest. 
                    employee.ScheduleEvents.Reverse();
                    foreach (var nextScheduleEvent in employee.ScheduleEvents.ToList())
                    {
                        var nextAvailablePlus5 = NextAvailableTime.Add(TimeSpan.FromMinutes(5));
                        if (nextScheduleEvent.StartTime <= nextAvailablePlus5 && nextScheduleEvent.EndTime > NextAvailableTime)
                        {
                            NextAvailableTime = nextScheduleEvent.EndTime;
                            employee.ScheduleEvents.Remove(nextScheduleEvent);
                        }
                        /*break the for each loop once we reach a schedule event that is not consecutive with NextAvailableTime. Meaning there is an available time in the schedule*/
                        else
                        {
                            break;
                        }
                    }
                }
                /*Build the list of schedule employees and their next available time by adding employees whose next available time does not exceed their schedule or are closers*/
                if (employee.Schedules != null)
                {
                    var estimateEmpSchEndTime = DateTime.Now.Date.Add(employee.Schedules.EndTime).AddMinutes(empSchVariance);
                    if ((NextAvailableTime < estimateEmpSchEndTime.TimeOfDay) || (employee.Schedules.EndTime >= locationCloseTime))
                    {
                        employee.NextAvailableTime = NextAvailableTime;
                        scheduleEmployees.Add(employee);
                    }
                }
            }

            /*Assign waiting interactions to employees. Each time we find the employee
                   With the earliest NextAvailableTime assign the interaction to this e	mployee, extend the NextAvailableTime by the estimatedServiceTime and search for any
                   Unvailable vent that will extend the NextAvailableTime further. */

            var waitingIntractions = db.ActiveInteraction.Where(o => o.IdLocation == idLocation && o.CheckinTime != null && o.ServiceStart == null && o.ServiceEnd == null)
                                                                                .OrderBy(s => s.ForceNext != null && s.ForceNext == 1).ThenBy(s => s.CheckinTime).ToList();

            /*If this function is used to calculate the employee available time on the Select Specific Employee page of Kiosk it means 
              that the interaction has already been created and therefore we must exclude the target interaction from being considered in the 
              next available time calculation*/
            if (activeInteractionId > 0)
            {
                waitingIntractions = waitingIntractions.Where(s => s.Id != activeInteractionId).ToList();
            }
            //distribute the waiting interactions to the next available employee. In this step we build a theoretical queue
            foreach (var interaction in waitingIntractions)
            {
                var employee = new SelfServeEmployeeCustom();
                /*If the interaction has an assigned employee then we must assign the interaction to this employee*/
                if (interaction.IdEmployee != null)
                {
                    employee = scheduleEmployees.FirstOrDefault(o => o.Id == interaction.IdEmployee);
                }
                //if there is no assigned employee then we assign the interaction to the next available employee with matching skillset
                else
                {
                    foreach (var emp in scheduleEmployees.OrderBy(o => o.NextAvailableTime))
                    {
                        /*there is an existing stored procedure that will determine if the employee Has the skillset to service the interaction.*/
                        if (EmployeeMatchSkillSet(interaction.Id, emp.Id))
                        {
                            employee = scheduleEmployees.DefaultIfEmpty(null).FirstOrDefault(o => o.Id == emp.Id);
                            break;
                        }
                    }
                }
                //Extend the employee's next available time by the estimated service time for this interaction
                if (employee != null)
                {
                    employee.NextAvailableTime = employee.NextAvailableTime + interaction.EstimateServiceTime;

                    /*check if NextAvailableTime should be extended for any upcoming unavailable event*/
                    if (employee.ScheduleEvents.Any())
                    {
                        //Loop thorugh all future schedule events from earliest to latest. This will allow us to find the next schedule event closest to the employee's
                        //calculated next available time. If the next schedule event was exceeded or in-progress based on the next available time then we must extend 
                        //the next avialable time by the schedule event duration. 
                        employee.ScheduleEvents.OrderByDescending(r => r.StartTime);
                        employee.ScheduleEvents.Reverse();
                        foreach (var nextUnavailableEvent in employee.ScheduleEvents.ToList())
                        {
                            if (nextUnavailableEvent.StartTime <= employee.NextAvailableTime.Add(TimeSpan.FromMinutes(5)))
                            {
                                employee.NextAvailableTime = employee.NextAvailableTime + (nextUnavailableEvent.EndTime - nextUnavailableEvent.StartTime);
                                employee.ScheduleEvents.Remove(nextUnavailableEvent);
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    var estEmpSchEndTime = DateTime.Now.Date.Add(employee.Schedules.EndTime).AddMinutes(empSchVariance);
                    //At this point we have calculated the estimated next available time for each employee based on in-service and waiting interactions and 
                    // all future schedule events. If the Next available time exceeds the employees schedule + error variance (exclude closers) then we must remove the employee
                    // from the results. 
                    if (employee.NextAvailableTime >= estEmpSchEndTime.TimeOfDay && (employee.Schedules.EndTime < locationCloseTime))
                    {
                        scheduleEmployees.Remove(employee);
                    }
                }
            }
            return scheduleEmployees;
        }
        public static string GetReasonName(int IdReason, int IdLanguage)
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                //if IdLanguage is undefined set default to english
                if(IdLanguage == 0)
                {
                    IdLanguage = Convert.ToInt32(Utils.LanguageUtils.Language.English);
                }
                var getReasonLanguage = db.ReasonLanguage.FirstOrDefault(s => s.IdLanguage == IdLanguage && s.IdReason == IdReason);
                if (getReasonLanguage != null)
                {
                    return getReasonLanguage.Description;
                }
                return "";
            }
        }
        public static bool EmployeeMatchSkillSet(int interActionId, int IdEmployee)
        {
            using (SqlConnection conn = new SqlConnection(GetConnectionString()))
            {
                conn.Open();
                if (conn.State == System.Data.ConnectionState.Open)
                {
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Connection = conn;
                        command.CommandTimeout = 300;
                        command.CommandText = "sp_employeematchskillset";
                        command.CommandType = System.Data.CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@IdInteraction", interActionId);
                        command.Parameters.AddWithValue("@IdEmployee", IdEmployee);

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader != null && reader.Read() && reader.FieldCount > 0)
                            {
                                return reader.IsDBNull(0) ? false : reader.GetBoolean(0);
                            }
                        }
                    }
                }
            }
            return false;
        }
        public static void InsertCallback(string Phone, int IdReason, int IdSelfServePerson, string name, int IdCategory)
        {
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                int idlocation = KioskUserHelper.GetKioskLocation(name);
                DateTime currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
                TimeSpan nowTime = MaintainenceUtils.CurrentDateTime(DateTime.Now).TimeOfDay;

                Reason selectReason = db.Reason.FirstOrDefault(r => r.Id == IdReason);

                SelfServeCallback callback = new SelfServeCallback();
                var SelfServePerson = db.SelfServePerson.Where(r => r.Id == IdSelfServePerson).FirstOrDefault();
                callback.FirstName = SelfServePerson.FirstName;
                callback.LastName = SelfServePerson.LastName;
                callback.PhoneNumber = Phone;
                callback.Account = SelfServePerson.AccountPtn;
                callback.CheckInTime = nowTime;
                callback.IdLocation = idlocation;
                callback.IdQueue = selectReason.IdQueue;
                callback.Reason = selectReason.Description;
                callback.Date = currentDateTime.Date;
                db.SelfServeCallback.Add(callback);
                db.SaveChanges();

                //check callbackEmail is null if not null then send email
                if (selectReason.CallBackEmail != null)
                {
                    var message = "A user has requested a call back. Details:<br><table>";
                    message = message + "<tr><td>Name</td><td>" + callback.FirstName + " " + callback.LastName + "</td></tr>";
                    message = message + "<tr><td>Phone Number</td><td>" + callback.PhoneNumber + "</td></tr>";
                    message = message + "<tr><td>Account</td><td>" + callback.Account + "</td></tr>";
                    message = message + "<tr><td>Check-In Time</td><td>" + callback.CheckInTime + "</td></tr>";
                    message = message + "<tr><td>Category</td><td>" + callback.Reason + "</td></tr></table>";
                    SendEmailHelper.SendEmail("admin@texell.org", selectReason.CallBackEmail, "Call Back Request", message);
                }
            }
        }

        /// <summary>
        /// Finds and retrieves the time of day that a location closes, based on the current day of the week
        /// </summary>
        /// <param name="idLocation"></param>
        /// <param name="currentDateTime"></param>
        /// <returns></returns>
        public static TimeSpan? GetLocationCloseTime(int idLocation, DateTime currentDateTime)
        {
            try
            {
                using (TexellCheckInContext db = new TexellCheckInContext())
                {
                    // Get the location record by id
                    var location = db.Location.FirstOrDefault(r => r.Id == idLocation);

                    TimeSpan? locationCloseTime = TimeSpan.Zero;

                    // Get the closing time from the correct field based on the day of the week
                    switch (currentDateTime.DayOfWeek)
                    {
                        case DayOfWeek.Sunday:
                            locationCloseTime = location.SundayEnd;
                            break;
                        case DayOfWeek.Monday:
                            locationCloseTime = location.MondayEnd;
                            break;
                        case DayOfWeek.Tuesday:
                            locationCloseTime = location.TuesdayEnd;
                            break;
                        case DayOfWeek.Wednesday:
                            locationCloseTime = location.WednesdayEnd;
                            break;
                        case DayOfWeek.Thursday:
                            locationCloseTime = location.ThursdayEnd;
                            break;
                        case DayOfWeek.Friday:
                            locationCloseTime = location.FridayEnd;
                            break;
                        case DayOfWeek.Saturday:
                            locationCloseTime = location.SaturdayEnd;
                            break;
                    }
                    return locationCloseTime;
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.Throw(ex);
                return TimeSpan.Zero;
            }
        }

        private static string GetConnectionString()
        {
            return Startup.ConfigurationInstance.GetConnectionString("DefaultConnection");
        }


    }
}
