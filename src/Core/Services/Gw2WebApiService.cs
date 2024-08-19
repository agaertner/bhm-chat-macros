using Blish_HUD.Extended;
using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nekres.ChatMacros.Core.Services {
    internal class Gw2WebApiService : IDisposable {
        public Gw2WebApiService() {
            /* NOOP */
        }

        public void Dispose() {
            /* NOOP */
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
    }
}
