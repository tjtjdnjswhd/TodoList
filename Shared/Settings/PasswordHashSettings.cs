#nullable disable

namespace TodoList.Shared.Settings
{
    public sealed class PasswordHashSettings
    {
        public string Pepper { get; set; }
        public int SaltLength { get; set; }
        public int HashLength { get; set; }
        public int HashIterations { get; set; }
    }
}
