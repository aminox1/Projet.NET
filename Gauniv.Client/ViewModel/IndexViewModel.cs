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
    public partial class IndexViewModel: ObservableObject
    {
        private readonly GameService gameService;
        private readonly LocalGameManager localGameManager;

        [ObservableProperty]
        private ObservableCollection<GameDto> games = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string searchText = string.Empty;

        public IndexViewModel()
        {
            gameService = new GameService();
            localGameManager = LocalGameManager.Instance;
            _ = LoadGamesAsync();
        }

        [RelayCommand]
        private async Task LoadGamesAsync()
        {
            IsLoading = true;
            try
            {
                var loadedGames = await gameService.GetGamesAsync(0, 50, null, SearchText);
                
                // Update local download status
                foreach (var game in loadedGames)
                {
                    game.IsDownloaded = localGameManager.IsGameDownloaded(game.Id);
                    game.IsRunning = localGameManager.IsGameRunning(game.Id);
                }
                
                Games.Clear();
                foreach (var game in loadedGames)
                {
                    Games.Add(game);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading games: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private async Task SearchGamesAsync()
        {
            await LoadGamesAsync();
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
        private async Task PurchaseGameAsync(GameDto game)
        {
            if (game == null) return;

            IsLoading = true;
            try
            {
                var success = await gameService.PurchaseGameAsync(game.Id);
                if (success)
                {
                    game.IsOwned = true;
                    await Application.Current.MainPage.DisplayAlert("Success", $"{game.Name} purchased successfully!", "OK");
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "Failed to purchase game", "OK");
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
    }
}
