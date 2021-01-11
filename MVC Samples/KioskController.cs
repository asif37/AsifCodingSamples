using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TexellCheckInKiosk.Helpers;
using Microsoft.Extensions.Localization;
using Microsoft.AspNetCore.Mvc.Localization;
using TexellCheckInKiosk.Resources;
using Microsoft.AspNetCore.Builder;
using TexellCheckInKiosk.Models.Custom;
using TexellCheckInKiosk.Utils;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Http;
using TexellCheckInKiosk.Models;
using Microsoft.AspNetCore.Authorization;
using System.Globalization;
using TexellCheckInKiosk.Constant;

namespace TexellCheckInKiosk.Controllers
{
    [Authorize]
    public class KioskController : Controller
    {
        private readonly IHtmlLocalizer<KioskController> _localizer;
        TexellCheckInContext db = new TexellCheckInContext();

        public KioskController(IHtmlLocalizer<KioskController> localizer)
        {
            _localizer = localizer;
        }
        public IActionResult Index()
        {
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture;

            //Validate that the currentCulture is a supportedCulture, otherwise change to default English
            var isSupportedCulture = LanguageHelper.IsSupportedCulture(currentCulture.Name);
            if (!isSupportedCulture)
            {
                return RedirectToAction("SetLanguage", "Kiosk", new { culture = "en-US", returnUrl = "Index" });
            }
            //Send to the view the language that is not the current culture so that it can be shown as an option to switch to. Currently only english and spanish is supported
            ViewBag.AvailableCulture = LanguageHelper.SupportedCultures().FirstOrDefault(r => r.cultureName != currentCulture.Name);
            return View();
        }

        public JsonResult GetKioskTimeout()
        {
            var adminSettings = db.SelfServeAdmin.FirstOrDefault();
            // session timeout is stored as seconds; convert to milliseconds for use in JavaScript
            var kiosk = (adminSettings.KioskSessionTimeOut) * 60000;
            return Json(kiosk);
        }

        [HttpGet]
        public IActionResult CustomerProfile(string FirstName = "", string LastName = "")
        {
            if (FirstName != "")
            {
                SelfServePersonCustom selfServePersonModel = new SelfServePersonCustom();
                selfServePersonModel.FirstName = FirstName;
                selfServePersonModel.LastName = LastName;
                return View(selfServePersonModel);
            }
            else
            {
                return View();
            }
        }

        [HttpPost]
        public IActionResult CustomerProfile(SelfServePersonCustom SelfServePersonModel)
        {
            int idSelfSrvPersonId = 0;
            string FullName = (SelfServePersonModel.FirstName + " " + SelfServePersonModel.LastName).ToString().ToUpper();
            string AccountPtn = "";
            if (SelfServePersonModel.AccountPTN != null)
            {
                if (!ValidateAccountNo(SelfServePersonModel.AccountPTN))
                {
                    ViewBag.InvalidMemberNo = "invalid Account No";
                    return View(SelfServePersonModel);
                }
            }

            try
            {
                HttpContext.Session.SetString("FullName", FullName);
                DateTime now = MaintainenceUtils.CurrentDateTime(DateTime.Now);
                SelfServePersonModel.idLocation = KioskUserHelper.GetKioskLocation(this.User.Identity.Name);
                HttpContext.Session.SetInt32("IdLocation", SelfServePersonModel.idLocation);

                if (!string.IsNullOrWhiteSpace(SelfServePersonModel.PhoneNumber))
                {
                    SelfServePersonModel.PhoneNumber = Regex.Replace(SelfServePersonModel.PhoneNumber, "[^0-9]", "");
                    AccountPtn = SelfServePersonModel.PhoneNumber;
                    HttpContext.Session.SetString("AccountPtn", AccountPtn);
                }
                else
                {
                    SelfServePersonModel.AccountPTN = Regex.Replace(SelfServePersonModel.AccountPTN, "[^0-9]", "");
                    AccountPtn = SelfServePersonModel.AccountPTN;
                    HttpContext.Session.SetString("AccountPtn", AccountPtn);
                }
                idSelfSrvPersonId = InteractionsHelper.InsertSelfServePerson(SelfServePersonModel);
                HttpContext.Session.SetInt32("IdSelfServePerson", idSelfSrvPersonId);
            }
            catch (Exception ex)
            {
                ExceptionHelper.Throw(ex);
                return View();
            }

            return Redirect("Reasons");
        }

