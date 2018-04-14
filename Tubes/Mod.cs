using System;
using System.Collections.Generic;

using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

using Pathoschild.Stardew.Common;
using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;
using StardewValley.Locations;
using StardewValley.Buildings;
using Pathoschild.Stardew.Automate.Framework;
using System.Linq;
using System.Collections;
using StardewValley.Menus;
using PyTK.Extensions;

namespace Tubes
{
    // Mod entry point.
    public class TubesMod : Mod
    {
        internal static IModHelper _helper;
        internal static IMonitor _monitor;
        internal static IAutomateAPI automateApi;

        internal Dictionary<GameLocation, TubeNetwork[]> tubeNetworks = new Dictionary<GameLocation, TubeNetwork[]> ();
        internal IEnumerable<TubeNetwork> allTubeNetworks { get => tubeNetworks.SelectMany(kv => kv.Value); }
        internal HashSet<GameLocation> reloadQueue = new HashSet<GameLocation>();

        public override void Entry(IModHelper helper)
        {
            _helper = helper;
            _monitor = Monitor;
            TubeObject.init();
            TubeTerrain.init();
            PortObject.init();

            GameEvents.OneSecondTick += this.GameEvents_OneSecondTick;
            LocationEvents.LocationsChanged += this.LocationEvents_LocationsChanged;
            LocationEvents.LocationObjectsChanged += this.LocationEvents_LocationObjectsChanged;
            MenuEvents.MenuChanged += MenuEvents_MenuChanged;
        }

        private void GameEvents_OneSecondTick(object sender, EventArgs e)
        {
            foreach (GameLocation location in this.reloadQueue)
                this.tubeNetworks[location] = TubeNetwork.getAllNetworksIn(location).ToArray();
            this.reloadQueue.Clear();

            foreach (TubeNetwork network in this.allTubeNetworks)
                network.process();
        }

        private void LocationEvents_LocationsChanged(object sender, EventArgsGameLocationsChanged e)
        {
            this.tubeNetworks.Clear();
            foreach (GameLocation location in CommonHelper.GetLocations())
                this.reloadQueue.Add(location);
        }

        private void LocationEvents_LocationObjectsChanged(object sender, EventArgsLocationObjectsChanged e)
        {
            this.tubeNetworks.Remove(Game1.currentLocation);
            this.reloadQueue.Add(Game1.currentLocation);

            // When the player places a TubeObject, it's placed as an object. Gather them and replace them with TubeTerrain
            // instead. This seems to be the only way to place terrain features.
            // We also remove any temporary "junk" objects, which are only used to trigger this event.
            List<Vector2> tubes = new List<Vector2>();
            List<Vector2> junk = new List<Vector2>();
            foreach (var obj in Game1.currentLocation.objects) {
                if (obj.Value.parentSheetIndex == TubeObject.objectData.sdvId)
                    tubes.Add(obj.Key);
                if (obj.Value.parentSheetIndex == JunkObject.objectData.sdvId)
                    junk.Add(obj.Key);
            }
            foreach (var pos in tubes) {
                SObject obj = Game1.currentLocation.objects[pos];
                Game1.currentLocation.objects.Remove(pos);

                if (!Game1.currentLocation.terrainFeatures.ContainsKey(pos)) {
                    Game1.currentLocation.terrainFeatures.Add(pos, new TubeTerrain());
                } else {
                    Game1.player.addItemToInventory(obj);
                }
            }
            foreach (var pos in junk) {
                Game1.currentLocation.objects.Remove(pos);
            }

            TubeTerrain.updateSpritesInLocation(Game1.currentLocation);
        }

        private void MenuEvents_MenuChanged(object sender, EventArgsClickableMenuChanged e)
        {
            // Override the crafting menu so that our recipe has the proper icon and text.
            if (Game1.activeClickableMenu is GameMenu activeMenu && Helper.Reflection.GetField<List<IClickableMenu>>(activeMenu, "pages").GetValue().Find(p => p is CraftingPage) is CraftingPage craftingPage) {
                for (int i = 0; i < craftingPage.pagesOfCraftingRecipes.Count; i++) {
                    if (craftingPage.pagesOfCraftingRecipes[i].Find(k => k.Value.name == TubeObject.blueprint.fullid) is KeyValuePair<ClickableTextureComponent, CraftingRecipe> kv && kv.Value != null && kv.Key != null) {
                        kv.Key.texture = TubeObject.icon;
                        kv.Key.sourceRect = TubeObject.objectData.sourceRectangle;
                        kv.Key.baseScale = 4.0f;
                        kv.Value.DisplayName = TubeObject.blueprint.name;
                        Helper.Reflection.GetField<string>(kv.Value, "description").SetValue(TubeObject.blueprint.description);
                    }
                    if (craftingPage.pagesOfCraftingRecipes[i].Find(k => k.Value.name == PortObject.blueprint.fullid) is KeyValuePair<ClickableTextureComponent, CraftingRecipe> kv2 && kv2.Value != null && kv2.Key != null) {
                        kv = kv2;
                        kv.Key.texture = PortObject.icon;
                        kv.Key.sourceRect = PortObject.objectData.sourceRectangle;
                        kv.Key.baseScale = 4.0f;
                        kv.Value.DisplayName = PortObject.blueprint.name;
                        Helper.Reflection.GetField<string>(kv.Value, "description").SetValue(PortObject.blueprint.description);
                    }
                }
            }
        }

        //// Called by Automate mod to check if there's a machine at the given tile.
        //public IMachine getMachineHook(GameLocation location, Vector2 tile, out Vector2 size)
        //{
        //    size = Vector2.Zero;
        //    if (!(location is Farm))
        //        return null;

        //    if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature feature) && feature is TubeTerrain) {
        //        size = Vector2.One;
        //        return new DummyMachine();
        //    }

        //    if (location is BuildableGameLocation buildableLocation) {
        //        foreach (Building building in buildableLocation.buildings) {
        //            Vector2 doorTile = new Vector2(building.tileX + building.humanDoor.X, building.tileY + building.humanDoor.Y);
        //            if (building.indoors != null && tile == doorTile) {
        //                size = Vector2.One;
        //                return new DummyMachine();
        //            }
        //        }
        //    }
        //    return null;
        //}

        //// Called by Automate mod to check if there's a container at the given tile.
        //public IContainer getContainerHook(GameLocation location, Vector2 tile, out Vector2 size)
        //{
        //    foreach (var warp in location.warps) {
        //        // Manhattan distance.
        //        if (Math.Abs(tile.X - warp.X) + Math.Abs(tile.Y - warp.Y) <= 1 && location.GetTiles().Contains(tile)) {
        //            size = Vector2.One;
        //            ContainerBridge bridge = new ContainerBridge(warp);
        //            this.Monitor.Log($"Adding container from {location.Name} at {tile} to {warp.TargetName} at {warp.TargetX}, {warp.TargetY}");
        //            if (!this.bridges.ContainsKey(location))
        //                this.bridges.Add(location, new HashSet<ContainerBridge>());
        //            this.bridges[location].Add(bridge);
        //            return bridge;
        //        }
        //    }

        //    size = Vector2.Zero;
        //    return null;
        //}
    }
}
