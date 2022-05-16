using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Security.Claims;
using System.Text;
using System.Web;
using System.Web.Http;
using System.Web.Routing;
using CryptexWeb.Models;
using CryptexWeb.Providers;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Org.BouncyCastle.Crypto;
using WebGrease.Css.Extensions;

namespace CryptexWeb.Controllers
{
    [RoutePrefix("api")]
    public class TestingController : BaseApiController
    {
        public TestingController()
        {
        }

        public TestingController(ApplicationUserManager userManager, ApplicationRoleManager roleManager,
            CryptoManager cryptoManager) : base(userManager, roleManager, cryptoManager)
        {
        }

        [Route("server/publickey")]
        [HttpGet]
        public HttpResponseMessage PublicKey()
        {
            var pub = ConfigurationManager.AppSettings["ServerPublicKey"];
            // ConfigurationManager.AppSettings["ServerPrivateKey"];
            return new HttpResponseMessage
            {
                Content = new StringContent(
                    pub,
                    Encoding.UTF8,
                    "text/plain"
                )
            };
        }

        [Route("contacts")]
        [HttpGet]
        public IHttpActionResult Contacts()
        {
            var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            var currentUser = GetCurrentUser();
            return Json(ctx.Contacts.Where(c => c.OwnerId == currentUser.Id).Select(c => new ContactGO
                {
                    id = c.Id,
                    color = c.Color,
                    description = c.Description,
                    userDescription = c.ContactUser.Description,
                    userName = c.ContactUser.UserName,
                    securityLevel = c.ContactUser.SecurityLevel
                })
                .ToList());
        }

        [Route("conversations")]
        [Authorize]
        [HttpGet]
        public IHttpActionResult Conversations()
        {
            var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            var currentUser = GetCurrentUser();
            var list = ctx.Conversations.Where(c => c.CreatorId == currentUser.Id || c.ParticipantId == currentUser.Id)
                .ToList();
            var r = new List<ConversationGO>();
            foreach (var conversation in list)
            {
                var g = new ConversationGO();
                g.id = conversation.Id;
                int uid;
                if (conversation.CreatorId == currentUser.Id)
                {
                    g.interlocutor = ctx.Users.First(u => u.Id == conversation.ParticipantId).UserName;
                    uid = conversation.ParticipantId;
                }
                else
                {
                    g.interlocutor = ctx.Users.First(u => u.Id == conversation.CreatorId).UserName;
                    uid = conversation.CreatorId;
                }
                var first = ctx.Messages.Where(message => message.ConversationId == conversation.Id)
                    .OrderByDescending(m => m.SendDate).FirstOrDefault();
                if (first != null)
                {
                    g.lastMessageDate = ((DateTimeOffset) first.SendDate).ToUnixTimeSeconds();
                    g.lastMessageId = first.Id;
                }
                PublicKey publicKey = ctx.PublicKeys.First(key => key.OwnerId == uid);
                if (publicKey != null)
                {
                    g.key = publicKey.KeyString;
                }
                r.Add(g);
            }
            return Json(r);
        }

        [Route("messages/{lastMessageId?}")]
        [HttpGet]
        public IHttpActionResult Messages(string lastMessageId)
        {
            var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            var currentUser = GetCurrentUser();
            List<int> conversations =
                ctx.Conversations.Where(c => c.CreatorId == currentUser.Id || c.ParticipantId == currentUser.Id)
                    .Select(c => c.Id).ToList();
            var mid = -1;
            int.TryParse(lastMessageId, out mid);
            if (mid == -1)
                return BadRequest("Parameter must be integer");
            if (conversations == null || conversations.Count == 0)
                return Json(new List<Message>());
            var messages = ctx.Messages.Where(m => conversations.Contains(m.ConversationId) && m.Id > mid).ToList()
                .Select(m => new MessageGO()
                {
                    id = m.Id,
                    content = m.Content,
                    conversationId = m.ConversationId,
                    securityLevel = m.SecurityLevel,
                    displayed = m.Displayed,
                    isSender = m.SenderId == currentUser.Id,
                    sendDate = ToUnixTime(m.SendDate)
                }).ToList();
            return Json(messages);
        }

