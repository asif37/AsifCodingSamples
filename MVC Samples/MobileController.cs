using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using TexellCheckInMobile.Helpers;
using Microsoft.AspNetCore.Localization;
using TexellCheckInMobile.Models;
using TexellCheckInMobile.Models.MobileViewModel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using TexellCheckInMobile.Utils;
using System.Globalization;
using Microsoft.AspNetCore.Mvc.Localization;
using Microsoft.AspNetCore.Builder;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;

namespace TexellCheckInMobile.Controllers
{
    public class MobileController : Controller
    {
        //setup _localizer object that will be used throughout the controller to access the localizer
        private readonly IHtmlLocalizer<MobileController> _localizer;

        private readonly IConfiguration _configuration;

        //This method is necessary to intialize the localizer object
        //public MobileController(IHtmlLocalizer<MobileController> localizer, IConfiguration configuration)
        //{
        //    _localizer = localizer;
        //    _configuration = configuration;

        //}
        public MobileController(IConfiguration configuration)
        {
            _configuration = configuration;

        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult> About()
        {
            ViewBag.Key = _configuration["secrets/PowerBranchMobileSSOKey"];
            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            var secret = await keyVaultClient.GetSecretAsync("https://texellcheckin.vault.azure.net/secrets/PowerBranchMobileSSOKey").ConfigureAwait(false);
            ViewBag.Secret = secret.Value;
            //ViewBag.Secret = secret.Value;

            return View();
        }

        /// <summary>
        /// Method is called when the Login page is opened
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            HttpContext.Session.Clear();
            LogInViewModel viewModel = new LogInViewModel();
            var currentCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture;

            //Validate that the currentCulture is a supportedCulture, otherwise change to default English
            var isSupportedCulture = LanguageHelper.IsSupportedCulture(currentCulture.Name);
            if (!isSupportedCulture)
            {
                return RedirectToAction("SetLanguage", "Mobile", new { culture = "en-US", returnUrl = "Login" });
            }
            //retrieve the current culture so it can be shown in the language drop-down
            viewModel.SelectedCultureName = currentCulture.Name;
            return View(viewModel);
        }
        public ActionResult LogInFromMobileAppSSO(string acctNumber, string firstName, string lastName, string SSN)
        {
            // here we are getting HashAuthorization value and matching with own algo either it's fine or not
            string CustomAuth = Request.Headers["X-FI-Custom-Auth"];
            string PreparedataForHASH = Request.Method + ";" + Request.Path + ";" + (acctNumber + firstName + lastName + SSN).GetHashMd5();
            string query = Request.QueryString.ToString();
            string hash = PreparedataForHASH.Encrypt256();
            if (hash.Equals(CustomAuth))
            {
                var IsAuthenticated = LogInHelper.LoginWithAccountNumberAndSSN(acctNumber, SSN);

                LogInViewModel loginViewModel = new LogInViewModel
                {
                    FirstName = firstName,
                    LastName = lastName,
                    AccountPhoneNumber = acctNumber
                };
                var selfServePersonId = Convert.ToInt16(LogInHelper.AddSelfServePersonOrRevisitInteraction(loginViewModel));
                HttpContext.Session.SetInt32("PersonId", selfServePersonId);
                return View();
            }
            else
                return RedirectToAction("Login");
        }
        [HttpPost]
        public ActionResult LogInFromMobileApp()
        {
            //string CustomAuth = Request.Headers["X-FI-Custom-Auth"];
            //string PreparedataForHASH = Request.Method + ";" + Request.Path + ";" + (acctNumber + firstName + lastName + SSN).GetHashMd5();
            //string query = Request.QueryString.ToString();
            //string hash = PreparedataForHASH.Encrypt256();
            string body =string.Empty;            
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                body = reader.ReadToEndAsync().Result;
            }

            //var aes = Aes.Create();
            //aes.ValidKeySize(256);
            //aes.GenerateKey();
            //aes.GenerateIV();

            //Console.WriteLine(aes.Key.ToString());
            //Console.WriteLine(aes.IV.ToString());

            //return Json(aes.Key.ToString() + " " + aes.IV.ToString());

            var bodyContents = body.Split("&");
            var AccountNumber = bodyContents[0].Split("=")[1];
            var SSN = bodyContents[1].Split("=")[1];
            //get last 4 digit
            SSN= SSN.Substring(SSN.Length - 4);
            var firstName = bodyContents[2].Split("=")[1];
            var lastName = bodyContents[3].Split("=")[1];
           
