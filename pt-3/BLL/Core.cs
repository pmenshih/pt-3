using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using psychoTest.Models;
using Microsoft.AspNet.Identity;
using System.Web.Script.Serialization;
using System.Net.Mail;
using System.Configuration;
using System.Collections.Generic;

namespace psychoTest.Core
{
    public class BLL
    {
        public static void SendEmail(string to, string subject, string message)
        {
            SmtpClient smtpClient = new SmtpClient(ConfigurationManager.AppSettings["EmailSystemHost"],
                                            Int32.Parse(ConfigurationManager.AppSettings["EmailSystemPort"]));
            smtpClient.EnableSsl = true;
            smtpClient.UseDefaultCredentials = false;
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.Credentials = new System.Net.NetworkCredential(
                                            ConfigurationManager.AppSettings["EmailSystemAddress"],
                                            ConfigurationManager.AppSettings["EmailSystemPwd"]);

            MailMessage mail = new MailMessage();
            mail.IsBodyHtml = true;
            mail.From = new MailAddress(ConfigurationManager.AppSettings["EmailSystemAddress"],
                                        ConfigurationManager.AppSettings["EmailSystemName"]);
            mail.To.Add(new MailAddress(to));

            mail.Body = message;
            mail.Subject = subject;

            smtpClient.Send(mail);
        }
        public static async Task<string> SendSMS(string phone, string message)
        {
            message += " С уважением, команда keyhabits.ru.";
            using (var client = new HttpClient())
            {
                phone = phone.TrimStart('+');
                //string message = "Пароль — " + code + ". С уважением, команда keyhabits.ru.";
                var responseString = await client.GetStringAsync("http://gate.smsaero.ru/send/?user=p.menshih@gmail.com&password=KXJ89D5fTLDE4jPC6V8P1RRMtfb&to=" + phone + "&text=" + message + "&from=Key habits&type=3");
                return responseString;
            }
        }

        public static string GenerateRandomDigitCode(int len)
        {
            string chars = "1234567890";
            string code = "";
            Random rnd = new Random();
            for (int i = 0; i < len; i++)
            {
                int next = rnd.Next(0, 10);
                code += chars.Substring(next, 1);
            }

            return code;
        }

        public static void SearchIndexUpdate(object entity, CRUDType cType)
        {
            string query = "DELETE FROM SearchIndexes WHERE instanceId='";
            bool delelteOp = (cType == CRUDType.Delete);
            Models.DBMain db = new Models.DBMain();

            var user = entity as Models.AspNetUser;
            if (user != null)
            {
                if (delelteOp)
                {
                    db.Database.ExecuteSqlCommand(query + user.Id + "'");
                    return;
                }

                Models.SearchIndex si = new Models.SearchIndex();
                try
                {
                    si = db.SearchIndexes.Where(x => x.instanceId == user.Id).Single();
                }
                catch (Exception)
                {
                    si.instanceId = user.Id;
                    si.instanceType = "AspNetUsers";
                    cType = CRUDType.Create;
                }
                si.searchString = user.Surname + " " + user.Name;
                if (user.Patronim != null && user.Patronim.Length > 0)
                    si.searchString += " " + user.Patronim;
                si.searchString += "@#@";
                si.searchString += user.Email;
                si.searchString += "@#@";
                si.searchString += user.PhoneNumber;
                if (cType == CRUDType.Create)
                {
                    db.SearchIndexes.Add(si);
                }
                db.SaveChanges();
                return;
            }

            var organisation = entity as Models.Organisations.Organisation;
            if (organisation != null)
            {
                if (delelteOp)
                {
                    db.Database.ExecuteSqlCommand(query + organisation.id + "'");
                    return;
                }

                Models.SearchIndex si = new Models.SearchIndex();
                try
                {
                    si = db.SearchIndexes.Where(x => x.instanceId == organisation.id).Single();
                }
                catch (Exception)
                {
                    si.instanceId = organisation.id;
                    si.instanceType = "Organisations";
                    cType = CRUDType.Create;
                }
                si.searchString = organisation.name;
                si.searchString += "@#@";
                if (cType == CRUDType.Create)
                {
                    db.SearchIndexes.Add(si);
                }
                db.SaveChanges();

                return;
            }
        }
    }

    public class Membership
    {
        //системные роли, назначаемые только администратором вручную через бд
        public const string admin = "admin";
        public const string coach = "coach";

        //пользовательские роли по убыванию уровня доступа
        public const string manager = "manager";
        public const string viewer = "viewer";
        public const string inspector = "inspector";
        public const string actor = "actor";

        //список ролей, отсортированный в порядке иерархии
        public static List<string> rolesList = new List<string>((IEnumerable<string>)
            new string[] { actor, inspector, viewer, manager, coach, admin });

