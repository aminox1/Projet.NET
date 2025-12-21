using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gauniv.WebServer.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Gauniv.WebServer.Hubs;

namespace Gauniv.WebServer.Services
{
    public class PlayerPresenceService
    {
        private class UserState
        {
            public string DisplayName = string.Empty;
            public HashSet<string> ConnectionIds = new HashSet<string>();
            public string? Status;
            public DateTime LastSeenUtc = DateTime.UtcNow;
        }

        private readonly ConcurrentDictionary<string, UserState> _users = new();
        private readonly IHubContext<PlayersHub> _hub;

        public PlayerPresenceService(IHubContext<PlayersHub> hub)
        {
            _hub = hub;
        }

        public async Task OnConnectedAsync(string userId, string connectionId, string displayName)
        {
            var state = _users.GetOrAdd(userId, _ => new UserState());
            lock (state)
            {
                state.DisplayName = displayName ?? state.DisplayName;
                state.ConnectionIds.Add(connectionId);
                state.LastSeenUtc = DateTime.UtcNow;
            }
            await BroadcastAsync();
        }

        public async Task OnDisconnectedAsync(string connectionId)
        {
            var pair = _users.FirstOrDefault(kv => kv.Value.ConnectionIds.Contains(connectionId));
            if (!pair.Equals(default(KeyValuePair<string, UserState>)))
            {
                var userId = pair.Key;
                var state = pair.Value;
                lock (state)
                {
                    state.ConnectionIds.Remove(connectionId);
                    if (!state.ConnectionIds.Any())
                    {
                        state.LastSeenUtc = DateTime.UtcNow;
                    }
                }
                await BroadcastAsync();
            }
        }

        public async Task SetStatusAsync(string userId, string? status)
        {
            var state = _users.GetOrAdd(userId, _ => new UserState());
            lock (state)
            {
                state.Status = status;
                state.LastSeenUtc = DateTime.UtcNow;
            }
            await BroadcastAsync();
        }

        public List<PlayerStatusViewModel> GetAll()
        {
            return _users.Select(kv =>
            {
                var s = kv.Value;
                return new PlayerStatusViewModel
                {
                    UserId = kv.Key,
                    DisplayName = s.DisplayName,
                    IsOnline = s.ConnectionIds.Count > 0,
                    Status = s.Status,
                    LastSeenUtc = s.LastSeenUtc
                };
            }).OrderByDescending(x => x.IsOnline).ThenBy(x => x.DisplayName).ToList();
        }

        public (List<PlayerStatusViewModel> Items, int Total) GetPaged(int page, int pageSize, string? search)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var all = _users.Select(kv =>
            {
                var s = kv.Value;
                return new PlayerStatusViewModel
                {
                    UserId = kv.Key,
                    DisplayName = s.DisplayName,
                    IsOnline = s.ConnectionIds.Count > 0,
                    Status = s.Status,
                    LastSeenUtc = s.LastSeenUtc
                };
            });

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLowerInvariant();
                all = all.Where(x => (x.DisplayName ?? string.Empty).ToLowerInvariant().Contains(q));
            }

            var ordered = all.OrderByDescending(x => x.IsOnline).ThenBy(x => x.DisplayName);
            var total = ordered.Count();
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (items, total);
        }

        public (List<PlayerStatusViewModel> Items, int Total) GetPagedByUserIds(IEnumerable<string> userIds, int page, int pageSize, string? search)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0) pageSize = 10;

            var idSet = new HashSet<string>(userIds ?? Enumerable.Empty<string>());

            var all = _users.Where(kv => idSet.Contains(kv.Key)).Select(kv =>
            {
                var s = kv.Value;
                return new PlayerStatusViewModel
                {
                    UserId = kv.Key,
                    DisplayName = s.DisplayName,
                    IsOnline = s.ConnectionIds.Count > 0,
                    Status = s.Status,
                    LastSeenUtc = s.LastSeenUtc
                };
            });

            if (!string.IsNullOrWhiteSpace(search))
            {
                var q = search.Trim().ToLowerInvariant();
                all = all.Where(x => (x.DisplayName ?? string.Empty).ToLowerInvariant().Contains(q));
            }

            var ordered = all.OrderByDescending(x => x.IsOnline).ThenBy(x => x.DisplayName);
            var total = ordered.Count();
            var items = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return (items, total);
        }

        private Task BroadcastAsync()
        {
            var list = GetAll();
            return _hub.Clients.All.SendAsync("PlayersUpdated", list);
        }
    }
}
