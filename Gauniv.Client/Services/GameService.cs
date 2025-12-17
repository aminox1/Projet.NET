using Gauniv.Client.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Gauniv.Client.Services
{
    public class GameService
    {
        private readonly NetworkService networkService;
        private readonly HttpClient httpClient;

        public GameService()
        {
            networkService = NetworkService.Instance;
            httpClient = networkService.httpClient;
        }

        public async Task<List<GameDto>> GetGamesAsync(int offset = 0, int limit = 20, int[]? categoryIds = null, string? name = null)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"offset={offset}",
                    $"limit={limit}"
                };

                if (categoryIds != null && categoryIds.Length > 0)
                {
                    foreach (var catId in categoryIds)
                    {
                        queryParams.Add($"category={catId}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(name))
                {
                    queryParams.Add($"name={Uri.EscapeDataString(name)}");
                }

                var query = string.Join("&", queryParams);
                var response = await httpClient.GetAsync($"/api/1.0.0/Games/List?{query}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<GameDto>>() ?? new List<GameDto>();
                }
                
                return new List<GameDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching games: {ex.Message}");
                return new List<GameDto>();
            }
        }

        public async Task<List<GameDto>> GetMyGamesAsync(int offset = 0, int limit = 20, int[]? categoryIds = null)
        {
            try
            {
                var queryParams = new List<string>
                {
                    $"offset={offset}",
                    $"limit={limit}"
                };

                if (categoryIds != null && categoryIds.Length > 0)
                {
                    foreach (var catId in categoryIds)
                    {
                        queryParams.Add($"category={catId}");
                    }
                }

                var query = string.Join("&", queryParams);
                var response = await httpClient.GetAsync($"/api/1.0.0/Games/MyGames?{query}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<GameDto>>() ?? new List<GameDto>();
                }
                
                return new List<GameDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching my games: {ex.Message}");
                return new List<GameDto>();
            }
        }

        public async Task<GameDto?> GetGameDetailsAsync(int gameId)
        {
            try
            {
                var response = await httpClient.GetAsync($"/api/1.0.0/Games/Details/{gameId}");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<GameDto>();
                }
                
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching game details: {ex.Message}");
                return null;
            }
        }

        public async Task<List<CategoryDto>> GetCategoriesAsync()
        {
            try
            {
                var response = await httpClient.GetAsync("/api/1.0.0/Games/Categories");
                
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<List<CategoryDto>>() ?? new List<CategoryDto>();
                }
                
                return new List<CategoryDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching categories: {ex.Message}");
                return new List<CategoryDto>();
            }
        }

        public async Task<bool> PurchaseGameAsync(int gameId)
        {
            try
            {
                var response = await httpClient.PostAsync($"/api/1.0.0/Games/Purchase/{gameId}", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error purchasing game: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> DownloadGameAsync(int gameId, string localPath, IProgress<double>? progress = null)
        {
            try
            {
                var response = await httpClient.GetAsync($"/api/1.0.0/Games/Download/{gameId}", HttpCompletionOption.ResponseHeadersRead);
                
                if (!response.IsSuccessStatusCode)
                    return false;

                var totalBytes = response.Content.Headers.ContentLength ?? 0;
                var downloadedBytes = 0L;

                using var contentStream = await response.Content.ReadAsStreamAsync();
                using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

                var buffer = new byte[8192];
                int bytesRead;

                while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await fileStream.WriteAsync(buffer, 0, bytesRead);
                    downloadedBytes += bytesRead;

                    if (totalBytes > 0 && progress != null)
                    {
                        var progressPercentage = (double)downloadedBytes / totalBytes * 100;
                        progress.Report(progressPercentage);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading game: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            try
            {
                var loginRequest = new LoginRequest
                {
                    Email = email,
                    Password = password
                };

                var response = await httpClient.PostAsJsonAsync("/Bearer/login", loginRequest);
                
                if (response.IsSuccessStatusCode)
                {
                    // Get the token from response headers or cookies
                    // This depends on your authentication setup
                    return true;
                }
                
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during login: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> RegisterAsync(string email, string password)
        {
            try
            {
                var registerRequest = new RegisterRequest
                {
                    Email = email,
                    Password = password
                };

                var response = await httpClient.PostAsJsonAsync("/register", registerRequest);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during registration: {ex.Message}");
                return false;
            }
        }
    }
}
