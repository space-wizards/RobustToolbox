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
    class GameConsole
    {
        static readonly GameConsole singleton = new GameConsole();

        private MiyagiResources mMiyagiRes;
        private OgreManager mEngine;

        private Panel consolePanel;
        private TextBox consoleTextbox;
        private GUI consoleGUI;
        private List<Label> entries = new List<Label>();
        private List<String> log = new List<string>();
        private int currentYpos = 0;

        private readonly int maxLines = 75;

        private Boolean reinitializing = false; //Used in the initialize method to check if were just resetting the GUI.

        private List<string> autocompleteList = new List<string>();

        private bool visible = false;
        public bool Visible
        {
            get
            {
                return visible;
            }

            set
            {
                if (consoleGUI != null)
                {
                    if (consoleGUI.Controls == null) // Something disposed the gui.
                    {
                        reinitializing = true;
                        Initialize(mEngine);
                        return;
                    }

                    consoleGUI.Visible = value;
                    consolePanel.Enabled = consoleTextbox.Enabled = value;
                    visible = value;
                    consoleTextbox.Focused = value;
                    consoleGUI.ZOrder = 100;
                    consoleGUI.EnsureZOrder();
                }


            }
        }

        static GameConsole()
        {
        }

        GameConsole()
        {
        }

        public static GameConsole Singleton
        {
            get
            {
                return singleton;
            }
        }

        public void Initialize(OgreManager engine)
        {
            mMiyagiRes = MiyagiResources.Singleton;
            mEngine = engine;

            consoleGUI = new GUI("Console");

            autocompleteList.Add("getstate");
            autocompleteList.Add("setstate");
            autocompleteList.Add("exit");
            autocompleteList.Add("cls");

            this.consolePanel = new Panel("ConsolePanel")
            {
                TabStop = false,
                TabIndex = 0,
                Size = new Size(800, 300),
                Location = new Point(0, 0),
                MinSize = new Size(100,100),
                AlwaysOnTop = false,
                Movable = true,
                ResizeThreshold = new Thickness(3),
                Padding = new Thickness(2,2,2,2),
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
            consolePanel.SizeChanged += new EventHandler(consolePanel_SizeChanged);
            consolePanel.LocationChanged += new EventHandler<ChangedValueEventArgs<Point>>(consolePanel_LocationChanged);

            this.consoleTextbox = new TextBox("ConsoleTextbox")
            {
                Size = new Size(800, 30),
                Location = new Point(0, 300),
                Padding = new Thickness(5, 3, 5, 3),
                AlwaysOnTop = false,
                DefocusOnSubmit = false,
                TextStyle =
                {
                    Offset = new Point(0,3),
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
                AutoCompleteSource = autocompleteList,
                ClearTextOnSubmit = true
            };
            consoleTextbox.Submit += new EventHandler<ValueEventArgs<string>>(consoleTextbox_Submit);

            consoleGUI.Controls.Add(consolePanel);
            consoleGUI.Controls.Add(consoleTextbox);

            consoleGUI.ZOrder = 10;

            mMiyagiRes.mMiyagiSystem.GUIManager.GUIs.Add(consoleGUI);

            if (!reinitializing) //We just reset the console because the GUI was disposed of.
                Visible = false;
            else
                reinitializing = false;        
        }

        public void AddLine(string text)
        {
            var label = new Label
            {
                Location = new Point(0, currentYpos),
                Text = text,
                AutoSize = true
            };
            label.SuccessfulHitTest += (s, e) => e.Cancel = true;
            this.consolePanel.Controls.Add(label);

            if (!consoleGUI.Visible) //Fuck miyagi. If the ui is hidden the size of it is 0,0. So here. Ugly hack.
            {
                consoleGUI.Visible = true;
                this.currentYpos += label.Size.Height;
                consoleGUI.Visible = false;
            }
            else this.currentYpos += label.Size.Height;

            this.entries.Add(label);
            this.log.Add(text);
            if (entries.Count > maxLines) Trim();
            this.consolePanel.ScrollToBottom();
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
            this.consolePanel.Controls.Add(label);

            if (!consoleGUI.Visible) //Fuck miyagi. If the ui is hidden the size of it is 0,0. So here. Ugly hack.
            {
                consoleGUI.Visible = true;
                this.currentYpos += label.Size.Height;
                consoleGUI.Visible = false;
            }
            else this.currentYpos += label.Size.Height;

            this.entries.Add(label);
            this.log.Add(text);
            if (entries.Count > maxLines) Trim();
            this.consolePanel.ScrollToBottom();
        }

        void consoleTextbox_Submit(object sender, ValueEventArgs<string> e)
        {
            processInput(e.Data);
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

        private void processInput(string text)
        {
            string[] seperator = new string[] {" "};
            string[] splitInput = text.Split(seperator, StringSplitOptions.RemoveEmptyEntries);
            if (splitInput.Length < 1) return;
            switch (splitInput[0].ToLowerInvariant())
            {
                case("exit"):
                    mEngine.Window.Destroy(); 
                    break;

                case("setstate"): //This doesnt quite work yet. Im terrible at this.
                    if (splitInput.Length < 2)
                    {
                        AddLine("State name required", Colours.Red);
                        return;
                    }
                    Type stateType = Type.GetType("SS3D.States." + splitInput[1], false, true);
                    if(stateType == null)
                    {
                        AddLine("Unknown State : " + splitInput[1], Colours.Red);
                        return;
                    }
                    AddLine("Switching to state : " + stateType.Name, Colours.Green);
                    mEngine.mStateMgr.RequestStateChange(stateType);
                    break;

                case ("getstate"):
                    AddLine("Current State : " + mEngine.mStateMgr.mCurrentState.GetType().Name, Colours.Green);
                    break;

                case("cls"):
                    clear();
                    break;
                    
                default:
                    AddLine("Invalid command : " + splitInput[0], Colours.Red);
                    break;
            }

        }

        void clear()
        {
            foreach (Label lbl in entries)
            {
                lbl.Dispose();
            }
            currentYpos = 0;
            consolePanel.ScrollToTop();
        }

        void consolePanel_LocationChanged(object sender, ChangedValueEventArgs<Point> e)
        {
            if (consoleTextbox != null)
            {
                Point newLoc = new Point(consolePanel.Location.X, consolePanel.Location.Y + consolePanel.Size.Height);
                consoleTextbox.Location = newLoc;
            }
        }

        void consolePanel_SizeChanged(object sender, EventArgs e)
        {
            if (consoleTextbox != null)
            {
                Size newSize = new Size(consolePanel.Size.Width, 30);
                Point newLoc = new Point(consolePanel.Location.X, consolePanel.Location.Y + consolePanel.Size.Height);
                consoleTextbox.Size = newSize;
                consoleTextbox.Location = newLoc;
            }
        }
    }
}
