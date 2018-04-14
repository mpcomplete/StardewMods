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

namespace Tubes
{
    // A filter that describes what kind/how many items to provide/request from the tube network and store
    // into the attached chest.
    internal class PortFilter
    {
        internal int category;
        internal int provideAmount;  // negative means request
        internal int requestAmount { get => -provideAmount; }
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
            crafting = "337 1",
        };

        internal static void init()
        {
            icon = TubesMod._helper.Content.Load<Texture2D>(@"Assets/port.png");
            objectData = blueprint.createObjectData(icon, typeof(PortObject));
        }

        public CustomObjectData data { get => objectData; }

        internal Chest attachedChest;
        internal PortFilter[] filters;
        internal IEnumerable<PortFilter> provides { get => filters.Where(f => f.provideAmount > 0);  }
        internal IEnumerable<PortFilter> requests { get => filters.Where(f => f.requestAmount > 0);  }

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
            return new Dictionary<string, string>() { { "tileLocation", tileLocation.X + "," + tileLocation.Y }, { "name", name }, { "stack", stack.ToString() } };
        }

        public object getReplacement()
        {
            return new Chest(true) { playerChoiceColor = Color.Magenta };
        }

        public void rebuild(Dictionary<string, string> additionalSaveData, object replacement)
        {
            //tileLocation = additionalSaveData["tileLocation"].Split(',').toList(i => i.toInt()).toVector<Vector2>();
            name = additionalSaveData["name"];
            stack = additionalSaveData["stack"].toInt();
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

        public override bool clicked(StardewValley.Farmer who)
        {
            if (filters == null || filters.Length == 2) {
                filters = new PortFilter[] {
                    new PortFilter() { category = -75, provideAmount = int.MaxValue }
                };
                Game1.showRedMessage("Providing crops.");
            } else {
                filters = new PortFilter[] {
                    new PortFilter() { category = -75, provideAmount = -200 },
                    new PortFilter() { category = -26, provideAmount = int.MaxValue },
                };
                Game1.showRedMessage("Requesting 200 crops, providing craftables.");
            }
            Game1.playSound("hammer");
            return false;
        }

        internal void updateAttachedChest(GameLocation location)
        {
            foreach (Vector2 adjacent in Utility.getAdjacentTileLocations(this.tileLocation)) {
                if (location.objects.TryGetValue(adjacent, out SObject o) && o is Chest chest) {
                    this.attachedChest = chest;
                    break;
                }
            }
        }

        internal void requestFrom(PortObject provider, PortFilter request, ref int numRequested)
        {
            List<Item> removedItems = new List<Item>();
            foreach (Item item in provider.attachedChest.items) {
                if (item.category == request.category) {
                    int take = Math.Min(numRequested, item.Stack);
                    item.Stack -= take;
                    numRequested -= take;
                    if (item.Stack <= 0)
                        removedItems.Add(item);
                    if (numRequested <= 0)
                        break;
                }
            }
            foreach (Item item in provider.attachedChest.items)
                provider.attachedChest.items.Remove(item);
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
