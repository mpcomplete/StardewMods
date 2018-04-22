using System;
using StardewValley;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyTK.CustomElementHandler;
using System.Collections.Generic;
using StardewValley.Objects;
using PyTK.Extensions;
using StardewValley.Tools;
using System.Linq;
using SObject = StardewValley.Object;
using Newtonsoft.Json;

namespace Tubes
{
    // A filter that describes what kind/how many items to provide/request from the tube network and store
    // into the attached chest.
    internal class PortFilter
    {
        [JsonProperty]
        internal int category = ItemCategories.Fruits;
        [JsonProperty]
        internal int requestAmount = int.MaxValue;  // unused for providers
    }

    // The Port object type. This object connects a chest to the tube network, and can be configured with
    // filters to pull items from other chests in the network into the attached chest.
    public class PortObject : StardewValley.Object, ICustomObject, ISaveElement, IDrawFromCustomObjectData
    {
        internal static Texture2D icon;
        internal static CustomObjectData objectData;

        internal static Blueprint blueprint = new Blueprint {
            fullid = "Pneumatic Tube Port",
            name = "Pneumatic Tube Port",
            category = "Crafting",
            price = 100,
            description = "Input/output port for a network of pneumatic tubes.",
            crafting = "337 1 787 1",
        };

        internal static void init()
        {
            icon = TubesMod._helper.Content.Load<Texture2D>(@"Assets/port.png");
            objectData = blueprint.createObjectData(icon, typeof(PortObject));
        }

        public CustomObjectData data { get => objectData; }

        internal Chest attachedChest;
        internal List<PortFilter> provides = new List<PortFilter>();
        internal List<PortFilter> requests = new List<PortFilter>();

        public PortObject()
        {
        }

        public PortObject(CustomObjectData data)
            : base(data.sdvId, 1)
        {
        }

        public PortObject(CustomObjectData data, Vector2 tileLocation)
            : base(tileLocation, data.sdvId)
        {
        }

        public Dictionary<string, string> getAdditionalSaveData()
        {
            return new Dictionary<string, string>() {
                { "tileLocation", tileLocation.X + "," + tileLocation.Y },
                { "name", name },
                { "stack", stack.ToString() },
                { "provides", JsonConvert.SerializeObject(this.provides) },
                { "requests", JsonConvert.SerializeObject(this.requests) },
            };
        }

        public object getReplacement()
        {
            return new Chest(true) { playerChoiceColor = Color.Magenta };
        }

        public void rebuild(Dictionary<string, string> additionalSaveData, object replacement)
        {
            tileLocation = additionalSaveData["tileLocation"].Split(',').Select((i) => i.toInt()).ToList().toVector<Vector2>();
            name = additionalSaveData["name"];
            stack = additionalSaveData["stack"].toInt();
            if (additionalSaveData.TryGetValue("provides", out string json))
                provides = JsonConvert.DeserializeObject<List<PortFilter>>(json);
            if (additionalSaveData.TryGetValue("requests", out json))
                requests = JsonConvert.DeserializeObject<List<PortFilter>>(json);
        }

        public override Item getOne()
        {
            return new PortObject(data) { tileLocation = Vector2.Zero, name = name };
        }

        public ICustomObject recreate(Dictionary<string, string> additionalSaveData, object replacement)
        {
            return new PortObject(data);
        }

        public override bool performToolAction(Tool t)
        {
            if (!(t is Pickaxe || t is Axe))
                return false;

            Game1.playSound("hammer");
            Game1.createRadialDebris(Game1.currentLocation, 12, (int)tileLocation.X, (int)tileLocation.Y, 4, false, -1, false, -1);
            Game1.currentLocation.debris.Add(new Debris((Item)new StardewValley.Object(data.sdvId, 1, false, -1, 0), tileLocation * (float)Game1.tileSize + new Vector2((float)(Game1.tileSize / 2), (float)(Game1.tileSize / 2))));
            Game1.currentLocation.objects.Remove(tileLocation);

            return false;
        }

        public override bool checkForAction(StardewValley.Farmer who, bool justCheckingForActivity = false)
        {
            if (!justCheckingForActivity) {
                Game1.activeClickableMenu = new PortMenu(requests, provides);
                Game1.playSound("bigSelect");
            }
            return false;
        }

        internal void updateAttachedChest(GameLocation location)
        {
            foreach (Vector2 adjacent in Utility.getAdjacentTileLocations(this.tileLocation)) {
                if (location.objects.TryGetValue(adjacent, out SObject o) && o is Chest chest) {
                    this.attachedChest = chest;
                    return;
                }
            }
        }

        internal void requestFrom(PortObject provider, PortFilter request, ref int numRequested)
        {
            if (!provider.canProvide(request))
               return;

            List<Item> removedItems = new List<Item>();
            foreach (Item item in provider.attachedChest.items) {
                if (item.category == request.category) {
                    int amountToTake = Math.Min(numRequested, item.Stack);
                    int amountTook = ChestHelper.addToChest(this.attachedChest, item, amountToTake);
                    item.Stack -= amountTook;
                    numRequested -= amountTook;
                    if (item.Stack <= 0)
                        removedItems.Add(item);
                    if (numRequested <= 0)
                        break;
                }
            }
            foreach (Item item in removedItems)
                provider.attachedChest.items.Remove(item);
        }

        internal bool canProvide(PortFilter request)
        {
            return this.provides.Any(p => p.category == request.category);
        }

        internal int amountMatching(PortFilter filter)
        {
            return attachedChest.items.Sum(i => {
                if (i.category == filter.category)
                    return i.Stack;
                return 0;
            });
        }
    }
}
