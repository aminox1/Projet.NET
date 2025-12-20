using Gauniv.WebServer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Gauniv.WebServer.ViewModels;
using Gauniv.WebServer.Services;

namespace Gauniv.WebServer.Controllers
{
    public class CategoriesController : Controller
    {
        private readonly CategoryService _categoryService;
        public CategoriesController(CategoryService categoryService)
        {
            _categoryService = categoryService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 12, string? search = null, int[]? categories = null)
        {
            var vm = await _categoryService.GetPagedAsync(page, pageSize, search, categories);
            return View("~/Views/Categories/Index.cshtml", vm);
        }

        [HttpGet]
        public async Task<IActionResult> GetImage(int id)
        {
            var (data, contentType) = await _categoryService.GetImageAsync(id);
            if (data == null) return NotFound();
            return File(data, contentType ?? "application/octet-stream");
        }
    }
}
