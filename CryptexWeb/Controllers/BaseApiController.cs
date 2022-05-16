using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using CryptexWeb.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace CryptexWeb.Controllers
{
    
    public abstract class BaseApiController : ApiController
    {
        protected BaseApiController()
        {
        }

        protected BaseApiController(ApplicationUserManager userManager, ApplicationRoleManager roleManager,
            CryptoManager cryptoManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _cryptoManager = cryptoManager;
        }

        private ApplicationUserManager _userManager;
        private CryptoManager _cryptoManager;
        private ApplicationRoleManager _roleManager;

        public ApplicationUserManager UserManager
        {
            get     
            {               
                return _userManager ?? HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>();
            }
            private set { _userManager = value; }
        }

        public ApplicationRoleManager RoleManager
        {
            get { return _roleManager ?? HttpContext.Current.GetOwinContext().Get<ApplicationRoleManager>(); }
            private set { _roleManager = value; }
        }

        public CryptoManager CryptoManager
        {
            get { return _cryptoManager ?? HttpContext.Current.GetOwinContext().Get<CryptoManager>(); }
            private set { _cryptoManager = value; }
        }

        //Pobiera id usera z obecnej sesji. Na podstawie user menagera
        public User GetCurrentUser()
        {
            var userid = User.Identity.GetUserId<int>();
            return UserManager.FindById(userid);
//            return controller.UserManager.Users.First(user => user.Id == userid);
        }
        //Konwersja na unix czas
        public long ToUnixTime(DateTime date)
        {
            var timeSpan = (date - new DateTime(1970, 1, 1, 0, 0, 0));
            return (long)timeSpan.TotalSeconds;
        }

    }
}