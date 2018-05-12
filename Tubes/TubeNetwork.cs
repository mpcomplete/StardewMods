﻿using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Common;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace Tubes
{
    internal class TubeNetwork
    {
        private PortObject[] ports;

        internal static IEnumerable<TubeNetwork> getAllNetworksIn(GameLocation location)
        {
            var visited = new HashSet<Vector2>();
            foreach (Vector2 tile in location.GetTiles()) {
                if (visited.Contains(tile))
                    continue;
                if (TubeNetwork.getNetworkAtTile(location, tile, visited) is TubeNetwork network)
                    yield return network;
            }
        }

        internal static TubeNetwork getNetworkAtTile(GameLocation location, Vector2 startTile, ISet<Vector2> visited)
        {
            var ports = new List<PortObject>();
            var toVisit = new Queue<Vector2>();
            toVisit.Enqueue(startTile);

            while (toVisit.Count > 0) {
                Vector2 tile = toVisit.Dequeue();
                if (visited.Contains(tile))
                    continue;
                visited.Add(tile);

                if (location.objects.TryGetValue(tile, out SObject o) && o is PortObject port) {
                    ports.Add(port);
                    port.updateAttachedChest(location);
                } else if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature t) && t is TubeTerrain) {
                    // Keep searching.
                } else if (TileHelper.TryGetBuildingEntrance(location, tile) is GameLocation indoors) {
                    var visitedIndoors = new HashSet<Vector2>();
                    foreach (Vector2 indoorTile in TileHelper.GetTilesNearWarps(indoors)) {
                        if (TubeNetwork.getNetworkAtTile(indoors, new Vector2(indoorTile.X, indoorTile.Y), visitedIndoors) is TubeNetwork network) {
                            ports.AddRange(network.ports);
                            // We stop at the first one because tiles may overlap, and we don't want to double-add networks. It's not strictly correct,
                            // but it should work as long as buildings don't have multiple entrances.
                            break;
                        }
                    }
                } else {
                    continue;
                }

                foreach (Vector2 adjacent in Utility.getAdjacentTileLocations(tile))
                    toVisit.Enqueue(adjacent);
            }

            if (ports.Count > 0)
                return new TubeNetwork() { ports = ports.ToArray() };
            return null;
        }

        internal void process()
        {
            foreach (PortObject port in ports) {
                foreach (PortFilter request in port.requests)
                    this.processRequest(port, request);
            }
        }

        internal void processRequest(PortObject requestor, PortFilter request)
        {
            if (requestor.attachedChest == null)
                return;

            int amountHave = requestor.amountMatching(request);
            int amountNeeded = request.requestAmount - amountHave;
            if (amountNeeded <= 0)
                return;

            foreach (PortObject provider in ports) {
                if (provider == requestor)
                    continue;
                requestor.requestFrom(provider, request, ref amountNeeded);
                if (amountNeeded <= 0)
                    break;
            }
        }
    }
}
