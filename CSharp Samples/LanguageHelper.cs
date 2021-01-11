using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TexellCheckInMobile.Models;
using TexellCheckInMobile.Models.Custom;
using TexellCheckInMobile.Models.MobileViewModel;

namespace TexellCheckInMobile.Helpers
{
    public static class LanguageHelper
    {
        public static List<Culture> SupportedCultures()
        {
            List<Culture> cultures = new List<Culture>();
            cultures.Add(new Culture { cultureId = 1, cultureName = "en-US", description = "English" });
            cultures.Add(new Culture { cultureId = 2, cultureName = "es", description = "Spanish" });
            return cultures;
        }

        public static int GetCultureId(string cultureName)
        {
            var cultures = SupportedCultures();
            int cultureId = cultures.FirstOrDefault(r => r.cultureName == cultureName).cultureId;
            return cultureId;
        }

        public static bool IsSupportedCulture(string culture)
        {
            var supportedCultures = SupportedCultures().Select(r => r.cultureName).ToList();
            if (supportedCultures.Contains(culture))
            {
                return true;
            }
            return false;
        }
    }
}
