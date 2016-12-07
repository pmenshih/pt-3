using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.SqlClient;

namespace psychoTest.Models.Organisations
{
    //модель организации
    public class Organisation
    {
        #region Поля таблицы Organisation в БД
        [Key]
        public string id { get; set; } = Guid.NewGuid().ToString();
        public string name { get; set; } = null;
        public DateTime dateCreate { get; set; } = DateTime.Now;
        public bool moderated { get; set; } = false;
        public bool deleted { get; set; } = false;
        #endregion
        
        public static Organisation GetById(string id)
        {
            return DBMain.db.Organisations.SingleOrDefault(x => x.id == id);
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
				                                    WHERE ou.userEmail=@email
                                                        AND ou.active = 1
                                                        AND ou.dateStop < GETDATE())";
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

        public bool UserAddRole(string email, string roleName)
        {
            try
            {
                using (DBMain db = new DBMain())
                {
                    string query = @"INSERT INTO OrganisationsUsersRoles 
                                    VALUES (@roleName, @userEmail, @orgId)";
                    db.Database.ExecuteSqlCommand(query
                                                    , new SqlParameter("roleName", roleName)
                                                    , new SqlParameter("userEmail", email)
                                                    , new SqlParameter("orgId", this.id));
                    return true;
                }
            }
            catch (Exception) { return false; }
        }

        public static bool UserRemoveRoles(string userEmail, string orgId)
        {
            try
            {
                using (DBMain db = new DBMain())
                {
                    string query = @"DELETE FROM OrganisationsUsersRoles 
                                    WHERE userEmail=@userEmail AND orgId=@orgId";
                    db.Database.ExecuteSqlCommand(query
                                                    , new SqlParameter("userEmail", userEmail)
                                                    , new SqlParameter("orgId", orgId));
                    return true;
                }
            }
            catch (Exception) { return false; }
        }

        public static bool UserIsActive(string orgId, string userEmail)
        {
            try
            {
                using (DBMain db = new DBMain())
                {
                    OrganisationsUsers ou = db.OrganisationUsers.Where(x => x.active == true && x.orgId == orgId && x.userEmail == userEmail && x.dateStop > DateTime.Now).Single();
                    if (ou != null) return true;
                    else return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string RolesGetAsString(string orgId, string userEmail)
        {
            string roles = "";

            using (DBMain db = new DBMain())
            {
                foreach (Models.Organisations.OrganisationsUsersRole role in db.OrganisationsUsersRoles.Where(x => x.userEmail == userEmail && x.orgId == orgId))
                {
                    roles += role.roleName + ",";
                }
                roles = roles.TrimEnd(',');
            }

            return roles;
        }

        public bool AddUser(string email)
        {
            try
            {
                using (DBMain db = new DBMain())
                {
                    OrganisationsUsers ou = new OrganisationsUsers();
                    ou.active = true;
                    ou.dateStart = DateTime.Now;
                    ou.dateStop = new DateTime(2222, 1, 1);
                    ou.orgId = this.id;
                    ou.userEmail = email;

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

        public static List<Views.UsersUploadHistoryViewEntity> GetUploadHistory(
            string orgId
            ,bool getDeleted = false)
        {
            using (DBMain db = new DBMain())
            {
                string query = @"
SELECT id, dateCreate, usersCount
FROM OrganisationsUsersFiles
WHERE orgId=@orgId
    AND deleted=0
ORDER BY dateCreate DESC";
                if (getDeleted)
                    query = query.Replace("AND deleted=0", "");
                List<Views.UsersUploadHistoryViewEntity> uploadHistory = db.Database.SqlQuery
                    <Models.Organisations.Views.UsersUploadHistoryViewEntity>
                    (query, new SqlParameter("orgId", orgId)).ToList();
                return uploadHistory;
            }
        }

        //создание организации
        public bool Create()
        {
            DBMain.db.Organisations.Add(this);
            DBMain.db.SaveChanges();

            return true;
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

    //таблица привязки пользователей к организации
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

    //таблица файлов со списками пользователей, которые загружались через инструмент импорта из файла
    public class OrganisationsUsersFile
    {
        [Key]
        public string id { get; set; }
        public string orgId { get; set; }
        public DateTime dateCreate { get; set; }
        public bool deleted { get; set; }
        public string usersData { get; set; }
        public int usersCount { get; set; }
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
                                                            AND active=0
                                                            AND dateStop > GETDATE())";
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

        public class Users
        {
            public string orgId { get; set; }
            public List<UsersUserEntity> orgUsers { get; set; }
        }

        public class UsersUserEntity
        {
            public string id { get; set; }
            public string surname { get; set; }
            public string name { get; set; }
            public string patronim { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string roles { get; set; }
            public bool active { get; set; }
        }

        public class UserCU
        {
            public string orgId { get; set; }
            public string userId { get; set; }
            public string surname { get; set; }
            public string name { get; set; }
            public string patronim { get; set; }
            public string email { get; set; }
            public string phone { get; set; }
            public string roles { get; set; }
            public byte? sex { get; set; }
            public string password { get; set; }
        }

        public class UsersImport
        {
            public string orgId { get; set; }
            public string separator { get; set; } = ";";
            public string filename { get; set; } = null;
            public bool showResult { get; set; } = false;
            public int rowsCount { get; set; } = 0;
            public int rowsCorrect { get; set; } = 0;
            public int rowsIncorrect { get; set; } = 0;
            public int usersAdded { get; set; } = 0;
            public int usersNotAdded { get; set; } = 0;
            public List<Core.UploadFailedString> errorLog { get; set; } 
                = new List<Core.UploadFailedString>();
            public List<UsersUploadHistoryViewEntity> uploadHistory { get; set; }
                = new List<UsersUploadHistoryViewEntity>();
        }

        public class UsersUploadHistoryViewEntity
        {
            public string id { get; set; }
            public DateTime dateCreate { get; set; }
            public int usersCount { get; set; }
        }
    }
}