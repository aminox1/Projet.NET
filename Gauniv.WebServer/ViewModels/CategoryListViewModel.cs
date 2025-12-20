using System.Collections.Generic;

namespace Gauniv.WebServer.ViewModels
{
    public class CategoryListViewModel
    {
        public List<CategoryViewModel> Items { get; set; } = new();
        public List<CategoryViewModel> AllCategories { get; set; } = new();
        public List<int> SelectedCategoryIds { get; set; } = new();

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 12;
        public int TotalItems { get; set; } = 0;
        public int TotalPages => (int)System.Math.Ceiling((double)TotalItems / System.Math.Max(1, PageSize));
        public string? Search { get; set; }
    }
}
