﻿using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Data.SqlClient;
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

        public static Research GetById(string researchId)
        {
            return DBMain.db.Researches.SingleOrDefault(x => x.id == researchId);
        }

        //получение списка всех исследований для таблицы индексной страницы исследований организации
        public static List<CustomSelects.ResearchListView> GetAllForLinkResearch(string orgId)
        {
            string query = $@"
SELECT r.id
    ,r.name
    ,r.descr
    ,r.dateCreate
    ,rt.nameText AS typeDescr
	,SUBSTRING((SELECT ',' + rg.name AS [text()]
				FROM ResearchGroups rg
				WHERE rg.id IN (
					SELECT rgi.groupId 
					FROM ResearchGroupsItems rgi 
					WHERE rgi.researchId=r.id)
				FOR XML PATH('')), 2, 1000) AS groupNames
    ,r.statusId
    ,(SELECT 'info') AS info
FROM Researches r, ResearchTypes rt
WHERE r.orgId = @orgId
    AND rt.id=r.typeId
ORDER BY r.dateCreate DESC
";
            SqlParameter[] pars = { new SqlParameter("orgId", orgId)};
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
    #endregion

    namespace CustomSelects
    {
        //данные для списка исследований организации
        public class ResearchListView
        {
            public string id { get; set; }
            public string name { get; set; }
            public string descr { get; set; }
            public DateTime dateCreate { get; set; }
            public string typeDescr { get; set; }
            public string groupNames { get; set; }
            public int statusId { get; set; }
            public string info { get; set; }
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
            public string password { get; set; }
        }

        public class ScenarioCU
        {
            [Required]
            public string orgId { get; set; }
            [Required]
            public string researchId { get; set; }
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

                public static Questionnaire DeserializeFromXmlString(string xml)
                {
                    XmlSerializer s = new XmlSerializer(typeof(Questionnaire));

                    //преобразуем строку xml в поток
                    MemoryStream stream = new MemoryStream();
                    StreamWriter writer = new StreamWriter(stream);
                    writer.Write(xml);
                    writer.Flush();
                    stream.Position = 0;

                    //десериализуем поток в класс
                    Questionnaire q = (Questionnaire)s.Deserialize(stream);
                    return q;
                }
            }

            public class Question
            {
                [XmlAttribute]
                public string type { get; set; }

                [XmlAttribute]
                public int position { get; set; }

                [XmlAttribute]
                public string isSecret { get; set; }

                [XmlAttribute]
                public string text { get; set; }

                [XmlArray("Answers")]
                [XmlArrayItem("Answer", typeof(Answer))]
                public Answer[] answers { get; set; }
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
                public string isSecret { get; set; }
            }
        }
    }
}