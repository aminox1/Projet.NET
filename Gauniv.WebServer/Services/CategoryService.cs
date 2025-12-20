using Gauniv.WebServer.Data;
using Gauniv.WebServer.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Services
{
    public class CategoryService
    {
        private readonly ApplicationDbContext _db;
        public CategoryService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<CategoryListViewModel> GetPagedAsync(int page, int pageSize, string? search, int[]? categoryIds)
        {
            var query = _db.Categories.AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c => c.Name.Contains(search));
            }

            if (categoryIds != null && categoryIds.Length > 0)
            {
                query = query.Where(c => categoryIds.Contains(c.Id));
            }

            var total = await query.CountAsync();

            var items = await query
                .Include(c => c.Games)
                .OrderByDescending(c => c.Games.Count)
                .ThenBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var allCats = await _db.Categories.OrderBy(c => c.Name).ToListAsync();

            var vm = new CategoryListViewModel
            {
                Items = items.Select(c => new CategoryViewModel { Id = c.Id, Name = c.Name, GamesCount = c.Games?.Count ?? 0, HasImage = c.ImageData != null && c.ImageData.Length > 0 }).ToList(),
                AllCategories = allCats.Select(c => new CategoryViewModel { Id = c.Id, Name = c.Name, GamesCount = c.Games?.Count ?? 0, HasImage = c.ImageData != null && c.ImageData.Length > 0 }).ToList(),
                SelectedCategoryIds = (categoryIds ?? Array.Empty<int>()).ToList(),
                Page = page,
                PageSize = pageSize,
                TotalItems = total,
                Search = search
            };

            return vm;
        }

        public async Task<(byte[]? Data, string? ContentType)> GetImageAsync(int categoryId)
        {
            var cat = await _db.Categories.Where(c => c.Id == categoryId).Select(c => new { c.ImageData, c.ImageContentType }).FirstOrDefaultAsync();
            if (cat == null || cat.ImageData == null) return (null, null);
            return (cat.ImageData, cat.ImageContentType);
        }

        public async Task<bool> SaveImageAsync(int categoryId, byte[] data, string contentType)
        {
            var cat = await _db.Categories.FindAsync(categoryId);
            if (cat == null) return false;
            cat.ImageData = data;
            cat.ImageContentType = contentType;
            _db.Categories.Update(cat);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