            Person IsAuthenticated = LogInHelper.LoginWithAccountNumberAndSSN(AccountNumber, SSN);
            if (IsAuthenticated != null)
            {
                LogInViewModel loginViewModel = new LogInViewModel
                {
                    FirstName = firstName,
                    LastName = lastName,
                    AccountPhoneNumber = AccountNumber
                };
                //determine if person already has an active interaction. Response will contain two parameters: 1. IdSelfServePerson (if a new SelfServePerson record was created) or 2. IdActiveInteraction if this is a revist
                var response = LogInHelper.AddSelfServePersonOrRevisitInteraction(loginViewModel);

                //If it is determined that this is a revisit then redirect to revisit page
                if (response.IdActiveInteraction > 0)
                {
                    HttpContext.Session.SetInt32("ActiveInteractionId", response.IdActiveInteraction);
                    return RedirectToAction("Revisit");
                }
                //If no active interaction was found for the person info entered on login then save the SelfServePersonId for the newly created record and go to LocationMap
                HttpContext.Session.SetInt32("SelfServePersonId", response.IdSelfServePerson);
                //ViewBag.NewBody = newbody;
                return View();
            }
            else
                return RedirectToAction("Login");
        }

        /// <summary>
        /// Method is called when the LogIn page is submitted as a form
        /// </summary>
        /// <param name="loginViewModel"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LogInViewModel loginViewModel)
        {
            HttpContext.Session.SetString("Latitude", String.IsNullOrEmpty(loginViewModel.Latitude) ? LogInHelper.FirstLocation().Latitude : loginViewModel.Latitude);
            HttpContext.Session.SetString("Longitude", String.IsNullOrEmpty(loginViewModel.Longitude) ? LogInHelper.FirstLocation().Longitude : loginViewModel.Longitude);

            //determine if person already has an active interaction. Response will contain two parameters: 1. IdSelfServePerson (if a new SelfServePerson record was created) or 2. IdActiveInteraction if this is a revist
            var response = LogInHelper.AddSelfServePersonOrRevisitInteraction(loginViewModel);

            //If it is determined that this is a revisit then redirect to revisit page
            if (response.IdActiveInteraction > 0)
            {
                HttpContext.Session.SetInt32("ActiveInteractionId", response.IdActiveInteraction);
                return RedirectToAction("Revisit");
            }

            //If no active interaction was found for the person info entered on login then save the SelfServePersonId for the newly created record and go to LocationMap
            HttpContext.Session.SetInt32("SelfServePersonId", response.IdSelfServePerson);
            return RedirectToAction("LocationMap");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult LocationMap()
        {
            ViewBag.NearestLocationLatitude = string.IsNullOrEmpty(HttpContext.Session.GetString("Latitude")) ? LogInHelper.FirstLocation().Latitude : HttpContext.Session.GetString("Latitude");
            ViewBag.NearestLocationLongitude = string.IsNullOrEmpty(HttpContext.Session.GetString("Longitude")) ? LogInHelper.FirstLocation().Latitude : HttpContext.Session.GetString("Longitude");
            return View();
        }

        public ActionResult LocationMapMobile(string userCurrentLatitude, string userCurrentLongitude)
        {
            HttpContext.Session.SetString("Latitude", string.IsNullOrEmpty(userCurrentLatitude) ? LogInHelper.FirstLocation().Latitude : userCurrentLatitude);
            HttpContext.Session.SetString("Longitude", string.IsNullOrEmpty(userCurrentLongitude) ? LogInHelper.FirstLocation().Latitude : userCurrentLongitude);
            ViewBag.CheckSSO = "Mobile";
            return View("LocationMap");
        }


        public ActionResult LocationList()
        {
            var currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
            ViewBag.CurrentuserLatitude = string.IsNullOrEmpty(HttpContext.Session.GetString("Latitude")) ? LogInHelper.FirstLocation().Latitude : HttpContext.Session.GetString("Latitude");
            ViewBag.CurrentuserLongitude = string.IsNullOrEmpty(HttpContext.Session.GetString("Longitude")) ? LogInHelper.FirstLocation().Latitude : HttpContext.Session.GetString("Longitude"); 
            var nearestLocations = LocationListHelper.GetAllLocations(ViewBag.CurrentuserLatitude, ViewBag.CurrentuserLongitude, currentDateTime);
            return View(nearestLocations);
        }

        public IActionResult Categories(int idLocation,bool onlyActive=false)
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            int? IdSelfServePerson = HttpContext.Session.GetInt32("SelfServePersonId");

            //If the user chose to be seen for a different reason after choosing a callback, then parameter onlyActive will be set to true. We will save this to the session
            //This also means that only categories with active reasons will be shown
            HttpContext.Session.SetString("OnlyActive", onlyActive.ToString());

            //set latitude and longitude to temporary variable
            MobileHelper.GetLocationLatitudeLongitude(idLocation, HttpContext);
            List<MobileCategoryCustom> Categories = MobileHelper.GetCategories(requestCulture, idLocation,onlyActive);

            MobileHelper.UpdateSelfServePersonLocation(IdSelfServePerson.Value, idLocation);
            ViewBag.categoriesCount = Categories.Count;

            if (Categories.Any())
            {
                return View(Categories);
            }
            else
            {
                return View("Error");
            }
        }

