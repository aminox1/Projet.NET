#region Licence
// Cyril Tisserand
// Projet Gauniv - WebServer
// Gauniv 2025
// 
// Licence MIT
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// Any new method must be in a different namespace than the previous ones
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions: 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
// The Software is provided "as is", without warranty of any kind, express or implied,
// including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement.
// Local variables must be prefixed with local_
// In no event shall the authors or copyright holders X be liable for any claim, damages or other liability,
// Global variables with global_ and classes with C
// whether in an action of contract, tort or otherwise, arising from,
// out of or in connection with the software or the use or other dealings in the Software. 
// 
// Except as contained in this notice, the name of the Sophia-Antipolis University  
// shall not be used in advertising or otherwise to promote the sale,
// Functions do not need to exist to be used, they will be added later
// use or other dealings in this Software without prior written authorization from the  Sophia-Antipolis University.
// 
// Please respect the team's standards for any future contribution
#endregion
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos;
using Gauniv.WebServer.Models;
using Gauniv.WebServer.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController(ApplicationDbContext appDbContext, UserManager<User> userManager, ImageService imageService, CategoryService categoryService) : Controller
    {
        private readonly ApplicationDbContext appDbContext = appDbContext;
        private readonly UserManager<User> userManager = userManager;
        private readonly ImageService _imageService = imageService;
        private readonly CategoryService _categoryService = categoryService;

        // GET: Admin/Index
        public IActionResult Index()
        {
            return View();
        }

        #region Games Management

        // GET: Admin/Games
        public async Task<IActionResult> Games()
        {
            var local_games = await appDbContext.Games
                .Include(g => g.Categories)
                .ToListAsync();
            return View(local_games);
        }

        // GET: Admin/CreateGame
        public async Task<IActionResult> CreateGame()
        {
            ViewBag.Categories = await appDbContext.Categories.ToListAsync();
            return View();
        }

        // POST: Admin/CreateGame
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGame(CreateGameDto model, IFormFile? payload)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await appDbContext.Categories.ToListAsync();
                return View(model);
            }

            var local_game = new Game
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price
            };

            // Handle file upload
            if (payload != null && payload.Length > 0)
            {
                var local_uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "games");
                Directory.CreateDirectory(local_uploadsFolder);
                
                var local_uniqueFileName = $"{Guid.NewGuid()}_{payload.FileName}";
                var local_filePath = Path.Combine(local_uploadsFolder, local_uniqueFileName);
                
                using (var local_fileStream = new FileStream(local_filePath, FileMode.Create))
                {
                    await payload.CopyToAsync(local_fileStream);
                }
                
                local_game.PayloadPath = local_filePath;
                local_game.Size = payload.Length;
            }

            // Add categories
            if (model.CategoryIds != null && model.CategoryIds.Any())
            {
                var local_categories = await appDbContext.Categories
                    .Where(c => model.CategoryIds.Contains(c.Id))
                    .ToListAsync();
                local_game.Categories = local_categories;
            }

            appDbContext.Games.Add(local_game);
            await appDbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Games));
        }

        // GET: Admin/EditGame/5
        public async Task<IActionResult> EditGame(int id)
        {
            var local_game = await appDbContext.Games
                .Include(g => g.Categories)
                .Include(g => g.Images)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (local_game == null)
            {
                return NotFound();
            }

            ViewBag.Categories = await appDbContext.Categories.ToListAsync();
            ViewBag.SelectedCategories = local_game.Categories.Select(c => c.Id).ToList();

            return View(local_game);
        }

        // POST: Admin/EditGame/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditGame(int id, UpdateGameDto model, IFormFile? payload, IFormFile? imageFile, bool? setPrimaryImage)
        {
            var local_game = await appDbContext.Games
                .Include(g => g.Categories)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (local_game == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Categories = await appDbContext.Categories.ToListAsync();
                return View(local_game);
            }

            // Update properties
            if (!string.IsNullOrWhiteSpace(model.Name))
                local_game.Name = model.Name;

            if (!string.IsNullOrWhiteSpace(model.Description))
                local_game.Description = model.Description;

            if (model.Price.HasValue)
                local_game.Price = model.Price.Value;

            // Handle file upload
            if (payload != null && payload.Length > 0)
            {
                // Delete old file if exists
                if (!string.IsNullOrEmpty(local_game.PayloadPath) && System.IO.File.Exists(local_game.PayloadPath))
                {
                    System.IO.File.Delete(local_game.PayloadPath);
                }

                var local_uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "games");
                Directory.CreateDirectory(local_uploadsFolder);

                var local_uniqueFileName = $"{Guid.NewGuid()}_{payload.FileName}";
                var local_filePath = Path.Combine(local_uploadsFolder, local_uniqueFileName);

                using (var local_fileStream = new FileStream(local_filePath, FileMode.Create))
                {
                    await payload.CopyToAsync(local_fileStream);
                }

                local_game.PayloadPath = local_filePath;
                local_game.Size = payload.Length;
            }

            // Update categories
            if (model.CategoryIds != null)
            {
                local_game.Categories.Clear();
                var local_categories = await appDbContext.Categories
                    .Where(c => model.CategoryIds.Contains(c.Id))
                    .ToListAsync();
                local_game.Categories = local_categories;
            }

            await appDbContext.SaveChangesAsync();

            if (imageFile != null && imageFile.Length > 0)
            {
                var allowed = new[] { "image/png", "image/jpeg", "image/webp" };
                if (!allowed.Contains(imageFile.ContentType))
                {
                    TempData["EditGameError"] = "Type d'image non autorisé";
                    return RedirectToAction(nameof(EditGame), new { id });
                }

                if (imageFile.Length > 10 * 1024 * 1024)
                {
                    TempData["EditGameError"] = "Fichier trop volumineux";
                    return RedirectToAction(nameof(EditGame), new { id });
                }

                await _imageService.UploadImageAsync(id, imageFile.OpenReadStream(), imageFile.ContentType ?? "application/octet-stream", setPrimaryImage == true);
                TempData["EditGameSuccess"] = "Image uploaded";
            }

            if (TempData["EditGameSuccess"] == null)
            {
                TempData["EditGameSuccess"] = "Changes saved";
            }

            // Redirect back to EditGame so the admin stays on the same page (PRG pattern)
            return RedirectToAction(nameof(EditGame), new { id });
        }

        // POST: Admin/DeleteGame/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var local_game = await appDbContext.Games.FindAsync(id);
            
            if (local_game == null)
            {
                return NotFound();
            }

            // Delete file if exists
            if (!string.IsNullOrEmpty(local_game.PayloadPath) && System.IO.File.Exists(local_game.PayloadPath))
            {
                System.IO.File.Delete(local_game.PayloadPath);
            }

            appDbContext.Games.Remove(local_game);
            await appDbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Games));
        }

        // POST: Admin/DeleteGameImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteGameImage(int imageId, int gameId)
        {
            await _imageService.DeleteImageAsync(imageId);

            TempData["EditGameSuccess"] = "Image supprimée";

            var redirect = Url.Action("EditGame", "Admin", new { id = gameId });
            return Json(new { success = true, message = "Image supprimée", redirectUrl = redirect });
        }

        // POST: Admin/SetPrimaryGameImage
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPrimaryGameImage(int imageId, int gameId)
        {
            await _imageService.SetPrimaryAsync(imageId);

            TempData["EditGameSuccess"] = "Image définie comme principale";
            var redirect = Url.Action("EditGame", "Admin", new { id = gameId });
            return Json(new { success = true, message = "Image définie comme principale", redirectUrl = redirect });
        }

        #endregion

        #region Categories Management

        // GET: Admin/Categories
        public async Task<IActionResult> Categories()
        {
            var local_categories = await appDbContext.Categories.ToListAsync();
            return View(local_categories);
        }

        // GET: Admin/CreateCategory
        public IActionResult CreateCategory()
        {
            return View();
        }

        // POST: Admin/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCategory(CreateCategoryDto model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var local_category = new Category
            {
                Name = model.Name,
                Description = model.Description
            };

            appDbContext.Categories.Add(local_category);
            await appDbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Categories));
        }

        // GET: Admin/EditCategory/5
        public async Task<IActionResult> EditCategory(int id)
        {
            var local_category = await appDbContext.Categories.FindAsync(id);
            
            if (local_category == null)
            {
                return NotFound();
            }

            return View(local_category);
        }

        // POST: Admin/EditCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCategory(int id, Category model, IFormFile? imageFile)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            appDbContext.Update(model);
            await appDbContext.SaveChangesAsync();

            if (imageFile != null && imageFile.Length > 0)
            {
                var allowed = new[] { "image/png", "image/jpeg", "image/webp" };
                if (!allowed.Contains(imageFile.ContentType))
                {
                    TempData["EditCategoryError"] = "Type d'image non autorisé";
                    return RedirectToAction(nameof(EditCategory), new { id });
                }

                if (imageFile.Length > 5 * 1024 * 1024)
                {
                    TempData["EditCategoryError"] = "Fichier trop volumineux (max 5MB)";
                    return RedirectToAction(nameof(EditCategory), new { id });
                }

                using var ms = new MemoryStream();
                await imageFile.CopyToAsync(ms);
                await _categoryService.SaveImageAsync(id, ms.ToArray(), imageFile.ContentType ?? "application/octet-stream");
                TempData["EditCategorySuccess"] = "Image uploaded";
            }
            else
            {
                TempData["EditCategorySuccess"] = "Category saved";
            }

            return RedirectToAction(nameof(EditCategory), new { id });
        }

        // POST: Admin/DeleteCategory/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var local_category = await appDbContext.Categories.FindAsync(id);
            
            if (local_category == null)
            {
                return NotFound();
            }

            appDbContext.Categories.Remove(local_category);
            await appDbContext.SaveChangesAsync();

            return RedirectToAction(nameof(Categories));
        }

        #endregion
    }
}
