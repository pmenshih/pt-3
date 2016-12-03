using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using psychoTest.Models;
using System.Net.Http;
using System.Web.Script.Serialization;
using System.Data.SqlClient;

namespace psychoTest.Controllers
{
    [Authorize]
    public class ManageController : Controller
    {
        private ApplicationSignInManager _signInManager;
        private ApplicationUserManager _userManager;

        public ManageController()
        {
        }

        public ManageController(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
        {
            UserManager = userManager;
            SignInManager = signInManager;
        }

        public ApplicationSignInManager SignInManager
        {
            get
            {
                return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
            }
            private set 
            { 
                _signInManager = value; 
            }
        }

        public ApplicationUserManager UserManager
        {
            get
            {
                return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set
            {
                _userManager = value;
            }
        }

        //
        // GET: /Manage/Index
        public async Task<ActionResult> Index(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
                : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
                : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
                : message == ManageMessageId.Error ? "An error has occurred."
                : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
                : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
                : "";

            var userId = User.Identity.GetUserId();

            if(Core.Membership.isAdmin(User))
            {
                //если в запросе есть параметр id, то отобразим данные другого человека
                if (Request.QueryString["userId"] != null)
                    userId = Request.QueryString["userId"].ToString();
            }
            
            var user = await UserManager.FindByIdAsync(userId);
            var model = new IndexViewModel
            {
                HasPassword = HasPassword(),
                PhoneNumber = user.PhoneNumber,
                TwoFactor = user.TwoFactorEnabled,
                Logins = await UserManager.GetLoginsAsync(userId),
                BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId),
                EmailConfirmed = user.EmailConfirmed,
                PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                surname = user.Surname,
                name = user.Name,
                patronim = user.Patronim,
                sex = user.Sex,
                email = user.Email,
                phone = user.PhoneNumber
                ,organisation = Models.Organisations.Organisation.GetByUserEmail(user.Email)
            };
            return View(model);
        }

        //
        // POST: /Manage/RemoveLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
        {
            ManageMessageId? message;
            var result = await UserManager.RemoveLoginAsync(User.Identity.GetUserId(), new UserLoginInfo(loginProvider, providerKey));
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                message = ManageMessageId.RemoveLoginSuccess;
            }
            else
            {
                message = ManageMessageId.Error;
            }
            return RedirectToAction("ManageLogins", new { Message = message });
        }
        
        public async Task<ActionResult> ConfirmEmailRequest()
        {
            string userId = User.Identity.GetUserId();

            if (userId != Request.QueryString["userId"] && Core.Membership.isAdmin(User))
            {
                userId = Request.QueryString["userId"];
            }

            Core.AjaxAnswer answer = new Core.AjaxAnswer();

            if (UserManager.IsEmailConfirmed(userId))
            {
                answer.result = Core.AjaxResults.EmailConfirmed;
                return answer.JsonContentResult();
            }

            try
            {
                string code = await UserManager.GenerateEmailConfirmationTokenAsync(userId);
                var callbackUrl = Url.Action("ConfirmEmail", "Account", new { userId = userId, code = code }, protocol: Request.Url.Scheme);
                await UserManager.SendEmailAsync(userId, "Подтверждение адреса электронной почты.", "Нажмите на <a href=\"" + callbackUrl + "\">ссылку</a>.");
                answer.result = Core.AjaxResults.Success;
            }
            catch (Exception)
            {
                answer.result = Core.AjaxResults.CodeError;
            }
            
            return answer.JsonContentResult();
        }

        //
        // GET: /Manage/VerifyPhoneNumber
        public async Task<ActionResult> VerifyPhoneNumber()
        {
            string userId = User.Identity.GetUserId();

            if (userId != Request.QueryString["userId"] && Core.Membership.isAdmin(User))
            {
                userId = Request.QueryString["userId"];
            }

            var user = await UserManager.FindByIdAsync(userId);
            string phoneNumber = user.PhoneNumber;
            var code = await UserManager.GenerateChangePhoneNumberTokenAsync(userId, phoneNumber);
            string message = "Код подтверждения: " + code + ".";
            
            await Core.BLL.SendSMS(phoneNumber, message);

            return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
        }
        
