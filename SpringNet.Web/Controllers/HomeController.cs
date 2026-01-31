using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace SpringNet.Web.Controllers
{
    public class HomeController : Controller
    {
        // Spring.NET을 통해 주입되는 프로퍼티
        public string TestService { get; set; }


        public ActionResult Index()
        {
            ViewBag.Message = TestService;
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}