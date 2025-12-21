using Gauniv.WebServer.Services;
using Gauniv.WebServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Threading.Tasks;

namespace Gauniv.WebServer.Controllers
{
    [Authorize(Roles = "User,Admin")]
    public class PlayersController : Controller
    {
        private readonly PlayerPresenceService _presence;
        private readonly UserManager<User> _userManager;

        public PlayersController(PlayerPresenceService presence, UserManager<User> userManager)
        {
            _presence = presence;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetMyRoles()
        {
            var userId = User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Json(new { authenticated = false });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return Json(new { authenticated = false });

            var roles = await _userManager.GetRolesAsync(user);
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Json(new { authenticated = true, userId = userId, roles = roles, claims = claims });
        }

        [HttpGet]
        public async Task<IActionResult> GetPlayers(int page = 1, int pageSize = 10, string? search = null)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var usersInRole = await _userManager.GetUsersInRoleAsync("User");
            var userIds = usersInRole.Select(u => u.Id).ToList();

            var (items, total) = _presence.GetPagedByUserIds(userIds, page, pageSize, search);
            return Json(new { items, total });
        }
    }
}
