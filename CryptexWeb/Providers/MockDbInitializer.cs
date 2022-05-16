using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Hosting;
using CryptexWeb.Models;
using Newtonsoft.Json;

namespace CryptexWeb.Providers
{
    public class MockDbInitializer
    {

        public MockDbInitializer()
        {
        }

        public List<User> LoadUsers()
        {
            string path = HostingEnvironment.MapPath("~\\App_Data\\users_e.json");
            var c = File.ReadAllText(path);
            List<UserMock> users = JsonConvert.DeserializeObject<List<UserMock>>(c);
            return users.Select((m, i) =>
            {
                return new User()
                {
                    Email = m.email,
                    UserName = m.userName,
                    Description = m.description,
                    SecurityLevel = m.securityLevel,
                    Registered = DateTime.Now,
                    Active = true
                };
            }).ToList();
        }

        public static IEnumerable<PublicKey> LoadPublicKeys(List<User> users)
        {
            string path = HostingEnvironment.MapPath("~\\App_Data\\pk.json");
            var c = File.ReadAllText(path);
            List<PublicKey> publicKeys = JsonConvert.DeserializeObject<List<PublicKey>>(c);
            publicKeys.ForEach(key => key.CreateDate = DateTime.UtcNow);
            return publicKeys;
        }

        public IEnumerable<Conversation> LoadConversations(List<User> users)
        {
            List<Conversation> conversations = new List<Conversation>();
            Random random = new Random();
            for (int i = 0; i < 100; i++)
            {
                int creator = i % users.Count;
                creator = users[creator].Id;
                int participant = creator;
                do
                {
                    participant = random.Next(0, users.Count - 1);
                    participant = users[participant].Id;
                } while (creator == participant);

                conversations.Add(new Conversation()
                {
                    CreatorId = creator,
                    ParticipantId = participant
                });
            }
            return conversations;
        }

        public IEnumerable<Contact> LoadContacts(List<User> users)
        {
            var s = LoadStrings();
            List<Contact> contacts = new List<Contact>();
            Random random = new Random();
            for (int i = 0; i < 100; i++)
            {
                int id = i % users.Count;
                id = users[id].Id;
                int contactId = id;
                do
                {
                    contactId = random.Next(1, users.Count - 1);
                    contactId = users[contactId].Id;
                } while (id == contactId);
                contacts.Add(new Contact()
                {
                    OwnerId = id,
                    ContactUserId = contactId,
                    Description = s[i],
                    InviteDate = DateTime.Now - TimeSpan.FromDays(1),
                    Enabled = true,
                    Accepted = id % 10 != 0,
                    Color = (int) RandomColor(),
                });
            }
            return contacts;
        }

        public static IEnumerable<Message> LoadMessages(List<User> users)
        {
            string path = HostingEnvironment.MapPath("~\\App_Data\\msg.json");
            var json = File.ReadAllText(path);

            List<MsgAnd> msg = JsonConvert.DeserializeObject<List<MsgAnd>>(json);
            Random randomDate = new Random();
            List<Message> messages = new List<Message>();
            for (int i = 0; i < msg.Count; i++)
            {
                MsgAnd m = msg[i];
                User user = users.First(user1 => user1.Id == m.senderId);
                messages.Add(new Message()
                {
                    SendDate = DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(randomDate.Next(1, 60 * 24 * 60))),
                    Displayed = false,
                    SenderId = m.senderId,
                    ConversationId = m.conversationId,
                    Content = m.content,
                    SecurityLevel = user.SecurityLevel,
                    SenderNumber = m.senderNumber,
                    Visible = true
                });
            }
            return messages;
        }

        public List<string> LoadStrings()
        {
            string path = HostingEnvironment.MapPath("~\\App_Data\\strings.json");
            var c = File.ReadAllText(path);
            return Json.Decode<List<string>>(c);
        }

        private uint RandomColor()
        {
            Random random = new Random();
            return MATERIAL_COLORS[random.Next(0, MATERIAL_COLORS.Length)];
        }


        private class UserMock
        {
            public string email;
            public string userName;
            public string description;
            public byte securityLevel;
        }

        private class Convers
        {
//            "id": 1,
//    "interlocutor": "dburke0",
//    "lastMessageDate": 1468155119,
//    "lastMessageId": 4,
//    "key":

            public string key;
        }

        class MsgAnd
        {
           /* public int Id { get; set; }
            public long SendDate { get; set; }
            public bool Displayed { get; set; }
            public bool IsSender { get; set; }
            public int ConversationId { get; set; }
            public string Content { get; set; }
            public byte SecurityLevel { get; set; }*/

            public string content;
            public int conversationId;
            public int senderId;
            public byte senderNumber;
        }


        private readonly uint[] MATERIAL_COLORS = new uint[]
        {
            0xFFF44336, // RED 500
            0xFFE91E63, // PINK 500
            0xFFFF2C93, // LIGHT PINK 500
            0xFF9C27B0, // PURPLE 500
            0xFF673AB7, // DEEP PURPLE 500
            0xFF3F51B5, // INDIGO 500
            0xFF2196F3, // BLUE 500
            0xFF03A9F4, // LIGHT BLUE 500
            0xFF00BCD4, // CYAN 500
            0xFF009688, // TEAL 500
            0xFF4CAF50, // GREEN 500
            0xFF8BC34A, // LIGHT GREEN 500
            0xFFCDDC39, // LIME 500
            0xFFFFEB3B, // YELLOW 500
            0xFFFFC107, // AMBER 500
            0xFFFF9800, // ORANGE 500
            0xFF795548, // BROWN 500
            0xFF9E9E9E, // GREY 500
        };
    }
}