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
using Newtonsoft.Json;

namespace TexellCheckInKiosk.Controllers
{
    public class LobbyController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        public PartialViewResult GetLobbyEmployee(int pageNo=0)
        {
            int idLocation = KioskUserHelper.GetKioskLocation(User.Identity.Name);
            DateTime currentDateTime = MaintainenceUtils.CurrentDateTime(DateTime.Now);
            var waitinginteraction = InteractionsHelper.GetInteractionsWaiting(idLocation, 0, currentDateTime.Date);
            waitinginteraction= waitinginteraction.OrderBy(r => r.LastName).ToList();
            var model = waitinginteraction.Skip(pageNo * 3).Take(3).ToList();
             ViewBag.PageNo = pageNo + 1;
            if (pageNo == 0)
            {
                var str = JsonConvert.SerializeObject(waitinginteraction);
                HttpContext.Session.SetString("shuffleinterection", str);

            }
            else
            {
                if (model.Count == 0)
                {
                    //For Save
                    var str = JsonConvert.SerializeObject(waitinginteraction);
                    HttpContext.Session.SetString("shuffleinterection", str);
                    model = waitinginteraction.Take(3).ToList();
                    ViewBag.PageNo = 1;
                }
                else
                {
                    // For Retrive
                    var str = HttpContext.Session.GetString("shuffleinterection");
                    var obj = JsonConvert.DeserializeObject<List<InteractionCustom>>(str);
                    model = obj.Skip(pageNo * 3).Take(3).ToList();
                }
            }
            return PartialView("_PartialLobbyQueue", model);
        }
    }
}