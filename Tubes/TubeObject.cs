using System;
using StardewValley;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PyTK.CustomElementHandler;
using System.Collections.Generic;
using StardewValley.Objects;
using PyTK.Extensions;

namespace Tubes
{
    public class Blueprint
    {
        public string fullid;
        public string name;
        public string category;
        public int price;
        public string description;
        public string crafting;

        public CustomObjectData createObjectData(Texture2D texture, Type type)
        {
            return new CustomObjectData(
                fullid,
                $"{name}/{price}/-300/{category} -24/{name}/{description}",
                texture,
                Color.White,
                0,
                false,
                type,
                crafting != null ? new CraftingData(fullid, crafting) : null);
        }
    }

    public class JunkObject {
        internal static Blueprint blueprint = new Blueprint {
            fullid = "Pneumatic Tube junk",
            name = "Pneumatic Tube junk",
            category = "Crafting",
            price = 100,
            description = "Temporary internal object left behind when a tube is junked. Player shouldn't see this.",
        };

        internal static CustomObjectData objectData;

        internal static void init(Texture2D icon)
        {
            objectData = blueprint.createObjectData(icon, null);
        }
    }

    // The Tube object type. This is used whenever the object is not placed on the ground (it's not a terrain feature).
    public class TubeObject : StardewValley.Object, ICustomObject, ISaveElement, IDrawFromCustomObjectData
    {
        internal static Texture2D icon;
        internal static CustomObjectData objectData;

        internal static Blueprint blueprint = new Blueprint {
            fullid = "Pneumatic Tube",
            name = "Pneumatic Tube",
            category = "Crafting",
            price = 100,
            description = "Connects machines together with the magic of vacuums.",
            crafting = "337 1",
        };

        internal static void init()
        {
            icon = TubesMod._helper.Content.Load<Texture2D>(@"Assets/icon.png");
            objectData = blueprint.createObjectData(icon, typeof(TubeObject));

            JunkObject.init(icon);
        }

        public CustomObjectData data { get => objectData; }

        public TubeObject()
        {
        }

        public TubeObject(CustomObjectData data)
            : base(data.sdvId, 1)
        {
        }

        public TubeObject(CustomObjectData data, Vector2 tileLocation)
            : base(tileLocation, data.sdvId)
        {
        }

        public Dictionary<string, string> getAdditionalSaveData()
        {
            return new Dictionary<string, string>() { { "name", name }, { "price", price.ToString() }, { "stack", stack.ToString() } };
        }

        public object getReplacement()
        {
            return new Chest(true) { playerChoiceColor = Color.Magenta };
        }

        public void rebuild(Dictionary<string, string> additionalSaveData, object replacement)
        {
            name = additionalSaveData["name"];
            price = additionalSaveData["price"].toInt();
            stack = additionalSaveData["stack"].toInt();
        }

        public override Item getOne()
        {
            return new TubeObject(data) { tileLocation = Vector2.Zero, name = name, price = price };
        }

        public ICustomObject recreate(Dictionary<string, string> additionalSaveData, object replacement)
        {
            return new TubeObject(data);
        }
    }
}
