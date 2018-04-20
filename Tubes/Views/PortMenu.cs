using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Pathoschild.Stardew.Common;
using StardewValley;
using StardewValley.Menus;

// TODO:
// - request/provide toggle? or 2 add buttons
// - request amount
// - scrolling

namespace Tubes
{
    internal delegate void PortDeleteFilter(PortFilter filter);

    internal enum PortFilterType
    {
        REQUESTS, PROVIDES
    }

    internal class PortFilterComponent
    {
        // toggle button for request/provide
        internal readonly PortFilter Filter;
        internal readonly DropdownComponent Dropdown;
        internal readonly ButtonComponent DeleteButton;
        internal ButtonComponent RequestAllToggle;
        internal SliderComponent RequestAmountSlider;
        internal bool RequestAmountChanged = false;

        public int Width { get; private set; }
        public int Height { get; private set; }

        internal PortFilterComponent(PortFilter filter, PortFilterType type, PortDeleteFilter onDeleted)
        {
            this.Filter = filter;

            int selected = 0;
            if (ItemHelper.NumToCategory.TryGetValue(this.Filter.category, out string category))
                selected = ItemHelper.Categories.IndexOf(category);

            this.Dropdown = new DropdownComponent(ItemHelper.Categories, "", 300) { visible = true, SelectionIndex = selected };
            this.Dropdown.DropDownOptionSelected += DropDownOptionSelected;

            this.DeleteButton = new ButtonComponent("", Sprites.Icons.Sheet, Sprites.Icons.Clear, 2, true) { visible = true, HoverText = "Delete" };
            this.DeleteButton.ButtonPressed += () => onDeleted(Filter);

            if (type == PortFilterType.REQUESTS)
                BuildSlider();
        }

        private void BuildSlider()
        {
            if (Filter.requestAmount == int.MaxValue) {
                this.RequestAllToggle = new ButtonComponent("Until full", Sprites.Icons.Sheet, Sprites.Icons.Set, 2, true) { visible = true, HoverText = "Click to request a specific amount" };
                this.RequestAllToggle.ButtonPressed += () => { Filter.requestAmount = 1; BuildSlider(); };
                this.RequestAmountSlider = null;
            } else {
                int min = Math.Max(0, 100 * (int)((Filter.requestAmount - 1) / 100));
                this.RequestAmountSlider = new SliderComponent("Amount", Math.Max(1, min), min + 101, 1, Filter.requestAmount, true, RequestAmountSlider?.X ?? 0, RequestAmountSlider?.Y ?? 0) { visible = true };
                this.RequestAmountSlider.SliderValueChanged += (v) => { Filter.requestAmount = (int)v; RequestAmountChanged = true; };
                this.RequestAmountChanged = false;
                this.RequestAllToggle = null;
            }
            UpdateLayout(Dropdown.X, Dropdown.Y, Width, Height);
        }

        internal void DropDownOptionSelected(int selected)
        {
            string category = ItemHelper.Categories[selected];
            this.Filter.category = ItemHelper.CategoryToNum[category];
        }

        public bool receiveLeftClick(int x, int y, bool playSound = true)
        {
            Dropdown.receiveLeftClick(x, y, playSound);
            RequestAmountSlider?.receiveLeftClick(x, y, playSound);
            RequestAllToggle?.receiveLeftClick(x, y, playSound);
            if (DeleteButton.containsPoint(x, y)) {
                DeleteButton.receiveLeftClick(x, y, playSound);
                return true;
            }
            return false;
        }

        public void leftClickHeld(int x, int y)
        {
            Dropdown.leftClickHeld(x, y);
            RequestAmountSlider?.leftClickHeld(x, y);
        }

        public void releaseLeftClick(int x, int y)
        {
            Dropdown.releaseLeftClick(x, y);
            RequestAmountSlider?.releaseLeftClick(x, y);
            if (RequestAmountChanged)
                BuildSlider();
        }

        public void performHoverAction(int x, int y)
        {
            RequestAllToggle?.performHoverAction(x, y);
            DeleteButton.performHoverAction(x, y);
        }

        public void draw(SpriteBatch b) {
            Dropdown.draw(Dropdown.IsActiveComponent() ? Game1.spriteBatch : b);
            RequestAllToggle?.draw(b);
            RequestAmountSlider?.draw(b);
            DeleteButton.draw(b);
        }

