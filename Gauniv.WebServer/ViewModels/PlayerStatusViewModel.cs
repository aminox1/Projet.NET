using System;

namespace Gauniv.WebServer.ViewModels
{
    public class PlayerStatusViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsOnline { get; set; }
        public string? Status { get; set; }
        public DateTime LastSeenUtc { get; set; }
    }
}