        [Route("message/all")]
        [HttpGet]
        public IHttpActionResult MessagesAll()
        {
            var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            var currentUser = GetCurrentUser();
            List<int> conversations =
                ctx.Conversations.Where(c => c.CreatorId == currentUser.Id || c.ParticipantId == currentUser.Id)
                    .Select(c => c.Id).ToList();
            if (conversations == null || conversations.Count == 0)
                return Json(new List<Message>());
            var messages = ctx.Messages.Where(m => conversations.Contains(m.ConversationId)).ToList().Select(m =>
                new MessageGO()
                {
                    id = m.Id,
                    content = m.Content,
                    conversationId = m.ConversationId,
                    securityLevel = m.SecurityLevel,
                    displayed = m.Displayed,
                    isSender = m.SenderId == currentUser.Id,
                    sendDate = ((DateTimeOffset) m.SendDate).ToUnixTimeSeconds()
                }).ToList();
            return Json(messages);
        }

        [Route("users")]
        [HttpGet]
        public IHttpActionResult UsersAll()
        {
            var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            var v = (from user in ctx.Users
                join publicKey in ctx.PublicKeys on user.Id equals publicKey.OwnerId
                where publicKey.DisableDate == null
                select new UserGO()
                {
                    id = user.Id,
                    userName = user.UserName,
                    userDescription = user.Description,
                    securityLevel = user.SecurityLevel,
                    publicKey = publicKey.KeyString
                });
            return Json(v.ToList());
        }
        //Pobieranie 
        [Route("users/{query}")]
        [HttpGet]
public IHttpActionResult UsersFiltered(string query)
{
  var ctxBase = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
  var usersSQL = (from user in ctxBase.Users
    join publicKey in ctxBase.PublicKeys on user.Id equals publicKey.OwnerId
    where publicKey.DisableDate == null && user.UserName.Contains(query)
    select new UserGO()
    {
      id = user.Id,
      userName = user.UserName,
      userDescription = user.Description,
      securityLevel = user.SecurityLevel,
      publicKey = publicKey.KeyString
    });
  return Json(usersSQL.ToList());
}

        [Route("user/update/publickey")]
        [HttpPost]
        public IHttpActionResult SaveUserPublicKey(FormDataCollection collection)
        {
            var c = "";
            if (collection != null)
            {
                var convertToDic = ConvertToDic(collection);
                c = DicToString(convertToDic);
            }
            var upload = new UploadResponse
            {
                id = 6,
                status = UploadStatus.Success,
                data = c
            };
            return Created("key", upload);
        }

        [Authorize]
        [Route("user/update/devicekey")]
        [HttpPost]
        public IHttpActionResult SaveUserDeviceKey(FormDataCollection collection)
        {
            if (collection != null && collection.Any())
            {
                var userFromToken = GetCurrentUser();
                var key = collection.First().Value;
                userFromToken.DeviceKey = key;
                var x = UserManager.Update(userFromToken);

                if (x.Succeeded)
                {
                    var upload = new UploadResponse
                    {
                        id = userFromToken.Id,
                        status = UploadStatus.Success
                    };
                    return Created("devicekey", upload);
                }
                var e = x.Errors.Aggregate(string.Empty, (s, s1) => s + "\n" + s1);
                return BadRequest("Error while updating: " + e);
            }
            return BadRequest("Device key is not present");
        }


        [Route("user/update/securityLvl")]
        [HttpPost]
        public IHttpActionResult SaveUserSecurityLvl(FormDataCollection collection)
        {
            if (collection != null && collection.Any())
            {
                var userFromToken = GetCurrentUser();
                var lvl = collection.First().Value;
                userFromToken.SecurityLevel = System.Convert.ToByte(lvl);
                var x = UserManager.Update(userFromToken);

                if (x.Succeeded)
                {
                    var upload = new UploadResponse
                    {
                        id = userFromToken.Id,
                        status = UploadStatus.Success
                    };
                    return Created("securityLvl", upload);
                }
                var e = x.Errors.Aggregate(string.Empty, (s, s1) => s + "\n" + s1);
                return BadRequest("Error while updating: " + e);
            }
            return BadRequest("Security Lvl is not present");
        }

        [Route("user/block")]
        [HttpPost]
        public IHttpActionResult BlockUser(FormDataCollection collection)
        {
            if (collection != null && collection.Any())
            {
                var userFromToken = GetCurrentUser();
                var key = collection.First().Value;

                var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
                var blocked = ctx.Users.FirstOrDefault(user => user.UserName.Contains(key));
                if (blocked == null)
                {
                    return NotFound();
                }
                var blockedUser = new BlockedUser()
                {
                    BlockedId = blocked.Id,
                    UserId = userFromToken.Id
                };
                ctx.BlockedUsers.Add(blockedUser);
                ctx.SaveChanges();
                var upload = new UploadResponse
                {
                    id = blockedUser.Id,
                    status = UploadStatus.Success
                };
                return Created("blocked", upload);
            }
            return BadRequest("Username is not present");
        }