        //
        // POST: /Manage/VerifyPhoneNumber
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
        {
            string userId = User.Identity.GetUserId();
            
            if (!ModelState.IsValid)
            {
                return View(model);
            }
            var result = await UserManager.ChangePhoneNumberAsync(userId, model.PhoneNumber, model.Code);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
            }
            // If we got this far, something failed, redisplay form
            ModelState.AddModelError("", "Failed to verify phone");
            return View(model);
        }

        //
        // GET: /Manage/ChangePassword
        public ActionResult ChangePassword()
        {
            return View();
        }

        //
        // POST: /Manage/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            string userId = User.Identity.GetUserId();

            if (userId != model.userId && Core.Membership.isAdmin(User))
            {
                userId = model.userId;
            }

            var result = await UserManager.ChangePasswordAsync(userId, model.OldPassword, model.NewPassword);
            if (result.Succeeded)
            {
                var user = await UserManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                }
                return RedirectToAction("Index", new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return View(model);
        }

        //
        // GET: /Manage/SetPassword
        public ActionResult SetPassword()
        {
            return View();
        }

        //
        // POST: /Manage/SetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                string userId = User.Identity.GetUserId();

                if (userId != model.userId && Core.Membership.isAdmin(User))
                {
                    userId = model.userId;
                }