        [HttpPost]
        public string CustomerProfileCard(SelfServePersonCustom SelfServePersonModel)
        {
            string message = "";
            try
            {
                DateTime now = MaintainenceUtils.CurrentDateTime(DateTime.Now);

                SelfServePersonModel.idLocation = KioskUserHelper.GetKioskLocation(this.User.Identity.Name);
                HttpContext.Session.SetInt32("IdLocation", SelfServePersonModel.idLocation);

                long? accountNum = InteractionsHelper.FindAccountOfCard(SelfServePersonModel);
                if (accountNum != null)
                {
                    SelfServePersonModel.AccountPTN = accountNum.ToString();
                    string FullName = (SelfServePersonModel.FirstName + " " + SelfServePersonModel.LastName).ToString();
                    string AccountPtn = SelfServePersonModel.AccountPTN;
                    HttpContext.Session.SetString("FullName", FullName);
                    HttpContext.Session.SetString("AccountPtn", AccountPtn);
                    int idSelfSrvPersonId = InteractionsHelper.InsertSelfServePerson(SelfServePersonModel);
                    HttpContext.Session.SetInt32("IdSelfServePerson", idSelfSrvPersonId);
                    return idSelfSrvPersonId.ToString();
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                ExceptionHelper.Throw(ex);
            }
            return (SelfServePersonModel.FirstName + " " + SelfServePersonModel.LastName).ToString();
        }

        [HttpGet]
        public IActionResult Reasons(int selfServePersonId = 0, bool onlyActive = false)
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            var idLocation = HttpContext.Session.GetInt32("IdLocation");
            if(idLocation == null)
            {
                idLocation = EmployeesHelper.GetEmployeeLocation(this.User.Identity.Name);
            }
            var Categories = SelfServeHelper.GetCategories(requestCulture, idLocation ?? 0, onlyActive);

            //if onlyActive is false then it means Reasons page is being presented for the first time and it is necessary 
            //to determine if there are activeReasons since if true the confirmation page will give the option to return 
            //to the Reasons page to be seen for an active reason. 
            if(onlyActive == false)
            {
                bool activeReasonExist = false;
                activeReasonExist = SelfServeHelper.ActiveReasonExist(Categories);

                HttpContext.Session.SetInt32("ActiveReasonExists",Convert.ToInt32(activeReasonExist));
            }
            return View(Categories);
        }

        [HttpGet]
        public IActionResult Confirmation()
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            var currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
            var idLanguage = LanguageHelper.GetCultureId(requestCulture);
            int activeInteractionId = HttpContext.Session.GetInt32("IdActiveInteraction") ?? 0;
            var activeInteraction = db.ActiveInteraction.FirstOrDefault(o => o.Id == activeInteractionId);
            int estimatedWait;
            int minWait = db.SelfServeAdmin.First().MinimumWaitTime;
            int maxWait = db.MaintenanceRanges.FirstOrDefault(r => r.Description == "MaxWaitTime").RangeEnd;

            estimatedWait = Convert.ToInt32(activeInteraction.EstimateServiceStart.TotalMinutes - currentDateTime.TimeOfDay.TotalMinutes);

            if (estimatedWait < minWait)
            {
                estimatedWait = minWait;
            }
            if (estimatedWait > maxWait)
            {
                estimatedWait = maxWait;
            }


            if (activeInteraction != null)
            {
                ViewBag.Estimatewait = estimatedWait;
                var getReason = db.ActiveInteractionReason.FirstOrDefault(s => s.IdInteraction == activeInteraction.Id);
                var reasonId = getReason != null ? getReason.IdReason : 0;
                ViewBag.IdActiveInteraction = activeInteractionId;
                @ViewBag.ReasonName = SelfServeHelper.GetReasonName(reasonId, idLanguage);
            }
            return View();
        }