        public void UpdateLayout(int x, int y, int width, int height)
        {
            int margin = 24;

            int xpos = x;
            Dropdown.updateLocation(xpos, y, 300);
            xpos += Dropdown.Width + margin;

            int yMid;
            if (RequestAmountSlider != null) {
                yMid = Math.Max(0, (Dropdown.Height - RequestAmountSlider.Height) / 2);
                RequestAmountSlider.updateLocation(xpos, y + yMid);
                xpos += RequestAmountSlider.Width + margin;
            } else if (RequestAllToggle != null) {
                yMid = Math.Max(0, (Dropdown.Height - RequestAllToggle.Height) / 2);
                RequestAllToggle.updateLocation(xpos, y + yMid);
                xpos += RequestAllToggle.Width + margin;
            }

            yMid = Math.Max(0, (Dropdown.Height - DeleteButton.Height) / 2);
            DeleteButton.updateLocation(x + width - DeleteButton.Width, y + yMid);

            Width = width;
            Height = Dropdown.Height;
        }
    }

    internal class PortFiltersPage
    {
        internal List<PortFilter> Filters;
        internal List<PortFilterComponent> Components = new List<PortFilterComponent>();
        internal Action OnChanged;
        internal PortFilterType Type;

        internal PortFiltersPage(List<PortFilter> filters, Action onChanged, PortFilterType type)
        {
            this.Filters = filters;
            this.OnChanged = onChanged;
            this.Type = type;
            foreach (var filter in Filters)
                Components.Add(new PortFilterComponent(filter, Type, DeleteFilter));
        }

        internal void AddFilter()
        {
            PortFilter filter = new PortFilter();
            Filters.Add(filter);
            Components.Add(new PortFilterComponent(filter, Type, DeleteFilter));
            OnChanged();
        }

        internal void DeleteFilter(PortFilter filter)
        {
            int index = Filters.IndexOf(filter);
            Filters.RemoveAt(index);
            Components.RemoveAt(index);
            OnChanged();
            Game1.playSound("hammer");
        }
    }

    internal class PortMenu : IClickableMenu
    {
        internal static readonly Rectangle kMenuTextureSourceRect = new Rectangle(0, 256, 60, 60);

        /// <summary>The aspect ratio of the page background.</summary>
        private readonly Vector2 AspectRatio = new Vector2(Sprites.Letter.Sprite.Width, Sprites.Letter.Sprite.Height);

        private readonly ButtonComponent RequestsTabButton;
        private readonly ButtonComponent ProvidesTabButton;

        private readonly ButtonComponent AddButton;

        private readonly ClickableTextureComponent ScrollUpButton;
        private readonly ClickableTextureComponent ScrollDownButton;

        private PortFilterType CurrentTab = PortFilterType.REQUESTS;

        private PortFiltersPage RequestsPage;
        private PortFiltersPage ProvidesPage;
        private PortFiltersPage Page { get => CurrentTab == PortFilterType.REQUESTS ? RequestsPage : ProvidesPage; }
        private List<PortFilterComponent> Filters { get => Page.Components; }

        public PortMenu(List<PortFilter> requests, List<PortFilter> provides)
        {
            // add scroll buttons
            this.ScrollUpButton = new ClickableTextureComponent(Rectangle.Empty, Sprites.Icons.Sheet, Sprites.Icons.UpArrow, 1);
            this.ScrollDownButton = new ClickableTextureComponent(Rectangle.Empty, Sprites.Icons.Sheet, Sprites.Icons.DownArrow, 1);

            RequestsPage = new PortFiltersPage(requests, UpdateLayout, PortFilterType.REQUESTS);
            ProvidesPage = new PortFiltersPage(provides, UpdateLayout, PortFilterType.PROVIDES);

            this.RequestsTabButton = new ButtonComponent("", Sprites.Icons.Sheet, Sprites.Icons.DownArrow, 1, true) { visible = true, HoverText = "Requests" };
            this.RequestsTabButton.ButtonPressed += () => { CurrentTab = PortFilterType.REQUESTS; UpdateLayout(); };
            this.ProvidesTabButton = new ButtonComponent("", Sprites.Icons.Sheet, Sprites.Icons.UpArrow, 1, true) { visible = true, HoverText = "Provides" };
            this.ProvidesTabButton.ButtonPressed += () => { CurrentTab = PortFilterType.PROVIDES; UpdateLayout(); };
            this.AddButton = new ButtonComponent("", Sprites.Icons.Sheet, Sprites.Icons.GreenPlus, 3, true) { visible = true };
            this.AddButton.ButtonPressed += () => { Page.AddFilter(); };

            this.UpdateLayout();
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            if (!this.isWithinBounds(x, y) && !RequestsTabButton.containsPoint(x, y) && !ProvidesTabButton.containsPoint(x, y)) {
                foreach (var filter in this.Filters)
                    filter.Dropdown.releaseLeftClick(x, y);
                this.exitThisMenu();
                return;
            }

            foreach (var filter in this.Filters) {
                if (filter.receiveLeftClick(x, y, playSound))
                    return;  // stop processing when a filter is clicked, because it may modify Filters.
            }

            RequestsTabButton.receiveLeftClick(x, y, playSound);
            ProvidesTabButton.receiveLeftClick(x, y, playSound);
            AddButton.receiveLeftClick(x, y, playSound);
        }

