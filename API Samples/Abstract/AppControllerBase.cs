using elearning.web.Code.Contexts;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace elearning.web.Controllers.Abstract
{
    public class AppControllerBase<T> : ControllerBase where T : AppControllerBase<T>
    {
        private AppDbContext _DbContext;

        public AppDbContext DbContext => _DbContext ?? (_DbContext = (AppDbContext)HttpContext?.RequestServices.GetService(typeof(AppDbContext)));

        // TODO: Need to get current logged in admin user id
        public int AdminUserID => 1;

    }
}
