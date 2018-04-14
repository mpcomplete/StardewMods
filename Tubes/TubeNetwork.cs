using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.TerrainFeatures;
using System.Collections.Generic;
using SObject = StardewValley.Object;

namespace Tubes
{
    internal class TubeNetwork
    {
        private PortObject[] ports;

        internal TubeNetwork(GameLocation location, Vector2 startTile)
        {
            List<PortObject> ports = new List<PortObject>();
            List<Vector2> visited = new List<Vector2>();
            Queue<Vector2> toVisit = new Queue<Vector2>();
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
                    // keep searching
                } else {
                    continue;
                }

                foreach (Vector2 adjacent in Utility.getAdjacentTileLocations(tile))
                    toVisit.Enqueue(adjacent);
            }

            this.ports = ports.ToArray();
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
