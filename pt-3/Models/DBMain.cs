using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;

namespace psychoTest.Models
{
    public class DBMain : DbContext
    {
        public static DBMain db
        {
            get
            {
                DBMain _db = (DBMain)HttpContext.Current.Items["db"];

                if (_db == null)
                {
                    _db = new DBMain();
                    HttpContext.Current.Items["db"] = _db;
                }
                return _db;
            }
        }

        public DBMain(): base("DefaultConnection"){ }

        public DbSet<AspNetUser> AspNetUsers { get; set; }
        public DbSet<SearchIndex> SearchIndexes { get; set; }
        public DbSet<Organisations.Organisation> Organisations { get; set; }
        public DbSet<Organisations.OrganisationsUsersRole> OrganisationsUsersRoles { get; set; }
        public DbSet<Organisations.OrganisationsUsers> OrganisationUsers { get; set; }
        public DbSet<Organisations.OrganisationsUsersFile> OrganisationsUsersFiles { get; set; }
        public DbSet<EntityStatus> EntityStatuses { get; set; }
        public DbSet<Researches.ResearchType> ResearchTypes { get; set; }
        public DbSet<Researches.Research> Researches { get; set; }
        public DbSet<Researches.ResearchGroup> ResearchGroups { get; set; }
        public DbSet<Researches.ResearchGroupsItems> ResearchGroupsItems { get; set; }
        public DbSet<Researches.Scenarios.ResearchScenario> ResearchScenario { get; set; }
        public DbSet<Researches.Sessions.ResearchSession> ResearchSessions { get; set; }
        public DbSet<Researches.ResearchDataSectionsRawResult> ResearchDataSectionsRawResults { get; set; }
    }

    public class AspNetUser
    { 
        public string Id { get; set; }
        public string Email { get; set; }
        public bool EmailConfirmed { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string PhoneNumber { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTime? LockoutEndDateUtc { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }
        public string UserName { get; set; }
        public string Surname { get; set; }
        public string Name { get; set; }
        public string Patronim { get; set; }
        public byte? Sex { get; set; }
    }

    public class SearchIndex
    {
        [Key]
        [MaxLength(128)]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string instanceId { get; set; }
        public string instanceType { get; set; }
        public string searchString { get; set; }
    }

    //класс для получения списка ролей пользователя
    public class UserRolesList
    {
        public string name { get; set; }
        public string val { get; set; }
    }

    //таблица статусов сущностей
    public class EntityStatus
    {
        [Key]
        public int id { get; set; }
        public string name { get; set; }
        public string nameText { get; set; }
    }
}