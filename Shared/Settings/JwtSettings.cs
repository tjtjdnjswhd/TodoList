﻿#nullable disable

namespace TodoList.Shared.Settings
{
    public class JwtSettings
    {
        public string Issuer { get; set; }
        public string Audience { get; set; }
        public string SecretKey { get; set; }
        public string SecurityAlgorithmName { get; set; }
    }
}
