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
using psychoTest.Models.Organisations;
using System.Web.Script.Serialization;

namespace psychoTest.Controllers
{
    [Authorize]
    public class OrganisationController : Controller
    {
        // GET: Organisation
        public ActionResult Index(string id)
        {
            Organisation org = new Organisation();

            try
            {
                org = Organisation.GetById(id);
            }
            catch (Exception) { return Redirect("/cabinet");  }

            if (!Organisation.isManager(User, org.id) && !Core.Membership.isAdmin(User)) 
                return Redirect("/cabinet");

            var model = new Models.Organisations.Views.Index(org);
            model.usersCount = org.GetActiveUsers().Count();

            return View(model);
        }

        [HttpPost]
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

                    //Перестроим поисковый индекс
                    Core.BLL.SearchIndexUpdate(newOrg, Core.CRUDType.Create);

                    //добавим пользователя к созданной организации
                    newOrg.AddUser(User);
                    //присвоим пользователю роль менеджера созданной организации
                    newOrg.SetManager(User);
                }
                return Index(newOrg.id);
            }
            else return Redirect("/cabinet");
        }

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

                        //Перестроим поисковый индекс
                        Core.BLL.SearchIndexUpdate(org, Core.CRUDType.Update);
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

        public ActionResult Moderate(string id)
        {
            Core.AjaxAnswer answer = new Core.AjaxAnswer();
            string orgId = id;

            if (Core.Membership.isAdmin(User))
            {
                using (DBMain db = new DBMain())
                {
                    try
                    {
                        Organisation org = db.Organisations.Where(x => x.id == orgId).Single();
                        org.moderated = true;
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

        public ActionResult ListAll()
        {
            Core.AjaxAnswer answer = new Core.AjaxAnswer();
            
            using (DBMain db = new DBMain())
            {
                try
                {
                    List<Models.Organisations.Views.ShortInfo> orgs = db.Database.SqlQuery<Models.Organisations.Views.ShortInfo>("SELECT id, name FROM Organisations ORDER BY name ASC").ToList();
                    var jsonSerialiser = new JavaScriptSerializer();
                    var json = jsonSerialiser.Serialize(orgs);
                    return Content(json, "application/json");
                }
                catch (Exception)
                {
                    answer.result = Core.AjaxResults.CodeError;
                }
            }
            answer.result = Core.AjaxResults.Success;

            return answer.JsonContentResult();
        }

        /*[HttpPost]*/
        public ActionResult JoinRequest(string orgId, string userEmail)
        {
            Core.AjaxAnswer answer = new Core.AjaxAnswer();

            if (User.Identity.Name != userEmail)
            {
                answer.result = Core.AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            using (DBMain db = new DBMain())
            {
                try
                {
                    //проверим, не состоит ли пользователь в запрашиваемой организации
                    if (db.OrganisationUsers.Where(x => x.orgId == orgId && x.userEmail == userEmail && x.dateStop > DateTime.Now).Count() > 0)
                    {
                        answer.result = Core.AjaxResults.UserAllreadyInOrg;
                        return answer.JsonContentResult();
                    }
                    //теперь выясним, нет ли у пользователя других заявок
                    if (db.OrganisationUsers.Where(x => x.userEmail == userEmail && x.active == false).Count() > 0)
                    {
                        answer.result = Core.AjaxResults.NoMultipleJoinRequests;
                        return answer.JsonContentResult();
                    }

                    OrganisationsUsers ou = new OrganisationsUsers();
                    ou.active = false;
                    ou.dateStart = DateTime.Now;
                    ou.dateStop = new DateTime(2222, 1, 1);
                    ou.orgId = orgId;
                    ou.userEmail = userEmail;
                    db.OrganisationUsers.Add(ou);
                    db.SaveChanges();
                }
                catch (Exception)
                {
                    answer.result = Core.AjaxResults.CodeError;
                }
            }
            answer.result = Core.AjaxResults.Success;

            return answer.JsonContentResult();
        }

        public ActionResult AcceptJoinRequest(string orgId, string userEmail)
        {
            Core.AjaxAnswer answer = new Core.AjaxAnswer();

            if (!Core.Membership.isAdmin(User) && !Organisation.isManager(User, orgId))
            {
                answer.result = Core.AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            using (DBMain db = new DBMain())
            {
                try
                {
                    OrganisationsUsers ou = db.OrganisationUsers.Where(x => x.orgId == orgId && x.userEmail == userEmail).Single();

                    OrganisationsUsersRole our = new OrganisationsUsersRole();
                    our.orgId = ou.orgId;
                    our.roleName = "actor";
                    our.userEmail = ou.userEmail;
                    db.OrganisationsUsersRoles.Add(our);
                    
                    ou.active = true;
                    db.SaveChanges();
                }
                catch (Exception)
                {
                    answer.result = Core.AjaxResults.CodeError;
                }
            }
            answer.result = Core.AjaxResults.Success;

            return answer.JsonContentResult();
        }

        public ActionResult RejectJoinRequest(string orgId, string userEmail)
        {
            Core.AjaxAnswer answer = new Core.AjaxAnswer();

            if (!Core.Membership.isAdmin(User) && !Organisation.isManager(User, orgId))
            {
                answer.result = Core.AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            using (DBMain db = new DBMain())
            {
                try
                {
                    OrganisationsUsers ou = db.OrganisationUsers.Where(x => x.orgId == orgId && x.userEmail == userEmail).Single();
                    db.OrganisationUsers.Remove(ou);
                    db.SaveChanges();
                }
                catch (Exception)
                {
                    answer.result = Core.AjaxResults.CodeError;
                }
            }
            answer.result = Core.AjaxResults.Success;

            return answer.JsonContentResult();
        }

        public ActionResult Users()
        {
            return View();
        }
    }
}