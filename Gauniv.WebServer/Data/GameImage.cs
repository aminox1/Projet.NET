using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Gauniv.WebServer.Data
{
    public class GameImage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        // Foreign key to Game
        public int GameId { get; set; }
        public Game Game { get; set; } = null!;

        // Optional: binary data (blob)
        public byte[]? Data { get; set; }

        // Content type for Data (e.g. "image/png")
        [MaxLength(100)]
        public string? ContentType { get; set; }

        // Optional: order / priority (0 = primary)
        public int SortOrder { get; set; } = 0;

        // Optional: mark primary image
        public bool IsPrimary { get; set; } = false;
    }
}
