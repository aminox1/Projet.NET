using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Gauniv.Client.Pages;
using Gauniv.Client.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.ViewModel
{
    public partial class MenuViewModel : ObservableObject
    {
        private readonly NetworkService networkService;

        [RelayCommand]
        public void GoToProfile() => NavigationService.Instance.Navigate<Profile>([]);

        [ObservableProperty]
        private bool isConnected;

        public MenuViewModel()
        {
            networkService = NetworkService.Instance;
            
            // Check initial state
            IsConnected = !string.IsNullOrEmpty(networkService.Token);
            
            // Subscribe to connection changes
            networkService.OnConnected += Instance_OnConnected;
            
            // Check token status periodically
            _ = CheckConnectionStatusAsync();
        }

        private void Instance_OnConnected()
        {
            IsConnected = true;
        }

        private async Task CheckConnectionStatusAsync()
        {
            while (true)
            {
                await Task.Delay(1000); // Check every second
                var wasConnected = IsConnected;
                var nowConnected = !string.IsNullOrEmpty(networkService.Token);
                
                if (wasConnected != nowConnected)
                {
                    IsConnected = nowConnected;
                    System.Diagnostics.Debug.WriteLine($"[MenuViewModel] Connection status changed: {IsConnected}");
                }
            }
        }
    }
}
