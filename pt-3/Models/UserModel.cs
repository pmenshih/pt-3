using System.Linq;


namespace psychoTest.Models.Users
{
    public class User
    {
        public static AspNetUser GetById(string id)
        {
            return DBMain.db.AspNetUsers.Single(x => x.Id == id);
        }

        public static AspNetUser GetByPhone(string phone)
        {
            return DBMain.db.AspNetUsers.SingleOrDefault(x => x.PhoneNumber == phone);
        }
    }
}