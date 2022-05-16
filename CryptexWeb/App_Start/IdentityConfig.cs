using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using CryptexWeb.Models;
using CryptexWeb.Providers;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using WebGrease.Css.Extensions;

namespace CryptexWeb
{
    public class EmailService : IIdentityMessageService
    {
        //info
        //https://docs.microsoft.com/en-us/aspnet/mvc/overview/security/create-an-aspnet-mvc-5-web-app-with-email-confirmation-and-password-reset
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your email service here to send an email.
            return Task.FromResult(0);
        }
    }

    public class SmsService : IIdentityMessageService
    {
        public Task SendAsync(IdentityMessage message)
        {
            // Plug in your SMS service here to send a text message.
            return Task.FromResult(0);
        }
    }

    // Configure the application user manager used in this application. UserManager is defined in ASP.NET Identity and is used by the application.
    public class ApplicationUserManager : UserManager<User, int>
    {
        public ApplicationUserManager(IUserStore<User, int> store)
            : base(store)
        {
        }

        public static ApplicationUserManager Create(IdentityFactoryOptions<ApplicationUserManager> options, IOwinContext context)
        {
            var manager = new ApplicationUserManager(new ApplicationUserStore(context.Get<ApplicationDbContext>()));
            // Configure validation logic for usernames
            manager.UserValidator = new UserValidator<User, int>(manager)
            {
                AllowOnlyAlphanumericUserNames = true,
                RequireUniqueEmail = true
            };

            // Configure validation logic for passwords
            manager.PasswordValidator = new PasswordValidator
            {
                RequiredLength = 6,
                RequireNonLetterOrDigit = false,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = false,
            };
            //todo set custom password hasher
            //manager.PasswordHasher = new CustomPasswordHasher(); 


            // Configure user lockout defaults
            manager.UserLockoutEnabledByDefault = true;
            manager.DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(5);
            manager.MaxFailedAccessAttemptsBeforeLockout = 5;

            // Register two factor authentication providers. This application uses Phone and Emails as a step of receiving a code for verifying the user
            // You can write your own provider and plug it in here.
            manager.RegisterTwoFactorProvider("Phone Code", new PhoneNumberTokenProvider<User, int>
            {
                MessageFormat = "Your security code is {0}"
            });
            manager.RegisterTwoFactorProvider("Email Code", new EmailTokenProvider<User, int>
            {
                Subject = "Security Code",
                BodyFormat = "Your security code is {0}"
            });
            manager.EmailService = new EmailService();
            manager.SmsService = new SmsService();
            var dataProtectionProvider = options.DataProtectionProvider;
            if (dataProtectionProvider != null)
            {
                manager.UserTokenProvider =
                    new DataProtectorTokenProvider<User, int>(dataProtectionProvider.Create("ASP.NET Identity"));
            }
            return manager;
        }
    }

    // PASS CUSTOM APPLICATION ROLE AND INT AS TYPE ARGUMENTS TO BASE:
    public class ApplicationRoleManager : RoleManager<Role, int>
    {
        // PASS CUSTOM APPLICATION ROLE AND INT AS TYPE ARGUMENTS TO CONSTRUCTOR:
        public ApplicationRoleManager(IRoleStore<Role, int> roleStore)
            : base(roleStore)
        {
        }

        // PASS CUSTOM APPLICATION ROLE AS TYPE ARGUMENT:
        public static ApplicationRoleManager Create(
            IdentityFactoryOptions<ApplicationRoleManager> options, IOwinContext context)
        {
            return new ApplicationRoleManager(
                new ApplicationRoleStore(context.Get<ApplicationDbContext>()));
        }
    }


    // Configure the application sign-in manager which is used in this application.
    public class ApplicationSignInManager : SignInManager<User, int>
    {
        public ApplicationSignInManager(ApplicationUserManager userManager, IAuthenticationManager authenticationManager)
            : base(userManager, authenticationManager)
        {
        }

        public override Task<ClaimsIdentity> CreateUserIdentityAsync(User user)
        {
            return user.GenerateUserIdentityAsync((ApplicationUserManager) UserManager);
        }

        public static ApplicationSignInManager Create(IdentityFactoryOptions<ApplicationSignInManager> options, IOwinContext context)
        {
            return new ApplicationSignInManager(context.GetUserManager<ApplicationUserManager>(), context.Authentication);
        }
    }

    // PASS CUSTOM APPLICATION ROLE AND INT AS TYPE ARGUMENTS TO BASE:


    // This is useful if you do not want to tear down the database each time you run the application.
    // public class ApplicationDbInitializer : DropCreateDatabaseAlways<ApplicationDbContext>
    // This example shows you how to create a new database if the Model changes
    public class ApplicationDbInitializer : DropCreateDatabaseIfModelChanges<ApplicationDbContext>
    {
        protected override void Seed(ApplicationDbContext context)
        {
           // InitializeIdentityForEF(context, ConfigurationManager.AppSettings);
            base.Seed(context);
        }

        public static void InitializeIdentityForEf(ApplicationDbContext db, NameValueCollection config)
        {
            var userManager = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
            var roleManager = HttpContext.Current.GetOwinContext().Get<ApplicationRoleManager>();
            //string name = config["AdminUser"];
            string password = config["AdminPass"];
            string email = config["AdminEmail"];
            

            //Create Role Admin if it does not exist
            var adminRole = roleManager.FindByName(Role.AdminRoleName);
            if (adminRole == null)
            {
                adminRole = new Role(Role.AdminRoleName);
                var roleresult = roleManager.Create(adminRole);
            }

            var user = userManager.FindByName(email);
            if (user == null)
            {
                user = new User {UserName = email, Email = email, Registered = DateTime.Now, Active = true, Description = "Temporary admin account"};
                var result = userManager.Create(user, password);
                result = userManager.SetLockoutEnabled(user.Id, false);
            }

            // Add user admin to Role Admin if not already added
            var rolesForUser = userManager.GetRoles(user.Id);
            if (!rolesForUser.Contains(adminRole.Name))
            {
                var result = userManager.AddToRole(user.Id, adminRole.Name);
            }

            #region Debug - temp init

            var userRole = roleManager.FindByName(Role.UserRoleName);
            if (userRole == null)
            {
                userRole = new Role(Role.UserRoleName);
                var roleresult = roleManager.Create(userRole);
            }

            email = "u@user.c";
            user = userManager.FindByName(email);
            if (user == null)
            {
                user = new User {UserName = email, Email = email, Registered = DateTime.Now, Active = true, Description = "Temporary user account"};
                var result = userManager.Create(user, password);
                result = userManager.SetLockoutEnabled(user.Id, false);
            }

            rolesForUser = userManager.GetRoles(user.Id);
            if (!rolesForUser.Contains(userRole.Name))
            {
                var result = userManager.AddToRole(user.Id, userRole.Name);
            }

            #endregion

            //Fill db
            /*MockDbInitializer dbInitializer = new MockDbInitializer();
            List<User> users = dbInitializer.LoadUsers();
            users.ForEach(u =>
            {
                var result = userManager.Create(u, password);
                result = userManager.SetLockoutEnabled(user.Id, false);
                result = userManager.AddToRole(u.Id, userRole.Name);
            });
            var list = db.Users.ToList();
            db.SaveChanges();
            MockDbInitializer.LoadPublicKeys(list).ForEach(key => db.PublicKeys.Add(key));
            dbInitializer.LoadContacts(list).ForEach(k => db.Contacts.Add(k));
            dbInitializer.LoadConversations(list).ForEach(k => db.Conversations.Add(k));
            MockDbInitializer.LoadMessages(list).ForEach(k => db.Messages.Add(k));
            db.SaveChanges();*/
        }
    }
}