using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using psychoTest.Models;


namespace psychoTest.Controllers
{
    [Authorize]
    public class CabinetController : Controller
    {
        // GET: Cabinet
        public ActionResult Index()
        {
            //если у пользователя нет ролей, посмотрим, подтвердил ли он почту и телефон. информацию об этом положим во вьюстэйт
            if (!Core.Membership.isInAnyRole(User))
            {
                using (DBMain db = new DBMain())
                {
                    string userId = User.Identity.GetUserId();
                    AspNetUser user = db.AspNetUsers.Where(x => x.Id == userId).Single();
                    ViewData.Add("confirmed", (user.EmailConfirmed && user.PhoneNumberConfirmed));
                }
            }

            return View();
        }
    }
}