using System.Web.Mvc;

namespace AVElectraFeed.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            ViewBag.Title = "Home Page";

            return this.View();
        }
    }
}
