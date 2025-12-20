using Gauniv.WebServer.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Gauniv.WebServer.Services
{
    public class ImageService
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<ImageService> _logger;

        public ImageService(ApplicationDbContext db, ILogger<ImageService> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<(byte[] Data, string ContentType)?> GetImageAsync(int id)
        {
            var img = await _db.GameImages.FindAsync(id);
            if (img == null || img.Data == null || string.IsNullOrEmpty(img.ContentType)) return null;
            return (img.Data, img.ContentType);
        }

        public async Task<int> UploadImageAsync(int gameId, Stream fileStream, string contentType, bool setPrimary = false)
        {
            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms);
            var bytes = ms.ToArray();

            if (setPrimary)
            {
                var prev = await _db.GameImages.Where(i => i.GameId == gameId && i.IsPrimary).ToListAsync();
                prev.ForEach(p => p.IsPrimary = false);
            }

            var img = new GameImage
            {
                GameId = gameId,
                Data = bytes,
                ContentType = contentType,
                SortOrder = 0,
                IsPrimary = setPrimary
            };
            _db.GameImages.Add(img);
            await _db.SaveChangesAsync();
            return img.Id;
        }

        public async Task<bool> SetPrimaryAsync(int imageId)
        {
            var img = await _db.GameImages.FindAsync(imageId);
            if (img == null) return false;
            var gameId = img.GameId;
            var prev = await _db.GameImages.Where(i => i.GameId == gameId && i.IsPrimary).ToListAsync();
            prev.ForEach(p => p.IsPrimary = false);
            img.IsPrimary = true;
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            var img = await _db.GameImages.FindAsync(imageId);
            if (img == null) return false;
            _db.GameImages.Remove(img);
            await _db.SaveChangesAsync();
            return true;
        }
    }
}