        [HttpPost]
        public IActionResult ConfirmationSpecific(int IdActiveInteraction, int IdSelectedEmployee, int estimateWait)
        {
            // If the First Available option was selected, then don't assign an employee and redirect to the normal confirmation screen
            if (IdSelectedEmployee <= 0)
            {
                return RedirectToAction("Confirmation");
            }else
            {
                // Set the IdEmployee and EstimateWaitTime for the interaction
                var getresponse = SelfServeHelper.ConfirmSpecificEmployee(IdActiveInteraction, IdSelectedEmployee, estimateWait);

                // If the displayed EstimateWaitTime will be less than the MinimumWaitTime, display that instead
                int minWait = db.SelfServeAdmin.First().MinimumWaitTime;
                int maxWait = db.MaintenanceRanges.FirstOrDefault(r => r.Description == "MaxWaitTime").RangeEnd;

                if (estimateWait < minWait)
                {
                    estimateWait = minWait;
                }
                if (estimateWait > maxWait)
                {
                    estimateWait = maxWait;
                }


                getresponse.EstimateWaitTime = TimeSpan.FromMinutes(estimateWait);


                // Display the ConfirmationSpecific view
                return View(getresponse);
            }
        }
        [HttpGet]
        public IActionResult RequestEmployee(int IdActiveInteraction)
        {
            var idLocation = KioskUserHelper.GetKioskLocation(User.Identity.Name);
            var viewModel = SelfServeHelper.RequestEmployee(IdActiveInteraction, idLocation);
            return View(viewModel);
        }

        [HttpPost]
        public int SaveInteraction(int IdReason, string reasonEstimateWait)
        {
            int selfServePersonId = HttpContext.Session.GetInt32("IdSelfServePerson") ?? 0;
            if(selfServePersonId > 0)
            {
                var idLocation = KioskUserHelper.GetKioskLocation(User.Identity.Name);
                var idActiveInteraction = SelfServeHelper.SaveKioskInteraction(idLocation, selfServePersonId, reasonEstimateWait, IdReason);
                HttpContext.Session.SetInt32("IdActiveInteraction", idActiveInteraction);
                return idActiveInteraction;
            }
            else
            {
                return 0;
            }

        }

        public bool ValidateAccountNo(string AcountNo)
        {
            AcountNo = AcountNo.Replace("-", "");
            long acountNo = Convert.ToInt64(AcountNo);
            return db.Person.Any(o => o.AccountNumber == acountNo && (o.IsNonMember == false || o.IsNonMember == null));
        }

        [HttpPost]
        public void SaveConfirmation(string selfServePersonId, bool status = false)
        {
            var selfServePerson = db.SelfServePerson.FirstOrDefault(r => r.Id == Convert.ToInt32(selfServePersonId));
            if (status)
            {
                db.Remove(selfServePerson);
                db.SaveChanges();
            }
        }
        [HttpGet]
        public ActionResult CallbackConfirmation()
        {
            int IdSelfServePerson=Convert.ToInt32(HttpContext.Session.GetInt32("IdSelfServePerson"));
            ViewBag.haveActiveReason = HttpContext.Session.GetInt32("ActiveReasonExists");
            return View(IdSelfServePerson);
        }
        [HttpPost]
        public PartialViewResult _PartialReasons(List<SelfServeReasonCustom> model)
        {
            return PartialView("_PartialReasons", model);
        }


        [HttpPost]
        public bool SaveCallBack(string Phone, int IdReason, int IdCategory)
        {
            int IdSelfServePerson = Convert.ToInt32(HttpContext.Session.GetInt32("IdSelfServePerson"));
            SelfServeHelper.InsertCallback(Phone, IdReason, IdSelfServePerson,this.User.Identity.Name, IdCategory);
            return true;
        }

        //the selected culture and the url to return to once the culture is changed. 
        //1. it changes the culture by setting the CookieRequestCultureProvider 2. it refreshes the page where the change was made/requested
        [HttpGet]
        public ActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );

            if (returnUrl != null)
            {
                return RedirectToAction("Index");
            }
            else
            {
                return Json(true);
            }

        }
    }
}