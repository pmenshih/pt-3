using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Data.SqlClient;
using System.Data.Entity;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using psychoTest.Models;
using psychoTest.Models.Organisations;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using psychoTest.Core;

namespace psychoTest.Controllers
{
    [Authorize]
    public class OrganisationController : Controller
    {
        private bool IsAdminOrManager(string id)
        {
            return (Membership.isManager(id) || Membership.isAdmin());
        }

        #region Контроллеры страниц
        public ActionResult Index(string orgId)
        {
            Organisation org = new Organisation();

            org = Organisation.GetById(orgId);
            if(org == null || !IsAdminOrManager(orgId))
                return Redirect("/cabinet"); 

            var model = new Models.Organisations.Views.Index(org);
            model.usersCount = org.GetActiveUsers().Count();

            return View(model);
        }

        [HttpPost]
        public ActionResult Index(FormCollection form)
        {
            //блок быстрого создания организации пользователем без ролей
            //для начала посмотрим, что он действительно без ролей. 
            //если это не так, отправляем его в индекс личного кабинета
            if (Membership.isInAnyRole() || form["action"] != "forceCreate")
                return Redirect("/cabinet");

            //в противном случае пробуем создать организацию и назначить пользователю роль "Менеджер"
            Organisation newOrg = new Organisation();
            newOrg.name = form["orgName"];
            newOrg.Create();
                
            //Перестроим поисковый индекс
            Core.BLL.SearchIndexUpdate(newOrg, Core.CRUDType.Create);

            //добавим пользователя к созданной организации
            newOrg.AddUser(User.Identity.Name);

            //присвоим пользователю роль менеджера созданной организации
            newOrg.UserAddRole(User.Identity.Name, "manager");

            return Index(newOrg.id);
        }
        
        public ActionResult Users(string orgId)
        {
            if (!IsAdminOrManager(orgId)) return Redirect("/cabinet");

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
            if (!IsAdminOrManager(orgId)) return Redirect("/cabinet");

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
            if (!IsAdminOrManager(model.orgId)) return Redirect("/cabinet");

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

                        //foreach (string role in model.roles.Split(','))
                        //{
                        org.UserAddRole(newUser.Email, model.roles);
                        //}

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
                    //foreach (string role in model.roles.Split(','))
                    //{
                        Organisation org = Organisation.GetById(model.orgId);
                        org.UserAddRole(model.email, model.roles);
                    //}
                }
            }

