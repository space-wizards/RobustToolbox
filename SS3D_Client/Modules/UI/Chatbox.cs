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

        public TextBox chatTextbox;
        

        public GUI chatGUI
        {
            private set;
            get;
        }

        private List<Label> entries = new List<Label>();

        private int currentYpos = 0;
        private readonly int maxLines = 20;

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
                Movable = true,
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
                ClearTextOnSubmit = true
            };

            chatTextbox.Submit += new EventHandler<ValueEventArgs<string>>(chatTextbox_Submit);

            chatGUI.Controls.Add(chatPanel);
            chatGUI.Controls.Add(chatTextbox);

            chatGUI.ZOrder = 10;
        }

        public void chatTextbox_Submit(object sender, ValueEventArgs<string> e)
        {
            AddLine(e.Data);
        }

        public void AddLine(string text)
        {
            var label = new Label
            {
                Location = new Point(0, currentYpos),
                Text = text,
                AutoSize = true,
                TextStyle =
                {
                    ForegroundColour = Colours.LightGrey
                }
            };
            label.SuccessfulHitTest += (s, e) => e.Cancel = true;
            this.chatPanel.Controls.Add(label);

            if (!chatGUI.Visible) //Fuck miyagi. If the ui is hidden the size of it is 0,0. So here. Ugly hack.
            {
                chatGUI.Visible = true;
                this.currentYpos += label.Size.Height;
                chatGUI.Visible = false;
            }
            else this.currentYpos += label.Size.Height;

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
                TextStyle =
                {
                    ForegroundColour = colour
                }
            };
            label.SuccessfulHitTest += (s, e) => e.Cancel = true;
            this.chatPanel.Controls.Add(label);

            if (!chatGUI.Visible) //Fuck miyagi. If the ui is hidden the size of it is 0,0. So here. Ugly hack.
            {
                chatGUI.Visible = true;
                this.currentYpos += label.Size.Height;
                chatGUI.Visible = false;
            }
            else this.currentYpos += label.Size.Height;

            this.entries.Add(label);
            if (entries.Count > maxLines) Trim();
            chatPanel.ScrollToBottom();
        }

        void Trim()
        {
            if (entries.Count < 2) return; //This should never happen. Just make sure maxlines is > 2.
            entries[1].Location = new Point(0, 0);
            Label toDelete = entries[0];
            entries.RemoveAt(0); //Remove the oldest element.
            currentYpos -= toDelete.Size.Height;
            toDelete.Dispose();
            for (int i = 0; i < entries.Count; i++) //Update the positions of the other elements.
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
