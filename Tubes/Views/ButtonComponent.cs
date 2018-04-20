using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;
using System;

namespace Tubes
{
    internal delegate void ButtonPressed();

    internal class ButtonComponent : OptionComponent
    {

        internal event ButtonPressed ButtonPressed;

        private Rectangle setButtonSource => OptionsInputListener.setButtonSource;
        private Rectangle okButtonSource => Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1);
        private Rectangle clearButtonSource => Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47, -1, -1);
        private Rectangle addButtonSource => new Rectangle(0, 410, 16, 16);
        private Rectangle doneButtonSource = new Rectangle(441, 411, 24, 13);
        private Rectangle giftButtonSource = new Rectangle(229, 410, 14, 14);


        //
        // Static Fields
        //

        //
        // Fields
        //

        public override int Width => button.bounds.Width;
        public override int Height => button.bounds.Height;
        public override int X => button.bounds.X;
        public override int Y => button.bounds.Y;
        public string HoverText { get => button.hoverText; set => button.hoverText = value; }

        protected ClickableTextureComponent button;

        internal ButtonComponent(string label, Texture2D texture, Rectangle source, int scale = 4, bool enabled = true) : base(label, enabled)
        {
            button = new ClickableTextureComponent(new Rectangle(0, 0, scale * source.Width, scale * source.Height), texture, source, scale);
            button.drawShadow = true;
        }

        public bool containsPoint(int x, int y)
        {
            return this.button.containsPoint(x, y);
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (this.button.containsPoint(x, y) && enabled && this.IsAvailableForSelection()) {
                this.ButtonPressed?.Invoke();
            }
        }

        private bool isHovering = false;
        public void performHoverAction(int x, int y)
        {
            isHovering = button.containsPoint(x, y);
            button.tryHover(x, y, maxScaleIncrease: 0.2f);
        }

        public void updateLocation(int x, int y)
        {
            button.bounds.X = x;
            button.bounds.Y = y;
        }

        public override void draw(SpriteBatch b, int x, int y)
        {
            button.bounds.X = x;
            button.bounds.Y = y;
            this.draw(b);
        }

        public override void draw(SpriteBatch b)
        {
            base.draw(b);

            // draw button
            var labelSize = Game1.dialogueFont.MeasureString(this.label);

            button.draw(b, Color.White * ((this.enabled) ? 1f : 0.33f), 0.88f);
            //Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (this.bounds.X), (float) (this.bounds.Y)), this.buttonSource, Color.White * ((this.enabled) ? 1f : 0.33f), 0f, Vector2.Zero, this.buttonScale, false, 0.15f, -1, -1, 0.35f);

            Utility.drawTextWithShadow(b, this.label, Game1.dialogueFont, new Vector2((float)(this.button.bounds.Right + Game1.pixelZoom * 4), (float)(this.button.bounds.Y + ((this.button.bounds.Height - labelSize.Y) / 2))), this.enabled ? Game1.textColor : (Game1.textColor * 0.33f), 1f, 0.1f, -1, -1, 1f, 3);

            if (isHovering)
                drawTooltip(Game1.smallFont, HoverText);
        }

        private void drawTooltip(SpriteFont font, string description)
        {
            Vector2 stringLength = font.MeasureString(description);
            int width = (int)stringLength.X + Game1.tileSize / 2 + 40;
            int height = (int)stringLength.Y + Game1.tileSize / 3 + 5;

            int x = (int)(Mouse.GetState().X / Game1.options.zoomLevel) + Game1.tileSize / 2;
            int y = (int)(Mouse.GetState().Y / Game1.options.zoomLevel) + Game1.tileSize / 2;

            if (y + height > Game1.graphics.GraphicsDevice.Viewport.Height)
                y = Game1.graphics.GraphicsDevice.Viewport.Height - height;

            IClickableMenu.drawTextureBox(Game1.spriteBatch, Game1.menuTexture, new Rectangle(0, 256, 60, 60), x, y, width, height, Color.White);
            Utility.drawTextWithShadow(Game1.spriteBatch, description, font, new Vector2(x + Game1.tileSize / 4, y + Game1.tileSize / 4), Game1.textColor);
        }
    }
}