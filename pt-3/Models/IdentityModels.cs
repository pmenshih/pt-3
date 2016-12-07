using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System.Web;
using Microsoft.AspNet.Identity.Owin;

namespace psychoTest.Models
{
    // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit http://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
    public class ApplicationUser : IdentityUser
    {
        public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
        {
            // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
            var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            userIdentity.AddClaim(new Claim("Email", this.Email));
            // Add custom user claims here
            return userIdentity;
        }

        public string Surname { get; set; }
        public string Name { get; set; }
        public string Patronim { get; set; }
        public byte? Sex { get; set; }
    }

    public static class IdentityExtensions
    {
        public static string GetEmail(this System.Security.Principal.IIdentity identity)
        {
            var email = ((ClaimsIdentity)identity).FindFirst("Email");
            return email?.Value;
        }
    }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext()
            : base("DefaultConnection", throwIfV1Schema: false)
        {
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }
    }
}