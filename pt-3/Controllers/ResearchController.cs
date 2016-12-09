using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using psychoTest.Models.Researches;
using psychoTest.Core;

namespace psychoTest.Controllers
{
    public class ResearchController : Controller
    {
        //пользователь является контролёром, или зрителем, или менеджером указанной организации
        //или пользователь коуч, или администратор
        private bool RolesIsICMCA(string orgId)
        {
            //администраторы
            if (Membership.isAdmin()) return true;

            //коучи
            if (Membership.isCoach()) return true;

            //пользователь с доступом контролёра и выше
            if (Membership.HaveSpecifiedOrStrongerUsersTypeRole(Membership.inspector, orgId)) return true;
            
            return false;
        }

        public ActionResult Index()
        {
            var model = new Models.Researches.Views.Index();
            model.orgId = Request.QueryString[RequestVals.orgId];

            //проверка права доступа
            if (!RolesIsICMCA(model.orgId)) return Redirect(RequestVals.nrURL);
            
            return View(model);
        }

        public ActionResult Create()
        {
            var model = new Models.Researches.Views.Create();
            model.orgId = Request.QueryString[RequestVals.orgId];

            //проверка права доступа
            if (!RolesIsICMCA(model.orgId)) return Redirect(RequestVals.nrURL);

            return View(model);
        }
    }
}