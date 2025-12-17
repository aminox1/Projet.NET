#region Licence
// Cyril Tisserand
// Projet Gauniv - WebServer
// Gauniv 2025
// 
// Licence MIT
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the “Software”), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// Any new method must be in a different namespace than the previous ones
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions: 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
// The Software is provided “as is”, without warranty of any kind, express or implied,
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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Text;
using CommunityToolkit.HighPerformance.Memory;
using CommunityToolkit.HighPerformance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using MapsterMapper;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Api
{
    [Route("api/1.0.0/[controller]/[action]")]
    [ApiController]
    public class GamesController(ApplicationDbContext appDbContext, IMapper mapper, UserManager<User> userManager, MappingProfile mp) : ControllerBase
    {
        private readonly ApplicationDbContext appDbContext = appDbContext;
        private readonly IMapper mapper = mapper;
        private readonly UserManager<User> userManager = userManager;
        private readonly MappingProfile mp = mp;

        // GET: api/1.0.0/Games/List
        // Liste tous les jeux avec filtres et pagination
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<GameDto>>> List(
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 10,
            [FromQuery] int[]? category = null,
            [FromQuery] string? name = null,
            [FromQuery] decimal? minPrice = null,
            [FromQuery] decimal? maxPrice = null)
        {
            var local_query = appDbContext.Games
                .Include(g => g.Categories)
                .Include(g => g.Owners)
                .AsQueryable();

            // Filtrer par catégories
            if (category != null && category.Length > 0)
            {
                local_query = local_query.Where(g => g.Categories.Any(c => category.Contains(c.Id)));
            }

            // Filtrer par nom
            if (!string.IsNullOrWhiteSpace(name))
            {
                local_query = local_query.Where(g => g.Name.Contains(name));
            }

            // Filtrer par prix
            if (minPrice.HasValue)
            {
                local_query = local_query.Where(g => g.Price >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                local_query = local_query.Where(g => g.Price <= maxPrice.Value);
            }

            var local_games = await local_query
                .Skip(offset)
                .Take(limit)
                .ToListAsync();

            var local_userId = userManager.GetUserId(User);
            
            var local_result = local_games.Select(g => new GameDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                Price = g.Price,
                Size = g.Size,
                Categories = g.Categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                }).ToList(),
                IsOwned = local_userId != null && g.Owners.Any(o => o.Id == local_userId)
            });

            return Ok(local_result);
        }

        // GET: api/1.0.0/Games/MyGames
        // Liste les jeux possédés par l'utilisateur connecté
        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<GameDto>>> MyGames(
            [FromQuery] int offset = 0,
            [FromQuery] int limit = 10,
            [FromQuery] int[]? category = null)
        {
            Console.WriteLine($"[MyGames] Request received, User.Identity.IsAuthenticated: {User.Identity?.IsAuthenticated}");
            
            var local_userId = userManager.GetUserId(User);
            Console.WriteLine($"[MyGames] UserId: {local_userId}");
            
            if (local_userId == null)
            {
                Console.WriteLine($"[MyGames] UserId is null, returning Unauthorized");
                return Unauthorized();
            }

            var local_query = appDbContext.Games
                .Include(g => g.Categories)
                .Include(g => g.Owners)
                .Where(g => g.Owners.Any(o => o.Id == local_userId));

            // Filtrer par catégories
            if (category != null && category.Length > 0)
            {
                local_query = local_query.Where(g => g.Categories.Any(c => category.Contains(c.Id)));
            }

            var local_games = await local_query
                .Skip(offset)
                .Take(limit)
                .ToListAsync();

            Console.WriteLine($"[MyGames] Found {local_games.Count} owned games for user {local_userId}");

            var local_result = local_games.Select(g => new GameDto
            {
                Id = g.Id,
                Name = g.Name,
                Description = g.Description,
                Price = g.Price,
                Size = g.Size,
                Categories = g.Categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                }).ToList(),
                IsOwned = true
            });

            return Ok(local_result);
        }

        // GET: api/1.0.0/Games/Categories
        // Liste toutes les catégories
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> Categories()
        {
            var local_categories = await appDbContext.Categories.ToListAsync();
            
            var local_result = local_categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description
            });

            return Ok(local_result);
        }

        // GET: api/1.0.0/Games/Download/{id}
        // Télécharger le binaire d'un jeu
        [HttpGet("{id}")]
        [Authorize]
        public async Task<IActionResult> Download(int id)
        {
            var local_userId = userManager.GetUserId(User);
            if (local_userId == null)
            {
                return Unauthorized();
            }

            var local_game = await appDbContext.Games
                .Include(g => g.Owners)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (local_game == null)
            {
                return NotFound("Game not found");
            }

            // Vérifier que l'utilisateur possède le jeu
            if (!local_game.Owners.Any(o => o.Id == local_userId))
            {
                return Forbid("You do not own this game");
            }

            if (string.IsNullOrEmpty(local_game.PayloadPath) || !System.IO.File.Exists(local_game.PayloadPath))
            {
                return NotFound("Game binary not found");
            }

            // Streamer le fichier pour éviter de charger tout en mémoire
            var local_stream = new FileStream(local_game.PayloadPath, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, useAsync: true);
            
            return File(local_stream, "application/octet-stream", $"{local_game.Name}.zip", enableRangeProcessing: true);
        }

        // POST: api/1.0.0/Games/Purchase/{id}
        // Acheter un jeu
        [HttpPost("{id}")]
        [Authorize]
        public async Task<IActionResult> Purchase(int id)
        {
            var local_userId = userManager.GetUserId(User);
            if (local_userId == null)
            {
                return Unauthorized();
            }

            var local_user = await appDbContext.Users
                .Include(u => u.OwnedGames)
                .FirstOrDefaultAsync(u => u.Id == local_userId);

            if (local_user == null)
            {
                return NotFound("User not found");
            }

            var local_game = await appDbContext.Games
                .Include(g => g.Owners)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (local_game == null)
            {
                return NotFound("Game not found");
            }

            // Vérifier si l'utilisateur possède déjà le jeu
            if (local_game.Owners.Any(o => o.Id == local_userId))
            {
                return BadRequest("You already own this game");
            }

            // Ajouter le jeu à la liste des jeux possédés
            Console.WriteLine($"[Purchase] User {local_userId} ({local_user.Email}) purchasing game {id} ({local_game.Name})");
            local_user.OwnedGames.Add(local_game);
            await appDbContext.SaveChangesAsync();
            Console.WriteLine($"[Purchase] Game purchased successfully. User now owns {local_user.OwnedGames.Count} games");

            return Ok(new { message = "Game purchased successfully" });
        }

        // GET: api/1.0.0/Games/Details/{id}
        // Obtenir les détails d'un jeu
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<GameDto>> Details(int id)
        {
            var local_game = await appDbContext.Games
                .Include(g => g.Categories)
                .Include(g => g.Owners)
                .FirstOrDefaultAsync(g => g.Id == id);

            if (local_game == null)
            {
                return NotFound("Game not found");
            }

            var local_userId = userManager.GetUserId(User);

            var local_result = new GameDto
            {
                Id = local_game.Id,
                Name = local_game.Name,
                Description = local_game.Description,
                Price = local_game.Price,
                Size = local_game.Size,
                Categories = local_game.Categories.Select(c => new CategoryDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description
                }).ToList(),
                IsOwned = local_userId != null && local_game.Owners.Any(o => o.Id == local_userId)
            };

            return Ok(local_result);
        }
    }
}