        [Route("contact/new")]
        [HttpPost]
        public IHttpActionResult NewContact(ContactUO c)
        {
            if (c == null)
                return BadRequest();
            var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            var userFromToken = GetCurrentUser();
            var first = ctx.Users.Where(user => user.UserName == c.userName).Select(user => user.Id).First();
            var contact = new Contact()
            {
                Accepted = false,
                Color = c.color,
                OwnerId = userFromToken.Id,
                ContactUserId = first,
                Description = c.description,
                Enabled = true,
                InviteDate = DateTime.Now
            };
            ctx.Contacts.Add(contact);
            ctx.SaveChanges();
            var u = new UploadResponse
            {
                id = contact.Id,
                status = UploadStatus.Success
            };
            return Created("contact", u);
        }

        [Route("contact/update")]
        [HttpPost]
        public IHttpActionResult UpdateContact(ContactUO c)
        {
            if (c == null)
                return BadRequest();
            var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            var userFromToken = GetCurrentUser();
            var first = ctx.Contacts.FirstOrDefault(con => con.ContactUser.UserName == c.userName && con.OwnerId == userFromToken.Id);
            if (first == null)
            {
                return NotFound();
            }
            first.Color = c.color;
            first.Description = c.description;
            ctx.SaveChanges();
            var u = new UploadResponse
            {
                id = first.Id,
                status = UploadStatus.Success
            };
            return Ok(u);
        }

        [Route("contact/delete/{id}")]
        [HttpGet]
        public IHttpActionResult DeleteContact(int id)
        {
            var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            var userFromToken = GetCurrentUser();
            var contact = ctx.Contacts.FirstOrDefault(c => c.Id == id && c.OwnerId == userFromToken.Id);
            if (contact == null)
            {
                return NotFound();
            }
            ctx.Contacts.Remove(contact);
            ctx.SaveChanges();
            return StatusCode(HttpStatusCode.NoContent);
        }

        [Route("conversation/new")]
        [HttpPost]
        public IHttpActionResult NewConversation(ConversationUO c)
        {
          if (c == null)
            return BadRequest();
          var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
          var userFromToken = GetCurrentUser();
          var first = ctx.Users.Where(user => user.UserName == c.interlocutor).Select(user => user.Id).First();
          var convers = new Conversation()
          {
            CreatorId = userFromToken.Id,
            ParticipantId = first
          };
          ctx.Conversations.Add(convers);
          ctx.SaveChanges();
          var u = new UploadResponse
          {
            id = convers.Id,
            status = UploadStatus.Success
          };
          return Created("conversation", u);
        }

        [Route("conversation/delete/{id}")]
        [HttpGet]
        public IHttpActionResult DeleteConversation(int id)
        {
            var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            var userFromToken = GetCurrentUser();
            var conversation = ctx.Conversations.FirstOrDefault(c => c.Id == id &&  (c.CreatorId == userFromToken.Id || c.ParticipantId == userFromToken.Id));
            if (conversation == null)
            {
                return NotFound();
            }
            var messages = ctx.Messages.Where(m => m.ConversationId == conversation.Id);
            foreach (var message in messages)
            {
                ctx.Messages.Remove(message);
            }
            ctx.SaveChanges();
            ctx.Conversations.Remove(conversation);
            ctx.SaveChanges();
            return StatusCode(HttpStatusCode.NoContent);
        }
        //Wysyłanie wiadomości
        [Route("message/new")]
        [HttpPost]
public IHttpActionResult NewMessage(MessageUO messageUo)
{
  if (messageUo == null)
    return BadRequest("Wrong input params");
  //klucz dostępu do usługi firebase
  var auth =
    "key=AAAA6ZJDKq0:APA91bEQlGhQVsdtmfNtxItp2LBDsEKGA" +
    "sT3yj1bHRfQ4Nqe_I26-NJqvYVt0bVmP6TFcco7AWNXtjJ0SRAgpxpbu" +
    "wdMRafhhekJeEc2DVf1GAbmDqOOq9SWm-gKIM5zKm9zVUV4zOMi";
  var url = "https://fcm.googleapis.com/fcm/send"; //adres usługi firebase
  var userFromToken = GetCurrentUser();
  var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
  var conversation = ctx.Conversations.First(convers => convers.Id == messageUo.conversationId);
  User userToSend = null;
  if (conversation.CreatorId == userFromToken.Id)
    userToSend = ctx.Users.First(u1 => u1.Id == conversation.ParticipantId);
  else
    userToSend = ctx.Users.First(u1 => u1.Id == conversation.CreatorId);

  string error = null;
  if (userToSend.DeviceKey != null)
  {
    //generowanie treści żądania, które zawiera dane konfiguracyjne
    var fireBase =
      $"{{\"to\":\"{userToSend.DeviceKey}\",\"priority\":\"high\"," +
      $"\"data\":{{\"conversId\":\"{messageUo.conversationId}\"" +
      $",\"userName\":\"{userFromToken.UserName}\"}},\"" +
      $"notification\":{{\"body_loc_key\":\"body_msg_notification\"" +
      $",\"body_loc_args\":\"[\\\"{userFromToken.UserName}\\\"]\"," +
      $"\"title_loc_key\":\"title_msg_notification\",\"click_action\":" +
      $"\"com.cryptex.OpenNewMessage\"}}}}";
    //wysłanie żadania usługi firebase
    using (var wc = new WebClient())
    {
      wc.Headers[HttpRequestHeader.ContentType] = "application/json";
      wc.Headers[HttpRequestHeader.Authorization] = auth;
      wc.UploadString(url, fireBase);
    }
  }
  else
  {
    error = "User has not logged in yet";
  }

  var message = new Message
  {
    Id = 0,
    SendDate = DateTime.UtcNow,
    Displayed = false,
    SenderId = userFromToken.Id,
    ConversationId = messageUo.conversationId,
    Content = messageUo.content,
    SecurityLevel = userFromToken.SecurityLevel,
    Sender = userFromToken,
    Visible = true
  };
  ctx.Messages.Add(message);
  ctx.SaveChanges();
  //o stworzeniu wiadomości i statusie
  var upload = new UploadResponse
  {
    id = message.Id,
    status = UploadStatus.Success,
    error = error
  };
  return Created("message", upload);
}

