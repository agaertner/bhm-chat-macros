﻿using Blish_HUD;
using Blish_HUD.Controls;
using Blish_HUD.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended.TextureAtlases;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nekres.ChatMacros.Core.UI {
    public class KeyValueDropdown<T> : Control {
        private class DropdownPanel : Control {

            private const int TOOLTIP_HOVER_DELAY = 800;
            private const int SCROLL_CLOSE_THRESHOLD = 20;

            private KeyValueDropdown<T> _assocDropdown;

            private int _highlightedItemIndex = -1;

            private int HighlightedItemIndex {
                get => _highlightedItemIndex;
                set {
                    if (SetProperty(ref _highlightedItemIndex, value)) {
                        _hoverTime = 0;
                    }
                }
            }

            private double _hoverTime;

            private int _startTop;

            private DropdownPanel(KeyValueDropdown<T> assocDropdown) {
                _assocDropdown = assocDropdown;
                _size = new Point(_assocDropdown.Width, _assocDropdown.Height * _assocDropdown._items.Count);
                _location = GetPanelLocation();
                _zIndex = Screen.TOOLTIP_BASEZINDEX;

                _startTop = _location.Y;

                this.Parent = Graphics.SpriteScreen;

                Input.Mouse.LeftMouseButtonPressed += InputOnMousedOffDropdownPanel;
                Input.Mouse.RightMouseButtonPressed += InputOnMousedOffDropdownPanel;
            }

            private Point GetPanelLocation() {
                var dropdownLocation = _assocDropdown.AbsoluteBounds.Location;

                int yUnderDef = Graphics.SpriteScreen.Bottom - (dropdownLocation.Y + _assocDropdown.Height + _size.Y);
                int yAboveDef = Graphics.SpriteScreen.Top + (dropdownLocation.Y - _size.Y);

                return yUnderDef > 0 || yUnderDef > yAboveDef
                           // flip down
                           ? dropdownLocation + new Point(0, _assocDropdown.Height - 1)
                           // flip up
                           : dropdownLocation - new Point(0, _size.Y + 1);
            }

            public static DropdownPanel ShowPanel(KeyValueDropdown<T> assocDropdown) {
                return new DropdownPanel(assocDropdown);
            }

            private void InputOnMousedOffDropdownPanel(object sender, MouseEventArgs e) {
                if (!this.MouseOver) {
                    if (e.EventType == MouseEventType.RightMouseButtonPressed) {
                        // Required to prevent right-click exiting the menu from eating the next left click
                        _assocDropdown.HideDropdownPanelWithoutDebounce();
                    } else {
                        _assocDropdown.HideDropdownPanel();
                    }
                }
            }

            protected override void OnMouseMoved(MouseEventArgs e) {
                this.HighlightedItemIndex = this.RelativeMousePosition.Y / _assocDropdown.Height;

                base.OnMouseMoved(e);
            }

            private KeyValuePair<T, string> GetActiveItem() {
                return _highlightedItemIndex > 0 && _highlightedItemIndex < _assocDropdown._items.Count
                           ? _assocDropdown._items.ElementAt(_highlightedItemIndex)
                           : default;
            }

            private void UpdateHoverTimer(double elapsedMilliseconds) {
                if (_mouseOver) {
                    _hoverTime += elapsedMilliseconds;
                } else {
                    _hoverTime = 0;
                }

                this.BasicTooltipText = _hoverTime > TOOLTIP_HOVER_DELAY
                                            ? GetActiveItem().Value
                                            : string.Empty;
            }

            private void UpdateDropdownLocation() {
                _location = GetPanelLocation();

                if (Math.Abs(_location.Y - _startTop) > SCROLL_CLOSE_THRESHOLD) {
                    Dispose();
                }
            }

            public override void DoUpdate(GameTime gameTime) {
                UpdateHoverTimer(gameTime.ElapsedGameTime.TotalMilliseconds);
                UpdateDropdownLocation();
            }

            protected override void OnClick(MouseEventArgs e) {
                _assocDropdown.SelectedItem = _assocDropdown._items.ElementAt(this.HighlightedItemIndex).Key;
                base.OnClick(e);
                Dispose();
            }

            protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
                spriteBatch.DrawOnCtrl(this, ContentService.Textures.Pixel, new Rectangle(Point.Zero, _size), Color.Black);

                int index = 0;
                foreach (var item in _assocDropdown._items.Values) {
                    if (index == this.HighlightedItemIndex) {
                        spriteBatch.DrawOnCtrl(this,
                                               ContentService.Textures.Pixel,
                                               new Rectangle(2,
                                                             2 + _assocDropdown.Height * index,
                                                             _size.X - 12 - _textureArrow.Width,
                                                             _assocDropdown.Height - 4),
                                               new Color(45, 37, 25, 255));

                        spriteBatch.DrawStringOnCtrl(this,
                                                     item,
                                                     Content.DefaultFont14,
                                                     new Rectangle(8,
                                                                   _assocDropdown.Height * index,
                                                                   bounds.Width - 13 - _textureArrow.Width,
                                                                   _assocDropdown.Height),
                                                     ContentService.Colors.Chardonnay);
                    } else {
                        spriteBatch.DrawStringOnCtrl(this,
                                                     item,
                                                     Content.DefaultFont14,
                                                     new Rectangle(8,
                                                                   _assocDropdown.Height * index,
                                                                   bounds.Width - 13 - _textureArrow.Width,
                                                                   _assocDropdown.Height),
                                                     Color.FromNonPremultiplied(239, 240, 239, 255));
                    }

                    index++;
                }
            }

            protected override void DisposeControl() {
                if (_assocDropdown != null) {
                    _assocDropdown._lastPanel = null;
                    _assocDropdown = null;
                }

                Input.Mouse.LeftMouseButtonPressed -= InputOnMousedOffDropdownPanel;
                Input.Mouse.RightMouseButtonPressed -= InputOnMousedOffDropdownPanel;

                base.DisposeControl();
            }

        }

        #region Load Static

        private static readonly Texture2D _textureInputBox = Content.GetTexture("input-box");

        private static readonly TextureRegion2D _textureArrow = Blish_HUD.Controls.Resources.Control.TextureAtlasControl.GetRegion("inputboxes/dd-arrow"); // GameService.Content.GetTextureAtlas(@"atlas\ui");
        private static readonly TextureRegion2D _textureArrowActive = Blish_HUD.Controls.Resources.Control.TextureAtlasControl.GetRegion("inputboxes/dd-arrow-active");

        #endregion

        #region Events

        /// <summary>
        /// Occurs when the <see cref="SelectedItem"/> property has changed.
        /// </summary>
        public event EventHandler<ValueChangedEventArgs<T>> ValueChanged;

        protected virtual void OnValueChanged(ValueChangedEventArgs<T> e) {
            this.ValueChanged?.Invoke(this, e);
        }

        #endregion

        private readonly SortedList<T, string> _items;

        private string _selectedItemText;

        private T _selectedItem;
        public T SelectedItem {
            get => _selectedItem;
            set {
                T previousValue = _selectedItem;

                if (SetProperty(ref _selectedItem, value)) {
                    _selectedItemText = _items.TryGetValue(value, out var displayText) ? displayText : string.Empty;
                    OnValueChanged(new ValueChangedEventArgs<T>(previousValue, _selectedItem));
                }
            }
        }

        private string _placeholderText;
        public string PlaceholderText {
            get => _placeholderText;
            set => SetProperty(ref _placeholderText, value);
        }

        /// <summary>
        /// Returns <c>true</c> if this <see cref="Dropdown"/> is actively
        /// showing the dropdown panel of options.
        /// </summary>
        public bool PanelOpen => _lastPanel != null;

        private DropdownPanel _lastPanel = null;
        private bool _hadPanel = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="Dropdown"/> class.
        /// </summary>
        public KeyValueDropdown() {
            _placeholderText = string.Empty;
            _selectedItemText = string.Empty;
            _items = new SortedList<T, string>();

            this.Size = Dropdown.Standard.Size;
        }

        public bool AddItem(T key, string value) {
            if (_items.ContainsKey(key)) {
                return false;
            }

            _items.Add(key, value);

            ItemsUpdated();
            Invalidate();

            return true;
        }

        public bool RemoveItem(T key) {
            if (!_items.Remove(key)) {
                return false;
            }

            ItemsUpdated();
            Invalidate();

            return true;
        }

        public void Clear() {
            _items.Clear();
            ItemsUpdated();
            Invalidate();
        }

        /// <summary>
        /// If the Dropdown box items are currently being shown, they are hidden.
        /// </summary>
        public void HideDropdownPanel() {
            _hadPanel = _mouseOver;
            _lastPanel?.Dispose();
        }

        private void HideDropdownPanelWithoutDebounce() {
            HideDropdownPanel();
            _hadPanel = false;
        }

        protected override void OnClick(MouseEventArgs e) {
            base.OnClick(e);

            if (_lastPanel == null && !_hadPanel) {
                _lastPanel = DropdownPanel.ShowPanel(this);
            } else {
                _hadPanel = false;
            }
        }

        private void ItemsUpdated() {
            // Update text in case SelectedItem was set before the items were added (eg. object initializer syntax)
            _selectedItemText = _items.TryGetValue(SelectedItem, out var displayText) ? displayText : string.Empty;

            if (Equals(SelectedItem, default)) {
                this.SelectedItem = _items.FirstOrDefault().Key; // Change the selected item if we do not already have one
            }
        }

        protected override void Paint(SpriteBatch spriteBatch, Rectangle bounds) {
            // Draw dropdown
            spriteBatch.DrawOnCtrl(this,
                                   _textureInputBox,
                                   new Rectangle(Point.Zero, _size).Subtract(new Rectangle(0, 0, 5, 0)),
                                   new Rectangle(0, 0,
                                                 Math.Min(_textureInputBox.Width - 5, this.Width - 5),
                                                 _textureInputBox.Height));

            // Draw right side of dropdown
            spriteBatch.DrawOnCtrl(this,
                                   _textureInputBox,
                                   new Rectangle(_size.X - 5, 0, 5, _size.Y),
                                   new Rectangle(_textureInputBox.Width - 5, 0,
                                                 5, _textureInputBox.Height));

            // Draw dropdown arrow
            spriteBatch.DrawOnCtrl(this,
                                   (this.Enabled && this.MouseOver) ? _textureArrowActive : _textureArrow,
                                   new Rectangle(_size.X - _textureArrow.Width - 5,
                                                 _size.Y / 2 - _textureArrow.Height / 2,
                                                 _textureArrow.Width,
                                                 _textureArrow.Height));

            // Draw text
            if (string.IsNullOrEmpty(_selectedItemText)) {
                spriteBatch.DrawStringOnCtrl(this,
                                             _placeholderText,
                                             Content.DefaultFont14,
                                             new Rectangle(5, 0,
                                                           _size.X - 10 - _textureArrow.Width,
                                                           _size.Y),
                                             (this.Enabled
                                                  ? Color.FromNonPremultiplied(209, 210, 209, 255)
                                                  : Control.StandardColors.DisabledText));
            } else {
                spriteBatch.DrawStringOnCtrl(this,
                                             _selectedItemText,
                                             Content.DefaultFont14,
                                             new Rectangle(5, 0,
                                                           _size.X - 10 - _textureArrow.Width,
                                                           _size.Y),
                                             (this.Enabled
                                                  ? Color.FromNonPremultiplied(239, 240, 239, 255)
                                                  : Control.StandardColors.DisabledText));
            }
        }
    }
}
