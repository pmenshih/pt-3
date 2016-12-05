using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Threading.Tasks;
using System.Net.Http;
using psychoTest.Models;
using Microsoft.AspNet.Identity;
using System.Data.SqlClient;
using System.Web.Script.Serialization;

namespace psychoTest.Core
{
    public class BLL
    {
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
                    si = db.SearchIndexes.Where(x => x.instanceId == user.Id).Single();
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
        public static bool isAdmin(System.Security.Principal.IPrincipal User)
        {
            return User.IsInRole("admin");
        }

        public static bool isInAnyRole(System.Security.Principal.IPrincipal User)
        {
            if (User.IsInRole("admin")) return true;

            DBMain db = new DBMain();
            string query = @"SELECT COUNT(*) 
                            FROM OrganisationsUsersRoles 
                            WHERE userEmail=(SELECT Email 
				                            FROM AspNetUsers 
				                            WHERE Id='" + User.Identity.GetUserId() + "')";
            int rolesCount = db.Database.SqlQuery<int>(query).Single();

            return rolesCount > 0;
        }
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
    }
}