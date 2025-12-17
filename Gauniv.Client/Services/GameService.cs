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
                var url = $"/api/1.0.0/Games/List?{query}";
                System.Diagnostics.Debug.WriteLine($"[GameService] Fetching games from: {httpClient.BaseAddress}{url}");
                
                var response = await httpClient.GetAsync(url);
                System.Diagnostics.Debug.WriteLine($"[GameService] Response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var games = await response.Content.ReadFromJsonAsync<List<GameDto>>() ?? new List<GameDto>();
                    System.Diagnostics.Debug.WriteLine($"[GameService] Received {games.Count} games");
                    return games;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[GameService] Error response: {errorContent}");
                return new List<GameDto>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GameService] Exception: {ex.GetType().Name} - {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[GameService] StackTrace: {ex.StackTrace}");
                Console.WriteLine($"Error fetching games: {ex.Message}");
                throw; // Re-throw to let ViewModel handle it
            }
        }

        public async Task<List<GameDto>> GetMyGamesAsync(int offset = 0, int limit = 20, int[]? categoryIds = null)
        {
            try
            {
                if (string.IsNullOrEmpty(networkService.Token))
                {
                    System.Diagnostics.Debug.WriteLine($"[GameService] GetMyGames - No auth token, user not logged in");
                    return new List<GameDto>();
                }

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
                var url = $"/api/1.0.0/Games/MyGames?{query}";
                System.Diagnostics.Debug.WriteLine($"[GameService] Fetching my games from: {httpClient.BaseAddress}{url}");
                
                var response = await httpClient.GetAsync(url);
                System.Diagnostics.Debug.WriteLine($"[GameService] MyGames response status: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    var games = await response.Content.ReadFromJsonAsync<List<GameDto>>() ?? new List<GameDto>();
                    System.Diagnostics.Debug.WriteLine($"[GameService] Received {games.Count} owned games");
                    return games;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[GameService] MyGames error: {errorContent}");
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
                if (string.IsNullOrEmpty(networkService.Token))
                {
                    System.Diagnostics.Debug.WriteLine($"[GameService] Cannot purchase - user not logged in");
                    throw new UnauthorizedAccessException("You must be logged in to purchase games");
                }

                var response = await httpClient.PostAsync($"/api/1.0.0/Games/Purchase/{gameId}", null);
                
                System.Diagnostics.Debug.WriteLine($"[GameService] Purchase response: {response.StatusCode}");
                
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[GameService] Purchase failed: {errorContent}");
                throw new Exception(errorContent);
            }
            catch (UnauthorizedAccessException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[GameService] Error purchasing game: {ex.Message}");
                throw new Exception($"Purchase failed: {ex.Message}");
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
                    var result = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (result?.AccessToken != null)
                    {
                        networkService.SetAuthToken(result.AccessToken);
                        System.Diagnostics.Debug.WriteLine($"[GameService] Login successful, token saved");
                        return true;
                    }
                }
                
                var errorContent = await response.Content.ReadAsStringAsync();
                System.Diagnostics.Debug.WriteLine($"[GameService] Login failed: {errorContent}");
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
