﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using StardewValley;
using StardewValley.Menus;
using StardewValley.Objects;
using System.Collections.Generic;
using System.Linq;
using SObject = StardewValley.Object;

namespace Tubes
{
    
    public static class ItemCategories
    {
        public const int Minerals = -2;
        public const int Fish = -4;
        public const int Metals = -15;
        public const int Artisan = -26;
        public const int TreeSaps = -27;
        public const int Vegetables = -75;
        public const int Fruits = -79;
        public const int Flowers = -80;
    }

    public static class ItemHelper
    {
        internal static Dictionary<string, int> CategoryToNum = typeof(ItemCategories).GetFields().ToDictionary(f => f.Name, f => (int)f.GetValue(null));
        internal static Dictionary<int, string> NumToCategory = CategoryToNum.ToDictionary(x => x.Value, x => x.Key);
        internal static List<string> Categories = CategoryToNum.Keys.ToList();
    }

    /// <summary>Simplifies access to the game's sprite sheets.</summary>
    /// <remarks>Each sprite is represented by a rectangle, which specifies the coordinates and dimensions of the image in the sprite sheet.</remarks>
    internal static class Sprites
    {
        /// <summary>Sprites used to draw a letter.</summary>
        public static class Letter
        {
            /// <summary>The sprite sheet containing the letter sprites.</summary>
            public static Texture2D Sheet => Game1.content.Load<Texture2D>("LooseSprites\\letterBG");

            /// <summary>The letter background (including edges and corners).</summary>
            public static readonly Rectangle Sprite = new Rectangle(0, 0, 320, 180);
        }

        /// <summary>Sprites used to draw icons.</summary>
        public static class Icons
        {
            /// <summary>The sprite sheet containing the icon sprites.</summary>
            public static Texture2D Sheet => Game1.mouseCursors;

            /// <summary>A filled heart indicating a friendship level.</summary>
            public static readonly Rectangle FilledHeart = new Rectangle(211, 428, 7, 6);

            /// <summary>An empty heart indicating a missing friendship level.</summary>
            public static readonly Rectangle EmptyHeart = new Rectangle(218, 428, 7, 6);

            /// <summary>A down arrow for scrolling content.</summary>
            public static readonly Rectangle DownArrow = new Rectangle(12, 76, 40, 44);

            /// <summary>An up arrow for scrolling content.</summary>
            public static readonly Rectangle UpArrow = new Rectangle(76, 72, 40, 44);

            /// <summary>A green plus icon.</summary>
            public static readonly Rectangle GreenPlus = new Rectangle(0, 410, 16, 16);

            /// <summary>A no-smoking circle, minus the cigarette.</summary>
            public static readonly Rectangle Clear = new Rectangle(322, 498, 12, 12);

            /// <summary>A no-smoking circle, minus the cigarette.</summary>
            public static readonly Rectangle Set = OptionsInputListener.setButtonSource;
        }

        /// <summary>A blank pixel which can be colorised and stretched to draw geometric shapes.</summary>
        public static readonly Texture2D Pixel = CommonHelper.Pixel;
    }

    public class ChestHelper
    {
        public static int addToChest(Chest chest, Item item, int amount)
        {
            if (amount <= 0)
                return 0;

            int totalAdded = 0;
            IList<Item> contents = chest.items;

            // try stack into existing slot
            foreach (Item slot in contents) {
                if (slot != null && item.canStackWith(slot)) {
                    int added = amount - slot.addToStack(amount);
                    totalAdded += added;
                    amount -= added;
                    if (amount <= 0)
                        return totalAdded;
                }
            }

            // try add to empty slot
            for (int i = 0; i < Chest.capacity && i < contents.Count; i++) {
                if (contents[i] == null) {
                    contents[i] = ChestHelper.cloneItem(item, amount);
                    return amount;
                }
            }

            // try add new slot
            if (contents.Count < Chest.capacity) {
                contents.Add(ChestHelper.cloneItem(item, amount));
                return amount;
            }

            return totalAdded;
        }

        private static Item cloneItem(Item original, int amount = 1)
        {
            if (original == null)
                return null;

            Item stack = original.getOne();
            stack.Stack = amount;

            if (original is SObject originalObj && stack is SObject stackObj) {
                // fix some fields not copied by getOne()
                stackObj.name = originalObj.name;
                stackObj.DisplayName = originalObj.DisplayName;
                stackObj.preserve = originalObj.preserve;
                stackObj.preservedParentSheetIndex = originalObj.preservedParentSheetIndex;
                stackObj.honeyType = originalObj.honeyType;
            }

            return stack;
        }
    }

}