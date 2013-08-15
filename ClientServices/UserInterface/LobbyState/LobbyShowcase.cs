using System;
using System.Collections.Generic;
using System.Drawing;
using ClientInterfaces.Resource;
using GorgonLibrary.InputDevices;
using SS13.IoC;

namespace ClientServices.UserInterface.Components
{
    public class LobbyShowcase : Showcase
    {
        protected int ScrollOffset = 0;
        public Size ItemOffsets = new Size(0,0);

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
            ClientArea = new Rectangle(Position, Size);

            _buttonRight.Position = new Point(ClientArea.Right - _buttonRight.ClientArea.Width, ClientArea.Top + (int) (ClientArea.Height/2f - _buttonRight.ClientArea.Height/2f));
            _buttonRight.Update(frameTime);

            _buttonLeft.Position = new Point(_buttonRight.ClientArea.X - _buttonRight.ClientArea.Width - _buttonLeft.ClientArea.Width, _buttonRight.ClientArea.Y);

            _buttonLeft.Update(frameTime);

            foreach (var curr in _items)
            {
                curr.Key.Update(frameTime);
            }

            _selectionGlow.Update(frameTime);
        }

        public override void Render()
        {
            if (_items.Count > 0)
            {
                if (ScrollOffset < 0 || ScrollOffset > _items.Count - 1)
                    ScrollOffset = 0;
                else
                {
                    if (_selectionGlow != null && Selected == ScrollOffset)
                    {
                        _selectionGlow.Position = new Point(ItemOffsets.Width + ClientArea.Left + (int)(ClientArea.Width / 2f - _selectionGlow.ClientArea.Width / 2f), ItemOffsets.Height + ClientArea.Top + (int)(ClientArea.Height / 2f - _selectionGlow.ClientArea.Height / 2f));
                        _selectionGlow.Render();
                    }

                    KeyValuePair<ImageButton, Object> middle = _items[ScrollOffset];
                    middle.Key.Position =
                        new Point(ItemOffsets.Width + ClientArea.Left + (int)(ClientArea.Width / 2f - middle.Key.ClientArea.Width / 2f),
                                  ItemOffsets.Height + ClientArea.Top + (int)(ClientArea.Height / 2f - middle.Key.ClientArea.Height / 2f));
                    if (FadeItems)
                        middle.Key.Color = Color.FromArgb(255, Color.White);

                    middle.Key.Render();

                    int lastPosLeft = middle.Key.ClientArea.Left - ItemSpacing;
                    int lastPosRight = middle.Key.ClientArea.Right + ItemSpacing;

                    for (int i = 1; i <= AdditionalColumns; i++)
                    {
                        float alphaAdj = 1 + AdditionalColumns - (AdditionalColumns/(float) i);
                        const float baseAlpha = 200;

                        //Left
                        if ((ScrollOffset - i) >= 0 && (ScrollOffset - i) <= _items.Count - 1)
                        {
                            KeyValuePair<ImageButton, Object> currLeft = _items[(ScrollOffset - i)];
                            currLeft.Key.Position = new Point(lastPosLeft - currLeft.Key.ClientArea.Width,ClientArea.Top + (int)(ClientArea.Height / 2f - currLeft.Key.ClientArea.Height / 2f));
                            lastPosLeft = currLeft.Key.ClientArea.Left - ItemSpacing;

                            if (_selectionGlow != null && (ScrollOffset - i) == Selected)
                            {
                                _selectionGlow.Position = new Point(currLeft.Key.ClientArea.Left + (int)(currLeft.Key.ClientArea.Width / 2f - _selectionGlow.ClientArea.Width / 2f), currLeft.Key.ClientArea.Top + (int)(currLeft.Key.ClientArea.Height / 2f - _selectionGlow.ClientArea.Height / 2f));
                                _selectionGlow.Render();
                            }

                            if (FadeItems)
                                currLeft.Key.Color = Color.FromArgb((int) (baseAlpha/alphaAdj), Color.White);

                            currLeft.Key.Render();
                        }

                        //Right
                        if ((ScrollOffset + i) >= 0 && (ScrollOffset + i) <= _items.Count - 1)
                        {
                            KeyValuePair<ImageButton, Object> currRight = _items[(ScrollOffset + i)];
                            currRight.Key.Position = new Point(lastPosRight, ClientArea.Top + (int)(ClientArea.Height / 2f - currRight.Key.ClientArea.Height / 2f));
                            lastPosRight = currRight.Key.ClientArea.Right + ItemSpacing;

                            if (_selectionGlow != null && (ScrollOffset + i) == Selected)
                            {
                                _selectionGlow.Position = new Point(currRight.Key.ClientArea.Left + (int)(currRight.Key.ClientArea.Width / 2f - _selectionGlow.ClientArea.Width / 2f), currRight.Key.ClientArea.Top + (int)(currRight.Key.ClientArea.Height / 2f - _selectionGlow.ClientArea.Height / 2f));
                                _selectionGlow.Render();
                            }

                            if (FadeItems)
                                currRight.Key.Color = Color.FromArgb((int) (baseAlpha/alphaAdj), Color.White);

                            currRight.Key.Render();
                        }
                    }
                }
            }

            if (ShowArrows && ScrollingNeeded())
            {
                _buttonLeft.Render();
                _buttonRight.Render();
            }
        }

        public bool ScrollingNeeded()
        {
            if (1 + (AdditionalColumns * 2) < _items.Count) return true;
            return false;
        }

        public override bool MouseWheelMove(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                if (ScrollingNeeded())
                {
                    if (e.WheelDelta > 0)
                    {
                        if (ScrollOffset + 1 <= _items.Count - 1) ScrollOffset++;
                        return true;
                    }
                    else if (e.WheelDelta < 0)
                    {
                        if (ScrollOffset - 1 >= 0) ScrollOffset--;
                        return true;
                    }
                }
            }
            return false;
        }

        public override void MouseMove(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
            {
                _buttonLeft.MouseMove(e);
                _buttonRight.MouseMove(e);

                foreach (var curr in _items)
                {
                    curr.Key.MouseMove(e);
                }
            }
        }

        public override bool MouseDown(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
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

        public override bool MouseUp(MouseInputEventArgs e)
        {
            if (ClientArea.Contains(new Point((int) e.Position.X, (int) e.Position.Y)))
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