﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using psychoTest.Models;
using psychoTest.Models.Organisations;
using System.Web.Script.Serialization;
using System.Text;
using System.Text.RegularExpressions;

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
            catch (Exception) { return Redirect("/cabinet"); }

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
                    newOrg.AddUser(User.Identity.Name);
                    //присвоим пользователю роль менеджера созданной организации
                    newOrg.UserAddRole(User.Identity.Name, "manager");
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
                    //если пользователь уже состоит в какой-либо организации, то досвидания
                    if(db.OrganisationUsers.Where(x => x.userEmail == userEmail && x.active && x.dateStop > DateTime.Now).Count() > 0)
                    {
                        answer.result = Core.AjaxResults.UserAllreadyInOrg;
                        return answer.JsonContentResult();
                    }
                    //теперь выясним, нет ли у пользователя других заявок
                    if (db.OrganisationUsers.Where(x => x.userEmail == userEmail && x.active == false && x.dateStop < DateTime.Now).Count() > 0)
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
                    return answer.JsonContentResult();
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

        public ActionResult Users(string orgId)
        {
            if (!Core.Membership.isAdmin(User) && !Organisation.isManager(User, orgId))
            {
                return Redirect("/cabinet");
            }

            Models.Organisations.Views.Users model = new Models.Organisations.Views.Users();
            model.orgId = orgId;

            using (DBMain db = new DBMain())
            {
                string query = @"
SELECT au.id
	,au.Surname AS surname
	,au.Name AS name
	,au.Patronim AS patronim
	,au.Email AS email
	,au.PhoneNumber AS phone
	,ou.active AS active
	,SUBSTRING((SELECT ',' + our.roleName AS [text()]
				FROM OrganisationsUsersRoles our
				WHERE our.userEmail = au.Email
				FOR XML PATH('')), 2, 1000) AS roles
FROM OrganisationsUsers ou, AspNetUsers au
WHERE ou.userEmail = au.Email
	AND ou.orgId = @orgId
    AND ou.active = 1
ORDER BY surname, name, patronim, email";
                model.orgUsers = db.Database.SqlQuery<Models.Organisations.Views.UsersUserEntity>(query, new SqlParameter("orgId", orgId)).ToList();
                return View(model);
            }
        }

        public ActionResult UserCU(string orgId, string userId = null)
        {
            if (!Core.Membership.isAdmin(User) && !Organisation.isManager(User, orgId))
            {
                return Redirect("/cabinet");
            }

            Models.Organisations.Views.UserCU model = new Models.Organisations.Views.UserCU();

            model.orgId = orgId;
            model.password = Core.BLL.GenerateRandomDigitCode(6);

            if (userId != null)
            {
                using (DBMain db = new DBMain())
                {
                    AspNetUser user = db.AspNetUsers.Where(x => x.Id == userId).Single();

                    if (!Organisation.UserIsActive(orgId, user.Email)) return Redirect("/cabinet");

                    model.userId = user.Id;
                    model.surname = user.Surname;
                    model.name = user.Name;
                    model.patronim = user.Patronim;
                    model.sex = user.Sex;
                    model.email = user.Email;
                    model.phone = user.PhoneNumber;
                    model.password = "";
                    model.roles = Models.Organisations.Organisation.RolesGetAsString(orgId, model.email);
                }
            }

            return View(model);
        }

        [HttpPost]
        public ActionResult UserCU(Models.Organisations.Views.UserCU model)
        {
            if (!Core.Membership.isAdmin(User) && !Organisation.isManager(User, model.orgId))
            {
                return Redirect("/cabinet");
            }

            using (DBMain db = new DBMain())
            {
                //если пользователя добавляют
                if (model.userId.Length < 30)
                {
                    var newUser = new ApplicationUser { UserName = model.email, Email = model.email };
                    newUser.Surname = model.surname;
                    newUser.Patronim = model.patronim;
                    newUser.Name = model.name;
                    newUser.PhoneNumber = model.phone;
                    newUser.Sex = model.sex;
                    var result = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>().Create(newUser, model.password);

                    //добавим пользователя в организацию
                    //присвоим ему роли
                    if (result.Succeeded)
                    {
                        Organisation org = Organisation.GetById(model.orgId);
                        org.AddUser(newUser.Email);

                        foreach (string role in model.roles.Split(','))
                        {
                            org.UserAddRole(newUser.Email, role);
                        }

                        //обновление индекса
                        Core.BLL.SearchIndexUpdate(newUser, Core.CRUDType.Create);
                    }
                }
                //если редактируют
                else
                {
                    AspNetUser user = db.AspNetUsers.Find(model.userId);
                    user.Surname = model.surname;
                    user.Patronim = model.patronim;
                    user.Name = model.name;
                    user.Sex = model.sex;

                    string oldEmail = user.Email;
                    //смена адреса почты
                    if (user.Email != model.email)
                    {
                        user.UserName = model.email;
                        user.Email = model.email;
                        user.EmailConfirmed = false;
                    }

                    if (user.PhoneNumber != model.phone)
                    {
                        user.PhoneNumber = model.phone;
                        user.PhoneNumberConfirmed = false;
                    }
                    db.Entry(user).State = EntityState.Modified;
                    db.SaveChanges();

                    //обновление индекса
                    Core.BLL.SearchIndexUpdate(user, Core.CRUDType.Update);

                    //смена адреса почты в ролях
                    if (oldEmail != model.email)
                    {
                        string query = @"UPDATE OrganisationsUsers SET userEmail='" + model.email + "' WHERE userEmail='" + oldEmail + "'";
                        db.Database.ExecuteSqlCommand(query);

                        query = @"UPDATE OrganisationsUsersRoles SET userEmail='" + model.email + "' WHERE userEmail='" + oldEmail + "'";
                        db.Database.ExecuteSqlCommand(query);
                    }

                    //смена пароля
                    if (model.password != null && model.password.Length > 0)
                    {
                        ApplicationUserManager aum = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
                        aum.RemovePassword(model.userId);
                        aum.AddPassword(model.userId, model.password);
                    }

                    //сбросим роли пользователя
                    Organisation.UserRemoveRoles(oldEmail, model.orgId);
                    //обновим роли пользователя
                    foreach (string role in model.roles.Split(','))
                    {
                        Organisation org = Organisation.GetById(model.orgId);
                        org.UserAddRole(model.email, role);
                    }
                }
            }

            return Redirect("/organisation/users?orgId=" + model.orgId);
        }

        public ActionResult UserDelete(string orgId, string userId)
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
                    AspNetUser user = db.AspNetUsers.Find(userId);

                    if (!Organisation.UserIsActive(orgId, user.Email))
                    {
                        answer.result = Core.AjaxResults.NoRights;
                        return answer.JsonContentResult();
                    } 

                    //снимаем с пользователя роли
                    string query = @"DELETE FROM OrganisationsUsersRoles WHERE userEmail=@userEmail AND orgId=@orgId";
                    db.Database.ExecuteSqlCommand(query, new SqlParameter("userEmail", user.Email), new SqlParameter("orgId", orgId));
                    //меняем статус активности и дату увольнения
                    OrganisationsUsers ou = db.OrganisationUsers.Where(x => x.userEmail == user.Email && x.orgId == orgId).Single();
                    ou.dateStop = DateTime.Now;
                    ou.active = false;
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

        public ActionResult UsersImport(string orgId)
        {
            if (!Core.Membership.isAdmin(User) && !Organisation.isManager(User, orgId))
            {
                return Redirect("/cabinet");
            }

            var model = new Models.Organisations.Views.UsersImport();
            model.orgId = orgId;

            return View(model);
        }

        [HttpPost]
        public ActionResult UsersImport(Models.Organisations.Views.UsersImport model)
        {
            if (!Core.Membership.isAdmin(User) && !Organisation.isManager(User, model.orgId))
            {
                return Redirect("/cabinet");
            }

            if (model.filename == null)
            {
                ViewData["serverError"] = Core.ErrorMessages.UploadFileNotSelect;
                return View(model);
            }

            //очистим все счетчики и журналы
            model.errorLog.Clear();
            model.rowsCount = 0;
            model.rowsCorrect = 0;
            model.rowsIncorrect = 0;
            model.usersAdded = 0;
            model.usersNotAdded = 0;

            //прочитаем файл в строку
            HttpPostedFileBase file = Request.Files["filename"];
            byte[] fileBytes = new byte[file.ContentLength];
            var data = file.InputStream.Read(fileBytes, 0, Convert.ToInt32(file.ContentLength));
            string usersFile = Encoding.UTF8.GetString(fileBytes);

            //рассплитуем файл построчно
            string [] ufStrings = Regex.Split(usersFile, "\r\n");
            //в файле меньше двух строк
            if (ufStrings.Count() < 2)
            {
                ViewData["serverError"] = Core.ErrorMessages.UploadUsersFileLessTwoStrings;
                return View(model);
            }

            int cnEmail = -1;
            int cnSurname = 1;
            int cnName = 2;
            int cnPatronim = 3;
            int cnSex = 4;
            int cnPhone = 5;
            int cnPwd = 6;
            int cnRoles = 7;

            //разберем первую строку-заголовок
            int cCounter = 0;
            foreach (string s in Regex.Split(ufStrings[0], model.separator))
            {
                //узнаем индексы нужных полей
                switch(s)
                {
                    case "email":
                        cnEmail = cCounter;
                        break;
                    case "surname":
                        cnSurname = cCounter;
                        break;
                    case "name":
                        cnName = cCounter;
                        break;
                    case "patronim":
                        cnPatronim = cCounter;
                        break;
                    case "sex":
                        cnSex = cCounter;
                        break;
                    case "phone":
                        cnPhone = cCounter;
                        break;
                    case "pwd":
                        cnPwd = cCounter;
                        break;
                    case "roles":
                        cnRoles = cCounter;
                        break;
                    default:
                        break;
                }
                cCounter++;
            }

            //если поля email нет, то сообщим об этом
            if (cnEmail == -1)
            {
                ViewData["serverError"] = Core.ErrorMessages.UploadUsersFileNoEmail;
                return View(model);
            }

            //начинаем собирать записи пользователей и добавлять их
            cCounter = 0;
            foreach (string fileString in ufStrings)
            {
                cCounter++;

                //первую строку пропустим
                if (cCounter-1 == 0)
                {    
                    continue;
                }

                //если строка пустая, тоже пропустим
                if (fileString.Length < 1)
                {
                    continue;
                }

                model.rowsCount++;

                string[] columns = Regex.Split(fileString, model.separator);

                //проверим поля на корректность
                //почта
                string email;
                try
                {
                    email = columns[cnEmail];
                }
                catch (Exception)
                {
                    model.rowsIncorrect++;
                    continue;
                }
                Regex regex = new Regex(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*" + "@" + @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$");
                Match match = regex.Match(email);
                if (!match.Success)
                {
                    model.rowsIncorrect++;

                    //добавим подробности в журнал ошибок
                    Core.UploadFailedString log = new Core.UploadFailedString();
                    log.rowNumber = cCounter;
                    log.rowData = fileString;
                    log.failedColumnName = "email";
                    log.failedColumnData = email;
                    model.errorLog.Add(log);

                    continue;
                }
                else
                {
                    model.rowsCorrect++;
                }

                //если почта верная, увеличим счетчик распознанного как строки пользователей

                /*var newUser = new ApplicationUser { UserName = model.email, Email = model.email };
                newUser.Surname = model.surname;
                newUser.Patronim = model.patronim;
                newUser.Name = model.name;
                newUser.PhoneNumber = model.phone;
                newUser.Sex = model.sex;
                var result = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>().Create(newUser, model.password);

                //добавим пользователя в организацию
                //присвоим ему роли
                if (result.Succeeded)
                {
                    Organisation org = Organisation.GetById(model.orgId);
                    org.AddUser(newUser.Email);

                    foreach (string role in model.roles.Split(','))
                    {
                        org.UserAddRole(newUser.Email, role);
                    }

                    //обновление индекса
                    Core.BLL.SearchIndexUpdate(newUser, Core.CRUDType.Create);
                }*/
            }

            //разрешим показать результаты
            model.showResult = true;

            return View(model);
        }
    }
}