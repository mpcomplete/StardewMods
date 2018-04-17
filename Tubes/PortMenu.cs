using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Pathoschild.Stardew.Common;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace Tubes
{


    internal class PortMenu : IClickableMenu
    {
        /// <summary>A callback which shows a new lookup for a given subject.</summary>
        //private readonly Action<ISubject> ShowNewPage;

        /// <summary>The aspect ratio of the page background.</summary>
        private readonly Vector2 AspectRatio = new Vector2(Sprites.Letter.Sprite.Width, Sprites.Letter.Sprite.Height);

        /// <summary>The clickable 'scroll up' icon.</summary>
        private readonly ClickableTextureComponent ScrollUpButton;

        /// <summary>The clickable 'scroll down' icon.</summary>
        private readonly ClickableTextureComponent ScrollDownButton;

        private bool validatedDrawMode;

        public PortMenu()
        {
            // add scroll buttons
            this.ScrollUpButton = new ClickableTextureComponent(Rectangle.Empty, Sprites.Icons.Sheet, Sprites.Icons.UpArrow, 1);
            this.ScrollDownButton = new ClickableTextureComponent(Rectangle.Empty, Sprites.Icons.Sheet, Sprites.Icons.DownArrow, 1);

            // update layout
            this.UpdateLayout();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            this.HandleLeftClick(x, y);
        }

        public override void receiveRightClick(int x, int y, bool playSound = true) { }

        public override void receiveScrollWheelAction(int direction)
        {
        }

        public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
        {
            this.UpdateLayout();
        }

        public override void receiveGamePadButton(Buttons button)
        {
            switch (button) {
                // left click
                case Buttons.A:
                    Point p = Game1.getMousePosition();
                    this.HandleLeftClick(p.X, p.Y);
                    break;

                // exit
                case Buttons.B:
                    this.exitThisMenu();
                    break;

                // scroll up
                case Buttons.RightThumbstickUp:
                    break;

                // scroll down
                case Buttons.RightThumbstickDown:
                    break;
            }
        }

        public void HandleLeftClick(int x, int y)
        {
            // close menu when clicked outside
            if (!this.isWithinBounds(x, y))
                this.exitThisMenu();

            // custom link fields
            //else {
            //    foreach (var area in this.LinkFieldAreas) {
            //        if (area.Value.Contains(x, y)) {
            //            ISubject subject = area.Key.GetLinkSubject();
            //            if (subject != null)
            //                this.ShowNewPage(subject);
            //            break;
            //        }
            //    }
            //}
        }

        /// <summary>Render the UI.</summary>
        /// <param name="spriteBatch">The sprite batch being drawn.</param>
        public override void draw(SpriteBatch spriteBatch)
        {
             TubesMod._monitor.InterceptErrors("drawing the lookup info", () => {
                // disable when game is using immediate sprite sorting
                // (This prevents Lookup Anything from creating new sprite batches, which breaks its core rendering logic.
                // Fortunately this very rarely happens; the only known case is the Stardew Valley Fair, when the only thing
                // you can look up anyway is the farmer.)
                if (!this.validatedDrawMode) {
                    IReflectedField<SpriteSortMode> sortModeField =
                        TubesMod._helper.Reflection.GetField<SpriteSortMode>(Game1.spriteBatch, "spriteSortMode", required: false) // XNA
                        ?? TubesMod._helper.Reflection.GetField<SpriteSortMode>(Game1.spriteBatch, "_sortMode"); // MonoGame
                    if (sortModeField.GetValue() == SpriteSortMode.Immediate) {
                         TubesMod._monitor.Log("Aborted the lookup because the game's current rendering mode isn't compatible with the mod's UI. This only happens in rare cases (e.g. the Stardew Valley Fair).", LogLevel.Warn);
                        this.exitThisMenu(playSound: false);
                        return;
                    }
                    this.validatedDrawMode = true;
                }

                // calculate dimensions
                int x = this.xPositionOnScreen;
                int y = this.yPositionOnScreen;
                const int gutter = 15;
                float leftOffset = gutter;
                float topOffset = gutter;
                float contentWidth = this.width - gutter * 2;
                float contentHeight = this.height - gutter * 2;
                int tableBorderWidth = 1;

                // get font
                SpriteFont font = Game1.smallFont;
                float lineHeight = font.MeasureString("ABC").Y;
                float spaceWidth = CommonHelper.GetSpaceWidth(font);

                // draw background
                // (This uses a separate sprite batch because it needs to be drawn before the
                // foreground batch, and we can't use the foreground batch because the background is
                // outside the clipping area.)
                using (SpriteBatch backgroundBatch = new SpriteBatch(Game1.graphics.GraphicsDevice)) {
                    backgroundBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);
                    backgroundBatch.DrawSprite(Sprites.Letter.Sheet, Sprites.Letter.Sprite, x, y, scale: this.width / (float)Sprites.Letter.Sprite.Width);
                    backgroundBatch.End();
                }

                // draw foreground
                // (This uses a separate sprite batch to set a clipping area for scrolling.)
                using (SpriteBatch contentBatch = new SpriteBatch(Game1.graphics.GraphicsDevice)) {
                    // begin draw
                    GraphicsDevice device = Game1.graphics.GraphicsDevice;
                    device.ScissorRectangle = new Rectangle(x + gutter, y + gutter, (int)contentWidth, (int)contentHeight);
                    contentBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });

                    // scroll view
                    //this.CurrentScroll = Math.Max(0, this.CurrentScroll); // don't scroll past top
                    //this.CurrentScroll = Math.Min(this.MaxScroll, this.CurrentScroll); // don't scroll past bottom
                    //topOffset -= this.CurrentScroll; // scrolled down == move text up

                    //// draw portrait
                    //if (subject.DrawPortrait(contentBatch, new Vector2(x + leftOffset, y + topOffset), new Vector2(70, 70)))
                    //    leftOffset += 72;

                    // draw fields
                    float wrapWidth = this.width - leftOffset - gutter;
                    {
                        // draw name & item type
                        //{
                        //    Vector2 nameSize = contentBatch.DrawTextBlock(font, $"{subject.Name}.", new Vector2(x + leftOffset, y + topOffset), wrapWidth, bold: Constant.AllowBold);
                        //    Vector2 typeSize = contentBatch.DrawTextBlock(font, $"{subject.Type}.", new Vector2(x + leftOffset + nameSize.X + spaceWidth, y + topOffset), wrapWidth);
                        //    topOffset += Math.Max(nameSize.Y, typeSize.Y);
                        //}

                        //// draw description
                        //if (subject.Description != null) {
                        //    Vector2 size = contentBatch.DrawTextBlock(font, subject.Description?.Replace(Environment.NewLine, " "), new Vector2(x + leftOffset, y + topOffset), wrapWidth);
                        //    topOffset += size.Y;
                        //}

                        //// draw spacer
                        //topOffset += lineHeight;

                        ////// draw custom fields
                        //if (this.Fields.Any()) {
                        //    ICustomField[] fields = this.Fields;
                        //    float cellPadding = 3;
                        //    float labelWidth = fields.Where(p => p.HasValue).Max(p => font.MeasureString(p.Label).X);
                        //    float valueWidth = wrapWidth - labelWidth - cellPadding * 4 - tableBorderWidth;
                        //    foreach (ICustomField field in fields) {
                        //        if (!field.HasValue)
                        //            continue;

                        //        // draw label & value
                        //        Vector2 labelSize = contentBatch.DrawTextBlock(font, field.Label, new Vector2(x + leftOffset + cellPadding, y + topOffset + cellPadding), wrapWidth);
                        //        Vector2 valuePosition = new Vector2(x + leftOffset + labelWidth + cellPadding * 3, y + topOffset + cellPadding);
                        //        Vector2 valueSize =
                        //            field.DrawValue(contentBatch, font, valuePosition, valueWidth)
                        //            ?? contentBatch.DrawTextBlock(font, field.Value, valuePosition, valueWidth);
                        //        Vector2 rowSize = new Vector2(labelWidth + valueWidth + cellPadding * 4, Math.Max(labelSize.Y, valueSize.Y));

                        //        // draw table row
                        //        Color lineColor = Color.Gray;
                        //        contentBatch.DrawLine(x + leftOffset, y + topOffset, new Vector2(rowSize.X, tableBorderWidth), lineColor); // top
                        //        contentBatch.DrawLine(x + leftOffset, y + topOffset + rowSize.Y, new Vector2(rowSize.X, tableBorderWidth), lineColor); // bottom
                        //        contentBatch.DrawLine(x + leftOffset, y + topOffset, new Vector2(tableBorderWidth, rowSize.Y), lineColor); // left
                        //        contentBatch.DrawLine(x + leftOffset + labelWidth + cellPadding * 2, y + topOffset, new Vector2(tableBorderWidth, rowSize.Y), lineColor); // middle
                        //        contentBatch.DrawLine(x + leftOffset + rowSize.X, y + topOffset, new Vector2(tableBorderWidth, rowSize.Y), lineColor); // right

                        //        // track link area
                        //        if (field is ILinkField linkField)
                        //            this.LinkFieldAreas[linkField] = new Rectangle((int)valuePosition.X, (int)valuePosition.Y, (int)valueSize.X, (int)valueSize.Y);

                        //        // update offset
                        //        topOffset += Math.Max(labelSize.Y, valueSize.Y);
                        //    }
                        //}
                    }

                    //// update max scroll
                    //this.MaxScroll = Math.Max(0, (int)(topOffset - contentHeight + this.CurrentScroll));

                    //// draw scroll icons
                    //if (this.MaxScroll > 0 && this.CurrentScroll > 0)
                    //    this.ScrollUpButton.draw(contentBatch);
                    //if (this.MaxScroll > 0 && this.CurrentScroll < this.MaxScroll)
                    //    this.ScrollDownButton.draw(spriteBatch);

                    // end draw
                    contentBatch.End();
                }

                // draw cursor
                this.drawMouse(Game1.spriteBatch);
            }, this.OnDrawError);
        }


        /*********
        ** Private methods
        *********/
        /// <summary>Update the layout dimensions based on the current game scale.</summary>
        private void UpdateLayout()
        {
            // update size
            this.width = Math.Min(Game1.tileSize * 14, Game1.viewport.Width);
            this.height = Math.Min((int)(this.AspectRatio.Y / this.AspectRatio.X * this.width), Game1.viewport.Height);

            // update position
            Vector2 origin = Utility.getTopLeftPositionForCenteringOnScreen(this.width, this.height);
            this.xPositionOnScreen = (int)origin.X;
            this.yPositionOnScreen = (int)origin.Y;

            // update up/down buttons
            int x = this.xPositionOnScreen;
            int y = this.yPositionOnScreen;
            int gutter = 3;
            float contentHeight = this.height - gutter * 2;
            this.ScrollUpButton.bounds = new Rectangle(x + gutter, (int)(y + contentHeight - Sprites.Icons.UpArrow.Height - gutter - Sprites.Icons.DownArrow.Height), Sprites.Icons.UpArrow.Height, Sprites.Icons.UpArrow.Width);
            this.ScrollDownButton.bounds = new Rectangle(x + gutter, (int)(y + contentHeight - Sprites.Icons.DownArrow.Height), Sprites.Icons.DownArrow.Height, Sprites.Icons.DownArrow.Width);
        }

        /// <summary>The method invoked when an unhandled exception is intercepted.</summary>
        /// <param name="ex">The intercepted exception.</param>
        private void OnDrawError(Exception ex)
        {
            TubesMod._monitor.InterceptErrors("handling an error in the lookup code", () => this.exitThisMenu());
        }
    }


    /// <summary>Simplifies access to the game's sprite sheets.</summary>
    /// <remarks>Each sprite is represented by a rectangle, which specifies the coordinates and dimensions of the image in the sprite sheet.</remarks>
    internal static class Sprites
    {
        /*********
        ** Accessors
        *********/
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

            /// <summary>A stardrop icon.</summary>
            public static readonly Rectangle Stardrop = new Rectangle(346, 392, 8, 8);
        }

        /// <summary>A blank pixel which can be colorised and stretched to draw geometric shapes.</summary>
        public static readonly Texture2D Pixel = CommonHelper.Pixel;
    }
}
