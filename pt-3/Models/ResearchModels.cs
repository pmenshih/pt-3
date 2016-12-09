using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public int typeId { get; set; }
        public int statusId { get; set; }
    }

    //таблица типов исследований
    public class ResearchType
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
        public string nameText { get; set; }
    }

    //таблица групп
    public class ResearchGroup
    {
        [Key]
        public string id { get; set; }
        public string name { get; set; }
    }

    //таблица привязки исследований к группе
    public class ResearchGroupsItems
    {
        [Key, Column(Order = 0)]
        public string groupId { get; set; }
        [Key, Column(Order = 1)]
        public string researchId { get; set; }
    }

    namespace Views
    {
        public class Index
        {
            public string orgId { get; set; }
        }

        public class Create
        {
            public string orgId { get; set; }
        }
    }
}