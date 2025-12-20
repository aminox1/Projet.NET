namespace Gauniv.WebServer.ViewModels
{
    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int GamesCount { get; set; }
        public bool HasImage { get; set; }
    }
}

