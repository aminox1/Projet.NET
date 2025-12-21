using Gauniv.WebServer.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Gauniv.WebServer.ViewModels;

namespace Gauniv.WebServer.Services
{
    public class GameService
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<User> _userManager;

        public GameService(ApplicationDbContext db, UserManager<User> userManager)
        {
            _db = db;
            _userManager = userManager;
        }

        public async Task<List<GameViewModel>> GetAllAsync(string? userId)
        {
            var games = await _db.Games
                .Include(g => g.Categories)
                .Include(g => g.Owners)
                .Include(g => g.Images)
                .ToListAsync();

            return games.Select(g => new GameViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                Price = g.Price,
                Size = g.Size ?? 0,
                Categories = g.Categories.Select(c => c.Name).ToList(),
                IsOwned = userId != null && g.Owners.Any(o => o.Id == userId),
                PrimaryImageId = (g.Images.FirstOrDefault(i => i.IsPrimary)
                                   ?? g.Images.OrderBy(i => i.SortOrder).FirstOrDefault())?.Id,
                Images = g.Images.Select(i => new Gauniv.WebServer.ViewModels.GameImageViewModel { Id = i.Id, IsPrimary = i.IsPrimary, SortOrder = i.SortOrder }).ToList()
            }).ToList();
        }

        public async Task<List<GameViewModel>> GetOwnedAsync(string userId)
        {
            var games = await _db.Games
                .Include(g => g.Categories)
                .Include(g => g.Owners)
                .Include(g => g.Images)
                .Where(g => g.Owners.Any(o => o.Id == userId))
                .ToListAsync();

            return games.Select(g => new GameViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                Price = g.Price,
                Size = g.Size ?? 0,
                Categories = g.Categories.Select(c => c.Name).ToList(),
                IsOwned = true,
                PrimaryImageId = (g.Images.FirstOrDefault(i => i.IsPrimary)
                                   ?? g.Images.OrderBy(i => i.SortOrder).FirstOrDefault())?.Id,
                Images = g.Images.Select(i => new Gauniv.WebServer.ViewModels.GameImageViewModel { Id = i.Id, IsPrimary = i.IsPrimary, SortOrder = i.SortOrder }).ToList()
            }).ToList();
        }

        public async Task<(bool Succeeded, string Message)> PurchaseAsync(string userId, int gameId)
        {
            var user = await _db.Users
                .Include(u => u.OwnedGames)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return (false, "Utilisateur introuvable");

            var game = await _db.Games
                .Include(g => g.Owners)
                .FirstOrDefaultAsync(g => g.Id == gameId);
            if (game == null)
                return (false, "Jeu introuvable");

            if (game.Owners.Any(o => o.Id == userId))
                return (false, "Vous possédez déjà ce jeu");

            user.OwnedGames.Add(game);
            await _db.SaveChangesAsync();
            return (true, "Achat du jeu effectué avec succès");
        }

        public async Task<(List<GameViewModel> Items, int TotalCount)> GetFilteredAsync(
            string? userId,
            string? name = null,
            decimal? minPrice = null,
            decimal? maxPrice = null,
            IEnumerable<string>? categories = null,
            bool? isOwned = null,
            long? minSize = null,
            long? maxSize = null,
            int page = 1,
            int pageSize = 10,
            bool orderById = false)
        {
            var query = _db.Games
                .Include(g => g.Categories)
                .Include(g => g.Owners)
                .Include(g => g.Images)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(g => g.Name.Contains(name));
            }
            if (minPrice.HasValue)
            {
                query = query.Where(g => g.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(g => g.Price <= maxPrice.Value);
            }
            if (categories != null)
            {
                var catList = categories.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()).ToList();
                if (catList.Any())
                {
                    query = query.Where(g => g.Categories.Any(c => catList.Contains(c.Name)));
                }
            }
            if (minSize.HasValue)
            {
                query = query.Where(g => (g.Size ?? 0) >= minSize.Value);
            }
            if (maxSize.HasValue)
            {
                query = query.Where(g => (g.Size ?? 0) <= maxSize.Value);
            }
            if (isOwned.HasValue && userId != null)
            {
                if (isOwned.Value)
                    query = query.Where(g => g.Owners.Any(o => o.Id == userId));
                else
                    query = query.Where(g => g.Owners.All(o => o.Id != userId));
            }

            var total = await query.CountAsync();

            var orderedQuery = orderById ? query.OrderBy(g => g.Id) : query.OrderBy(g => g.Name);

            var games = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var items = games.Select(g => new GameViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                Price = g.Price,
                Size = g.Size ?? 0,
                Categories = g.Categories.Select(c => c.Name).ToList(),
                IsOwned = userId != null && g.Owners.Any(o => o.Id == userId),
                PrimaryImageId = (g.Images.FirstOrDefault(i => i.IsPrimary)
                                   ?? g.Images.OrderBy(i => i.SortOrder).FirstOrDefault())?.Id,
                Images = g.Images.Select(i => new Gauniv.WebServer.ViewModels.GameImageViewModel { Id = i.Id, IsPrimary = i.IsPrimary, SortOrder = i.SortOrder }).ToList()
            }).ToList();

            return (items, total);
        }

        // Récupère un jeu par id et mappe en GameViewModel (ou null si non trouvé)
        public async Task<GameViewModel?> GetByIdAsync(int id, string? userId)
        {
            var g = await _db.Games
                .Include(x => x.Categories)
                .Include(x => x.Owners)
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (g == null) return null;

            return new GameViewModel
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                Price = g.Price,
                Size = g.Size ?? 0,
                Categories = g.Categories.Select(c => c.Name).ToList(),
                IsOwned = userId != null && g.Owners.Any(o => o.Id == userId),
                PrimaryImageId = (g.Images.FirstOrDefault(i => i.IsPrimary)
                                   ?? g.Images.OrderBy(i => i.SortOrder).FirstOrDefault())?.Id,
                Images = g.Images.Select(i => new Gauniv.WebServer.ViewModels.GameImageViewModel { Id = i.Id, IsPrimary = i.IsPrimary, SortOrder = i.SortOrder }).ToList()
            };
        }
    }
}
