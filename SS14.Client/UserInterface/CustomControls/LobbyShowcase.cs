using System;
using System.Collections.Generic;
using OpenTK.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.UserInterface.Components;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Maths;
using Vector2i = SS14.Shared.Maths.Vector2i;

namespace SS14.Client.UserInterface.CustomControls
{
    public class LobbyShowcase : Showcase
    {
        protected int ScrollOffset = 0;
        private Vector2i itemOffsets = new Vector2i(0, 0);

        public Vector2i ItemOffsets { get => itemOffsets; set => itemOffsets = value; }

        protected override void _buttonRight_Clicked(ImageButton sender)
        {
            if (ScrollOffset + 1 <= _items.Count - 1) ScrollOffset++;
        }

        protected override void _buttonLeft_Clicked(ImageButton sender)
        {
            if (ScrollOffset - 1 >= 0) ScrollOffset--;
        }

        public override void AddItem(ImageButton button, Object associatedData)
        {
            if (button == null || associatedData == null) return;

            if (!_items.Exists(x => x.Key == button || x.Value == associatedData))
            {
                _items.Add(new KeyValuePair<ImageButton, object>(button, associatedData));
                button.Clicked += button_Clicked;
            }

            ScrollOffset = (int)Math.Floor(_items.Count / 2f); //start in the middle. cosmetic thing only.
            Selected = ScrollOffset;
        }

        protected override void button_Clicked(ImageButton sender)
        {
            if (_items.Exists(x => x.Key == sender))
            {
                KeyValuePair<ImageButton, object> sel = _items.Find(x => x.Key == sender);
                Selected = _items.IndexOf(sel);
            }
        }

        public override KeyValuePair<ImageButton, Object>? GetSelection()
        {
            if (Selected < 0 || Selected > _items.Count - 1 || _items.Count == 0)
                return null;
            else return _items[_selected];
        }

        public override void RemoveItem(Object toRemove)
        {
            if (toRemove is ImageButton)
                _items.RemoveAll(x => x.Key == toRemove);
            else
                _items.RemoveAll(x => x.Value == toRemove);
        }

        public override void Update(float frameTime)
        {
            ClientArea = Box2i.FromDimensions(Position, Size);

            _buttonRight.Position = new Vector2i(ClientArea.Right - _buttonRight.ClientArea.Width, ClientArea.Top + (int)(ClientArea.Height / 2f - _buttonRight.ClientArea.Height / 2f));
            _buttonRight.Update(frameTime);

            _buttonLeft.Position = new Vector2i(_buttonRight.ClientArea.Left - _buttonRight.ClientArea.Width - _buttonLeft.ClientArea.Width, _buttonRight.ClientArea.Top);

            _buttonLeft.Update(frameTime);

            foreach (var curr in _items)
            {
                curr.Key.Update(frameTime);
            }

            _selectionGlow.Update(frameTime);
        }

        public override void Draw()
        {
            if (_items.Count > 0)
            {
                if (ScrollOffset < 0 || ScrollOffset > _items.Count - 1)
                    ScrollOffset = 0;
                else
                {
                    KeyValuePair<ImageButton, Object> middle = _items[ScrollOffset];
                    middle.Key.Position =
                        new Vector2i(ItemOffsets.X + ClientArea.Left + (int)(ClientArea.Width / 2f - middle.Key.ClientArea.Width / 2f),
                                  ItemOffsets.Y + ClientArea.Top + (int)(ClientArea.Height / 2f - middle.Key.ClientArea.Height / 2f));
                    if (FadeItems)
                        middle.Key.ForegroundColor = Color4.White;

                    if (_selectionGlow != null && Selected == ScrollOffset)
                    {
                        _selectionGlow.Position = new Vector2i(ItemOffsets.X + ClientArea.Left + (int)(ClientArea.Width / 2f - _selectionGlow.ClientArea.Width / 2f), middle.Key.ClientArea.Top + (int)(middle.Key.ClientArea.Height / 2f - _selectionGlow.ClientArea.Height / 2f));
                        _selectionGlow.Draw();
                    }

                    middle.Key.Draw();

                    int lastPosLeft = middle.Key.ClientArea.Left - ItemSpacing;
                    int lastPosRight = middle.Key.ClientArea.Right + ItemSpacing;

                    for (int i = 1; i <= AdditionalColumns; i++)
                    {
                        float alphaAdj = 1 + AdditionalColumns - (AdditionalColumns / (float)i);
                        const float baseAlpha = 200;

                        //Left
                        if ((ScrollOffset - i) >= 0 && (ScrollOffset - i) <= _items.Count - 1)
                        {
                            KeyValuePair<ImageButton, Object> currLeft = _items[(ScrollOffset - i)];
                            currLeft.Key.Position = new Vector2i(lastPosLeft - currLeft.Key.ClientArea.Width, ClientArea.Top + (int)(ClientArea.Height / 2f - currLeft.Key.ClientArea.Height / 2f));
                            lastPosLeft = currLeft.Key.ClientArea.Left - ItemSpacing;

                            if (_selectionGlow != null && (ScrollOffset - i) == Selected)
                            {
                                _selectionGlow.Position = new Vector2i(currLeft.Key.ClientArea.Left + (int)(currLeft.Key.ClientArea.Width / 2f - _selectionGlow.ClientArea.Width / 2f), currLeft.Key.ClientArea.Top + (int)(currLeft.Key.ClientArea.Height / 2f - _selectionGlow.ClientArea.Height / 2f));
                                _selectionGlow.Draw();
                            }

                            if (FadeItems)
                                currLeft.Key.ForegroundColor = Color.White.WithAlpha(baseAlpha / alphaAdj);

                            currLeft.Key.Draw();
                        }

                        //Right
                        if ((ScrollOffset + i) >= 0 && (ScrollOffset + i) <= _items.Count - 1)
                        {
                            KeyValuePair<ImageButton, Object> currRight = _items[(ScrollOffset + i)];
                            currRight.Key.Position = new Vector2i(lastPosRight, ClientArea.Top + (int)(ClientArea.Height / 2f - currRight.Key.ClientArea.Height / 2f));
                            lastPosRight = currRight.Key.ClientArea.Right + ItemSpacing;

                            if (_selectionGlow != null && (ScrollOffset + i) == Selected)
                            {
                                _selectionGlow.Position = new Vector2i(currRight.Key.ClientArea.Left + (int)(currRight.Key.ClientArea.Width / 2f - _selectionGlow.ClientArea.Width / 2f), currRight.Key.ClientArea.Top + (int)(currRight.Key.ClientArea.Height / 2f - _selectionGlow.ClientArea.Height / 2f));
                                _selectionGlow.Draw();
                            }

                            if (FadeItems)
                                currRight.Key.ForegroundColor = Color.White.WithAlpha(baseAlpha / alphaAdj);

                            currRight.Key.Draw();
                        }
                    }
                }
            }

            if (ShowArrows && ScrollingNeeded())
            {
                _buttonLeft.Draw();
                _buttonRight.Draw();
            }
        }