            return Redirect("/organisation/users?orgId=" + model.orgId);
        }
        
        public ActionResult UsersImport(string orgId)
        {
            if (!IsAdminOrManager(orgId)) return Redirect("/cabinet");

            var model = new Models.Organisations.Views.UsersImport();
            model.orgId = orgId;
            model.uploadHistory = Organisation.GetUploadHistory(orgId);
            
            return View(model);
        }

        [HttpPost]
        public ActionResult UsersImport(Models.Organisations.Views.UsersImport model)
        {
            if (!IsAdminOrManager(model.orgId)) return Redirect("/cabinet");

            model.uploadHistory = Organisation.GetUploadHistory(model.orgId);

            if (model.filename == null)
            {
                ViewData["serverError"] = Core.ErrorMessages.UploadFileNotSelect;
                return View(model);
            }

            //если не указан сепаратор
            if (String.IsNullOrEmpty(model.separator))
            {
                ViewData["serverError"] = Core.ErrorMessages.UploadFileNoSeparator;
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
            string usersFile = BLL.ReadUploadedFileToString(Request.Files["filename"]);

            //рассплитуем файл построчно
            string [] ufStrings = Regex.Split(usersFile, "\r\n");
            //в файле меньше двух строк
            if (ufStrings.Count() < 2)
            {
                ViewData["serverError"] = Core.ErrorMessages.UploadUsersFileLessTwoStrings;
                return View(model);
            }

            int scnEmail = -1;
            int scnSurname = -1;
            int scnName = -1;
            int scnPatronim = -1;
            int scnSex = -1;
            int scnPhone = -1;
            int scnPwd = -1;
            int scnRoles = -1;

            int dcnEmail = -1;
            int dcnSurname = -1;
            int dcnName = -1;
            int dcnPatronim = -1;
            int dcnSex = -1;
            int dcnPhone = -1;
            int dcnPwd = -1;
            int dcnRoles = -1;
            int destColumnCounter = 0;

            string historyFile = "";

            //разберем первую строку-заголовок
            int dataColumnCounter = 0;
            foreach (string s in Regex.Split(ufStrings[0], model.separator))
            {
                //узнаем индексы нужных полей
                switch(s)
                {
                    case "email":
                        scnEmail = dataColumnCounter;
                        historyFile += s + model.separator;
                        dcnEmail = destColumnCounter;
                        destColumnCounter++;
                        break;
                    case "surname":
                        scnSurname = dataColumnCounter;
                        historyFile += s + model.separator;
                        dcnSurname = destColumnCounter;
                        destColumnCounter++;
                        break;
                    case "name":
                        scnName = dataColumnCounter;
                        historyFile += s + model.separator;
                        dcnName = destColumnCounter;
                        destColumnCounter++;
                        break;
                    case "patronim":
                        scnPatronim = dataColumnCounter;
                        historyFile += s + model.separator;
                        dcnPatronim = destColumnCounter;
                        destColumnCounter++;
                        break;
                    case "sex":
                        scnSex = dataColumnCounter;
                        historyFile += s + model.separator;
                        dcnSex = destColumnCounter;
                        destColumnCounter++;
                        break;
                    case "phone":
                        scnPhone = dataColumnCounter;
                        historyFile += s + model.separator;
                        dcnPhone = destColumnCounter;
                        destColumnCounter++;
                        break;
                    case "pwd":
                        scnPwd = dataColumnCounter;
                        historyFile += s + model.separator;
                        dcnPwd = destColumnCounter;
                        destColumnCounter++;
                        break;
                    case "roles":
                        scnRoles = dataColumnCounter;
                        historyFile += s + model.separator;
                        dcnRoles = destColumnCounter;
                        destColumnCounter++;
                        break;
                    default:
                        break;
                }
                dataColumnCounter++;
            }

            //если в исходном файле нет ИЛИ поля с паролем, ИЛИ поля с ролями
            //то нам нужно добавить их в конец таблицы для истории загрузок
            if (dcnPwd == -1)
            {
                historyFile += "pwd" + model.separator;
                dcnPwd = destColumnCounter;
                destColumnCounter++;
            }
            if (dcnRoles == -1)
            {
                historyFile += "roles" + model.separator;
                dcnRoles = destColumnCounter;
                destColumnCounter++;
            }

            //если поля email нет, то сообщим об этом
            if (scnEmail == -1)
            {
                ViewData["serverError"] = Core.ErrorMessages.UploadUsersFileNoEmail;
                return View(model);
            }

            DBMain db = new DBMain();

            //начинаем собирать записи пользователей и добавлять их
            int rowsCounter = 0;
            foreach (string fileString in ufStrings)
            {
                string [] historyString = new string[destColumnCounter];

                rowsCounter++;

                //первую строку пропустим
                if (rowsCounter - 1 == 0) continue;

                //если строка пустая, тоже пропустим
                if (fileString.Length < 1) continue;

                model.rowsCount++;

                string[] columns = Regex.Split(fileString, model.separator);

                if (columns.Length < dataColumnCounter)
                {
                    model.rowsIncorrect++;
                    string a = "Количество стобцов меньше чем в заголовке";
                    string b = columns.Length.ToString() + " из " + dataColumnCounter.ToString();
                    Core.UploadFailedString log = new Core.UploadFailedString(rowsCounter
                                                                                , fileString
                                                                                , a
                                                                                , b);
                    model.errorLog.Add(log);
                    continue;
                }
                
                //проверим поля на корректность
                //почта
                string email = null;
                try { email = columns[scnEmail]; }
                catch (Exception)
                {
                    model.rowsIncorrect++;
                    continue;
                }
                Regex regex = new Regex(@"^[\w!#$%&'*+\-/=?\^_`{|}~]+(\.[\w!#$%&'*+\-/=?\^_`{|}~]+)*" + "@" + @"((([\-\w]+\.)+[a-zA-Z]{2,4})|(([0-9]{1,3}\.){3}[0-9]{1,3}))$");
                Match match = regex.Match(email);
                //если адрес почты не соответствует шаблону
                if (!match.Success)
                {
                    model.rowsIncorrect++;

                    //добавим подробности в журнал ошибок
                    Core.UploadFailedString log = new Core.UploadFailedString(rowsCounter
                                                                                , fileString
                                                                                , "email"
                                                                                , email);
                    model.errorLog.Add(log);

                    continue;
                }
                else if (db.AspNetUsers.Where(x => x.Email == email).Count() > 0)
                {
                    model.rowsCorrect++;
                    model.usersNotAdded++;
                    string b = "адрес уже зарегистрирован";
                    Core.UploadFailedString log = new Core.UploadFailedString(rowsCounter
                                                                                , fileString
                                                                                , "email"
                                                                                , b);
                    model.errorLog.Add(log);
                    continue;
                }
                historyString[dcnEmail] = email;

                //фамилия
                string surname = null;
                if (scnSurname != -1 && columns[scnSurname] != String.Empty)
                {
                    surname = columns[scnSurname];
                    if (surname.Length > 30)
                    {
                        model.rowsIncorrect++;
                        Core.UploadFailedString log = new Core.UploadFailedString(rowsCounter
                                                                                    , fileString
                                                                                    , "surname"
                                                                                    , columns[scnSurname]);
                        model.errorLog.Add(log);
                        continue;
                    }
                }
                if(scnSurname != -1) historyString[dcnSurname] = surname;

                //имя
                string name = null;
                if (scnName != -1 && columns[scnName] != String.Empty)
                {
                    name = columns[scnName];
                    if (name.Length > 30)
                    {
                        model.rowsIncorrect++;
                        Core.UploadFailedString log = new Core.UploadFailedString(rowsCounter
                                                                                    , fileString
                                                                                    , "name"
                                                                                    , columns[scnName]);
                        model.errorLog.Add(log);
                        continue;
                    }
                }
                if (scnName != -1) historyString[dcnName] = name;

                //отчество
                string patronim = null;
                if (scnPatronim != -1 && columns[scnPatronim] != String.Empty)
                {
                    patronim = columns[scnPatronim];
                    if (patronim.Length > 30)
                    {
                        model.rowsIncorrect++;
                        Core.UploadFailedString log = new Core.UploadFailedString(rowsCounter
                                                                                    , fileString
                                                                                    , "patronim"
                                                                                    , columns[scnPatronim]);
                        model.errorLog.Add(log);
                        continue;
                    }
                }
                if (scnPatronim != -1) historyString[dcnPatronim] = patronim;

                //пол
                byte? sex = null;
                if (scnSex != -1 && columns[scnSex] != String.Empty)
                {
                    if (columns[scnSex] == "1" || columns[scnSex] == "0")
                        sex = Byte.Parse(columns[scnSex]);
                    else
                    {
                        model.rowsIncorrect++;
                        Core.UploadFailedString log = new Core.UploadFailedString(rowsCounter
                                                                                    , fileString
                                                                                    , "sex"
                                                                                    , columns[scnSex]);
                        model.errorLog.Add(log);
                        continue;
                    }
                }
                if (scnSex != -1) historyString[dcnSex] = sex.ToString();

                //телефон
                string phone = null;
                if (scnPhone != -1 && columns[scnPhone] != String.Empty)
                {
                    phone = columns[scnPhone];
                    regex = new Regex(@"^\d{11,15}$");
                    match = regex.Match(phone);
                    phone = "+" + phone;
                    //если номер телефона не соответствует шаблону
                    if (!match.Success)
                    {
                        model.rowsIncorrect++;
                        Core.UploadFailedString log = new Core.UploadFailedString(rowsCounter
                                                                                    , fileString
                                                                                    , "phone"
                                                                                    , columns[scnPhone]);
                        model.errorLog.Add(log);
                        continue;
                    }
                    else if (db.AspNetUsers.Where(x => x.PhoneNumber == phone).Count() > 0)
                    {
                        model.rowsCorrect++;
                        model.usersNotAdded++;
                        string b = "номер телефона уже зарегистрирован";
                        Core.UploadFailedString log = new Core.UploadFailedString(rowsCounter
                                                                                    , fileString
                                                                                    , "phone"
                                                                                    , b);
                        model.errorLog.Add(log);
                        continue;
                    }
                }
                if (scnPhone != -1) historyString[dcnPhone] = phone;

                //пароль
                string password = null;
                if (scnPwd != -1 && columns[scnPwd] != String.Empty)
                {
                    password = columns[scnPwd];
                    //минимум 1 латинская буква, минимум 1 НЕбуква, минимум 6 символов
                    regex = new Regex(@"^(.{0,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{4,})|(.{1,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{3,})|(.{2,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{2,})|(.{3,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{1,})|(.{4,}(([a-zA-Z][^a-zA-Z])|([^a-zA-Z][a-zA-Z])).{0,})$");
                    match = regex.Match(password);
                    //если пароль не соответствует шаблону
                    if (!match.Success)
                    {
                        model.rowsIncorrect++;
                        Core.UploadFailedString log = new Core.UploadFailedString(rowsCounter
                                                                                    , fileString
                                                                                    , "pwd"
                                                                                    , columns[scnPwd]);
                        model.errorLog.Add(log);
                        continue;
                    }
                }
                else
                {
                    password = Core.BLL.GenerateRandomDigitCode(6);
                }
                historyString[dcnPwd] = password;

                //роли
                string roles = null;
                if (scnRoles != -1 && !String.IsNullOrEmpty(columns[scnRoles]))
                {
                    foreach (string s in Regex.Split(columns[scnRoles], ","))
                    {
                        if ((s != "actor" && s != "manager" && s != "viewer" && s != "inspector")
                            || s.Length > 32)
                        {
                            model.rowsIncorrect++;
                            Core.UploadFailedString log = new Core.UploadFailedString(rowsCounter
                                                                                        , fileString
                                                                                        , "roles"
                                                                                        , columns[scnRoles]);
                            model.errorLog.Add(log);
                            continue;
                        }
                        else if (roles == null) roles = s + ",";
                        else if (!roles.Contains(s)) roles += s + ",";
                    }
                    if (roles != null) roles = roles.TrimEnd(',');
                }
                else roles = "actor";
                historyString[dcnRoles] = roles;

                //если все проверки пройдены, увеличим счетчик распознанного как строки пользователей
                model.rowsCorrect++;

                //добавим пользователя в БД
                var newUser = new ApplicationUser { UserName = email, Email = email };
                newUser.Surname = surname;
                newUser.Patronim = patronim;
                newUser.Name = name;
                newUser.PhoneNumber = phone;
                newUser.Sex = sex;
                var result = HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>()
                    .Create(newUser, password);
                if (result.Succeeded)
                {
                    //добавим пользователя к организации
                    Organisation org = Organisation.GetById(model.orgId);
                    org.AddUser(email);

                    //добавим пользователю роли
                    foreach (string role in roles.Split(','))
                    {
                        org.UserAddRole(email, role);
                    }

                    //обновление поискового индекса
                    Core.BLL.SearchIndexUpdate(newUser, Core.CRUDType.Create);

                    model.usersAdded++;
                }

                //сформируем строку параметров пользователя для файла в историю загрузок
                historyFile += "\r\n";
                foreach (string s in historyString)
                {
                    historyFile += s + model.separator;
                }
            }
            
            //если пользователи добавлены, сохраним файл историй в БД
            if(model.usersAdded > 0)
            {
                OrganisationsUsersFile oufs = new OrganisationsUsersFile();
                oufs.dateCreate = DateTime.Now;
                oufs.deleted = false;
                oufs.id = Guid.NewGuid().ToString();
                oufs.orgId = model.orgId;
                oufs.usersCount = model.usersAdded;
                oufs.usersData = historyFile;
                db.OrganisationsUsersFiles.Add(oufs);
                db.SaveChanges();
                db.Dispose();
            }

            model.uploadHistory = Organisation.GetUploadHistory(model.orgId);
            model.showResult = true;

            return View(model);
        }

        public ActionResult UsersUploadFile(string id)
        {
            DBMain db = new DBMain();
            OrganisationsUsersFile ouf = db.OrganisationsUsersFiles.Single(x => x.id == id);

            if (!IsAdminOrManager(ouf.orgId))
            {
                return new EmptyResult();
            }
            
            Encoding srcEnc = Encoding.UTF8;

            string filename = String.Format("kh-usersupload-{0}-{1}.csv"
                                            ,ouf.dateCreate.ToString("dd.MM.yyyy HH:mm:ss")
                                            ,ouf.usersCount);
            return File(srcEnc.GetBytes(ouf.usersData), "text/csv", filename);
        }
        #endregion

        #region AJAX-методы
        public ActionResult ChangeName(string id, string newval)
        {
            Core.AjaxAnswer answer = new Core.AjaxAnswer();
            string orgId = id;

            if (IsAdminOrManager(orgId))
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
                        return answer.JsonContentResult();
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

            if (Core.Membership.isAdmin())
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
                        return answer.JsonContentResult();
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
                    return answer.JsonContentResult();
                }
            }
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
                    if (db.OrganisationUsers.Where(x => x.userEmail == userEmail && x.active && x.dateStop > DateTime.Now).Count() > 0)
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

            if (!IsAdminOrManager(orgId))
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
                    return answer.JsonContentResult();
                }
            }
            answer.result = Core.AjaxResults.Success;

            return answer.JsonContentResult();
        }

        public ActionResult RejectJoinRequest(string orgId, string userEmail)
        {
            Core.AjaxAnswer answer = new Core.AjaxAnswer();

            if (!IsAdminOrManager(orgId))
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
                    return answer.JsonContentResult();
                }
            }
            answer.result = Core.AjaxResults.Success;

            return answer.JsonContentResult();
        }

        public ActionResult UserDelete(string orgId, string userId)
        {
            Core.AjaxAnswer answer = new Core.AjaxAnswer();

            if (!IsAdminOrManager(orgId))
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
                    SqlParameter sqlParEmail = new SqlParameter("userEmail", user.Email);
                    SqlParameter sqlParOrgId = new SqlParameter("orgId", orgId);
                    db.Database.ExecuteSqlCommand(query, sqlParEmail, sqlParOrgId);

                    //если у пользователя не подтвержден ни номер телефона, ни адрес почты, то удалим:
                    if (!user.EmailConfirmed && !user.PhoneNumberConfirmed)
                    {
                        // - его самого из таблицы пользователей
                        query = @"
DELETE FROM AspNetUsers WHERE Email=@userEmail";
                        sqlParEmail = new SqlParameter("userEmail", user.Email);
                        db.Database.ExecuteSqlCommand(query, sqlParEmail);
                        // - историю его скитаний по организациям из таблицы привязки пользователей к организациям
                        query = @"
DELETE FROM OrganisationsUsers WHERE userEmail=@userEmail";
                        sqlParEmail = new SqlParameter("userEmail", user.Email);
                        db.Database.ExecuteSqlCommand(query, sqlParEmail);
                    }
                    else
                    {
                        //меняем статус активности и дату увольнения
                        OrganisationsUsers ou = db.OrganisationUsers.Where(x => x.userEmail == user.Email && x.orgId == orgId).Single();
                        ou.dateStop = DateTime.Now;
                        ou.active = false;
                        db.SaveChanges();
                    }
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
        #endregion
    }
}