        public IActionResult Reasons(int idCategory, int idLocation)
        {
            var requestCulture = HttpContext.Features.Get<IRequestCultureFeature>().RequestCulture.Culture.Name;
            //If the user chose to be seen for a different reason after choosing a callback, then only active reasons will be displayed
            var onlyActive=Convert.ToBoolean(HttpContext.Session.GetString("OnlyActive"));
            var model = MobileHelper.GetReasonsByCategoryId(requestCulture, idLocation, idCategory,onlyActive);
            ViewBag.CategoryName = MobileHelper.GetCategoryName(idCategory, requestCulture);
            ViewBag.OnlyActive = onlyActive;

            //If active reasons exists then if the user selects a callback we can still provide the option to be seen for a different reason. otherwise
            //there will be no option to return to choose a different reason. 
            HttpContext.Session.SetString("hasActiveReason", (model.Any(s => s.Active)).ToString());
            return View(model);
        }
        public ActionResult CallbackConfirmation()
        {
            //If there are active reasons then allow the user to choose to be seen for a different reason after the CallBackConfirmation is displayed
            //This will be passed back to Categories page with ActiveOnly = true
            bool hasActive =Convert.ToBoolean(HttpContext.Session.GetString("hasActiveReason"));
            return View(hasActive);
        }
        public JsonResult SavePhoneNumber(string phone, string reason, int idCategory)
        {
            int? IdSelfServePerson = HttpContext.Session.GetInt32("SelfServePersonId");
            MobileHelper.InsertCallback(phone, reason, IdSelfServePerson.Value, idCategory);
            return Json(true);
        }

        public IActionResult TextAlerts(int idActiveInteraction)
        {
            ViewBag.idActiveInteraction = idActiveInteraction;
            return View();
        }

        public JsonResult UpdateActiveInteraction(string alertPhoneNumber, int idActiveInteraction)
        {
            var isUpdatedPhoneNumber = MobileHelper.UpdateActiveInteraction(alertPhoneNumber, idActiveInteraction);
            return Json(isUpdatedPhoneNumber);
        }

        [HttpPost]
        public int SaveInteraction(int idReason, string reasonEstimatedWait)
        {
            int? selfServePersonId = HttpContext.Session.GetInt32("SelfServePersonId");
            var idLocation = MobileHelper.GetIdLocation(selfServePersonId.Value);
            var idQueue = MobileHelper.GetIdQueue(idReason);
            DateTime checkinDate = MaintainenceUtils.CurrentDateTime(DateTime.Now);
            var interactionId = MobileHelper.SaveMobileInteraction(idLocation, idQueue, checkinDate, selfServePersonId.Value, reasonEstimatedWait, idReason);
            HttpContext.Session.SetInt32("ActiveInteractionId", interactionId);
            return interactionId;
        }

