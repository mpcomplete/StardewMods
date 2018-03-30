using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Input;
using Pathoschild.Stardew.Common;
using mpcomplete.Stardew.QuickCraft.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;

namespace mpcomplete.Stardew.QuickCraft
{
    internal class ModEntry : Mod
    {
        private ModConfig Config;
 
        public override void Entry(IModHelper helper)
        {
            this.Config = helper.ReadConfig<ModConfig>();

            InputEvents.ButtonPressed += this.InputEvents_ButtonPressed;
        }
        
        private void InputEvents_ButtonPressed(object sender, EventArgsInput e)
        {
            if (!Context.IsWorldReady)
                return;

            this.Monitor.InterceptErrors("handling your input", $"handling input '{e.Button}'", () => {
                if ((e.IsUseToolButton || e.IsActionButton) && IsEnabled()) {
                    switch (Game1.activeClickableMenu) {
                        case GameMenu menu: {
                            List<IClickableMenu> pages = this.Helper.Reflection.GetField<List<IClickableMenu>>(menu, "pages").GetValue();
                            CraftingPage craftingPage = pages[menu.currentTab] as CraftingPage;
                            if (craftingPage == null)
                                return;

                            CraftingRecipe recipe = this.Helper.Reflection.GetField<CraftingRecipe>(craftingPage, "hoverRecipe").GetValue();
                            if (recipe == null)
                                return;

                            int repeat = e.IsUseToolButton ? 1 : 5;
                            bool didCraft = false;
                            while (repeat-- > 0 && recipe.doesFarmerHaveIngredientsInInventory()) {
                                recipe.consumeIngredients();
                                Game1.player.addItemToInventory(recipe.createItem());
                                didCraft = true;
                            }

                            if (didCraft) {
                                e.SuppressButton();
                                Game1.playSound("Ship");
                            }
                            break;
                        }
                        case ShopMenu menu: {
                            int repeat = e.IsUseToolButton ? 10 : 100;
                            while (repeat-- > 0) {
                                menu.receiveLeftClick(Game1.getMouseX(), Game1.getMouseY(), false);
                            }
                            e.SuppressButton();
                            break;
                        }
                    }
                }
            });
        }

        private bool IsEnabled()
        {
            KeyboardState state = Keyboard.GetState();
            return this.Config.Controls.HoldToActivate.Any(button => button.TryGetKeyboard(out Keys key) && state.IsKeyDown(key));
        }
    }
}