        public override void leftClickHeld(int x, int y)
        {
            foreach (var filter in this.Filters)
                filter.leftClickHeld(x, y);
        }

        public override void releaseLeftClick(int x, int y)
        {
            foreach (var filter in this.Filters)
                filter.releaseLeftClick(x, y);
        }

        public override void performHoverAction(int x, int y)
        {
            foreach (var filter in this.Filters)
                filter.performHoverAction(x, y);
            AddButton.performHoverAction(x, y);
            ProvidesTabButton.performHoverAction(x, y);
            RequestsTabButton.performHoverAction(x, y);
        }

        public override void receiveRightClick(int x, int y, bool playSound = true)
        {
        }

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
                    this.receiveLeftClick(p.X, p.Y);
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

        public override void draw(SpriteBatch spriteBatch)
        {
            TubesMod._monitor.InterceptErrors("drawing the port filter menu", () => {
                int x = this.xPositionOnScreen;
                int y = this.yPositionOnScreen;

                // draw background
                // (This uses a separate sprite batch because it needs to be drawn before the
                // foreground batch, and we can't use the foreground batch because the background is
                // outside the clipping area.)
                using (SpriteBatch backgroundBatch = new SpriteBatch(Game1.graphics.GraphicsDevice)) {
                    backgroundBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, null);
                    IClickableMenu.drawTextureBox(backgroundBatch, Game1.menuTexture, kMenuTextureSourceRect, this.xPositionOnScreen, this.yPositionOnScreen, width, height, Color.White);
                    RequestsTabButton.draw(backgroundBatch, x, y - RequestsTabButton.Height);
                    ProvidesTabButton.draw(backgroundBatch, x + RequestsTabButton.Width + 16, y - RequestsTabButton.Height);
                    backgroundBatch.End();
                }

                const int gutter = 15;
                int contentWidth = this.width - gutter * 2;
                int contentHeight = this.height - gutter * 2;

                // draw foreground
                // (This uses a separate sprite batch to set a clipping area for scrolling.)
                using (SpriteBatch contentBatch = new SpriteBatch(Game1.graphics.GraphicsDevice)) {
                    // begin draw
                    GraphicsDevice device = Game1.graphics.GraphicsDevice;
                    device.ScissorRectangle = new Rectangle(x + gutter, y + gutter, contentWidth, contentHeight);
                    contentBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied, SamplerState.PointClamp, null, new RasterizerState { ScissorTestEnable = true });

                    this.AddButton.draw(contentBatch);

                    foreach (var filter in this.Filters.Reverse<PortFilterComponent>())
                        filter.draw(contentBatch);

                    contentBatch.End();
                }

                //    // scroll view
                //    //this.CurrentScroll = Math.Max(0, this.CurrentScroll); // don't scroll past top
                //    //this.CurrentScroll = Math.Min(this.MaxScroll, this.CurrentScroll); // don't scroll past bottom
                //    //topOffset -= this.CurrentScroll; // scrolled down == move text up

                //    //// draw portrait
                //    //if (subject.DrawPortrait(contentBatch, new Vector2(x + leftOffset, y + topOffset), new Vector2(70, 70)))
                //    //    leftOffset += 72;

                //    // draw fields
                //    float wrapWidth = this.width - leftOffset - gutter;
                //    {
                //        // draw name & item type
                //        //{
                //        //    Vector2 nameSize = contentBatch.DrawTextBlock(font, $"{subject.Name}.", new Vector2(x + leftOffset, y + topOffset), wrapWidth, bold: Constant.AllowBold);
                //        //    Vector2 typeSize = contentBatch.DrawTextBlock(font, $"{subject.Type}.", new Vector2(x + leftOffset + nameSize.X + spaceWidth, y + topOffset), wrapWidth);
                //        //    topOffset += Math.Max(nameSize.Y, typeSize.Y);
                //        //}

                //        //// draw description
                //        //if (subject.Description != null) {
                //        //    Vector2 size = contentBatch.DrawTextBlock(font, subject.Description?.Replace(Environment.NewLine, " "), new Vector2(x + leftOffset, y + topOffset), wrapWidth);
                //        //    topOffset += size.Y;
                //        //}

                //        //// draw spacer
                //        //topOffset += lineHeight;

                //        ////// draw custom fields
                //        //if (this.Fields.Any()) {
                //        //    ICustomField[] fields = this.Fields;
                //        //    float cellPadding = 3;
                //        //    float labelWidth = fields.Where(p => p.HasValue).Max(p => font.MeasureString(p.Label).X);
                //        //    float valueWidth = wrapWidth - labelWidth - cellPadding * 4 - tableBorderWidth;
                //        //    foreach (ICustomField field in fields) {
                //        //        if (!field.HasValue)
                //        //            continue;

                //        //        // draw label & value
                //        //        Vector2 labelSize = contentBatch.DrawTextBlock(font, field.Label, new Vector2(x + leftOffset + cellPadding, y + topOffset + cellPadding), wrapWidth);
                //        //        Vector2 valuePosition = new Vector2(x + leftOffset + labelWidth + cellPadding * 3, y + topOffset + cellPadding);
                //        //        Vector2 valueSize =
                //        //            field.DrawValue(contentBatch, font, valuePosition, valueWidth)
                //        //            ?? contentBatch.DrawTextBlock(font, field.Value, valuePosition, valueWidth);
                //        //        Vector2 rowSize = new Vector2(labelWidth + valueWidth + cellPadding * 4, Math.Max(labelSize.Y, valueSize.Y));

                //        //        // draw table row
                //        //        Color lineColor = Color.Gray;
                //        //        contentBatch.DrawLine(x + leftOffset, y + topOffset, new Vector2(rowSize.X, tableBorderWidth), lineColor); // top
                //        //        contentBatch.DrawLine(x + leftOffset, y + topOffset + rowSize.Y, new Vector2(rowSize.X, tableBorderWidth), lineColor); // bottom
                //        //        contentBatch.DrawLine(x + leftOffset, y + topOffset, new Vector2(tableBorderWidth, rowSize.Y), lineColor); // left
                //        //        contentBatch.DrawLine(x + leftOffset + labelWidth + cellPadding * 2, y + topOffset, new Vector2(tableBorderWidth, rowSize.Y), lineColor); // middle
                //        //        contentBatch.DrawLine(x + leftOffset + rowSize.X, y + topOffset, new Vector2(tableBorderWidth, rowSize.Y), lineColor); // right

                //        //        // track link area
                //        //        if (field is ILinkField linkField)
                //        //            this.LinkFieldAreas[linkField] = new Rectangle((int)valuePosition.X, (int)valuePosition.Y, (int)valueSize.X, (int)valueSize.Y);

                //        //        // update offset
                //        //        topOffset += Math.Max(labelSize.Y, valueSize.Y);
                //        //    }
                //        //}
                //    }

                //    //// update max scroll
                //    //this.MaxScroll = Math.Max(0, (int)(topOffset - contentHeight + this.CurrentScroll));

                //    //// draw scroll icons
                //    //if (this.MaxScroll > 0 && this.CurrentScroll > 0)
                //    //    this.ScrollUpButton.draw(contentBatch);
                //    //if (this.MaxScroll > 0 && this.CurrentScroll < this.MaxScroll)
                //    //    this.ScrollDownButton.draw(spriteBatch);

                //    // end draw
                //    contentBatch.End();
                //}

                // draw cursor
                this.drawMouse(Game1.spriteBatch);
            }, (Exception ex) => {
                TubesMod._monitor.InterceptErrors("handling an error in PortMenu.draw", () => this.exitThisMenu());
            });
        }

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
            int margin = 24;
            int x = this.xPositionOnScreen + margin;
            int y = this.yPositionOnScreen + margin;
            int gutter = 3;
            int contentHeight = this.height - gutter * 2;
            this.ScrollUpButton.bounds = new Rectangle(x + gutter, (int)(y + contentHeight - Sprites.Icons.UpArrow.Height - gutter - Sprites.Icons.DownArrow.Height), Sprites.Icons.UpArrow.Height, Sprites.Icons.UpArrow.Width);
            this.ScrollDownButton.bounds = new Rectangle(x + gutter, (int)(y + contentHeight - Sprites.Icons.DownArrow.Height), Sprites.Icons.DownArrow.Height, Sprites.Icons.DownArrow.Width);

            // update filters
            foreach (PortFilterComponent filter in this.Filters) {
                filter.UpdateLayout(x, y, width - margin * 2, height - margin * 2);
                y += filter.Height + margin;
            }

            this.AddButton.updateLocation(x, y);
            this.AddButton.HoverText = CurrentTab == PortFilterType.REQUESTS ? "New Request" : "New Provider";
        }
    }
}