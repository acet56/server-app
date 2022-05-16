using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Newtonsoft.Json;

namespace CryptexWeb.Models
{
    public class UserLogin : IdentityUserLogin<int>
    {
    }

    public class UserClaim : IdentityUserClaim<int>
    {
    }

    public class UserRole : IdentityUserRole<int>
    {
    }

    // You can add profile data for the user by adding more properties to your User class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.


    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    [SuppressMessage("ReSharper", "UnassignedGetOnlyAutoProperty")]
    public class User : IdentityUser<int, UserLogin, UserRole, UserClaim>, IUser<int>
    {
        public const byte SecurityLevelBasic = 0;
        public const byte SecurityLevelExtened = 1;
        public const byte SecurityLevelAdvanced = 2;

        public bool Active { get; set; }
        public DateTime Registered { get; set; }
        public DateTime? LastLoginInApp { get; set; }
        public DateTime? LastLoginInWeb { get; set; }
        public DateTime? LastSynchronization { get; set; }
        public byte SecurityLevel { get; set; }
        public string Description { get; set; }
        public string DeviceKey { get; set; }
        public string AdditionalData { get; set; }

        [JsonIgnore]
        public virtual ICollection<Conversation> ConversationsAsCreator { get; set; }
        [JsonIgnore] public virtual ICollection<Conversation> ConversationsAsParticipant { get; set; }
        [JsonIgnore] public virtual ICollection<PublicKey> PublicKeys { get; set; }
        [JsonIgnore] public virtual ICollection<Contact> ContactsAsOwner { get; set; }
        [JsonIgnore] public virtual ICollection<Message> SentMessages { get; set; }
        //  public virtual ICollection<Contact> ContactsAsReceiver { get; }

        public User()
        {
            SecurityLevel = SecurityLevelBasic;
        }


        public async Task<ClaimsIdentity>
            GenerateUserIdentityAsync(UserManager<User, int> manager)
        {
            var userIdentity = await manager
                .CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
            // Add custom user claims here
//            userIdentity.AddClaim(new Claim(ClaimTypes.NameIdentifier, userIdentity.GetUserId()));
            return userIdentity;
        }

        public override string ToString()
        {
            return $"User: {UserName}, {PasswordHash} | Id:{Id}";
        }
    }


    public class Role : IdentityRole<int, UserRole>, IRole<int>
    {
        public string Description { get; set; }

        public Role() : base()
        {
        }

        public Role(string name)
            : this()
        {
            this.Name = name;
        }

        public Role(string name, string description)
            : this(name)
        {
            this.Description = description;
        }

        public const string UserRoleName = "User";
        public const string AdminRoleName = "Admin";
    }

    [DbConfigurationType(typeof(MySql.Data.Entity.MySqlEFConfiguration))]
    public class ApplicationDbContext
        : IdentityDbContext<User, Role, int,
            UserLogin, UserRole, UserClaim>
    {
        // public virtual IDbSet<Message> Messages { get; set; }
        public virtual IDbSet<BlockedUser> BlockedUsers { get; set; }
        public virtual IDbSet<Message> Messages { get; set; }
        public virtual IDbSet<Contact> Contacts { get; set; }
        public virtual IDbSet<Conversation> Conversations { get; set; }
        public virtual IDbSet<PublicKey> PublicKeys { get; set; }

        public ApplicationDbContext()
            : base("MySqlConnection")
        {
            DbConfiguration.SetConfiguration(new MySql.Data.Entity.MySqlEFConfiguration());
        }

        static ApplicationDbContext()
        {
            Database.SetInitializer<ApplicationDbContext>(new ApplicationDbInitializer());
        }

        public static ApplicationDbContext Create()
        {
            return new ApplicationDbContext();
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);


            modelBuilder.Entity<UserClaim>().ToTable("UserClaims");
            modelBuilder.Entity<UserLogin>().ToTable("UserLogins");
            modelBuilder.Entity<UserRole>().ToTable("UserRoles");
            modelBuilder.Entity<PublicKey>().Property(key => key.KeyString).IsRequired();
            var messages = modelBuilder.Entity<Message>();
            messages.Property(key => key.Content).IsRequired();
//            messages.Property(key => key.SenderSymmetricKey).HasMaxLength(256);
//            messages.Property(key => key.ReceiverSymmetricKey).HasMaxLength(256);

            modelBuilder.Entity<Role>().ToTable("Roles")
                .Property(c => c.Name).HasMaxLength(128).IsRequired();


            var users = modelBuilder.Entity<User>();
            users.ToTable("Users")
                .Property(c => c.UserName).HasMaxLength(128).IsRequired();
            users.Property(c => c.PasswordHash).HasMaxLength(128);
            users.Property(c => c.PhoneNumber).HasMaxLength(40);
            users.Property(c => c.SecurityStamp).HasMaxLength(128);
            users.Property(c => c.Description).HasMaxLength(128);
            users.Property(c => c.DeviceKey).HasMaxLength(256);
            users.Property(c => c.Email).IsRequired();
        }
    }


    public class ApplicationUserStore :
        UserStore<User, Role, int,
            UserLogin, UserRole, UserClaim>, IUserStore<User, int>, IDisposable
    {
        public ApplicationUserStore()
            : this(new IdentityDbContext())
        {
            base.DisposeContext = true;
        }

        public ApplicationUserStore(DbContext context)
            : base(context)
        {
        }
    }


    public class ApplicationRoleStore
        : RoleStore<Role, int, UserRole>, IQueryableRoleStore<Role, int>, IRoleStore<Role, int>, IDisposable
    {
        public ApplicationRoleStore()
            : base(new IdentityDbContext())
        {
            base.DisposeContext = true;
        }

        public ApplicationRoleStore(DbContext context)
            : base(context)
        {
        }
    }
}