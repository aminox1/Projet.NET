using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Models;
using Gauniv.Client.Pages;
using Gauniv.Client.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.ViewModel
{
    public partial class MyGamesViewModel: ObservableObject
    {
        private readonly GameService gameService;
        private readonly LocalGameManager localGameManager;

        [ObservableProperty]
        private ObservableCollection<GameDto> myGames = new();

        [ObservableProperty]
        private bool isLoading;

        public MyGamesViewModel()
        {
            gameService = new GameService();
            localGameManager = LocalGameManager.Instance;
            _ = LoadMyGamesAsync();
        }

        [RelayCommand]
        private async Task LoadMyGamesAsync()
        {
            IsLoading = true;
            try
            {
                Debug.WriteLine("[MyGamesViewModel] Loading my games...");
                var loadedGames = await gameService.GetMyGamesAsync(0, 50);
                Debug.WriteLine($"[MyGamesViewModel] Loaded {loadedGames.Count} owned games");
                
                // Update local download status
                foreach (var game in loadedGames)
                {
                    game.IsDownloaded = localGameManager.IsGameDownloaded(game.Id);
                    game.IsRunning = localGameManager.IsGameRunning(game.Id);
                    Debug.WriteLine($"[MyGamesViewModel] Game: {game.Name}, Downloaded: {game.IsDownloaded}, Running: {game.IsRunning}");
                }
                
                MyGames.Clear();
                foreach (var game in loadedGames)
                {
                    MyGames.Add(game);
                }
                
                Debug.WriteLine($"[MyGamesViewModel] MyGames collection now has {MyGames.Count} items");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[MyGamesViewModel] Error loading my games: {ex.Message}");
                Debug.WriteLine($"[MyGamesViewModel] StackTrace: {ex.StackTrace}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load owned games: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GameSelectedAsync(GameDto game)
        {
            if (game != null)
            {
                await Shell.Current.GoToAsync($"gamedetails?gameId={game.Id}");
            }
        }

        [RelayCommand]
        private async Task DownloadGameAsync(GameDto game)
        {
            if (game == null || game.IsDownloaded) return;

            IsLoading = true;
            try
            {
                var localPath = localGameManager.GetDownloadPath(game.Id, game.Name);
                var progress = new Progress<double>(percent =>
                {
                    Debug.WriteLine($"Download progress: {percent:F1}%");
                });

                var success = await gameService.DownloadGameAsync(game.Id, localPath, progress);
                
                if (success)
                {
                    localGameManager.RegisterDownloadedGame(game.Id, game.Name, localPath, game.Size ?? 0);
                    game.IsDownloaded = true;
                    game.LocalPath = localPath;
                    await Application.Current.MainPage.DisplayAlert("Success", $"{game.Name} downloaded successfully!", "OK");
                    await LoadMyGamesAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to download game", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task LaunchGameAsync(GameDto game)
        {
            if (game == null || !game.IsDownloaded) return;

            try
            {
                var success = localGameManager.LaunchGame(game.Id);
                if (success)
                {
                    // Find the game in the collection and update its IsRunning property
                    var gameInCollection = MyGames.FirstOrDefault(g => g.Id == game.Id);
                    if (gameInCollection != null)
                    {
                        gameInCollection.IsRunning = true;
                    }
                    await Application.Current.MainPage.DisplayAlert("Game Launched", $"{game.Name} is now running", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to launch game", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task StopGameAsync(GameDto game)
        {
            if (game == null || !game.IsRunning) return;

            try
            {
                localGameManager.StopGame(game.Id);
                
                // Find the game in the collection and update its IsRunning property
                var gameInCollection = MyGames.FirstOrDefault(g => g.Id == game.Id);
                if (gameInCollection != null)
                {
                    gameInCollection.IsRunning = false;
                }
                await Application.Current.MainPage.DisplayAlert("Game Stopped", $"{game.Name} has been stopped", "OK");
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
            }
        }

        [RelayCommand]
        private async Task DeleteGameAsync(GameDto game)
        {
            if (game == null || !game.IsDownloaded) return;

            var confirm = await Application.Current.MainPage.DisplayAlert(
                "Confirm Delete", 
                $"Are you sure you want to delete {game.Name}?", 
                "Yes", 
                "No");

            if (confirm)
            {
                try
                {
                    localGameManager.DeleteGame(game.Id);
                    game.IsDownloaded = false;
                    game.LocalPath = string.Empty;
                    await LoadMyGamesAsync();
                    await Application.Current.MainPage.DisplayAlert("Success", $"{game.Name} has been deleted", "OK");
                }
                catch (Exception ex)
                {
                    await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
                }
            }
        }
    }
}
