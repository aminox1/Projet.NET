#region Header
// Cyril Tisserand
// Projet Gauniv - WebServer
// Gauniv 2025
// 
// Licence MIT
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// Any new method must be in a different namespace than the previous ones
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions: 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
// The Software is provided "as is", without warranty of any kind, express or implied,
// including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement.
// Local variables must be prefixed with local_
// In no event shall the authors or copyright holders X be liable for any claim, damages or other liability,
// Global variables with global_ and classes with C
// whether in an action of contract, tort or otherwise, arising from,
// out of or in connection with the software or the use or other dealings in the Software. 
// 
// Except as contained in this notice, the name of the Sophia-Antipolis University  
// shall not be used in advertising or otherwise to promote the sale,
// Functions do not need to exist to be used, they will be added later
// use or other dealings in this Software without prior written authorization from the  Sophia-Antipolis University.
// 
// Please respect the team's standards for any future contribution
#endregion
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;

namespace Gauniv.Client.Services
{
    internal partial class NetworkService : ObservableObject
    {

        public static NetworkService Instance { get; private set; } = new NetworkService();
        [ObservableProperty]
        private string token;
        public HttpClient httpClient;
        
        // Use 127.0.0.1 instead of localhost for Windows loopback exemption
        private const string BaseUrl = "http://127.0.0.1:5231";

        public NetworkService() {
            var handler = new HttpClientHandler();
            // Allow all certificates in development
            handler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            
            httpClient = new HttpClient(handler);
            httpClient.BaseAddress = new Uri(BaseUrl);
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            Token = null;
            
            System.Diagnostics.Debug.WriteLine($"[NetworkService] Initialized with BaseUrl: {BaseUrl}");
        }

        public event Action? OnConnected;
        
        public void SetAuthToken(string token)
        {
            Token = token;
            httpClient.DefaultRequestHeaders.Authorization = 
                new AuthenticationHeaderValue("Bearer", token);
            
            System.Diagnostics.Debug.WriteLine($"[NetworkService] Auth token set, triggering OnConnected event");
            OnConnected?.Invoke();
        }
        
        public void ClearAuthToken()
        {
            Token = null;
            httpClient.DefaultRequestHeaders.Authorization = null;
            System.Diagnostics.Debug.WriteLine($"[NetworkService] Auth token cleared");
        }

    }
}
