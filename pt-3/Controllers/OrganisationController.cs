using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;

namespace psychoTest.Controllers
{
    public class OrganisationController : Controller
    {
        // GET: Organisation
        public ActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        public ActionResult Index(FormCollection form)
        {
            //блок быстрого создания организации пользователем без ролей
            //для начала посмотрим, что он действительно без ролей. если это не так, отправляем его в индекс личного кабинета
            if (Core.isInAnyRole(User)) return Redirect("/cabinet");
            //в противном случае пробуем создать организацию и назначить пользователю роль "Менеджер"
            if (form["action"] == "forceCreate")
            {
                Models.Organisation newOrg = new Models.Organisation();
                newOrg.name = form["orgName"];
                using (Models.DBMain db = new Models.DBMain())
                {
                    db.SaveChanges();

                    //присвоим пользователю роль менеджера созданной организации
                    string query = @"INSERT INTO OrganisationUserRoles 
                                    VALUES (@roleName, @userEmail, @organisationId)";
                    db.Database.ExecuteSqlCommand(query
                                                    ,new SqlParameter("roleName", "manager")
                                                    ,new SqlParameter("userEmail", User.Identity.Name)
                                                    ,new SqlParameter("organisationId", newOrg.id));
                }
            }

            return View();
        }
    }
}