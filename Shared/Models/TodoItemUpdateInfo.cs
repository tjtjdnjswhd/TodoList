namespace TodoList.Shared.Models
{
    public sealed class TodoItemUpdateInfo
    {
        public int ItemId { get; set; }
        public string? NewName { get; set; }
    }
}
