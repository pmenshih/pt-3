using System;
using System.Web.Mvc;
using psychoTest.Models.Researches;
using psychoTest.Models.Researches.Interpretations;
using psychoTest.Core;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Text;


namespace psychoTest.Controllers
{
    [Authorize]
    public class ResearchController : Controller
    {
        public class UploadScenarioErrorsDesc { public string excMes1; public string excMes2; }

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
            model.researches = Research.GetAllForAjax(model.orgId);
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

        public ActionResult DataSectionDownloadRaw(string orgId
                                                    ,string researchId
                                                    ,string scenarioId
                                                    ,string resultId)
        {
            var dsrr = ResearchDataSectionsRawResult.GetById(resultId);
            var research = Research.GetById(researchId);
            var org = Models.Organisations.Organisation.GetById(orgId);

            //проверка прав доступа
            if (org.id != research.orgId || (!Membership.isAdmin()
                                                && !Membership.isViewer(orgId)
                                                && !Membership.isCoach()))
            {
                AjaxAnswer answer = new AjaxAnswer();
                answer.result = AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            Encoding srcEnc = Encoding.UTF8;

            return File(srcEnc.GetBytes(dsrr.raw), "text/csv", dsrr.filename);
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
            model.descr = research.descr;
            model.typeDescr = ResearchType.GetById(research.typeId)?.nameText;
            model.orgId = org.id;
            model.researchId = research.id;
            model.password = research.password;
            //заполнение срезов данных
            model.dataSections = research.GetDataSections();
            //получение Id активного сценария
            model.activeScenario = research.GetActualActiveScenario();

            return View(model);
        }

        public ActionResult ScenarioDownload(string scenarioId, string orgId, string researchId)
        {
            Models.Researches.Scenarios.ResearchScenario rs
                = Models.Researches.Scenarios.ResearchScenario.GetById(scenarioId);

            //проверка прав
            if (researchId != rs.researchId || (!Membership.isAdmin() && !Membership.isManager(orgId)))
                throw new Exception();
            
            Encoding srcEnc = Encoding.UTF8;

            string filename = String.Format("kh-scenario-{0}.xml"
                                            ,DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            return File(srcEnc.GetBytes(rs.raw), "text/xml", filename);
        }

        [HttpPost]
        public ActionResult ScenarioGetRaw(string scenarioId, string orgId, string researchId)
        {
            Models.Researches.Scenarios.ResearchScenario rs
                = Models.Researches.Scenarios.ResearchScenario.GetById(scenarioId);

            //проверка прав
            if (researchId != rs.researchId 
                || (!Membership.isAdmin() && !Membership.isCoach() && !Membership.isManager(orgId)))
                throw new Exception();

            Encoding srcEnc = Encoding.UTF8;

            string filename = String.Format("kh-scenario-{0}.xml"
                                            , DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            return Content(rs.raw, "text/xml");
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
            Models.Researches.Scenarios.Questionnaires.QuestionnaireWI quest
                = (Models.Researches.Scenarios.Questionnaires.QuestionnaireWI)BLL.DeSerializeFromXmlString(
                    rs.raw
                    ,typeof(Models.Researches.Scenarios.Questionnaires.QuestionnaireWI));

            //проматываем все вопросы, если
            //сессия завершена
            //или дата окончания меньше текущей
            //или статус не равен активному
            if (rs.finished
                || rs.dateFinish < DateTime.Now
                || rs.statusId != EntityStatuses.enabled.val)
            {
                quest.curQuestionIdx = quest.questions.Length;
            }

            //если индекс текущего вопроса больше их количества, покажем финиш
            if(quest.curQuestionIdx >= quest.questions.Length)
                return Redirect($"/research/finish");

            //создадим и заполним модель страницы
            Models.Researches.Views.Filling model = new Models.Researches.Views.Filling();
            model.sid = sid;
            ///!!!
            ///Fill меняет ответы вопроса, а значит должен идти ВСЕГДА ПОСЛЕ сохранения текущей версии
            ///опросника в базу данных!!!
            ///!!!
            model.Fill(quest);

            return View(model);
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Filling(Models.Researches.Views.Filling model)
        {
            Models.Researches.Sessions.ResearchSession rs
                = Models.Researches.Sessions.ResearchSession.GetActiveByIdShort(model.sid);

            //выходим, если 
            //сессии нет
            //или она уже завершена
            //или дата окончания меньше текущей
            //или статус не равен активному
            if (rs == null
                || rs.finished
                || rs.dateFinish < DateTime.Now
                || rs.statusId != EntityStatuses.enabled.val) throw new Exception();

            //десериализуем опросник
            Models.Researches.Scenarios.Questionnaires.QuestionnaireWI quest
                = (Models.Researches.Scenarios.Questionnaires.QuestionnaireWI)BLL.DeSerializeFromXmlString(
                    rs.raw
                    , typeof(Models.Researches.Scenarios.Questionnaires.QuestionnaireWI));

            if (model.action == "prev")
            {
                //если нажата кнопка назад и индекс текщуего вопроса равен нулю, то ничего не делаем
                if (quest.curQuestionIdx == 0) return Redirect($"/research/filling/{model.sid}");
                //если индекс текущего вопроса равен количеству вопросов, то
                //это какая-то херня, падаем
                else if (quest.curQuestionIdx == quest.questions.Length)
                    throw new Exception();
            }
            else if (model.action == "next")
            {
                //если нажата кнопка вперед и индекс текущего вопроса больше количества вопросов,
                //то ничего не делаем
                if (quest.curQuestionIdx > quest.questions.Length) return Redirect($"/research/finish");
                else 
                {
                    Models.Researches.Scenarios.Questionnaires.Question q 
                        = quest.questions[quest.curQuestionIdx];
                    //жесткая альтернатива
                    if (q.type == Models.Researches.Scenarios.Questionnaires.QuestionTypes.hard
                        //нужно, чтобы работал пролог
                        && q.answers?.Length > 0)
                    {
                        //ответ не выбран, возвращаем исходную страницу
                        if(!q.allowEmpty && String.IsNullOrEmpty(model.answer))
                            return Redirect($"/research/filling/{model.sid}");

                        quest.questions[quest.curQuestionIdx].answer = model.answer;
                    }
                    //мягкая альтернатива
                    else if (q.type == Models.Researches.Scenarios.Questionnaires.QuestionTypes.soft
                        && q.answers?.Length > 0)
                    {
                        //ответ не выбран, возвращаем исходную страницу
                        if (!q.allowEmpty && String.IsNullOrEmpty(model.answer))
                            return Redirect($"/research/filling/{model.sid}");

                        quest.questions[quest.curQuestionIdx].answer = model.answer;
                    }
                    //открытый вопрос
                    else if (q.type == Models.Researches.Scenarios.Questionnaires.QuestionTypes.text)
                    {
                        //ответ не выбран, возвращаем исходную страницу
                        if (!q.allowEmpty && String.IsNullOrEmpty(model.answer))
                            return Redirect($"/research/filling/{model.sid}");

                        quest.questions[quest.curQuestionIdx].answer = model.answer;
                    }
                }   
            }
            
            //разберемся с позицией следующего вопроса
            quest.curQuestionIdx = quest.FindNearestAvailableQ(quest.curQuestionIdx, model.action);

            //если индекс текущего вопроса больше или равен количеству вопросов, то
            //финализируем сессию
            if (quest.curQuestionIdx >= quest.questions.Length)
            {
                rs.dateFinish = DateTime.Now;
                rs.finished = true;
            }

            //сериализуем опросник в строку
            string raw = BLL.SerializeToXmlString(
                            typeof(Models.Researches.Scenarios.Questionnaires.QuestionnaireWI)
                            ,quest);
            //сохраним сессию в БД
            rs.raw = raw;
            rs.Save();

            //сделаем редирект на страницу инициализации
            return Redirect($"/research/filling/{model.sid}");
        }

        [AllowAnonymous]
        public ActionResult Finish()
        {
            return View();
        }

        public ActionResult Interpretation()
        {
            //проверка прав доступа

            var model = new Models.Researches.Views.Interpretation();
            model.orgId = Request.QueryString[RequestVals.orgId];
            model.researchId = Request.QueryString[RequestVals.researchId];

            string xml = @"
<SpecificationInterpretation>
	<Objects>
		<Object type='table' id='table1' dataSource='this.sc.q1.answers'>
			<Objects>
				<Object title='Возраст' value='dataSource.text' />
				<Object value='count(this.data.q1.answers.answer==dataSource.position)' />
			</Objects>
		</Object>
	</Objects>
</SpecificationInterpretation>
";

            if (!xml.Contains("<Interpretation>"))
            {
                xml = "<Interpretation>" + xml + "</Interpretation>";
            }

            Interpretation interpretation = BLL.DeSerializeFromXmlString(xml, typeof(Interpretation)) 
                as Interpretation;

            Models.Researches.Scenarios.ResearchScenario scenario
                = Models.Researches.Scenarios.ResearchScenario.GetById(
                    Request.QueryString[RequestVals.scenarioId]);

            Models.Researches.Scenarios.Questionnaires.Questionnaire questionnaire
                = BLL.DeSerializeFromXmlString(scenario.raw
                    , typeof(Models.Researches.Scenarios.Questionnaires.Questionnaire)) 
                as Models.Researches.Scenarios.Questionnaires.Questionnaire;

            if (interpretation.sInt != null && interpretation.sInt.objects != null)
            {
                foreach (PXSObject sIntObject in interpretation.sInt.objects)
                {
                    if (!String.IsNullOrEmpty(sIntObject.dataSource))
                    {
                        dynamic b = questionnaire.questions[1].answers;

                        foreach (var a in b)
                        {
                            a.value = "1";
                            try
                            {
                                a.prop = "3";
                            }
                            catch (Exception exc) { }
                        }
                    }
                }
            }
            
            return View(model);
        }
        

        #region AJAX-методы
        //смена/установка описания сценария
        public ActionResult SetDescr()
        {
            var org = Models.Organisations.Organisation.GetById(Request[RequestVals.orgId]);
            var research = Research.GetById(Request[RequestVals.researchId]);
            var newVal = Request[RequestVals.val];

            AjaxAnswer answer = new AjaxAnswer();

            //права
            if (org.id != research.orgId || (!Membership.isAdmin() && !Membership.isManager(org.id)))
            {
                answer.result = AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            research.descr = newVal;

            if (research.Save())
                answer.result = AjaxResults.Success;
            else answer.result = AjaxResults.ResearchPasswordExist;

            return answer.JsonContentResult();
        }

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

        //смена/установка названия сценария
        public ActionResult SetName()
        {
            var org = Models.Organisations.Organisation.GetById(Request[RequestVals.orgId]);
            var research = Research.GetById(Request[RequestVals.researchId]);
            var valName = Request[RequestVals.val];

            AjaxAnswer answer = new AjaxAnswer();

            //права
            if (org.id != research.orgId || (!Membership.isAdmin() && !Membership.isManager(org.id)))
            {
                answer.result = AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            research.name = valName;

            if (research.Save())
                answer.result = AjaxResults.Success;
            else answer.result = AjaxResults.ResearchPasswordExist;

            return answer.JsonContentResult();
        }

        public ActionResult GetDataSections(string orgId, string researchId)
        {
            AjaxAnswer answer = new AjaxAnswer();
            answer.result = AjaxResults.CodeError;

            var research = Research.GetById(researchId);
            var org = Models.Organisations.Organisation.GetById(orgId);

            //проверка права доступа
            if (org.id != research.orgId || !RolesIsIVMCA(orgId))
            {
                answer.result = AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            List<Models.Researches.CustomSelects.DataSectionListView> ds
                = research.GetDataSections();

            answer.data = new JavaScriptSerializer().Serialize(ds);
            answer.result = AjaxResults.Success;

            return answer.JsonContentResult();
        }

        public ActionResult GetAll(string orgId)
        {
            AjaxAnswer answer = new AjaxAnswer();
            answer.result = AjaxResults.CodeError;

            //проверка права доступа
            if (!RolesIsIVMCA(orgId))
            {
                answer.result = AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            List<Models.Researches.CustomSelects.ResearchListView> res
                = Research.GetAllForAjax(orgId);

            answer.data = new JavaScriptSerializer().Serialize(res);
            answer.result = AjaxResults.Success;

            return answer.JsonContentResult();
        }

        [HttpPost]
        public ActionResult Delete(string orgId, string researchId)
        {
            AjaxAnswer answer = new AjaxAnswer();

            //проверка права доступа
            if (!Membership.isAdmin() && !Membership.isManager(orgId))
            {
                answer.result = AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            answer.result = AjaxResults.CodeError;
            if (Research.DeletePseudoById(researchId))
                answer.result = AjaxResults.Success;

            return answer.JsonContentResult();
        }

        [HttpPost]
        public ActionResult DataSectionDelete(string orgId, string researchId, string scenarioId)
        {
            AjaxAnswer answer = new AjaxAnswer();

            var org = Models.Organisations.Organisation.GetById(orgId);
            var research = Research.GetById(researchId);

            //проверка права доступа
            if (org.id != research.orgId || (!Membership.isAdmin() && !Membership.isManager(orgId)))
            {
                answer.result = AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            answer.result = AjaxResults.CodeError;
            if (Models.Researches.Sessions.ResearchSession.DeletePseudoByScenarioId(scenarioId))
                answer.result = AjaxResults.Success;

            return answer.JsonContentResult();
        }

        public ActionResult DataSectionPrepareDownloadRaw(string orgId, string researchId, string scenarioId)
        {
            var research = Research.GetById(researchId);
            var org = Models.Organisations.Organisation.GetById(orgId);

            AjaxAnswer answer = new AjaxAnswer();
            answer.result = AjaxResults.CodeError;

            //проверка прав доступа
            if (org.id != research.orgId || (!Membership.isAdmin()
                                                && !Membership.isViewer(orgId)
                                                && !Membership.isCoach()))
            {
                answer.result = AjaxResults.NoRights;
                return answer.JsonContentResult();
            }

            var dsrr = ResearchDataSectionsRawResult.GetByScenarioId(scenarioId);
            //выясним, совпадает ли количество активных завершенных сессий с счетчиком ответов
            //если да, то делать ничего не надо
            //если нет и файл результатов уже существует, то его удалить
            if (dsrr != null)
            {
                if (research.GetDataSectionByScenarioId(scenarioId).answersCount != dsrr.answersCount)
                {
                    ResearchDataSectionsRawResult.DeleteById(dsrr.id);
                    dsrr = null;
                }
            }

            //если результатов нет
            if (dsrr == null)
            {
                //определим тип исследования
                if (ResearchType.GetById(research.typeId).name == "anonsurvey")
                    dsrr = ResearchDataSectionsRawResult.FormAnonCalculateAndCreate(scenarioId);
            }

            answer.data = dsrr.id;
            answer.result = AjaxResults.Success;

            return answer.JsonContentResult();
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
                q = (Models.Researches.Scenarios.Questionnaires.Questionnaire)BLL.DeSerializeFromXmlString(
                        rawString
                        , typeof(Models.Researches.Scenarios.Questionnaires.Questionnaire));
            }
            catch (Exception exc)
            {
                var errs = new UploadScenarioErrorsDesc()
                {
                    excMes1 = exc.Message
                                                            ,
                    excMes2 = exc.InnerException?.Message
                };

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
        #endregion
    }
}