                var result = await UserManager.AddPasswordAsync(userId, model.NewPassword);
                if (result.Succeeded)
                {
                    var user = await UserManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
                    }
                    return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
                }
                AddErrors(result);
            }

            // If we got this far, something failed, redisplay form
            return View(model);
        }

        //
        // GET: /Manage/ManageLogins
        public async Task<ActionResult> ManageLogins(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
                : message == ManageMessageId.Error ? "An error has occurred."
                : "";
            var user = await UserManager.FindByIdAsync(User.Identity.GetUserId());
            if (user == null)
            {
                return View("Error");
            }
            var userLogins = await UserManager.GetLoginsAsync(User.Identity.GetUserId());
            var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
            ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
            return View(new ManageLoginsViewModel
            {
                CurrentLogins = userLogins,
                OtherLogins = otherLogins
            });
        }

        //
        // POST: /Manage/LinkLogin
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LinkLogin(string provider)
        {
            // Request a redirect to the external login provider to link a login for the current user
            return new AccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage"), User.Identity.GetUserId());
        }

        //
        // GET: /Manage/LinkLoginCallback
        public async Task<ActionResult> LinkLoginCallback()
        {
            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, User.Identity.GetUserId());
            if (loginInfo == null)
            {
                return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
            }
            var result = await UserManager.AddLoginAsync(User.Identity.GetUserId(), loginInfo.Login);
            return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _userManager != null)
            {
                _userManager.Dispose();
                _userManager = null;
            }

            base.Dispose(disposing);
        }

        public ActionResult SurnameChange(string newval, string userId)
        {
            Core.AjaxAnswer ans = new Core.AjaxAnswer();

            string curUserId = User.Identity.GetUserId();
            if (userId != curUserId && !Core.Membership.isAdmin(User))
            {
                ans.result = Core.AjaxResults.NoRights;
                return ans.JsonContentResult();
            }

            using (DBMain db = new DBMain())
            {
                try
                {
                    AspNetUser user = db.AspNetUsers.Where(x => x.Id == userId).Single();
                    user.Surname = newval;
                    db.SaveChanges();

                    //обновление индекса
                    Core.BLL.SearchIndexUpdate(user, Core.CRUDType.Update);
                }
                catch (Exception)
                {
                    ans.result = Core.AjaxResults.CodeError;
                    return ans.JsonContentResult();
                }
            }

            ans.result = Core.AjaxResults.Success;
            return ans.JsonContentResult();
        }

        public ActionResult NameChange(string newval, string userId)
        {
            Core.AjaxAnswer ans = new Core.AjaxAnswer();

            string curUserId = User.Identity.GetUserId();
            if (userId != curUserId && !Core.Membership.isAdmin(User))
            {
                ans.result = Core.AjaxResults.NoRights;
                return ans.JsonContentResult();
            }

            using (DBMain db = new DBMain())
            {
                try
                {
                    AspNetUser user = db.AspNetUsers.Where(x => x.Id == userId).Single();
                    user.Name = newval;
                    db.SaveChanges();

                    //обновление индекса
                    Core.BLL.SearchIndexUpdate(user, Core.CRUDType.Update);
                }
                catch (Exception)
                {
                    ans.result = Core.AjaxResults.CodeError;
                    return ans.JsonContentResult();
                }
            }

            ans.result = Core.AjaxResults.Success;
            return ans.JsonContentResult();
        }

        public ActionResult PatronimChange(string newval, string userId)
        {
            Core.AjaxAnswer ans = new Core.AjaxAnswer();

            string curUserId = User.Identity.GetUserId();
            if (userId != curUserId && !Core.Membership.isAdmin(User))
            {
                ans.result = Core.AjaxResults.NoRights;
                return ans.JsonContentResult();
            }

            using (DBMain db = new DBMain())
            {
                try
                {
                    AspNetUser user = db.AspNetUsers.Where(x => x.Id == userId).Single();
                    user.Patronim = newval;
                    db.SaveChanges();

                    //обновление индекса
                    Core.BLL.SearchIndexUpdate(user, Core.CRUDType.Update);
                }
                catch (Exception)
                {
                    ans.result = Core.AjaxResults.CodeError;
                    return ans.JsonContentResult();
                }
            }

            ans.result = Core.AjaxResults.Success;
            return ans.JsonContentResult();
        }

        public ActionResult SexChange(string newval, string userId)
        {
            Core.AjaxAnswer ans = new Core.AjaxAnswer();

            string curUserId = User.Identity.GetUserId();
            if (userId != curUserId && !Core.Membership.isAdmin(User))
            {
                ans.result = Core.AjaxResults.NoRights;
                return ans.JsonContentResult();
            }

            using (DBMain db = new DBMain())
            {
                try
                {
                    AspNetUser user = db.AspNetUsers.Where(x => x.Id == userId).Single();
                    user.Sex = Byte.Parse(newval);
                    db.SaveChanges();

                    //обновление индекса
                    Core.BLL.SearchIndexUpdate(user, Core.CRUDType.Update);
                }
                catch (Exception)
                {
                    ans.result = Core.AjaxResults.CodeError;
                    return ans.JsonContentResult();
                }
            }

            ans.result = Core.AjaxResults.Success;
            return ans.JsonContentResult();
        }

        public ActionResult EmailChange(string newval, string userId)
        {
            Core.AjaxAnswer ans = new Core.AjaxAnswer();

            string curUserId = User.Identity.GetUserId();
            if (userId != curUserId && !Core.Membership.isAdmin(User))
            {
                ans.result = Core.AjaxResults.NoRights;
                return ans.JsonContentResult();
            }

            using (DBMain db = new DBMain())
            {
                try
                {
                    AspNetUser user = db.AspNetUsers.Where(x => x.Id == userId).Single();
                    user.Email = newval;
                    user.EmailConfirmed = false;
                    db.SaveChanges();

                    //обновление индекса
                    Core.BLL.SearchIndexUpdate(user, Core.CRUDType.Update);
                }
                catch (Exception)
                {
                    ans.result = Core.AjaxResults.CodeError;
                    return ans.JsonContentResult();
                }
            }

            ans.result = Core.AjaxResults.Success;
            return ans.JsonContentResult();
        }

        public ActionResult PhoneChange(string newval, string userId)
        {
            Core.AjaxAnswer ans = new Core.AjaxAnswer();

            string curUserId = User.Identity.GetUserId();
            if (userId != curUserId && !Core.Membership.isAdmin(User))
            {
                ans.result = Core.AjaxResults.NoRights;
                return ans.JsonContentResult();
            }

            using (DBMain db = new DBMain())
            {
                try
                {
                    AspNetUser user = db.AspNetUsers.Where(x => x.Id == userId).Single();
                    
                    //проверка уникальности телефонного номера
                    if (newval.Length > 0
                        && newval != user.PhoneNumber
                        && db.AspNetUsers.Where(x => x.PhoneNumber == newval).Count() > 0)
                    {
                        ans.result = Core.AjaxResults.PhoneAllreadyExist;
                        return ans.JsonContentResult();
                    }

                    user.PhoneNumber = newval;
                    user.PhoneNumberConfirmed = false;

                    db.SaveChanges();

                    //обновление индекса
                    Core.BLL.SearchIndexUpdate(user, Core.CRUDType.Update);
                }
                catch (Exception)
                {
                    ans.result = Core.AjaxResults.CodeError;
                    return ans.JsonContentResult();
                }
            }

            ans.result = Core.AjaxResults.Success;
            return ans.JsonContentResult();
        }

        public ActionResult Search()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Search(FormCollection form)
        {
            DBMain db = new DBMain();
            string searchString = form["searchString"];
            string query = "SELECT * FROM SearchIndexes WHERE searchString LIKE N'%" + searchString + "%'";

            string result = "<ul>";
            if(Core.Membership.isAdmin(User))
            {
                foreach (SearchIndex si in db.Database.SqlQuery<SearchIndex>(query))
                {
                    if (si.instanceType == "AspNetUsers")
                    {
                        result += "<li><a href='/manage?userId=" + si.instanceId + "'>" + si.searchString + "</a></li>";
                    }
                    else if (si.instanceType == "Organisations")
                    {
                        result += "<li><a href='/organisation?id=" + si.instanceId + "'>" + si.searchString + "</a></li>";
                    }
                }
            }
            result += "</ul>";
            ViewData.Add("result", result);

            return View();
        }

        public ActionResult RoleGetForUser(string userId, string orgId)
        {
            Core.AjaxAnswer ans = new Core.AjaxAnswer();

            if (!Core.Membership.isAdmin(User) && !Models.Organisations.Organisation.isManager(User, orgId))
            {
                ans.result = Core.AjaxResults.NoRights;
                return ans.JsonContentResult();
            }

            using (DBMain db = new DBMain())
            {
                try
                {
                    string query = @"SELECT 
	                            r.Name
	                            ,(CASE 
		                            WHEN (SELECT ur.userEmail 
				                            FROM OrganisationsUsersRoles ur 
				                            WHERE 
					                            ur.roleName=r.Name
					                            AND ur.userEmail=@email) IS NOT NULL 
			                            THEN '1' 
		                            ELSE '0' 
	                            END) as Val
                            FROM AspNetRoles r
                            WHERE (r.Name <> 'admin' AND r.Name <> 'coach')";

                    var jsonSerialiser = new JavaScriptSerializer();
                    string email = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(userId).UserName; ;
                    System.Collections.Generic.List<UserRolesList> rList = db.Database.SqlQuery<UserRolesList>(query, new SqlParameter("email", email)).ToList();
                    var json = jsonSerialiser.Serialize(rList);
                    return Content(json, "application/json");
                }
                catch (Exception)
                {
                    ans.result = Core.AjaxResults.CodeError;
                    return ans.JsonContentResult();
                }
            }
        }

        public ActionResult RoleSetForUser(string userId, string orgId, string roleName, string val)
        {
            Core.AjaxAnswer ans = new Core.AjaxAnswer();

            if (!Core.Membership.isAdmin(User) && !Models.Organisations.Organisation.isManager(User, orgId))
            {
                ans.result = Core.AjaxResults.NoRights;
                return ans.JsonContentResult();
            }

            using (DBMain db = new DBMain())
            {
                try
                {
                    string query = "";

                    if (val == "1")
                    {
                        query = @"INSERT INTO OrganisationsUsersRoles VALUES (@roleName, @userEmail, @orgId)";
                    }
                    else if (val == "0")
                    {
                        if (User.Identity.GetUserId() == userId && Models.Organisations.Organisation.isManager(User, orgId))
                        {
                            ans.result = Core.AjaxResults.NoManagerSuicide;
                            return ans.JsonContentResult();
                        }

                        query = @"DELETE FROM OrganisationsUsersRoles WHERE orgId=@orgId AND roleName=@roleName AND userEmail=@userEmail";
                    }
                    else
                    {
                        ans.result = Core.AjaxResults.IncorrectParameters;
                        return ans.JsonContentResult();
                    }
                    string email = System.Web.HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>().FindById(userId).UserName;
                    object[] sqlPars = {
                        new SqlParameter("roleName", roleName)
                        ,new SqlParameter("userEmail", email)
                        ,new SqlParameter("orgId", orgId)
                    };
                    db.Database.ExecuteSqlCommand(query, sqlPars);
                }
                catch (Exception)
                {
                    ans.result = Core.AjaxResults.CodeError;
                    return ans.JsonContentResult();
                }
            }

            ans.result = Core.AjaxResults.Success;
            return ans.JsonContentResult();
        }

        public ActionResult CheckEmailExist(string email, string userId = null)
        {
            Core.AjaxAnswer ans = new Core.AjaxAnswer();

            using (DBMain db = new DBMain())
            {
                if (userId == null)
                {
                    if (db.AspNetUsers.Where(x => x.Email == email).Count() == 0) ans.result = Core.AjaxResults.Success;
                    else ans.result = Core.AjaxResults.EmailAllreadyExist;
                }
                else
                {
                    if (db.AspNetUsers.Where(x => x.Email == email && x.Id != userId).Count() == 0) ans.result = Core.AjaxResults.Success;
                    else ans.result = Core.AjaxResults.EmailAllreadyExist;
                }

                return ans.JsonContentResult();
            }
        }

        public ActionResult CheckPhoneExist(string phone, string userId = null)
        {
            Core.AjaxAnswer ans = new Core.AjaxAnswer();

            using (DBMain db = new DBMain())
            {
                if (userId == null)
                {
                    if (db.AspNetUsers.Where(x => x.PhoneNumber == phone).Count() == 0) ans.result = Core.AjaxResults.Success;
                    else ans.result = Core.AjaxResults.PhoneAllreadyExist;
                }
                else
                {
                    if (db.AspNetUsers.Where(x => x.PhoneNumber == phone && x.Id != userId).Count() == 0) ans.result = Core.AjaxResults.Success;
                    else ans.result = Core.AjaxResults.PhoneAllreadyExist;
                }
                
                return ans.JsonContentResult();
            }
        }

        #region Helpers
        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        private IAuthenticationManager AuthenticationManager
        {
            get
            {
                return HttpContext.GetOwinContext().Authentication;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error);
            }
        }

        private bool HasPassword()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PasswordHash != null;
            }
            return false;
        }

        private bool HasPhoneNumber()
        {
            var user = UserManager.FindById(User.Identity.GetUserId());
            if (user != null)
            {
                return user.PhoneNumber != null;
            }
            return false;
        }

        public enum ManageMessageId
        {
            AddPhoneSuccess,
            ChangePasswordSuccess,
            SetTwoFactorSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
            RemovePhoneSuccess,
            Error
        }

#endregion
    }
}