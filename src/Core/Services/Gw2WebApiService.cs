using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Extended;
using Flurl.Http;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Nekres.ChatMacros.Core.Services {
    internal class Gw2WebApiService : IDisposable {
        //private const int UPDATE_INTERVAL_MIN = 5; // Should reflect GW2 API update interval.

        //private DateTime _lastUpdate = DateTime.UtcNow.AddMinutes(-UPDATE_INTERVAL_MIN);

        private readonly IReadOnlyList<TokenPermission> _requires = new List<TokenPermission> {
            TokenPermission.Account,
            TokenPermission.Progression,
            TokenPermission.Inventories,
            TokenPermission.Characters,
            TokenPermission.Builds
        };

        public bool HasSubtoken;

        public IReadOnlyList<TokenPermission> MissingPermissions;

        private Regex _apiKeyPattern = new(@"^[A-F0-9]{8}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{20}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{4}-[A-F0-9]{12}$");

        private string _baseApiUrl = "https://api.guildwars2.com/";

        public Gw2WebApiService() {
            MissingPermissions = new List<TokenPermission>();

            ChatMacros.Instance.Gw2ApiManager.SubtokenUpdated += OnSubtokenUpdated;
        }

        public void Dispose() {
            ChatMacros.Instance.Gw2ApiManager.SubtokenUpdated -= OnSubtokenUpdated;
        }

        public bool IsApiDown(out string message) {

            try {

                using var response = _baseApiUrl.AllowHttpStatus(HttpStatusCode.ServiceUnavailable)
                                                .AllowHttpStatus(HttpStatusCode.InternalServerError)
                                                .GetAsync(default, HttpCompletionOption.ResponseHeadersRead).Result;

                // API is broken.
                if (response.StatusCode == HttpStatusCode.InternalServerError) {
                    message = "GW2 API is down. Please, try again later.";
                    return true;
                }

                // API is down for maintenance. Chances are high body contains a message.
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable) {
                    var body = response.Content.ReadAsStringAsync().Result;
                    message = body.GetTextBetweenTags("p");
                    return true;
                }

            } catch (Exception e) {
                ChatMacros.Logger.Warn(e, "Failed to check API status.");
            }

            // If we don't immediately get a 503 then the API is probably available.
            message = string.Empty;
            return false;
        }

        public bool IsApiAvailable() {
            if (IsApiDown(out var message)) {
                ScreenNotification.ShowNotification(message, ScreenNotification.NotificationType.Error);
                return false;
            }
            return true;
        }

        public bool IsAuthorizedApiAvailable() {
            if (!IsApiAvailable()) {
                return false;
            }

            if (string.IsNullOrWhiteSpace(GameService.Gw2Mumble.PlayerCharacter.Name)) {
                ScreenNotification.ShowNotification("API unavailable. Please, login to a character.", ScreenNotification.NotificationType.Error);
                return false;
            }

            if (!HasSubtoken) {
                ScreenNotification.ShowNotification("Missing API key. Please, add an API key to BlishHUD.", ScreenNotification.NotificationType.Error);
                return false;
            }

            if (MissingPermissions.Any()) {
                var missing = string.Join(", ", MissingPermissions);
                ScreenNotification.ShowNotification($"Insufficient API permissions.\nRequired: {missing}", ScreenNotification.NotificationType.Error);
                return false;
            }
            return true;
        }

        public async Task<List<string>> GetClears() {
            var clears = await TaskUtil.TryAsync(() => ChatMacros.Instance.Gw2ApiManager.Gw2ApiClient.V2.Account.Raids.GetAsync());
            return (clears ?? Enumerable.Empty<string>()).ToList();
        }

        public async Task<List<AccountItem>> GetBank() {
            var bank = await TaskUtil.TryAsync(() => ChatMacros.Instance.Gw2ApiManager.Gw2ApiClient.V2.Account.Bank.GetAsync());
            return bank.ToList();
        }

        public async Task<List<AccountItem>> GetSharedBags() {
            var sharedBags = await TaskUtil.TryAsync(() => ChatMacros.Instance.Gw2ApiManager.Gw2ApiClient.V2.Account.Inventory.GetAsync());
            return sharedBags.ToList();
        }

        public async Task<Dictionary<Character, List<AccountItem>>> GetBagsByCharacter() {
            var characters = await GetCharacters();

            var bagsByCharacter = new Dictionary<Character, List<AccountItem>>();

            foreach (var character in characters) {

                if (character.Bags == null) {
                    continue;
                }

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                var bags = character.Bags.Where(bag => bag != null); // bag slots can be empty.

                var filtered = bags.SelectMany(bag => bag.Inventory).ToList();

                if (!filtered.Any()) {
                    continue;
                }

                bagsByCharacter.Add(character, filtered);
            }

            return bagsByCharacter;
        }

        public bool HasCorrectFormat(string apiKey) {
            return !string.IsNullOrWhiteSpace(apiKey) && _apiKeyPattern.IsMatch(apiKey);
        }

        public async Task<IReadOnlyList<Item>> GetItems(params int[] itemIds) {
            var response = await TaskUtil.TryAsync(() => GameService.Gw2WebApi.AnonymousConnection.Client.V2.Items.ManyAsync(itemIds));
            return response ?? Enumerable.Empty<Item>().ToList();
        }

        public async Task<IReadOnlyList<Map>> GetMaps(params int[] mapIds) {
            var response = await TaskUtil.TryAsync(() => ChatMacros.Instance.Gw2ApiManager.Gw2ApiClient.V2.Maps.ManyAsync(mapIds));
            return response ?? Enumerable.Empty<Map>().ToList();
        }

        public async Task<IReadOnlyList<Map>> GetMaps() {
            var response = await TaskUtil.TryAsync(() => ChatMacros.Instance.Gw2ApiManager.Gw2ApiClient.V2.Maps.AllAsync());
            return response?.ToList() ?? Enumerable.Empty<Map>().ToList();
        }

        public async Task<IReadOnlyList<ContinentFloorRegionMap>> GetRegionMap(Map map) {
            var regionMaps = new List<ContinentFloorRegionMap>();
            foreach (int floor in map.Floors) {
                var regionMap = await TaskUtil.TryAsync(() => ChatMacros.Instance.Gw2ApiManager.Gw2ApiClient.V2
                                                                          .Continents[map.ContinentId]
                                                                          .Floors[floor]
                                                                          .Regions[map.RegionId]
                                                                          .Maps[map.Id].GetAsync());
                regionMaps.Add(regionMap);
            }
            return regionMaps;
        }

        public async Task<List<ContinentFloorRegionMapSector>> GetMapSectors(Map map) {
            var result = new List<ContinentFloorRegionMapSector>();
            foreach (var floor in map.Floors) {
                var sectors = await TaskUtil.RetryAsync(() => ChatMacros.Instance.Gw2ApiManager.Gw2ApiClient.V2.Continents[map.ContinentId].Floors[floor].Regions[map.RegionId].Maps[map.Id].Sectors.AllAsync());
                if (sectors != null && sectors.Any()) {
                    result.AddRange(sectors.DistinctBy(sector => sector.Id));
                }
            }
            return result;
        }

        private async Task<IEnumerable<Character>> GetCharacters() {
            var characters = await TaskUtil.TryAsync(() => ChatMacros.Instance.Gw2ApiManager.Gw2ApiClient.V2.Characters.AllAsync());
            return characters ?? Enumerable.Empty<Character>();
        }

        private void OnSubtokenUpdated(object sender, ValueEventArgs<IEnumerable<TokenPermission>> e) {
            HasSubtoken = true;
            MissingPermissions = _requires.Except(e.Value).ToList();
        }
    }
}
