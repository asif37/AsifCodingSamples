using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TexellCheckInMobile.Models;
using TexellCheckInMobile.Helpers;

namespace TexellCheckInMobile.Utils
{
    public static class LanguageUtils
    {
        public static SelfServeLanguage GetDefaultLanguage ()
        {
            //English is the default language
            try
            {
                using (TexellCheckInContext db = new TexellCheckInContext())
                {
                    SelfServeLanguage defaultLanguage = db.SelfServeLanguage.DefaultIfEmpty(null).FirstOrDefault(r => r.Name.ToUpper().Trim() == "ENGLISH");
                    return defaultLanguage;
                }
            }
            catch (Exception ex)
            {
                ExceptionHelper.Throw(ex);
            }
            return null;
        }

        public enum Language
        {
            English = 1,
            Spanish = 2
        }

        public static class Culture
        {
            public const string English = "en-US";
            public const string Spanish = "es";
        }
    }
}
