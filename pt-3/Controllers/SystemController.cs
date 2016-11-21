using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace psychoTest.Controllers
{
    public class SystemController : Controller
    {
        [Authorize(Roles = "admin")]
        public ActionResult SearchIndexBuilder()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "admin")]
        public ActionResult SearchIndexBuilder(FormCollection form)
        {
            Models.DBMain db = new Models.DBMain();
            string query;

            query = "DELETE FROM SearchIndexes;";
            db.Database.ExecuteSqlCommand(query);

            //пользователи
            foreach (Models.AspNetUser user in db.AspNetUsers)
            {
                Models.SearchIndex si = new Models.SearchIndex();
                si.instanceId = user.Id;
                si.instanceType = "AspNetUsers";

                si.searchString = user.Surname;
                si.searchString += " " + user.Name;
                if (user.Patronim != null && user.Patronim.Length > 0)
                    si.searchString += " " + user.Patronim;

                si.searchString += "@#@";
                si.searchString += user.Email;

                si.searchString += "@#@";
                si.searchString += user.PhoneNumber;

                db.SearchIndexes.Add(si);
            }

            db.SaveChanges();

            return View();
        }
    }
}