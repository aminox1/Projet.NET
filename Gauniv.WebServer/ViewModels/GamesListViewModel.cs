using System.Collections.Generic;

namespace Gauniv.WebServer.ViewModels
{
    public class GamesListViewModel
    {
        public List<GameViewModel> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        // Filters
        public string? Name { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? Category { get; set; }
        public bool? IsOwned { get; set; }
        public long? MinSize { get; set; }
        public long? MaxSize { get; set; }
    }
}

