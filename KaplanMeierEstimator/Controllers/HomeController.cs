using KaplanMeierEstimator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KaplanMeierEstimator.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult NewJob()
        {
            var strategies = new List<SelectListItem>() {
                new SelectListItem { Text = "Begin Percentage", Value = "0" },
                new SelectListItem { Text = "End Percentage", Value = "1" },
                new SelectListItem { Text = "Top Delta Percentage", Value = "2", Selected = true }
            };

            ViewBag.Strategies = strategies;
            return View();
        }

        [HttpPost]
        public ActionResult NewJob(EstimateJob job)
        {
            return View();
        }

        public ActionResult ListJobs()
        {
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