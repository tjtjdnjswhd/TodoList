#nullable disable

using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoList.Shared.Data.Models
{
    [Table(nameof(User))]
    [Index(nameof(Email), IsUnique = true)]
    public sealed class User
    {
        public User()
        {
        }

        public User(string email, string name, bool isEmailVerified, string passwordHashBase64, string saltBase64, string roleName)
        {
            Email = email;
            Name = name;
            IsEmailVerified = isEmailVerified;
            PasswordHashBase64 = passwordHashBase64;
            SaltBase64 = saltBase64;
            RoleName = roleName;
        }

        [Key]
        public Guid Id { get; set; }
        [Required]
        [DataType(DataType.EmailAddress)]
        [MaxLength(320)]
        public string Email { get; set; }
        [Required]
        [MaxLength(12)]
        public string Name { get; set; }
        [Required]
        public bool IsEmailVerified { get; set; }
        [Required]
        [Unicode(false)]
        public string PasswordHashBase64 { get; set; }
        [Required]
        [MaxLength(20)]
        [Unicode(false)]
        public string SaltBase64 { get; set; }
        [Required]
        public DateTimeOffset SignupDate { get; set; }

        [Required]
        public string RoleName { get; set; }

        [ForeignKey(nameof(RoleName))]
        public Role Role { get; set; }
        public List<TodoItem> TodoItems { get; set; }
    }
}
