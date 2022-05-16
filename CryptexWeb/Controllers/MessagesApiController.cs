using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using CryptexWeb.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;

namespace CryptexWeb.Controllers
{
    [System.Web.Http.RoutePrefix("api")]
    public class MessagesApiController : BaseApiController
    {
        private const string hashtest = "AHiSV0QLscY1ffofMIGr1RkuJzyXNT/TYRqmQsNDkrgmAY/rjrwthxr8nPvybumaLQ=="; //u12345

        public MessagesApiController()
        {
        }

        public MessagesApiController(ApplicationUserManager userManager, ApplicationRoleManager roleManager, CryptoManager cryptoManager) : base(userManager, roleManager, cryptoManager)
        {
        }

        [Route("msgall")]
        [HttpGet]
        public IHttpActionResult All()
        {
            string hash = UserManager.PasswordHasher.HashPassword("u12345");
            bool v = UserManager.PasswordHasher.VerifyHashedPassword(hashtest, "u12345") == PasswordVerificationResult.Success;
            List<string> s = new List<string>();
            var r = RouteTable.Routes;

            foreach (var routes in r)
            {
                s.Add(((Route) routes).Url);
            }


            return Ok(new
            {
                Test = "Hasher",
                Hash = hash,
                VerfictaionFor = hashtest,
                VerficationResult = v,
            });
        }

        [Route("msgtest")]
        [HttpGet]
        public IHttpActionResult Lol()
        {
            return Ok(new
            {
                Test = "test",
                Li = 0
            });
        }
    }
}