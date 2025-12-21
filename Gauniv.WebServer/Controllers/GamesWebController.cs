using Gauniv.WebServer.Services;
using Gauniv.WebServer.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.IO;
using System.Linq;

namespace Gauniv.WebServer.Controllers
{
    public class GamesWebController : Controller
    {
        private readonly GameService _gameService;
        private readonly UserManager<User> _userManager;
        private readonly ImageService _imageService;

        public GamesWebController(GameService gameService, UserManager<User> userManager, ImageService imageService)
        {
            _gameService = gameService;
            _userManager = userManager;
            _imageService = imageService;
        }
        
        [HttpGet]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> MyGames(string? name, decimal? minPrice, decimal? maxPrice, string[]? category, long? minSize, long? maxSize, int page = 1, int pageSize = 10)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();
            
            IEnumerable<string>? cats = category;
            const long BYTES_IN_MO = 1024L * 1024L;
            long? minSizeBytes = null;
            long? maxSizeBytes = null;
            if (minSize.HasValue) minSizeBytes = minSize.Value * BYTES_IN_MO;
            if (maxSize.HasValue) maxSizeBytes = maxSize.Value * BYTES_IN_MO;

            var (items, total) = await _gameService.GetFilteredAsync(userId, name, minPrice, maxPrice, cats, isOwned: true, minSize: minSizeBytes, maxSize: maxSizeBytes, page: page, pageSize: pageSize);

            var vm = new Gauniv.WebServer.ViewModels.GamesListViewModel
            {
                Items = items,
                TotalCount = total,
                Page = page,
                PageSize = pageSize,
                Name = name,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                Category = category != null ? string.Join(",", category) : null,
                IsOwned = true,
                MinSize = minSize,
                MaxSize = maxSize
            };

            return View("~/Views/Games/MyGames.cshtml", vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "User")]
        public async Task<IActionResult> Purchase(int id)
        {
            var userId = _userManager.GetUserId(User);
            if (userId == null) return Challenge();

            var result = await _gameService.PurchaseAsync(userId, id);
            if (!result.Succeeded)
            {
                TempData["Error"] = result.Message;
            }
            else
            {
                TempData["Success"] = result.Message;
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Details(int id)
        {
            var userId = _userManager.GetUserId(User);
            var vm = await _gameService.GetByIdAsync(id, userId);
            if (vm == null) return NotFound();

            return View("~/Views/Games/Details.cshtml", vm);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetImage(int id)
        {
            var img = await _imageService.GetImageAsync(id);
            if (img == null) return NotFound();
            return File(img.Value.Data, img.Value.ContentType);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadImage(int gameId, IFormFile file, bool setPrimary = false)
        {
            if (file == null || file.Length == 0) return BadRequest("Fichier non fourni");
            var allowed = new[] { "image/png", "image/jpeg", "image/webp" };
            if (!allowed.Contains(file.ContentType)) return BadRequest("Type non autorisé");
            if (file.Length > 10 * 1024 * 1024) return BadRequest("Fichier trop volumineux");

            await _imageService.UploadImageAsync(gameId, file.OpenReadStream(), file.ContentType ?? "application/octet-stream", setPrimary);
            return RedirectToAction("Details", new { id = gameId });
        }
    }
}
