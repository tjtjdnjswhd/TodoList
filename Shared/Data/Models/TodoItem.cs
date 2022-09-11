#nullable disable

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoList.Shared.Data.Models
{
    [Table(nameof(TodoItem))]
    public sealed class TodoItem
    {
        public TodoItem()
        {
        }

        public TodoItem(string name, Guid userId)
        {
            Name = name;
            UserId = userId;
        }

        [Key]
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
        [Required]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public DateTime CreatedAt { get; set; }
        [Required]
        [DefaultValue(false)]
        public bool IsComplete { get; set; }

        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; }
    }
}
