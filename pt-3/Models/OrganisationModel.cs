using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;

namespace psychoTest.Models.Organisations
{
    public class Organisation
    {
        [Key]
        public string id { get; set; }
        public string name { get; set; }
        public DateTime dateCreate { get; set; }
        public bool moderated { get; set; }

        public Organisation()
        {
            id = Guid.NewGuid().ToString();
            dateCreate = DateTime.Now;
        }

        public static Organisation GetById(string id)
        {
            try
            {
                using (DBMain db = new DBMain())
                {
                    return db.Organisations.Where(x => x.id == id).Single();
                }
            }
            catch (Exception) { return null; }
        }

        public static Organisation GetByManager(System.Security.Principal.IPrincipal User)
        {
            try
            {
                using (DBMain db = new DBMain())
                {
                    string query = @"SELECT * 
                                    FROM Organisations o
                                    WHERE o.id IN (SELECT our.orgId 
				                                    FROM OrganisationsUsersRoles our
				                                    WHERE our.userEmail=@email
                                                        AND roleName='manager')";
                    return db.Database.SqlQuery<Organisation>(query, new SqlParameter("email", User.Identity.Name)).Single();
                }
            }
            catch (Exception) { return null; }
        }

        public static Organisation GetByUserEmail(string email)
        {
            try
            {
                using (DBMain db = new DBMain())
                {
                    string query = @"SELECT * 
                                    FROM Organisations o
                                    WHERE o.id IN (SELECT ou.orgId 
				                                    FROM OrganisationsUsers ou
				                                    WHERE ou.userEmail=@email)";
                    return db.Database.SqlQuery<Organisation>(query, new SqlParameter("email", email)).Single();
                }
            }
            catch (Exception) { return null; }
        }

        public static bool isManager(System.Security.Principal.IPrincipal User, string orgId = null)
        {
            int rolesCount = 0;
            using (DBMain db = new DBMain())
            {
                string query = @"SELECT COUNT(*) FROM OrganisationsUsersRoles WHERE userEmail=@email AND roleName='manager'";

                List<SqlParameter> pars = new List<SqlParameter>();
                pars.Add(new SqlParameter("email", User.Identity.Name));
                if (orgId != null)
                {
                    query += " AND orgId=@orgId";
                    pars.Add(new SqlParameter("orgId", orgId));
                }

                rolesCount = db.Database.SqlQuery<int>(query, pars.ToArray()).Single();
            }
            return rolesCount > 0;
        }

        public bool SetManager(System.Security.Principal.IPrincipal User)
        {
            try
            {
                using (DBMain db = new DBMain())
                {
                    string query = @"INSERT INTO OrganisationsUsersRoles 
                                    VALUES (@roleName, @userEmail, @orgId)";
                    db.Database.ExecuteSqlCommand(query
                                                    , new SqlParameter("roleName", "manager")
                                                    , new SqlParameter("userEmail", User.Identity.Name)
                                                    , new SqlParameter("orgId", this.id));
                    return true;
                }
            }
            catch (Exception) { return false; }
        }

        public bool AddUser(System.Security.Principal.IPrincipal User)
        {
            try
            {
                using (DBMain db = new DBMain())
                {
                    OrganisationsUsers ou = new OrganisationsUsers();
                    ou.active = true;
                    ou.dateStart = DateTime.Now;
                    ou.orgId = this.id;
                    ou.userEmail = User.Identity.Name;

                    db.OrganisationUsers.Add(ou);
                    db.SaveChanges();

                    return true;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public IEnumerable<Views.OrganisationUsers> GetActiveUsers()
        {
            using (DBMain db = new DBMain())
            {
                string query = @"SELECT u.Email as email
		                                ,u.Id as id
                                FROM AspNetUsers u, OrganisationsUsers ou
                                WHERE ou.active = 1
		                                AND ou.dateStop > GETDATE()
		                                AND ou.dateStart < GETDATE()
		                                AND ou.userEmail = u.Email
		                                AND ou.orgId = @orgId";
                return db.Database.SqlQuery<Views.OrganisationUsers>(query, new SqlParameter("orgId", id)).ToList<Views.OrganisationUsers>();
            }
        }
    }

    //таблица привязки ролей к пользователям организации
    public class OrganisationsUsersRole
    {
        [Key, Column(Order = 0)]
        public string roleName { get; set; }
        [Key, Column(Order = 1)]
        public string userEmail { get; set; }
        [Key, Column(Order = 2)]
        public string orgId { get; set; }
    }

    public class OrganisationsUsers
    {
        [Key, Column(Order = 1)]
        public string orgId { get; set; }
        [Key, Column(Order = 2)]
        public string userEmail { get; set; }
        public DateTime dateStart { get; set; }
        public DateTime dateStop { get; set; }
        public bool active { get; set; }

        public OrganisationsUsers()
        {
            dateStop = new DateTime(2222, 1, 1);
        }
    }

    namespace Views
    {
        [NotMapped]
        public class Index : Organisation
        {
            public int usersCount { get; set; }
            public List<AspNetUser> joinRequests { get; set; }
            public Index() { }
            public Index(Organisation org)
            {
                id = org.id;
                name = org.name;
                dateCreate = org.dateCreate;
                moderated = org.moderated;

                using (DBMain db = new DBMain())
                {
                    string query = @"SELECT * 
                                    FROM AspNetUsers u
                                    WHERE u.email IN (SELECT userEmail
                                                        FROM OrganisationsUsers
                                                        WHERE orgId=@orgId
                                                            AND active=0)";
                    joinRequests = db.Database.SqlQuery<AspNetUser>(query, new SqlParameter("orgId", id)).ToList();
                }
            }
        }

        public class OrganisationUsers
        {
            public string id { get; set; }
            public string email { get; set; }
        }

        //сокращенный список всех организаций
        public class ShortInfo
        {
            public string id { get; set; }
            public string name { get; set; }
        }
    }
}