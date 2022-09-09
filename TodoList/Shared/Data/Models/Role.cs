#nullable disable

using Microsoft.EntityFrameworkCore;

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TodoList.Shared.Data.Models
{
    [Table(nameof(Role))]
    public sealed class Role
    {
        [Key]
        [MaxLength(20)]
        [Unicode(false)]
        public string Name { get; set; }
        [Required]
        public int Priority { get; set; }

        public List<User> Users { get; set; }
    }
}