        //проверка на наличие указанной или более мощной пользовательской роли
        public static bool HaveSpecifiedOrStrongerUsersTypeRole(string roleName, string orgId = null)
        {
            System.Security.Claims.ClaimsPrincipal user
                = HttpContext.Current.GetOwinContext().Authentication.User;

            if (rolesList.IndexOf(roleName) >= rolesList.IndexOf(coach))
                return false;

            if (orgId == null)
                orgId = Models.Organisations.Organisation.GetByIdOrDefault()?.id;
            if (orgId == null)
                return false;

            Models.Organisations.Organisation org = Models.Organisations.Organisation.GetById(orgId);

            var userRoles = org.RolesGetForUser();
            foreach (string s in userRoles)
                if (rolesList.IndexOf(s) >= rolesList.IndexOf(roleName))
                    return true;

            return false;
        }

        public static bool isAdmin()
        {
            System.Security.Claims.ClaimsPrincipal user
                = HttpContext.Current.GetOwinContext().Authentication.User;

            return user.IsInRole(admin);
        }

        public static bool isCoach()
        {
            System.Security.Claims.ClaimsPrincipal user
                = HttpContext.Current.GetOwinContext().Authentication.User;

            return user.IsInRole(coach);
        }

        public static bool isManager(string orgId)
        {
            System.Security.Claims.ClaimsPrincipal user
                = HttpContext.Current.GetOwinContext().Authentication.User;
            string userEmail = user.Identity.GetEmail();

            return HaveSpecifiedOrStrongerUsersTypeRole(manager, orgId);
        }

        public static bool isInAnyRole()
        {
            System.Security.Claims.ClaimsPrincipal user
                = HttpContext.Current.GetOwinContext().Authentication.User;
            string userEmail = user.Identity.GetEmail();

            if (user.IsInRole(Membership.admin) || user.IsInRole(Membership.coach)) return true;

            return DBMain.db.OrganisationsUsersRoles.Count(x => x.userEmail == userEmail) > 0;
        }
    }

    //набор переменных HTTP-запроса
    public class RequestVals
    {
        public const string orgId = "orgId";
        //адрес, куда попадает пользователь, не прошедший проверку прав доступа
        public const string nrURL = "/cabinet";
    }

    public class AjaxAnswer
    {
        public AjaxResults result;

        public ContentResult JsonContentResult()
        {
            ContentResult content = new ContentResult();
            
            JsonResult answer = new JsonResult();
            answer.JsonRequestBehavior = JsonRequestBehavior.AllowGet;

            content.ContentType = "application/json";
            content.Content = new JavaScriptSerializer().Serialize(this);

            return content;
        }
    }

    public class UploadFailedString
    {
        public UploadFailedString() { }
        public UploadFailedString(int rN, string rD, string fCN, string fCD)
        {
            rowNumber = rN;
            rowData = rD;
            failedColumnName = fCN;
            failedColumnData = fCD;
        }
        public int rowNumber { get; set; }
        public string rowData { get; set; }
        public string failedColumnName { get; set; }
        public string failedColumnData { get; set; }
    }

    public enum CRUDType
    {
        Create = 1
            , Read
            , Update
            , Delete
    }

    public enum AjaxResults
    {
        Success = 0
        ,NoRights = 1
        ,CodeError = 2
        ,EmailConfirmed = 3                     //почта уже подтверждена
        ,PhoneAllreadyExist = 4                 //такой номер телефона уже занят
        ,IncorrectParameters = 5                //неверные входные параметры
        ,NoManagerSuicide = 6                   //менеджер не может сам с себя снять роль менеджера
        ,UserAllreadyInOrg = 7                  //пользователь уже состоит в указанной организации
        ,NoMultipleJoinRequests = 8             //нельзя подавать несколько заявок на присоединение к организации
        ,EmailAllreadyExist = 9                 //почта уже занята
    }

    public static class ErrorMessages
    {
        public const string LoginFail = "Неверный логин или пароль.";
        public const string LoginIncorrect = "Введен некорректный логин.";
        public const string EmailRegistered = "Указанный адрес электронной почты уже зарегистрирован.";
        public const string EmailIncorrect = "Указан неверный адрес электронной почты.";
        public const string SMSCodeIncorrect = "Указан неверный код из СМС.";
        public const string UploadFileNotSelect = "Не выбран файл для загрузки.";
        public const string UploadUsersFileLessTwoStrings = "В загруженном файле меньше двух строк.";
        public const string UploadUsersFileNoEmail = "В файле отсутствует поле \"email\".";
        public const string UploadFileNoSeparator = "Не указан разделитель столбцов.";
    }
}