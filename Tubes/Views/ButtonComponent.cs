using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using StardewValley.Menus;

namespace Tubes
{
    internal delegate void ButtonPressed();

    public enum OptionActionType
    {
        OK, SET, CLEAR, ADD, DONE, GIFT
    }

    internal class ButtonComponent : OptionComponent
    {

        internal event ButtonPressed ButtonPressed;

        private Rectangle setButtonSource => OptionsInputListener.setButtonSource;
        private Rectangle okButtonSource => Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 46, -1, -1);
        private Rectangle clearButtonSource => Game1.getSourceRectForStandardTileSheet(Game1.mouseCursors, 47, -1, -1);
        private Rectangle addButtonSource => new Rectangle(0, 410, 16, 16);
        private Rectangle doneButtonSource = new Rectangle(441, 411, 24, 13);
        private Rectangle giftButtonSource = new Rectangle(229, 410, 14, 14);

        protected Rectangle buttonSource {
            get {
                switch (this.ActionType) {
                    case OptionActionType.DONE:
                        return this.doneButtonSource;
                    case OptionActionType.CLEAR:
                        return this.clearButtonSource;
                    case OptionActionType.OK:
                        return this.okButtonSource;
                    case OptionActionType.SET:
                        return this.setButtonSource;
                    case OptionActionType.GIFT:
                        return this.giftButtonSource;
                    case OptionActionType.ADD:
                        return this.addButtonSource;
                    default:
                        return new Rectangle();
                }
            }
        }

        protected float buttonScale {
            get {
                switch (this.ActionType) {
                    case OptionActionType.DONE:
                        return (float)Game1.pixelZoom;
                    case OptionActionType.CLEAR:
                        return 1f;
                    case OptionActionType.OK:
                        return 1f;
                    case OptionActionType.SET:
                        return (float)Game1.pixelZoom;
                    case OptionActionType.GIFT:
                        return (float)Game1.pixelZoom;
                    case OptionActionType.ADD:
                        return 3f;
                    default:
                        return (float)Game1.pixelZoom;
                }
            }
        }

        public virtual OptionActionType ActionType {
            get {
                return _ActionType;
            }
            set {
                _ActionType = value;
            }
        }

        protected OptionActionType _ActionType;

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

        protected ClickableTextureComponent button;

        internal ButtonComponent(string label, OptionActionType type, int x, int y, bool enabled = true) : base(label, enabled)
        {
            this._ActionType = type;

            button = new ClickableTextureComponent(new Rectangle(x, y, (int)(buttonScale * buttonSource.Width), (int)(buttonScale * buttonSource.Height)), Game1.mouseCursors, buttonSource, buttonScale);
            button.drawShadow = true;
        }

        internal ButtonComponent(string label, OptionActionType type, bool enabled = true) : this(label, type, 0, 0, enabled) { }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            if (this.button.containsPoint(x, y) && enabled && this.IsAvailableForSelection()) {
                this.ButtonPressed?.Invoke();
            }

        }

        public void performHoverAction(int x, int y)
        {
            button.tryHover(x, y, maxScaleIncrease: 0.2f);
        }

        private OptionActionType oldType = OptionActionType.CLEAR;

        public void updateButton()
        {
            if (oldType != this.ActionType) {
                button = new ClickableTextureComponent(new Rectangle(button.bounds.X, button.bounds.Y, (int)(buttonScale * buttonSource.Width), (int)(buttonScale * buttonSource.Height)), Game1.mouseCursors, buttonSource, buttonScale);
                button.drawShadow = true;
                oldType = this.ActionType;
            }
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

            updateButton();
            button.draw(b, Color.White * ((this.enabled) ? 1f : 0.33f), 0.88f);
            //Utility.drawWithShadow(b, Game1.mouseCursors, new Vector2((float) (this.bounds.X), (float) (this.bounds.Y)), this.buttonSource, Color.White * ((this.enabled) ? 1f : 0.33f), 0f, Vector2.Zero, this.buttonScale, false, 0.15f, -1, -1, 0.35f);

            Utility.drawTextWithShadow(b, this.label, Game1.dialogueFont, new Vector2((float)(this.button.bounds.Right + Game1.pixelZoom * 4), (float)(this.button.bounds.Y + ((this.button.bounds.Height - labelSize.Y) / 2))), this.enabled ? Game1.textColor : (Game1.textColor * 0.33f), 1f, 0.1f, -1, -1, 1f, 3);
        }
    }
}