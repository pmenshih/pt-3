using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using psychoTest.Models;
using psychoTest.Models.Organisation;

namespace psychoTest.Controllers
{
    public class OrganisationController : Controller
    {
        // GET: Organisation
        [Authorize]
        public ActionResult Index()
        {
            Organisation org = new Organisation();

            if (Request.QueryString["id"] != null && Core.Membership.isAdmin(User)) org.id = Request.QueryString["id"];
            else org = Organisation.GetByManager(User);

            if (org == null) return Redirect("/cabinet");

            var model = new Models.Organisation.Views.Index(org);
            model.usersCount = org.GetActiveUsers().Count();

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public ActionResult Index(FormCollection form)
        {
            //блок быстрого создания организации пользователем без ролей
            //для начала посмотрим, что он действительно без ролей. если это не так, отправляем его в индекс личного кабинета
            if (Core.Membership.isInAnyRole(User)) return Redirect("/cabinet");
            //в противном случае пробуем создать организацию и назначить пользователю роль "Менеджер"
            if (form["action"] == "forceCreate")
            {
                Organisation newOrg = new Organisation();
                newOrg.name = form["orgName"];
                using (DBMain db = new DBMain())
                {
                    db.Organisations.Add(newOrg);
                    db.SaveChanges();

                    //добавим пользователя к созданной организации
                    newOrg.AddUser(User);
                    //присвоим пользователю роль менеджера созданной организации
                    newOrg.SetManager(User);
                }
            }

            return Index();
        }

        [Authorize]
        public ActionResult ChangeName(string id, string newval)
        {
            Core.AjaxAnswer answer = new Core.AjaxAnswer();
            string orgId = id;

            if (Core.Membership.isAdmin(User) || Organisation.isManager(User, orgId))
            {
                using (DBMain db = new DBMain())
                {
                    try
                    {
                        Organisation org = db.Organisations.Where(x => x.id == orgId).Single();
                        org.name = newval;
                        org.moderated = false;
                        db.SaveChanges();
                    }
                    catch (Exception)
                    {
                        answer.result = Core.AjaxResults.CodeError;
                    }
                }
                answer.result = Core.AjaxResults.Success;
            }
            else answer.result = Core.AjaxResults.NoRights;

            return answer.JsonContentResult();
        }
    }
}