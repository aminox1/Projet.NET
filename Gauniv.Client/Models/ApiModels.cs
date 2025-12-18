using CommunityToolkit.Mvvm.ComponentModel;

namespace Gauniv.Client.Models
{
    public partial class GameDto : ObservableObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public long? Size { get; set; }
        public List<CategoryDto> Categories { get; set; } = new();
        public bool IsOwned { get; set; }
        
        [ObservableProperty]
        private bool isDownloaded;
        
        public string LocalPath { get; set; } = string.Empty;
        
        [ObservableProperty]
        private bool isRunning;
    }

    public class CategoryDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public string? TokenType { get; set; }
        public string? AccessToken { get; set; }
        public int ExpiresIn { get; set; }
        public string? RefreshToken { get; set; }
    }
}
