/*
 * Программка переноса пользователей из таблицы AspNetUSers провайдера ASPIdentity в такую же таблицы другой БД.
 * Нюансы:
 *      1. Таблица расширена полями Surname, Name, Patronim и Sex.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//дифениции EF
using System.Data.Entity;
using System.ComponentModel.DataAnnotations.Schema;

namespace UsersImporter
{
    [Table("AspNetUsers")]
    class AspNetUsers
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
        public int Sex { get; set; }
    }

    class DB : DbContext
    {
        public DB(string cs) : base(cs)
        {
            Database.SetInitializer<DB>(null);
        }
        
        public DbSet<AspNetUsers> AspNetUsers { get; set; }
    }

    class Transformer
    {
        public DB dbSource;
        public DB dbDest;

        public Transformer()
        {
            dbSource = new DB(@"Data Source=BASE\SQLEXP2014;Initial Catalog=pt-2b;Uid=testpsychoru;Pwd=123456qW;MultipleActiveResultSets=true;");
            dbDest = new DB(@"Data Source=BASE\SQLEXP2014;Initial Catalog=pt-3;Uid=testpsychoru;Pwd=123456qW;MultipleActiveResultSets=true;");
        }

        public void Start()
        {
            //откроем соединения
            dbSource.Database.Connection.Open();
            dbDest.Database.Connection.Open();
        }

        public void Stop()
        {
            //закроем соединения
            dbSource.Database.Connection.Close();
            dbDest.Database.Connection.Close();
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Transformer tr = new Transformer();

            try
            {
                tr.Start();
            }
            catch (Exception exc)
            {
                Console.Write(exc.Message);
            }

            foreach (AspNetUsers userSource in tr.dbSource.AspNetUsers)
            {
                tr.dbDest.AspNetUsers.Add(userSource);
            }

            tr.dbDest.SaveChanges();

            tr.Stop();
            Console.ReadLine();
        }
    }
}