        public bool ScrollingNeeded()
        {
            if (1 + (AdditionalColumns * 2) < _items.Count) return true;
            return false;
        }

        public override bool MouseWheelMove(MouseWheelScrollEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                if (ScrollingNeeded())
                {
                    if (e.Delta > 0)
                    {
                        if (ScrollOffset + 1 <= _items.Count - 1) ScrollOffset++;
                        return true;
                    }
                    else if (e.Delta < 0)
                    {
                        if (ScrollOffset - 1 >= 0) ScrollOffset--;
                        return true;
                    }
                }
            }
            return false;
        }

        public override void MouseMove(MouseMoveEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                _buttonLeft.MouseMove(e);
                _buttonRight.MouseMove(e);

                foreach (var curr in _items)
                {
                    curr.Key.MouseMove(e);
                }
            }
        }

        public override bool MouseDown(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                if (ShowArrows && ScrollingNeeded())
                {
                    if (_buttonLeft.MouseDown(e)) return true;
                    if (_buttonRight.MouseDown(e)) return true;
                }

                if (_items.Count > 0)
                {
                    if (ScrollOffset >= 0 || ScrollOffset <= _items.Count - 1)
                    {
                        KeyValuePair<ImageButton, Object> selected = _items[ScrollOffset];
                        if (selected.Key.MouseDown(e)) return true;

                        for (int i = 1; i <= AdditionalColumns; i++)
                        {
                            if ((ScrollOffset - i) >= 0 && (ScrollOffset - i) <= _items.Count - 1)
                            {
                                KeyValuePair<ImageButton, Object> selectedLeft = _items[(ScrollOffset - i)];
                                if (selectedLeft.Key.MouseDown(e)) return true;
                            }

                            if ((ScrollOffset + i) >= 0 && (ScrollOffset + i) <= _items.Count - 1)
                            {
                                KeyValuePair<ImageButton, Object> selectedRight = _items[(ScrollOffset + i)];
                                if (selectedRight.Key.MouseDown(e)) return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public override bool MouseUp(MouseButtonEventArgs e)
        {
            if (ClientArea.Contains(e.X, e.Y))
            {
                if (_buttonLeft.MouseUp(e)) return true;
                if (_buttonRight.MouseUp(e)) return true;

                if (_items.Count > 0)
                {
                    if (ScrollOffset >= 0 || ScrollOffset <= _items.Count - 1)
                    {
                        KeyValuePair<ImageButton, Object> selected = _items[ScrollOffset];
                        if (selected.Key.MouseUp(e)) return true;

                        for (int i = 1; i <= AdditionalColumns; i++)
                        {
                            if ((ScrollOffset - i) >= 0 && (ScrollOffset - i) <= _items.Count - 1)
                            {
                                KeyValuePair<ImageButton, Object> selectedLeft = _items[(ScrollOffset - i)];
                                if (selectedLeft.Key.MouseUp(e)) return true;
                            }

                            if ((ScrollOffset + i) >= 0 && (ScrollOffset + i) <= _items.Count - 1)
                            {
                                KeyValuePair<ImageButton, Object> selectedRight = _items[(ScrollOffset + i)];
                                if (selectedRight.Key.MouseUp(e)) return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
    }
}
