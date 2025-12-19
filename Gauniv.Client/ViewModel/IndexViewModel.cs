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
        private ObservableCollection<CategoryDto> categories = new();
        
        [ObservableProperty]
        private List<GameDto> allGames = new();

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string searchText = string.Empty;
        
        [ObservableProperty]
        private decimal minPrice;
        
        [ObservableProperty]
        private decimal maxPrice = 1000;
        
        [ObservableProperty]
        private CategoryDto? selectedCategory;
        
        [ObservableProperty]
        private bool showOwnedOnly;
        
        [ObservableProperty]
        private bool showNotOwnedOnly;

        public IndexViewModel()
        {
            gameService = new GameService();
            localGameManager = LocalGameManager.Instance;
            _ = LoadCategoriesAsync();
            _ = LoadGamesAsync();
        }

        [RelayCommand]
        private async Task LoadGamesAsync()
        {
            IsLoading = true;
            try
            {
                Debug.WriteLine("Starting to load games...");
                var loadedGames = await gameService.GetGamesAsync(0, 200, null, null);
                Debug.WriteLine($"Loaded {loadedGames.Count} games from API");
                
                // Update local download status
                foreach (var game in loadedGames)
                {
                    game.IsDownloaded = localGameManager.IsGameDownloaded(game.Id);
                    game.IsRunning = localGameManager.IsGameRunning(game.Id);
                }
                
                AllGames = loadedGames;
                ApplyFilters();
                
                Debug.WriteLine($"Games collection now has {Games.Count} items after filters");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading games: {ex.Message}");
                Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                await Application.Current.MainPage.DisplayAlert("Error", $"Failed to load games: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }
        
        [RelayCommand]
        private async Task LoadCategoriesAsync()
        {
            try
            {
                Debug.WriteLine("Loading categories...");
                var loadedCategories = await gameService.GetCategoriesAsync();
                Categories.Clear();
                Categories.Add(new CategoryDto { Id = 0, Name = "All Categories" });
                foreach (var category in loadedCategories)
                {
                    Categories.Add(category);
                }
                SelectedCategory = Categories.FirstOrDefault();
                Debug.WriteLine($"Loaded {Categories.Count} categories");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading categories: {ex.Message}");
            }
        }
        
        private void ApplyFilters()
        {
            var filtered = AllGames.AsEnumerable();
            
            // Filter by search text
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(g => 
                    g.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    g.Description.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }
            
            // Filter by category
            if (SelectedCategory != null && SelectedCategory.Id != 0)
            {
                filtered = filtered.Where(g => g.Categories.Any(c => c.Id == SelectedCategory.Id));
            }
            
            // Filter by price range
            filtered = filtered.Where(g => g.Price >= MinPrice && g.Price <= MaxPrice);
            
            // Filter by ownership
            if (ShowOwnedOnly)
            {
                filtered = filtered.Where(g => g.IsOwned);
            }
            if (ShowNotOwnedOnly)
            {
                filtered = filtered.Where(g => !g.IsOwned);
            }
            
            // Sort by name (alphabetically)
            filtered = filtered.OrderBy(g => g.Name);
            
            Games.Clear();
            foreach (var game in filtered)
            {
                Games.Add(game);
            }
        }

        [RelayCommand]
        private void SearchGames()
        {
            ApplyFilters();
        }
        
        [RelayCommand]
        private void ClearFilters()
        {
            SearchText = string.Empty;
            MinPrice = 0;
            MaxPrice = 1000;
            SelectedCategory = Categories.FirstOrDefault();
            ShowOwnedOnly = false;
            ShowNotOwnedOnly = false;
            ApplyFilters();
        }
        
        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }
        
        partial void OnSelectedCategoryChanged(CategoryDto? value)
        {
            ApplyFilters();
        }
        
        partial void OnShowOwnedOnlyChanged(bool value)
        {
            if (value) ShowNotOwnedOnly = false;
            ApplyFilters();
        }
        
        partial void OnShowNotOwnedOnlyChanged(bool value)
        {
            if (value) ShowOwnedOnly = false;
            ApplyFilters();
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
                    var goToMyGames = await Application.Current.MainPage.DisplayAlert(
                        "Success", 
                        $"{game.Name} purchased successfully!\n\nGo to 'My Games' to download and play it.", 
                        "Go to My Games", 
                        "Stay Here");
                    
                    if (goToMyGames)
                    {
                        // Navigate to My Games tab
                        Shell.Current.FlyoutIsPresented = false;
                        await Shell.Current.GoToAsync("///mygames");
                    }
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
