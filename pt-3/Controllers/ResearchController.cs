using System;
using System.Web.Mvc;
using psychoTest.Models.Researches;
using psychoTest.Core;
using System.Web.Script.Serialization;

namespace psychoTest.Controllers
{
    [Authorize]
    public class ResearchController : Controller
    {
        //пользователь является контролёром, или зрителем, или менеджером указанной организации
        //или пользователь коуч, или администратор
        private bool RolesIsIVMCA(string orgId)
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
            if (!RolesIsIVMCA(model.orgId)) return Redirect(RequestVals.nrURL);
            
            return View(model);
        }

        public ActionResult Create()
        {
            var model = new Models.Researches.Views.Create();
            model.orgId = Request.QueryString[RequestVals.orgId];
            model.groups = ResearchGroupsItems.GetByOrgId(model.orgId);

            //проверка права доступа
            if (!Membership.isAdmin() && !Membership.isManager(model.orgId)) return Redirect(RequestVals.nrURL);

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
            if (org.id != research.orgId || !RolesIsIVMCA(org.id)) return Redirect(RequestVals.nrURL);

            var model = new Models.Researches.Views.Show();
            model.name = research.name;
            model.orgId = org.id;
            model.researchId = research.id;
            model.password = research.password;

            return View(model);
        }

        public ActionResult ScenarioCU()
        {
            var model = new Models.Researches.Views.ScenarioCU();
            var org = Models.Organisations.Organisation.GetById(Request.QueryString[RequestVals.orgId]);
            var research = Research.GetById(Request.QueryString[RequestVals.researchId]);

            model.orgId = org.id;
            model.researchId = research.id;

            //права
            if (org.id != research.orgId || (!Membership.isAdmin() && !Membership.isManager(org.id)))
                return Redirect(RequestVals.nrURL);

            return View(model);
        }

        public ActionResult UploadScenario()
        {
            var org = Models.Organisations.Organisation.GetById(Request[RequestVals.orgId]);
            var research = Research.GetById(Request[RequestVals.researchId]);

            AjaxAnswer answer = new AjaxAnswer();

            //права
            if (org.id != research.orgId || (!Membership.isAdmin() && !Membership.isManager(org.id)))
            {
                answer.result = AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            //прочитаем файл в строку
            string rawString = BLL.ReadUploadedFileToString(Request.Files["filename"]);

            Models.Researches.Scenarios.Questionnaires.Questionnaire q
                = new Models.Researches.Scenarios.Questionnaires.Questionnaire();

            //пробуем десереализовать
            try
            {
                q = Models.Researches.Scenarios.Questionnaires
                        .Questionnaire.DeSerializeFromXmlString(rawString);
            }
            catch (Exception exc)
            {
                var errs = new UploadScenarioErrorsDesc() { excMes1 = exc.Message
                                                            ,excMes2 = exc.InnerException?.Message };

                answer.data = new JavaScriptSerializer().Serialize(errs);
                answer.result = AjaxResults.ScenarioXMLError;
                return answer.JsonContentResult();
            }

            //тут надо сделать что-то что нужно сделать перед сменой сценария исследования

            //сохраняем новый сценарий в БД
            Models.Researches.Scenarios.ResearchScenario scenario
                    = new Models.Researches.Scenarios.ResearchScenario();
            scenario.id = Guid.NewGuid().ToString();
            scenario.dateCreate = DateTime.Now;
            scenario.descr = q.descr;
            scenario.name = q.name;
            scenario.raw = rawString;
            scenario.researchId = Request[RequestVals.researchId];
            scenario.statusId = EntityStatuses.enabled.val;
            scenario.Add();

            answer.result = AjaxResults.Success;
            return answer.JsonContentResult();
        }

        public class UploadScenarioErrorsDesc { public string excMes1; public string excMes2; }

        //смена/установка кодового слова
        public ActionResult SetPassword()
        {
            var org = Models.Organisations.Organisation.GetById(Request[RequestVals.orgId]);
            var research = Research.GetById(Request[RequestVals.researchId]);
            var valPassword = Request[RequestVals.val];

            AjaxAnswer answer = new AjaxAnswer();

            //права
            if (org.id != research.orgId || (!Membership.isAdmin() && !Membership.isManager(org.id)))
            {
                answer.result = AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            research.password = valPassword;

            if (research.SetPassword())
                answer.result = AjaxResults.Success;
            else answer.result = AjaxResults.ResearchPasswordExist;

            return answer.JsonContentResult();
        }

        [AllowAnonymous]
        public ActionResult Go(string code, string sid)
        {
            if (sid != null)
            {
                Models.Researches.Sessions.ResearchSession rs
                    = Models.Researches.Sessions.ResearchSession.GetActiveByIdShort(sid);
                if (rs != null)
                    return Redirect($"/research/filling/{sid}");
                else
                {
                    ViewData["serverError"] = ErrorMessages.ResearchSessionNotExist;
                    return View();
                }
            }
            else if (code != null)
            {
                //выясним, есть ли анкета с таким кодом
                Research r = Research.GetByPassword(code);
                if (r == null)
                {
                    ViewData["serverError"] = ErrorMessages.ResearchIncorrectPassword;
                    return View();
                }

                //проверим статус анкеты
                if (r.statusId != EntityStatuses.enabled.val)
                {
                    ViewData["serverError"] = ErrorMessages.ResearchNotActive;
                    return View();
                }

                //создадим сессию
                Models.Researches.Sessions.ResearchSession rSession
                    = new Models.Researches.Sessions.ResearchSession();
                //получим актуальный сценарий исследования
                Models.Researches.Scenarios.ResearchScenario rScenario = r.GetActualActiveScenario();
                //если сценария нет, то ничего не выйдет
                if (rScenario == null)
                {
                    ViewData["serverError"] = ErrorMessages.ResearchNoActiveScenario;
                    return View();
                }
                //заполним сессию
                rSession.dateFinish = new DateTime(2222, 1, 1);
                rSession.dateStart = DateTime.Now;
                rSession.finished = false;
                rSession.idShort = BLL.GenerateRandomDigStrCode(5);
                rSession.raw = rScenario.raw;
                rSession.researchId = r.id;
                rSession.scenarioId = rScenario.id;
                rSession.statusId = EntityStatuses.enabled.val;
                if (rSession.Create()) return Redirect($"/research/filling/{rSession.idShort}");
                throw new Exception();
            }

            return View();
        }
        
        [AllowAnonymous]
        public ActionResult Filling(string sid)
        {
            Models.Researches.Sessions.ResearchSession rs 
                = Models.Researches.Sessions.ResearchSession.GetActiveByIdShort(sid);

            //выходим, если сессии нет
            if (rs == null) throw new Exception();

            //десериализуем опросник
            Models.Researches.Scenarios.Questionnaires.Questionnaire quest
                = Models.Researches.Scenarios.Questionnaires.Questionnaire.DeSerializeFromXmlString(rs.raw);

            return View();
        }
    }
}