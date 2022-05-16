using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity.ModelConfiguration.Configuration;
using System.Linq;
using System.Web;
using Newtonsoft.Json;

namespace CryptexWeb.Models
{
    public class Message
    {
        public Message()
        {
        }

        public const int MaxMessageLenght = 1500;
        public int Id { get; set; } //INT AUTO_INCREMENT NOT NULL,
        public DateTime SendDate { get; set; } // DATETIME NOT NULL,
        public DateTime? ReceiveDate { get; set; } //     DATETIME,
        public bool Displayed { get; set; } //   BOOLEAN NOT NULL,

        [ForeignKey("Conversation")]
        public int ConversationId { get; set; } //  INT NOT NULL,
        public Conversation Conversation { get; set; } //  INT NOT NULL,
        public string Content { get; set; } // TEXT NOT NULL,
        public byte SecurityLevel { get; set; } //  TINYINT(2) NOT NULL,
        public byte SenderNumber { get; set; } //  TINYINT(2) NOT NULL,

        [ForeignKey("Sender")]
        [InverseProperty("SentMessages")]
        public int SenderId { get; set; }
        public User Sender { get; set; }

        //todo probably to remove
//        public string SenderSymmetricKey { get; set; } //  TINYINT(2) NOT NULL,

        //todo probably to remove
//        public string ReceiverSymmetricKey { get; set; } //  TINYINT(2) NOT NULL,

//        [ForeignKey("ReceiverPublicKey")]
//        public int ReceiverPublicKeyId { get; set; } //INT NOT NULL,
//        public PublicKey ReceiverPublicKey { get; set; } 
//        [ForeignKey("SenderPublicKey")]
//        public int SenderPublicKeyId { get; set; } //INT NOT NULL,
//        public PublicKey SenderPublicKey { get; set; } 
        public bool Visible { get; set; } //BOOLEAN NOT NULL,   
    }

    public class Conversation
    {
        public Conversation()
        {
        }

        public int Id { get; set; }
        // [ForeignKey("Sender")]
        //  public int SenderId { get; set; }
        [ForeignKey("CreatorId")]
        [InverseProperty("ConversationsAsCreator")]
        public User Creator { get; set; }

        public int CreatorId { get; set; }

        [ForeignKey("ParticipantId")]
        [InverseProperty("ConversationsAsParticipant")]
        public User Participant { get; set; }

        public int ParticipantId { get; set; }

        [JsonIgnore] public virtual ICollection<Message> Messages { get; set; }
    }

    public class PublicKey
    {
        public PublicKey()
        {
        }

        public int Id { get; set; }

        [ForeignKey("Owner")]
        public int OwnerId { get; set; }

        public User Owner { get; set; }
        public DateTime CreateDate { get; set; }
        public DateTime? DisableDate { get; set; }

        public string KeyString { get; set; }
    }

    public class BlockedUser
    {
        public BlockedUser()
        {
        }

        public int Id { get; set; }

        public User User { get; set; }

        [ForeignKey("User")]
        public int UserId { get; set; }

        public User Blocked { get; set; }

        [ForeignKey("Blocked")]
        public int BlockedId { get; set; }

        public DateTime BlockDate { get; set; }
    }

    public class Contact
    {
        public Contact()
        {
        }

        public Contact(User owner, User contactUser, DateTime inviteDate)
        {
            ContactUser = contactUser;
            InviteDate = inviteDate;
            Owner = owner;
        }

        public int Id { get; set; }

        [ForeignKey("OwnerId")]
        [InverseProperty("ContactsAsOwner")]
        public User Owner { get; set; }

        public int OwnerId { get; set; }

        //    [InverseProperty("ContactsAsReceiver")]
        [ForeignKey("ContactUserId")]
        public User ContactUser { get; set; }

        public int ContactUserId { get; set; }

        public bool Accepted { get; set; }
        public DateTime InviteDate { get; set; }
        public bool Enabled { get; set; }
        public int Color { get; set; }
        public string Description { get; set; }
    }

    public class ContactUO
    {
        public int color;
        public string userName;
        public string description;
    }

    public class ContactGO
    {
        public int id;
        public int color;
        public string userName;
        public string userDescription;
        public string description;
        public byte securityLevel;
    }

  /*  "id": 1,
    "interlocutor": "dburke0",
    "lastMessageDate": 1468155119,
    "lastMessageId": 4,
    "key"*/

    public class ConversationUO
    {
        public string interlocutor;
    }

    public class ConversationGO
    {
        public int id;
        public string interlocutor;
        public long lastMessageDate;
        public int lastMessageId;
        public string key;
    }

    public class MessageUO
    {
        public string content;
        public int conversationId;
    }
 
    public class MessageGO
    {
        public int id;
        public string content;
        public byte securityLevel;
        public bool isSender;
        public long sendDate;
        public bool displayed;
        public int conversationId;
    }

    public class UserGO
    {
        public int id;
        public string userName;
        public string userDescription;
        public byte securityLevel;
        public string publicKey;
    }

    public class UploadResponse
    {
        public int id;
        public UploadStatus status;
        public string error;
        public string data;
    }

    public enum UploadStatus
    {
        Success = 1,
        Failure = 10
    }
}