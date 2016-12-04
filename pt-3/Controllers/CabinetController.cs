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

                    //если у пользователя уже есть заявка на присоединение к организации
                    if (db.OrganisationUsers.Where(x => x.userEmail == User.Identity.Name && x.active == false && x.dateStop > DateTime.Now).Count() > 0)
                    {
                        ViewData.Add("requestedallready", "1");
                    }
                }
            }
            //если пользователь с правами администратора, добавим ему список не модерированных организаций
            else if (Core.Membership.isAdmin(User))
            {
                using (DBMain db = new DBMain())
                {
                    ViewData.Add("unmoderated", db.Organisations.Where(x => x.moderated == false).ToList());
                }
            }

            return View();
        }
    }
}