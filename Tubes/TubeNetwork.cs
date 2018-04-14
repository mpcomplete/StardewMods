using Microsoft.Xna.Framework;
using StardewValley;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tubes
{
    internal class TubeNetwork
    {
        private PortObject[] ports;

        internal void updateNetwork(GameLocation location, Vector2 tileLocation)
        {
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
