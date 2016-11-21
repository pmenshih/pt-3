﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using System.Net.Http;

namespace psychoTest
{
    public class Core
    {
        public static async Task<string> SendSMS(string phone, string message)
        {
            message += " С уважением, команда psycho.ru.";
            using (var client = new HttpClient())
            {
                phone = phone.TrimStart('+');
                //string message = "Пароль — " + code + ". С уважением, команда psycho.ru.";
                var responseString = await client.GetStringAsync("http://gate.smsaero.ru/send/?user=p.menshih@gmail.com&password=KXJ89D5fTLDE4jPC6V8P1RRMtfb&to=" + phone + "&text=" + message + "&from=psycho.ru&type=3");
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
                    if (cType == CRUDType.Create)
                    {
                        si.instanceId = user.Id;
                        si.instanceType = "AspNetUsers";
                    }
                    else return;
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
            }
        }

        public static bool isAdmin(System.Security.Principal.IPrincipal User)
        {
            return User.IsInRole("admin");
        }

        public enum CRUDType
        {
            Create = 1
            ,Read
            ,Update
            ,Delete
        }
    }
}