using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace psychoTest.Models.Researches
{
    //модель исследования
    public class Research
    {
        [Key]
        public string id { get; set; }
        public string orgId { get; set; }
        public DateTime dateCreate { get; set; } = DateTime.Now;
        public string name { get; set; }
        public string descr { get; set; }
        public string password { get; set; }
        public int typeId { get; set; }
        public int statusId { get; set; } = Core.EntityStatuses.enabled.val;

        //создание
        public bool Create()
        {
            DBMain.db.Researches.Add(this);
            DBMain.db.SaveChanges();
            return true;
        }

        public static bool DeletePseudoById(string researchId)
        {
            Research r = DBMain.db.Researches.SingleOrDefault(x => x.id == researchId);
            r.statusId = Core.EntityStatuses.deleted.val;
            DBMain.db.SaveChanges();
            return true;
        }

        public static Research GetById(string researchId)
        {
            return DBMain.db.Researches.SingleOrDefault(x => x.id == researchId);
        }

        public static Research GetByPassword(string password)
        {
            return DBMain.db.Researches.SingleOrDefault(x => x.password == password);
        }

        public Scenarios.ResearchScenario GetActualActiveScenario()
        {
            return DBMain.db.ResearchScenario.Where(x => x.researchId == this.id
                                                        && x.statusId == Core.EntityStatuses.enabled.val)
                .OrderByDescending(x => x.dateCreate)
                .FirstOrDefault();
        }

        //получение списка всех исследований для AJAX-таблицы индексной страницы исследований организации
        public static List<CustomSelects.ResearchListView> GetAllForAjax(string orgId)
        {
            string query = $@"
SELECT r.id
    ,r.name
    ,r.descr
    ,(SELECT FORMAT(r.dateCreate, 'dd.MM.yyyy')) as dateCreate
	,SUBSTRING((SELECT ',' + rg.name AS [text()]
				FROM ResearchGroups rg
				WHERE rg.id IN (
					SELECT rgi.groupId 
					FROM ResearchGroupsItems rgi 
					WHERE rgi.researchId=r.id)
				FOR XML PATH('')), 2, 1000) AS groupNames
    ,rt.nameText AS typeDescr
    ,es.nameText AS statusDescr
    ,(SELECT 'info') AS info
FROM Researches r, ResearchTypes rt, EntityStatuses es
WHERE r.orgId = @orgId
    AND rt.id=r.typeId
	AND es.id = r.statusId
ORDER BY r.dateCreate DESC
";
            SqlParameter[] pars = { new SqlParameter("orgId", orgId) };
            List<CustomSelects.ResearchListView> list
                = DBMain.db.Database.SqlQuery<CustomSelects.ResearchListView>(query, pars).ToList();

            return list;
        }

        public bool SetPassword()
        {
            Research r = DBMain.db.Researches.SingleOrDefault(x => x.password == this.password);

            if (r == null || r.id == this.id)
            {
                DBMain.db.SaveChanges();
                return true;
            }
            else return false;
        }

        public bool Save()
        {
            DBMain.db.SaveChanges();
            return true;
        }

        //получение таблицы срезов данных
        public List<CustomSelects.DataSectionListView> GetDataSections()
        {
            string query = $@"
SELECT 
	rs.scenarioId
	,(SELECT FORMAT(MIN(rs.dateStart), 'dd.MM.yyyy HH:mm:ss')) as dateBegin
	,COUNT(*) as answersCount
FROM ResearchSessions rs
WHERE rs.researchId = @researchId 
	AND rs.finished = @finished
	AND rs.statusId = @statusActive
GROUP BY rs.scenarioId
ORDER BY dateBegin DESC
";
            SqlParameter[] pars = {
                new SqlParameter("researchId", this.id)
                ,new SqlParameter("finished", true)
                ,new SqlParameter("statusActive", Core.EntityStatuses.enabled.val)
            };
            List<CustomSelects.DataSectionListView> list
                = DBMain.db.Database.SqlQuery<CustomSelects.DataSectionListView>(query, pars).ToList();

            return list;
        }

        public CustomSelects.DataSectionListView GetDataSectionByScenarioId(string scenarioId)
        {
            string query = $@"
SELECT 
	rs.scenarioId
	,(SELECT FORMAT(MIN(rs.dateStart), 'dd.MM.yyyy HH:mm:ss')) as dateBegin
	,COUNT(*) as answersCount
FROM ResearchSessions rs
WHERE rs.researchId = @researchId 
	AND rs.finished = @finished
	AND rs.statusId = @statusActive
    AND rs.scenarioId = @scenarioId
GROUP BY rs.scenarioId
ORDER BY dateBegin DESC
";
            SqlParameter[] pars = {
                new SqlParameter("researchId", this.id)
                ,new SqlParameter("finished", true)
                ,new SqlParameter("scenarioId", scenarioId)
                ,new SqlParameter("statusActive", Core.EntityStatuses.enabled.val)
            };
            CustomSelects.DataSectionListView ds
                = DBMain.db.Database.SqlQuery<CustomSelects.DataSectionListView>(query, pars).FirstOrDefault();

            return ds;
        }
    }

    #region Вспомогательные таблицы
    //таблица типов исследований
    public class ResearchType
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
        public string nameText { get; set; }

        public static List<ResearchType> GetTypes()
        {
            return DBMain.db.ResearchTypes.OrderBy(x => x.name).ToList(); ;
        }

        public static ResearchType GetById(int id)
        {
            return DBMain.db.ResearchTypes.SingleOrDefault(x => x.id == id);
        }
    }

    //таблица групп
    public class ResearchGroup
    {
        [Key]
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string name { get; set; }
        public string orgId { get; set; }

        public bool Create()
        {
            DBMain.db.ResearchGroups.Add(this);
            DBMain.db.SaveChanges();
            return true;
        }
    }

    //таблица привязки исследований к группе
    public class ResearchGroupsItems
    {
        [Key, Column(Order = 0)]
        public string groupId { get; set; }
        [Key, Column(Order = 1)]
        public string researchId { get; set; }

        public bool Create()
        {
            DBMain.db.ResearchGroupsItems.Add(this);
            DBMain.db.SaveChanges();
            return true;
        }

        public static List<ResearchGroup> GetByOrgId(string orgId)
        {
            string query = $@"
SELECT 
    rg.id
    ,rg.orgId
	,rg.name
FROM ResearchGroups rg
	,ResearchGroupsItems rgi
WHERE rg.id = rgi.groupId
	AND rg.orgId = @orgId
";
            SqlParameter[] pars = {
                new SqlParameter("orgId", orgId)};

            return DBMain.db.Database.SqlQuery<ResearchGroup>(query, pars).ToList();
        }
    }

    //таблица файлов RAW результатов исследований
    public class ResearchDataSectionsRawResult
    {
        [Key]
        public string id { get; set; }
        public string scenarioId { get; set; }
        public string researchId { get; set; }
        public int statusId { get; set; }
        public DateTime dateCreate { get; set; }
        public string raw { get; set; }
        public string filename { get; set; }
        public int answersCount { get; set; }

        public static ResearchDataSectionsRawResult GetById(string resultId)
        {
            return DBMain.db.ResearchDataSectionsRawResults.SingleOrDefault(x => x.id == resultId);
        }

        public bool Create()
        {
            DBMain.db.ResearchDataSectionsRawResults.Add(this);
            DBMain.db.SaveChanges();
            return true;
        }

        public static bool DeleteById(string resultId)
        {
            string query = @"DELETE FROM ResearchDataSectionsRawResults WHERE id=@id";
            DBMain.db.Database.ExecuteSqlCommand(query, new SqlParameter("id", resultId));
            return true;
        }

        public static ResearchDataSectionsRawResult GetByScenarioId(string scenarioId)
        {
            return DBMain.db.ResearchDataSectionsRawResults.FirstOrDefault(x => x.scenarioId == scenarioId);
        }

        public static ResearchDataSectionsRawResult FormAnonCalculateAndCreate(string scenarioId)
        {
            var sc = Scenarios.ResearchScenario.GetById(scenarioId);
            var sessions = Sessions.ResearchSession.GetFinishedByScenarioId(sc.id);
            var research = Research.GetById(sc.researchId);
            ResearchDataSectionsRawResult dsrr = new ResearchDataSectionsRawResult();

            dsrr.answersCount = 0;
            dsrr.dateCreate = sessions[0].dateStart;
            dsrr.filename = String.Format("kh-raw-{0}-{1}.csv"
                                            , research.name
                                            , dsrr.dateCreate.ToString("dd.MM.yyyy HH:mm:ss")); ;
            dsrr.id = Guid.NewGuid().ToString();
            dsrr.raw = "";
            dsrr.researchId = sc.researchId;
            dsrr.scenarioId = sc.id;
            dsrr.statusId = Core.EntityStatuses.enabled.val;

            //десереализуем шаблон опросника
            Scenarios.Questionnaires.Questionnaire questionnaire
                = (Scenarios.Questionnaires.Questionnaire)Scenarios.Questionnaires.Questionnaire
                    .DeSerializeFromXmlString(
                        sc.raw
                        , typeof(Scenarios.Questionnaires.Questionnaire));

            //построим заголовок файла
            int questionsCount = 0;
            foreach (Scenarios.Questionnaires.Question q in questionnaire.questions)
            {
                questionsCount++;

                //вставляем q0 если есть преамбула
                if (questionsCount == 1 && q.answers.Count() == 0)
                {
                    questionsCount--;
                }

                dsrr.raw += ";q" + questionsCount.ToString();
            }
            dsrr.raw += ";\r\n";

            foreach (Sessions.ResearchSession rs in sessions)
            {
                dsrr.answersCount++;
                //десереализуем заполненный опросник
                Scenarios.Questionnaires.QuestionnaireWI filledQuestionnaire
                    = (Scenarios.Questionnaires.QuestionnaireWI)Scenarios.Questionnaires.QuestionnaireWI
                        .DeSerializeFromXmlString(
                            rs.raw
                            , typeof(Scenarios.Questionnaires.QuestionnaireWI));

                //порядковый номер ответа
                dsrr.raw += $"{dsrr.answersCount};";

                //перебираем вопросы
                foreach (Scenarios.Questionnaires.Question q in filledQuestionnaire.questions)
                {
                    //меняем указанные символы на палочки. это нужно для корректного представления 
                    //в программе просмотра
                    string answer = q.answer?.Replace(";", ",").Replace("\r", "|").Replace("\n", "|");

                    dsrr.raw += $"{answer};";
                }

                dsrr.raw += "\r\n";
            }

            //если в момент добавления расчета происходит ошибка, значит результаты по сценарию уже посчитаны
            //в этом случае вернем данные, которые уже в БД
            try
            {
                DBMain.db.ResearchDataSectionsRawResults.Add(dsrr);
                DBMain.db.SaveChanges();
            }
            catch (Exception)
            {
                dsrr = GetByScenarioId(sc.id);
            }

            return dsrr;
        }
    }
    #endregion

    namespace CustomSelects
    {
        //данные для списка исследований организации
        public class ResearchListView
        {
            public string id { get; set; }
            public string name { get; set; }
            public string descr { get; set; }
            public string dateCreate { get; set; }
            public string typeDescr { get; set; }
            public string groupNames { get; set; }
            //public int statusId { get; set; }
            public string statusDescr { get; set; }
            public string info { get; set; }
        }

        //данные для сводки срезов данных исследования
        public class DataSectionListView
        {
            public string dateBegin { get; set; }
            public string scenarioId { get; set; }
            public int answersCount { get; set; }
        }
    }

    namespace Views
    {
        public class Index
        {
            [Required]
            public string orgId { get; set; }
            public List<CustomSelects.ResearchListView> researches { get; set; } = new List<CustomSelects.ResearchListView>();
            public List<ResearchGroup> orgResearchsGroups { get; set; } = new List<ResearchGroup>();
        }

        public class Create
        {
            [Required]
            public string orgId { get; set; }
            public List<ResearchGroup> groups { get; set; } = new List<ResearchGroup>();
            public List<ResearchType> types { get; set; } = ResearchType.GetTypes();
        }

        public class CreateSubmit
        {
            [Required]
            public string orgId { get; set; }
            [Required]
            public string name { get; set; }
            public string descr { get; set; }
            public string groupId { get; set; }
            public string groupName { get; set; }
            [Required]
            public string typeId { get; set; }
        }

        public class Show
        {
            [Required]
            public string orgId { get; set; }
            [Required]
            public string researchId { get; set; }
            public string name { get; set; } 
            public string descr { get; set; }
            public string typeDescr { get; set; }
            public string password { get; set; }
            public List<CustomSelects.DataSectionListView> dataSections { get; set; } 
                = new List<CustomSelects.DataSectionListView>();
            public Scenarios.ResearchScenario activeScenario { get; set; } = null;
        }

        public class ScenarioCU
        {
            [Required]
            public string orgId { get; set; }
            [Required]
            public string researchId { get; set; }
        }

        public class Filling
        {
            public string sid { get; set; }
            public int curQuestionIdx { get; set; }
            public int questionsCount { get; set; }
            public Scenarios.Questionnaires.Question question { get; set; }
            public string action { get; set; }
            public string answer { get; set; }

            //метод заполнения модели для вьюшки заполнения анкеты
            public void Fill(Scenarios.Questionnaires.QuestionnaireWI quest)
            {
                curQuestionIdx = quest.curQuestionIdx;
                questionsCount = quest.questions[quest.questions.Count()-1].position;
                question = quest.questions[curQuestionIdx];

                //костыль!
                //заменим \n на <br/> в тексте вопроса
                question.text = question.text.Replace("\\n", "<br/>");

                //спрячем секретные ответы, если к ним нет ключей
                if (question.answers == null) return;
                foreach (Scenarios.Questionnaires.Answer a in question.answers)
                {
                    if (a.isSecret)
                    {
                        bool haveKey = false;
                        foreach (Scenarios.Questionnaires.Question q in quest.questions)
                        {
                            //если ответа нет, или он пустой, то пропускаем вопрос
                            if (String.IsNullOrEmpty(q.answer)) continue;

                            foreach (Scenarios.Questionnaires.Answer qA in q.answers)
                            {
                                if (Array.IndexOf(Regex.Split(q.answer
                                                    , Scenarios.Questionnaires.Vars.answersSeparator)
                                        , qA.position.ToString()) != -1
                                    && qA.keyto != null)
                                {
                                    foreach (string s 
                                        in Regex.Split(qA.keyto, Scenarios.Questionnaires.Vars.keysSeparator))
                                    {
                                        if (s == $"{question.position}{Scenarios.Questionnaires.Vars.subkeysSeparator}{a.position}")
                                        {
                                            haveKey = true;
                                            break;
                                        }
                                    }
                                }
                                    
                            }
                            if (haveKey) break;
                        }
                        if (haveKey) a.isSecret = false;
                    }
                }
            }
        }
    }

    namespace Scenarios
    {
        public class ResearchScenario
        {
            public string id { get; set; }
            public string name { get; set; }
            public string descr { get; set; }
            public string researchId { get; set; }
            public DateTime dateCreate { get; set; } = DateTime.Now;
            public string raw { get; set; }
            public int statusId { get; set; } = Core.EntityStatuses.enabled.val;

            //добавление сценария
            public bool Add()
            {
                DBMain.db.ResearchScenario.Add(this);
                DBMain.db.SaveChanges();
                return true;
            }

            public static ResearchScenario GetById(string scenarioId)
            {
                return DBMain.db.ResearchScenario.SingleOrDefault(x => x.id == scenarioId);
            }
        }

        namespace Questionnaires
        {
            public class Questionnaire
            {
                [XmlArray("Questions")]
                [XmlArrayItem("Question", typeof(Question))]
                public Question[] questions { get; set; }

                [XmlAttribute]
                public string name { get; set; }

                [XmlAttribute]
                public string descr { get; set; }

                public static object DeSerializeFromXmlString(string xml, Type type)
                {
                    XmlSerializer s = new XmlSerializer(type);

                    //преобразуем строку xml в поток
                    MemoryStream stream = new MemoryStream();
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write(xml);
                    writer.Flush();
                    stream.Position = 0;

                    //десериализуем поток в класс и возвращаем его
                    return s.Deserialize(stream);
                }

                public string SerializeToXmlString(Type type)
                {
                    XmlSerializer s = new XmlSerializer(type);
                    Core.Utf8StringWriter sw = new Core.Utf8StringWriter();
                    s.Serialize(sw, this);
                    sw.Close();
                    return sw.ToString();
                }

                public int FindNearestAvailableQ(int curQI, string direction)
                {
                    if (curQI < 0 || curQI > questions.Length) return -1;

                    int newCurQI = curQI;

                    if (direction == "next")
                        newCurQI++;
                    else if (direction == "prev")
                        newCurQI--;

                    Question curQ = null;
                    try
                    {
                        curQ = questions[newCurQI];
                    }
                    catch (Exception) { return newCurQI; }

                    //проверка на секретный вопрос и наличие ключей к нему
                    if (curQ != null && curQ.isSecret)
                    {
                        foreach (Question q in questions)
                        {
                            //если ответа нет, или он пустой, то пропускаем вопрос
                            if (String.IsNullOrEmpty(q.answer) || q.answers == null) continue;
                            
                            foreach (Answer a in q.answers)
                            {
                                if (Array.IndexOf(Regex.Split(q.answer, Vars.answersSeparator)
                                        , a.position.ToString()) != -1
                                    && a.keyto != null)
                                {
                                    foreach (string s in Regex.Split(a.keyto, Vars.keysSeparator))
                                    {
                                        if(Regex.Split(s, Vars.subkeysSeparator)[0]
                                            == curQ.position.ToString())
                                            return newCurQI;
                                    }
                                }
                            }
                        }
                        return FindNearestAvailableQ(newCurQI, direction);
                    }

                    return newCurQI;
                }                
            }

            [XmlRoot(ElementName = "Questionnaire")]
            public class QuestionnaireWI : Questionnaire
            {
                [XmlAttribute]
                public int curQuestionIdx { get; set; } = 0;
            }

            public class Question
            {
                [XmlAttribute]
                public string type { get; set; }

                [XmlAttribute]
                public int position { get; set; }

                [XmlAttribute]
                public bool isSecret { get; set; }
                
                [XmlAttribute]
                public bool allowEmpty { get; set; }

                [XmlAttribute]
                public string text { get; set; }

                [XmlArray("Answers")]
                [XmlArrayItem("Answer", typeof(Answer))]
                public Answer[] answers { get; set; }

                [XmlAttribute]
                public string answer { get; set; }
            }

            public class Answer
            {
                [XmlAttribute]
                public int position { get; set; }

                [XmlAttribute]
                public string keyto { get; set; }

                [XmlText]
                public string value { get; set; }

                [XmlAttribute]
                public bool isSecret { get; set; }
            }

            public class QuestionTypes
            {
                public const string hard = "hard";
                public const string text = "text";
                public const string soft = "soft";
            }

            public class Vars
            {
                public const string answersSeparator = "#";
                public const string keysSeparator = ";";
                public const string subkeysSeparator = ":";
            }
        }
    }

    namespace Sessions
    {
        public class ResearchSession
        {
            [Key]
            public string id { get; set; } = Guid.NewGuid().ToString();
            public string idShort { get; set; }
            public string researchId { get; set; }
            public string scenarioId { get; set; }
            public DateTime dateStart { get; set; } = DateTime.Now;
            public DateTime dateFinish { get; set; }
            public bool finished { get; set; } = false;
            public int statusId { get; set; }
            public string raw { get; set; }

            public bool Create()
            {
                DBMain.db.ResearchSessions.Add(this);
                DBMain.db.SaveChanges();
                return true;
            }

            public bool Save()
            {
                DBMain.db.SaveChanges();
                return true;
            }

            public static ResearchSession GetActiveByIdShort(string idShort)
            {
                return DBMain.db.ResearchSessions.SingleOrDefault(x => x.idShort == idShort
                                                                    && x.statusId == Core.EntityStatuses.enabled.val);
            }
            
            public static bool DeletePseudoByScenarioId(string scenarioId)
            {
                string query = @"
UPDATE ResearchSessions
SET statusId=@statusId
WHERE scenarioId=@scenarioId
";
                SqlParameter[] pars = {
                    new SqlParameter("statusId", Core.EntityStatuses.deleted.val)
                    ,new SqlParameter("scenarioId", scenarioId)
                };
                DBMain.db.Database.ExecuteSqlCommand(query, pars);
                return true;
            }

            public static List<ResearchSession> GetFinishedByScenarioId(string scenarioId)
            {
                return DBMain.db.ResearchSessions
                    .Where(x => x.scenarioId == scenarioId && x.finished)
                    .OrderBy(x => x.dateFinish)
                    .ToList();
            }
        }
    }
}