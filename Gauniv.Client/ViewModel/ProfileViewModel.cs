using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.ViewModel
{
    public partial class ProfileViewModel : ObservableObject
    {
        private readonly GameService gameService;
        private readonly NetworkService networkService;

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool isLoggedIn;

        [ObservableProperty]
        private bool isLoading;

        [ObservableProperty]
        private string statusMessage = string.Empty;

        public ProfileViewModel()
        {
            gameService = new GameService();
            networkService = NetworkService.Instance;
            
            // Check if already logged in
            IsLoggedIn = !string.IsNullOrEmpty(networkService.Token);
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                StatusMessage = "Please enter both email and password";
                return;
            }

            IsLoading = true;
            StatusMessage = string.Empty;

            try
            {
                Debug.WriteLine($"[ProfileViewModel] Attempting login for {Email}");
                var success = await gameService.LoginAsync(Email, Password);

                if (success)
                {
                    IsLoggedIn = true;
                    StatusMessage = $"Logged in as {Email}";
                    Password = string.Empty; // Clear password
                    Debug.WriteLine($"[ProfileViewModel] Login successful");
                }
                else
                {
                    StatusMessage = "Login failed. Please check your credentials.";
                    Debug.WriteLine($"[ProfileViewModel] Login failed");
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                StatusMessage = " Admin accounts cannot access this application.\nPlease use the web interface to manage games.";
                Password = string.Empty;
                Debug.WriteLine($"[ProfileViewModel] Admin login blocked: {ex.Message}");
                await Application.Current.MainPage.DisplayAlert("Access Denied", 
                    "Admin accounts are restricted from using the client application.", 
                    "OK");
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                Debug.WriteLine($"[ProfileViewModel] Login error: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void Logout()
        {
            networkService.ClearAuthToken();
            IsLoggedIn = false;
            Email = string.Empty;
            Password = string.Empty;
            StatusMessage = "Logged out successfully";
            Debug.WriteLine($"[ProfileViewModel] User logged out");
        }
    }
}
