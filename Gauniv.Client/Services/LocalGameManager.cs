using Gauniv.Client.Models;
using System.Diagnostics;
using System.Text.Json;
using System.IO.Compression;

namespace Gauniv.Client.Services
{
    public class LocalGameManager
    {
        private static LocalGameManager? instance;
        public static LocalGameManager Instance => instance ??= new LocalGameManager();

        private readonly string gamesDirectory;
        private readonly string metadataFile;
        private Dictionary<int, GameMetadata> gameMetadata;

        public class GameMetadata
        {
            public int GameId { get; set; }
            public string GameName { get; set; } = string.Empty;
            public string LocalPath { get; set; } = string.Empty;
            public DateTime DownloadedAt { get; set; }
            public long Size { get; set; }
            public bool IsRunning { get; set; }
            public Process? GameProcess { get; set; }
        }

        private LocalGameManager()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            gamesDirectory = Path.Combine(appDataPath, "Gauniv", "Games");
            metadataFile = Path.Combine(appDataPath, "Gauniv", "metadata.json");
            
            Directory.CreateDirectory(gamesDirectory);
            LoadMetadata();
        }

        private void LoadMetadata()
        {
            if (File.Exists(metadataFile))
            {
                try
                {
                    var json = File.ReadAllText(metadataFile);
                    gameMetadata = JsonSerializer.Deserialize<Dictionary<int, GameMetadata>>(json) 
                                   ?? new Dictionary<int, GameMetadata>();
                }
                catch
                {
                    gameMetadata = new Dictionary<int, GameMetadata>();
                }
            }
            else
            {
                gameMetadata = new Dictionary<int, GameMetadata>();
            }
        }

        private void SaveMetadata()
        {
            try
            {
                var json = JsonSerializer.Serialize(gameMetadata, new JsonSerializerOptions { WriteIndented = true });
                Directory.CreateDirectory(Path.GetDirectoryName(metadataFile)!);
                File.WriteAllText(metadataFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving metadata: {ex.Message}");
            }
        }

        public bool IsGameDownloaded(int gameId)
        {
            if (!gameMetadata.ContainsKey(gameId))
                return false;

            var metadata = gameMetadata[gameId];
            return File.Exists(metadata.LocalPath);
        }

        public string GetGamePath(int gameId)
        {
            if (gameMetadata.ContainsKey(gameId))
            {
                return gameMetadata[gameId].LocalPath;
            }
            return string.Empty;
        }

        public string GetDownloadPath(int gameId, string gameName)
        {
            var safeGameName = string.Join("_", gameName.Split(Path.GetInvalidFileNameChars()));
            var gameFolder = Path.Combine(gamesDirectory, $"{gameId}_{safeGameName}");
            Directory.CreateDirectory(gameFolder);
            return Path.Combine(gameFolder, $"{safeGameName}.zip");
        }

        public void RegisterDownloadedGame(int gameId, string gameName, string localPath, long size)
        {
            gameMetadata[gameId] = new GameMetadata
            {
                GameId = gameId,
                GameName = gameName,
                LocalPath = localPath,
                DownloadedAt = DateTime.Now,
                Size = size,
                IsRunning = false
            };
            SaveMetadata();
        }

        public void DeleteGame(int gameId)
        {
            if (gameMetadata.ContainsKey(gameId))
            {
                var metadata = gameMetadata[gameId];
                
                // Stop the game if running
                if (metadata.IsRunning && metadata.GameProcess != null && !metadata.GameProcess.HasExited)
                {
                    metadata.GameProcess.Kill();
                }

                // Delete the game folder
                var gameFolder = Path.GetDirectoryName(metadata.LocalPath);
                if (Directory.Exists(gameFolder))
                {
                    try
                    {
                        Directory.Delete(gameFolder, true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting game folder: {ex.Message}");
                    }
                }

                gameMetadata.Remove(gameId);
                SaveMetadata();
            }
        }

        public bool LaunchGame(int gameId)
        {
            if (!gameMetadata.ContainsKey(gameId))
                return false;

            var metadata = gameMetadata[gameId];

            // Check if already running
            if (metadata.IsRunning && metadata.GameProcess != null && !metadata.GameProcess.HasExited)
            {
                return false;
            }

            try
            {
                // Extract ZIP file and run the game executable
                var zipPath = metadata.LocalPath;
                if (!File.Exists(zipPath))
                {
                    Debug.WriteLine($"[LocalGameManager] Game ZIP not found: {zipPath}");
                    return false;
                }

                // Extract to a subfolder
                var extractPath = Path.Combine(Path.GetDirectoryName(zipPath)!, "Extracted");
                
                // Extract if not already extracted
                if (!Directory.Exists(extractPath))
                {
                    Debug.WriteLine($"[LocalGameManager] Extracting game to: {extractPath}");
                    System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, extractPath);
                }

                // Find the .bat file (game executable)
                var batFiles = Directory.GetFiles(extractPath, "*.bat");
                if (batFiles.Length == 0)
                {
                    Debug.WriteLine($"[LocalGameManager] No .bat file found in extracted game");
                    // Try to open the extracted folder instead
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = extractPath,
                        UseShellExecute = true
                    });
                    return true;
                }

                var gameExe = batFiles[0];
                Debug.WriteLine($"[LocalGameManager] Launching game executable: {gameExe}");

                var processInfo = new ProcessStartInfo
                {
                    FileName = gameExe,
                    WorkingDirectory = extractPath,
                    UseShellExecute = true
                };

                metadata.GameProcess = Process.Start(processInfo);
                metadata.IsRunning = true;

                if (metadata.GameProcess != null)
                {
                    metadata.GameProcess.EnableRaisingEvents = true;
                    metadata.GameProcess.Exited += (sender, e) =>
                    {
                        metadata.IsRunning = false;
                        metadata.GameProcess = null;
                        Debug.WriteLine($"[LocalGameManager] Game {gameId} process exited");
                    };
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[LocalGameManager] Error launching game: {ex.Message}");
                Debug.WriteLine($"[LocalGameManager] StackTrace: {ex.StackTrace}");
                metadata.IsRunning = false;
                return false;
            }
        }

        public void StopGame(int gameId)
        {
            if (gameMetadata.ContainsKey(gameId))
            {
                var metadata = gameMetadata[gameId];
                if (metadata.IsRunning && metadata.GameProcess != null && !metadata.GameProcess.HasExited)
                {
                    try
                    {
                        metadata.GameProcess.Kill();
                        metadata.IsRunning = false;
                        metadata.GameProcess = null;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error stopping game: {ex.Message}");
                    }
                }
            }
        }

        public bool IsGameRunning(int gameId)
        {
            if (gameMetadata.ContainsKey(gameId))
            {
                var metadata = gameMetadata[gameId];
                return metadata.IsRunning && metadata.GameProcess != null && !metadata.GameProcess.HasExited;
            }
            return false;
        }

        public Dictionary<int, GameMetadata> GetAllDownloadedGames()
        {
            return new Dictionary<int, GameMetadata>(gameMetadata);
        }
    }
}
