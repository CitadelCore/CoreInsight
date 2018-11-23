using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CoreServer.Controllers
{
    [Authorize]
    public class ApplicationsController : Controller
    {
        public ActionResult DRXEditor()
        {
            ViewBag.Title = "DRX Editor";

            return View();
        }
    }
}