        [Route("get/{k}")]
        [HttpGet]
        public IHttpActionResult Get(string k)
        {
            var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            if (k == "b")
                return Json(ctx.BlockedUsers.ToList());
            if (k == "m")
                return Json(ctx.Messages.ToList());
            if (k == "ct")
                return Json(ctx.Contacts.ToList());
            if (k == "cn")
                return Json(ctx.Conversations.ToList());
            if (k == "p")
                return Json(ctx.PublicKeys.ToList());
            return Json(ctx.Users.ToList());
        }

        public static NameValueCollection Convert(FormDataCollection formDataCollection)
        {
            var pairs = formDataCollection.GetEnumerator();

            var collection = new NameValueCollection();

            while (pairs.MoveNext())
            {
                var pair = pairs.Current;

                collection.Add(pair.Key, pair.Value);
            }

            return collection;
        }

        public static Dictionary<string, string> ConvertToDic(FormDataCollection formDataCollection)
        {
            var pairs = formDataCollection.GetEnumerator();

            var collection = new Dictionary<string, string>();

            while (pairs.MoveNext())
            {
                var pair = pairs.Current;

                collection.Add(pair.Key, pair.Value);
            }

            return collection;
        }

        public static string DicToString(Dictionary<string, string> dic)
        {
            var builder = new StringBuilder();
            foreach (var s in dic)
                builder.Append(s.Key).Append(" = ").Append(s.Value).Append("\n");
            return builder.ToString();
        }

        [Serializable]
        public class JsonUserTest
        {
            public string description;
            public int id;
            public byte securityLevel;
            public string userName;
        }


        //endpoint for testing
        [Route("all")]
        [HttpGet]
        public IHttpActionResult All()
        {
            string hashtest = "AHiSV0QLscY1ffofMIGr1RkuJzyXNT/TYRqmQsNDkrgmAY/rjrwthxr8nPvybumaLQ=="; //u12345
            var hash = UserManager.PasswordHasher.HashPassword("u12345");
            var v = UserManager.PasswordHasher.VerifyHashedPassword(hashtest, "u12345") ==
                    PasswordVerificationResult.Success;
            var s = new List<string>();
            var r = RouteTable.Routes;

            foreach (var routes in r)
                s.Add(((Route) routes).Url);


            return Ok(new
            {
                Test = "Hasher",
                Hash = hash,
                VerfictaionFor = hashtest,
                VerficationResult = v
            });
        }

