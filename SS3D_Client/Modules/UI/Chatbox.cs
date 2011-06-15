using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Miyagi;
using Miyagi.UI;
using Miyagi.UI.Controls;
using Miyagi.Common;
using Miyagi.Common.Data;
using Miyagi.Common.Resources;
using Miyagi.Common.Events;
using Miyagi.TwoD;

namespace SS3D.Modules.UI
{
    class Chatbox
    {
        private MiyagiResources mMiyagiRes;

        public Panel chatPanel
        {
            private set;
            get;
        }

        private TextBox chatTextbox;

        public GUI chatGUI
        {
            private set;
            get;
        }

        public delegate void TextSubmitHandler(Chatbox Chatbox, string Text);

        private List<Label> entries = new List<Label>();

        private int currentYpos = 0;
        private readonly int maxLines = 20;
        private int transparency = 1;

        public int Transparency
        {
            get
            {
                return transparency;
            }

            set
            {
                transparency = Math.Min(Math.Max(value,1), 100);
                chatGUI.Fade((float)transparency * 0.01f, (float)transparency * 0.01f, 1); //Since we don't have direct access to that :(
            }
        }

        public Chatbox(string name)
        {
            mMiyagiRes = MiyagiResources.Singleton;
            chatGUI = new GUI(name);

            this.chatPanel = new Panel(name+"Panel")
            {
                TabStop = false,
                TabIndex = 0,
                Size = new Size(400, 150),
                Location = new Point(0, 0),
                MinSize = new Size(100, 100),
                AlwaysOnTop = false,
                ResizeThreshold = new Thickness(3),
                Padding = new Thickness(2, 2, 2, 2),
                BorderStyle =
                {
                    Thickness = new Thickness(3, 3, 3, 3)
                },
                HScrollBarStyle =
                {
                    ShowButtons = false,
                    Extent = 16,
                    BorderStyle =
                    {
                        Thickness = new Thickness(2, 2, 2, 2)
                    },
                    ThumbStyle =
                    {
                        BorderStyle =
                        {
                            Thickness = new Thickness(2, 2, 2, 2)
                        }
                    }
                },
                VScrollBarStyle =
                {
                    ShowButtons = false,
                    Extent = 16,
                    BorderStyle =
                    {
                        Thickness = new Thickness(2, 2, 2, 2)
                    },
                    ThumbStyle =
                    {
                        BorderStyle =
                        {
                            Thickness = new Thickness(2, 2, 2, 2)
                        }
                    }
                },
                TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
                Skin = MiyagiResources.Singleton.Skins["ConsolePanelSkin"]
            };
            chatPanel.SizeChanged += new EventHandler(chatPanel_SizeChanged);
            chatPanel.LocationChanged += new EventHandler<ChangedValueEventArgs<Point>>(chatPanel_LocationChanged);

            this.chatTextbox = new TextBox("ChatTextbox")
            {
                Size = new Size(400, 30),
                Location = new Point(0, 150),
                Padding = new Thickness(5, 3, 5, 3),
                AlwaysOnTop = false,
                DefocusOnSubmit = false,
                BorderStyle =
                {
                    Thickness = new Thickness(3, 3, 3, 3)
                },
                TextStyle =
                {
                    Offset = new Point(0, 3),
                    Alignment = Alignment.MiddleLeft,
                    ForegroundColour = Colours.White
                },
                TextBoxStyle =
                {
                    CaretStyle =
                    {
                        Size = new Size(2, 16),
                        Colour = Colours.White
                    }
                },
                Skin = MiyagiResources.Singleton.Skins["ConsoleTextBoxSkin"],
                TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
                ClearTextOnSubmit = true
            };

            chatTextbox.Submit += new EventHandler<ValueEventArgs<string>>(chatTextbox_Submit);

            chatGUI.Controls.Add(chatPanel);
            chatGUI.Controls.Add(chatTextbox);

            chatGUI.ZOrder = 10;
        }


        public void SetInputFocus(bool focus = true)
        {
            chatTextbox.Focused = focus;
        }

        public bool HasFocus()
        {
            return chatTextbox.Focused;
        }

        public event TextSubmitHandler TextSubmitted;

        private void chatTextbox_Submit(object sender, ValueEventArgs<string> e)
        {
            if (string.IsNullOrWhiteSpace(e.Data)) return;
            TextSubmitted(this, e.Data);
            //chatTextbox.Focused = false;
        }

        public void AddLine(string text)
        {
            var label = new Label
            {
                Location = new Point(0, currentYpos),
                Text = text,
                AutoSize = true,
                TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
                TextStyle =
                {
                    ForegroundColour = Colours.LightGrey
                }
            };
            label.SuccessfulHitTest += (s, e) => e.Cancel = true;
            this.chatPanel.Controls.Add(label);

            #region Workaround for a bug
            if (!chatGUI.Visible)
            {
                chatGUI.Visible = true;
                this.currentYpos += label.Size.Height;
                chatGUI.Visible = false;
            }
            else this.currentYpos += label.Size.Height; 
            #endregion

            this.entries.Add(label);
            if (entries.Count > maxLines) Trim();
            chatPanel.ScrollToBottom();
        }

        public void AddLine(string text, Colour colour)
        {
            var label = new Label
            {
                Location = new Point(0, currentYpos),
                Text = text,
                AutoSize = true,
                TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
                TextStyle =
                {
                    ForegroundColour = colour
                }
            };
            label.SuccessfulHitTest += (s, e) => e.Cancel = true;
            this.chatPanel.Controls.Add(label);

            #region Workaround for a bug
            if (!chatGUI.Visible)
            {
                chatGUI.Visible = true;
                this.currentYpos += label.Size.Height;
                chatGUI.Visible = false;
            }
            else this.currentYpos += label.Size.Height; 
            #endregion

            this.entries.Add(label);
            if (entries.Count > maxLines) Trim();
            chatPanel.ScrollToBottom();
        }

        void Trim()
        {
            if (entries.Count < 2) return;
            entries[1].Location = new Point(0, 0);
            Label toDelete = entries[0];
            entries.RemoveAt(0);
            currentYpos -= toDelete.Size.Height;
            toDelete.Dispose();
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].Location = (i != 0) ? new Point(0, entries[i - 1].Location.Y + entries[i - 1].Size.Height) : new Point(0, 0);
            }
        }

        void Clear()
        {
            foreach (Label lbl in entries)
            {
                lbl.Dispose();
            }
            currentYpos = 0;
            chatPanel.ScrollToTop();
        }

        void chatPanel_LocationChanged(object sender, ChangedValueEventArgs<Point> e)
        {
            if (chatTextbox != null)
            {
                Point newLoc = new Point(chatPanel.Location.X, chatPanel.Location.Y + chatPanel.Size.Height);
                chatTextbox.Location = newLoc;
            }
        }

        void chatPanel_SizeChanged(object sender, EventArgs e)
        {
            if (chatTextbox != null)
            {
                Size newSize = new Size(chatPanel.Size.Width, 30);
                Point newLoc = new Point(chatPanel.Location.X, chatPanel.Location.Y + chatPanel.Size.Height);
                chatTextbox.Size = newSize;
                chatTextbox.Location = newLoc;
            }
        }
    }
}
