using System;
using System.Web.Mvc;
using psychoTest.Models.Researches;
using psychoTest.Core;

namespace psychoTest.Controllers
{
    [Authorize]
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
            model.researches = Research.GetAllForLinkResearch(model.orgId);
            model.orgResearchsGroups = ResearchGroupsItems.GetByOrgId(model.orgId);

            //проверка права доступа
            if (!RolesIsICMCA(model.orgId)) return Redirect(RequestVals.nrURL);
            
            return View(model);
        }

        public ActionResult Create()
        {
            var model = new Models.Researches.Views.Create();
            model.orgId = Request.QueryString[RequestVals.orgId];
            model.groups = ResearchGroupsItems.GetByOrgId(model.orgId);

            //проверка права доступа
            if (!RolesIsICMCA(model.orgId)) return Redirect(RequestVals.nrURL);

            return View(model);
        }

        [HttpPost]
        public ActionResult Create(Models.Researches.Views.CreateSubmit model)
        {
            //права
            if(!Membership.isAdmin() && !Membership.isManager(model.orgId)) return Redirect(RequestVals.nrURL);

            //проверим валидность параметров
            if (!ModelState.IsValid) throw new Exception(Core.ErrorMessages.ResearchCreateValidate);

            //создаем исследование и заполняем данные
            Research r = new Research();
            r.name = model.name;
            r.typeId = Int32.Parse(model.typeId);
            r.orgId = model.orgId;
            r.dateCreate = DateTime.Now;
            r.id = Guid.NewGuid().ToString();
            r.descr = model.descr;
            r.statusId = EntityStatuses.enabled.val;

            //сохраняем исслодвание
            r.Create();

            //группы
            //если указано имя новой группы, сделаем её и присвоим новому исследованию
            if (!String.IsNullOrEmpty(model.groupName))
            {
                ResearchGroup rg = new ResearchGroup();
                rg.name = model.groupName;
                rg.orgId = model.orgId;
                rg.Create();

                ResearchGroupsItems rgi = new ResearchGroupsItems();
                rgi.groupId = rg.id;
                rgi.researchId = r.id;
                rgi.Create();
            }
            //если указан id группы, присвоим её новому исследованию
            else if (!String.IsNullOrEmpty(model.groupId))
            {
                ResearchGroupsItems rgi = new ResearchGroupsItems();
                rgi.groupId = model.groupId;
                rgi.researchId = r.id;
                rgi.Create();
            }

            string url = $@"/research/show?{RequestVals.orgId}={model.orgId}&{RequestVals.researchId}={r.id}";
            return Redirect(url);
        }

        public ActionResult Show()
        {
            //сбор параметров запроса
            var org = Models.Organisations.Organisation.GetById(Request.QueryString[RequestVals.orgId]);
            var research = Research.GetById(Request.QueryString[RequestVals.researchId]);

            //проверка на принадлежность исследования организации и права доступа
            if (org.id != research.orgId || !RolesIsICMCA(org.id)) return Redirect(RequestVals.nrURL);

            var model = new Models.Researches.Views.Show();
            model.name = research.name;
            model.orgId = org.id;
            model.researchId = research.id;

            return View(model);
        }

        public ActionResult ScenarioCU()
        {
            var model = new Models.Researches.Views.ScenarioCU();
            model.orgId = Request.QueryString[RequestVals.orgId];
            model.researchId = Request.QueryString[RequestVals.researchId];

            //права
            if (!Membership.isAdmin() && !Membership.isManager(model.orgId)) return Redirect(RequestVals.nrURL);

            return View(model);
        }

        public ActionResult UploadScenario()
        {
            Models.Organisations.Organisation org 
                = Models.Organisations.Organisation.GetById(Request[RequestVals.orgId]);

            AjaxAnswer answer = new AjaxAnswer();

            //права
            if (!Membership.isAdmin() && !Membership.isManager(org.id))
            {
                answer.result = AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            //прочитаем файл в строку
            string rawString = BLL.ReadUploadedFileToString(Request.Files["filename"]);

            Models.Researches.Questionnaire.Questionnaire q
                = new Models.Researches.Questionnaire.Questionnaire();

            //десереализуем
            try
            {
                q = Models.Researches.Questionnaire.Questionnaire.DeserializeFromXmlString(rawString);
            }
            catch (Exception exc)
            {

            }

            answer.data = "hello";
            answer.result = AjaxResults.Success;

            return answer.JsonContentResult();
        }
    }
}