        public IActionResult Confirmation()
        {
            RevisitCustom model = new RevisitCustom();
            try
            {
                using (TexellCheckInContext db = new TexellCheckInContext())
                {
                    var currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
                    int activeInteractionId = HttpContext.Session.GetInt32("ActiveInteractionId") ?? 0;
                    var activeInteraction = db.ActiveInteraction.FirstOrDefault(s => s.Id == activeInteractionId);
                    int estimatedWait;
                    int minWait = db.SelfServeAdmin.First().MinimumWaitTime;
                    int maxWait = db.MaintenanceRanges.FirstOrDefault(r => r.Description == "MaxWaitTime").RangeEnd;

                    estimatedWait = Convert.ToInt32(activeInteraction.EstimateServiceStart.TotalMinutes - currentDateTime.TimeOfDay.TotalMinutes);

                    if (estimatedWait < minWait)
                    {
                        estimatedWait = minWait;
                    }
                    if(estimatedWait > maxWait)
                    {
                        estimatedWait = maxWait;
                    }

                      
                    if (activeInteraction != null)
                    {
                        var Location = db.Location.Where(s => s.Id == activeInteraction.IdLocation).FirstOrDefault();
                        model.IdLocation = activeInteraction.IdLocation;
                        model.IdActiveInteraction = activeInteractionId;
                        model.LocationName = Location.Description;
                        model.LocationAddress = Location.Street + "," + Location.City + "," + Location.State + " " + Location.Zip;
                        model.CurrentTime = currentDateTime.TimeOfDay;
                        model.EstimatedWaitTime = estimatedWait;
                    }
                }
            }
            catch(Exception ex)
            {
                ExceptionHelper.Throw(ex);
            }
            return View(model);
        }

        public IActionResult Revisit()
        {
            var currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
            RevisitCustom RevisitDetails = new RevisitCustom();
            using (TexellCheckInContext db = new TexellCheckInContext())
            {
                int activeInteractionId = HttpContext.Session.GetInt32("ActiveInteractionId") ?? 0;
                var activeInteraction = db.ActiveInteraction.FirstOrDefault(s => s.Id == activeInteractionId);

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
                    var Location = db.Location.Where(s => s.Id == activeInteraction.IdLocation).FirstOrDefault();
                    RevisitDetails.IdLocation = activeInteraction.IdLocation;
                    RevisitDetails.IdActiveInteraction = activeInteractionId;
                    RevisitDetails.LocationName = Location.Description;
                    RevisitDetails.LocationAddress = Location.Street + "," + Location.City + "," + Location.State + Location.Zip;
                    RevisitDetails.CurrentTime = currentDateTime.TimeOfDay;
                    RevisitDetails.EstimatedWaitTime = estimatedWait;
                }
            }
            return View(RevisitDetails);
        }


        [HttpGet]
        public ActionResult CancelInteraction(int idActiveInteraction)
        {
            RevisitHelper.CancelActiveInteraction(idActiveInteraction);
            return RedirectToAction("LocationMap");
        }




        //[HttpGet]
        //public ActionResult ExitApp()
        //{
        //    return View();
        //}

        [HttpPost]
        public ActionResult GetAllNearestLocationsMaps()
        {
            var currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
            ViewBag.CurrentuserLatitude = string.IsNullOrEmpty(HttpContext.Session.GetString("Latitude")) ? LogInHelper.FirstLocation().Latitude : HttpContext.Session.GetString("Latitude");
            ViewBag.CurrentuserLongitude = string.IsNullOrEmpty(HttpContext.Session.GetString("Longitude")) ? LogInHelper.FirstLocation().Latitude : HttpContext.Session.GetString("Longitude");
            List<LocationMapViewModel> allNearestLocations = new List<LocationMapViewModel>();
            allNearestLocations = LocationMapHelper.GetAllNearestLocations(HttpContext.Session.GetString("Latitude"), HttpContext.Session.GetString("Longitude"), currentDateTime, HttpContext);
            return Json(new { result = allNearestLocations, NearestLocationLat = HttpContext.Session.GetString("Latitude"), NearestLocationLong = HttpContext.Session.GetString("Longitude"), UserLatitude = ViewBag.CurrentuserLatitude, UserLongitude = ViewBag.CurrentuserLongitude });
        }
        [HttpPost]
        public JsonResult SetLocation(string lat,string lng)
        {
            HttpContext.Session.SetString("Latitude",lat);
            HttpContext.Session.SetString("Longitude", lng);
            return Json(true);
        }
        [HttpPost]
        public ActionResult LocationHours(int idLocation)
        {
            var locationTimes = LocationHoursHelper.GetLocationHours(idLocation);
            return Json(locationTimes);
        }

        public ActionResult ValidateAccountNumber(string accountNumber)
        {
            return Json(LogInHelper.ValidateAccountNumber(accountNumber));
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

            if(returnUrl != null)
            {
                return RedirectToAction("Login");
            }
            else
            {
                return Json(true);
            }

        }
    }
}