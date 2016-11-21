using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Data.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace psychoTest.Models
{
    public class DBMain : DbContext
    {
        public DBMain(): base("DefaultConnection"){ }

        public DbSet<AspNetUser> AspNetUsers { get; set; }
        public DbSet<SearchIndex> SearchIndexes { get; set; }
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
}