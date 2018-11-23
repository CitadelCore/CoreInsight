using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;
using CoreServer.Models;

namespace CoreServer.Controllers
{
    //[Authorize]
    public class AuditLogController : Controller
    {
        private CoreDB dataBase = new CoreDB();

        // GET: AuditLog/Details/5
        public ActionResult Details(int id)
        {
            List<Models.Audit> AuditsList = dataBase.Audits.ToList();

            if (AuditsList != null) return View(AuditsList);

            return View();
        }

        // POST: AuditLog/Create
        [HttpPost]
        public ActionResult Create(FormCollection collection)
        {
            try
            {
                // TODO: Add insert logic here
                string Reason = collection["reason"];
                string Application = collection["application"];

                string User = Convert.ToBase64String(Encoding.UTF8.GetBytes(HttpContext.User.Identity.Name));



                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            catch
            {
                return new HttpStatusCodeResult(HttpStatusCode.InternalServerError);
            }
        }
    }
}