        //endpoint for testing
        [Route("test")]
        [HttpGet]
        public IHttpActionResult Lol()
        {
            AsymmetricCipherKeyPair pair = CryptoManager.GenerateKeyPair();
            ;
            var encryptWithPublic = CryptoManager.RsaEncryptWithPublic("", pair.Public);
            var decryptWithPrivate = CryptoManager.RsaDecryptWithPrivate(encryptWithPublic, pair.Private);


            return Ok(new
            {
                EncryptWithPublic = encryptWithPublic,
                DecryptWithPrivate = decryptWithPrivate
            });
        }


        //endpoint for testing
        [Route("server/decrypt")]
        [HttpPost]
        public IHttpActionResult DecryptOnServer(FormDataCollection formData)
        {
            var valueMap = Convert(formData);
            try
            {
                var dec = CryptoManager.RsaDecryptWithPrivate(valueMap["enc"], CryptoManager.loadPrivateKey());
                return Ok(new {DECRYPT = dec});
            }
            catch (Exception e)
            {
                var ex = e.Message + " _$+$_ " + e.StackTrace;
                return Ok(new {EX = ex});
            }
        }

        //endpoint for testing
        [Route("key")]
        [HttpGet]
        public IHttpActionResult Keys()
        {
            AsymmetricCipherKeyPair pair = CryptoManager.GenerateKeyPair();
            ;
            //            PrivateKeyInfo k = PrivateKeyInfoFactory.CreatePrivateKeyInfo(pair.Private);
            //            byte[] serializedPrivateBytes = k.ToAsn1Object().GetDerEncoded();
            //            string serializedPrivate = Convert.ToBase64String(serializedPrivateBytes);
            //
            //            SubjectPublicKeyInfo publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(pair.Public);
            //            byte[] serializedPublicBytes = publicKeyInfo.ToAsn1Object().GetDerEncoded();
            //            string serializedPublic = Convert.ToBase64String(serializedPublicBytes);
            //
            //            RsaPrivateCrtKeyParameters privateKey = (RsaPrivateCrtKeyParameters)PrivateKeyFactory.CreateKey(Convert.FromBase64String(serializedPrivate));
            //            RsaKeyParameters publicKey = (RsaKeyParameters)PublicKeyFactory.CreateKey(Convert.FromBase64String(serializedPublic));
            var encryptWithPublic = "-";
            var decryptWithPrivate = "-";
            var ex = "-";
            var d =
                "T57RzxnP0QhrFsTVj2CUIWXmb8y5bc4jhzdzVCNDUU0Bye6JctYw6ByEbi5t/OjByCMSbO1MnP65MWp9k1flIN9DzdI/7aPIgHzD38d4k4+wjb04CgMrnJ5e+gX2NRBWNIkc/ce/oNpn7MRq6pkHjpbtf3Ym9l6uq3kThQGToas=";
            var dec = "-";
            try
            {
                dec = CryptoManager.RsaDecryptWithPrivate(d, CryptoManager.loadPrivateKey());
                encryptWithPublic = CryptoManager.RsaEncryptWithPublic("a", pair.Public);
                decryptWithPrivate = CryptoManager.RsaDecryptWithPrivate(encryptWithPublic, pair.Private);
            }
            catch (Exception e)
            {
                ex = e.Message + " _$+$_ " + e.StackTrace;
            }

            return Ok(new
            {
                DEC = dec,
                //                PublicKey64= serializedPublic,
                //                PrivateKey64= serializedPrivate,
                //                PublicHex= CryptoManager.ByteArrayToHex(serializedPublicBytes),
                //                PrivateHex= CryptoManager.ByteArrayToHex(serializedPrivateBytes),
                //                LenPriv= serializedPrivateBytes.Length,
                //                LenPub= serializedPublicBytes.Length,
                //                Priv = privateKey.Equals(pair.Private),
                //                Pub = publicKey.Equals(pair.Public),
                EncryptWithPublic = encryptWithPublic,
                DecryptWithPrivate = decryptWithPrivate,
                Ex = ex
            });
        }


        //endpoint for testing
        [Route("auth")]
        [HttpGet]
        public IHttpActionResult Auth()
        {
            //endpoint for testing
            var ctx = HttpContext.Current.GetOwinContext().Get<ApplicationDbContext>();
            var list = ctx.Users.ToList();
            ctx.Database.ExecuteSqlCommand("delete from PublicKeys");
            ctx.Database.ExecuteSqlCommand("delete from Messages");
            ctx.SaveChanges();
            MockDbInitializer.LoadPublicKeys(list).ForEach(key => ctx.PublicKeys.Add(key));
            MockDbInitializer.LoadMessages(list).ForEach(k => ctx.Messages.Add(k));
            ctx.SaveChanges();
            return Ok(new
            {
                Test = "done"
            });
        }
    }
}