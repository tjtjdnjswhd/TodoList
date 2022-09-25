#nullable disable

namespace TodoList.Shared.Data.Dtos
{
    public sealed class TodoItemDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsComplete { get; set; }
        public Guid UserId { get; set; }

        public override bool Equals(object obj)
        {
            return obj is TodoItemDto dto &&
                   Id == dto.Id;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Id);
        }
    }
}