using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Models;
using Gauniv.Client.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.ViewModel
{
    [QueryProperty(nameof(GameId), "gameId")]
    public partial class GameDetailsViewModel : ObservableObject
    {
        private readonly GameService gameService;
        private readonly LocalGameManager localGameManager;

        [ObservableProperty]
        private GameDto? game;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private int gameId;

        public GameDetailsViewModel()
        {
            gameService = new GameService();
            localGameManager = LocalGameManager.Instance;
        }

        partial void OnGameIdChanged(int value)
        {
            if (value > 0)
            {
                _ = LoadGameDetailsAsync(value);
            }
        }

        private async Task LoadGameDetailsAsync(int id)
        {
            IsLoading = true;
            try
            {
                Debug.WriteLine($"[GameDetailsViewModel] Loading details for game {id}");
                Game = await gameService.GetGameDetailsAsync(id);
                
                if (Game != null)
                {
                    Game.IsDownloaded = localGameManager.IsGameDownloaded(Game.Id);
                    Game.IsRunning = localGameManager.IsGameRunning(Game.Id);
                    Debug.WriteLine($"[GameDetailsViewModel] Loaded: {Game.Name}");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[GameDetailsViewModel] Error: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load game details: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task PurchaseGameAsync()
        {
            if (Game == null) return;

            IsLoading = true;
            try
            {
                var success = await gameService.PurchaseGameAsync(Game.Id);
                if (success)
                {
                    await Application.Current.MainPage.DisplayAlert("Success", $"You purchased {Game.Name}!", "OK");
                    Game.IsOwned = true;
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to purchase game", "OK");
                }
            }
            catch (Exception ex)
            {
                await Application.Current.MainPage.DisplayAlert("Error", $"Purchase failed: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task GoBackAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
