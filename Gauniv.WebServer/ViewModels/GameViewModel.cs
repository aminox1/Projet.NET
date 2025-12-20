using System.Collections.Generic;

namespace Gauniv.WebServer.ViewModels
{
    public class GameViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public long Size { get; set; }
        public List<string> Categories { get; set; } = new();
        public bool IsOwned { get; set; }
        public int? PrimaryImageId { get; set; }
        public List<GameImageViewModel> Images { get; set; } = new();
    }
}
