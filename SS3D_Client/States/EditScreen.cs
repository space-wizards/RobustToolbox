using System;
using System.IO;

using Mogre;
using Lidgren.Network;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Items;
using SS3D.Modules.Network;

using SS3D_shared;

using System.Collections.Generic;
using System.Reflection;

using Miyagi;
using Miyagi.UI;
using Miyagi.UI.Controls;
using Miyagi.Common;
using Miyagi.Common.Data;
using Miyagi.Common.Resources;
using Miyagi.Common.Events;
using Miyagi.TwoD;

namespace SS3D.States
{
    public class EditScreen : State
    {
        #region Variables
        private OgreManager mEngine;
        private StateManager mStateMgr;
        private Map map;
        private ItemManager itemManager;
        private MapSaver mapSaver;
        private GUI guiEditOptions;
        private GUI guiEditScreen;
        private Dictionary<TileType, string> tileList = new Dictionary<TileType, string>();
        private TileType newTileType = TileType.None;
        private ItemType newItemType = ItemType.None;
        private ushort lastID = 0;
        private AtomBaseClass selectedItem;

        private bool inEditor = false;

        #region New map vars
        //private int mapWidth;
        //private int mapHeight;
        //private bool mapWallSurround = true;
        //private int mapPartitionSize;
        //private string mapName;
        //private bool mapStartBlank = false;
        #endregion

        #endregion

        public EditScreen()
        {
            mEngine = null;
        }

        #region Startup, Shutdown, Update
        public override bool Startup(StateManager _mgr)
        {
            mEngine = _mgr.Engine;
            mStateMgr = _mgr;

            mEngine.SceneMgr.ShadowTextureSelfShadow = true;
            mEngine.SceneMgr.SetShadowTextureCasterMaterial("shadow_caster");
            mEngine.SceneMgr.SetShadowTexturePixelFormat(PixelFormat.PF_FLOAT16_RGB);
            mEngine.SceneMgr.ShadowCasterRenderBackFaces = false;
            mEngine.SceneMgr.SetShadowTextureSize(512);

            mEngine.SceneMgr.SetSkyBox(true, "SkyBox", 900f, true);

            mEngine.SceneMgr.ShadowTechnique = ShadowTechnique.SHADOWTYPE_TEXTURE_ADDITIVE_INTEGRATED;
            map = new Map(mEngine);

            mapSaver = new MapSaver(map, mEngine);

            mEngine.mNetworkMgr.SetMap(map);

            mEngine.SceneMgr.AmbientLight = ColourValue.White;

            mEngine.Camera.Position = new Mogre.Vector3(0, 300, 0);
            mEngine.Camera.LookAt(new Mogre.Vector3(160,64,160));

            itemManager = new ItemManager(mEngine, map, mEngine.mNetworkMgr);

            if (!mEngine.mNetworkMgr.isConnected)
            {
                AddSetUpButtons();
            }
            else
            {
                mEngine.mNetworkMgr.MessageArrived += new NetworkMsgHandler(mNetworkMgr_MessageArrived);

                mEngine.mNetworkMgr.RequestMap();
                while (!mEngine.mNetworkMgr.mapRecieved)
                {
                    mEngine.mNetworkMgr.UpdateNetwork();
                }
                map.LoadNetworkedMap(mEngine.mNetworkMgr.GetTileArray(), mEngine.mNetworkMgr.GetMapWidth(), mEngine.mNetworkMgr.GetMapHeight());
                inEditor = true;
                AddEditorButtons();
            }

            return true;
        }

        public override void Shutdown()
        {
            if (inEditor)
            {
                mEngine.mMiyagiSystem.GUIManager.GUIs.Remove(guiEditScreen);
                map.Shutdown();
                map = null;
                itemManager.Shutdown();
                itemManager = null;
                
            }
            else
            {
                guiEditOptions.Visible = false;
            }
            mEngine.mNetworkMgr.Disconnect();
        }

        public override void Update(long _frameTime)
        {
            if (!inEditor)
            {
                mEngine.Camera.Pitch((Radian)0.001);
                mEngine.Camera.Yaw((Radian)0.001);
                mEngine.Camera.Roll((Radian)0.001);
            }
            itemManager.Update();
        }

        private void mNetworkMgr_MessageArrived(NetworkManager netMgr, NetIncomingMessage msg)
        {
            if (msg == null)
            {
                return;
            }
            switch (msg.MessageType)
            {
                case NetIncomingMessageType.Data:
                    NetMessage messageType = (NetMessage)msg.ReadByte();
                    switch (messageType)
                    {
                        case NetMessage.ChangeTile:
                            ChangeTile(msg);
                            break;
                        case NetMessage.ItemMessage:
                            itemManager.HandleNetworkMessage(msg);
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }

        private void ChangeTile(NetIncomingMessage msg)
        {
            if (map == null)
            {
                return;
            }
            int x = msg.ReadInt32();
            int z = msg.ReadInt32();
            TileType newTile = (TileType)msg.ReadByte();
            map.ChangeTile(x, z, newTile);
        }

        public void WriteBoundingBox(AxisAlignedBox box, float scale)
        {
            FileStream fs = new FileStream("crowbarAABB", FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);

            Mogre.Vector3[] corners = box.GetAllCorners();

            sw.WriteLine("Crowbar:");
            sw.WriteLine("Deafult size:");
            for (int i = 0; i < corners.Length; i++)
            {
                sw.WriteLine("Corner: " + i + " : " + (corners[i]));
            }
            sw.WriteLine("Current scaled size:");
            for (int i = 0; i < corners.Length; i++)
            {
                sw.WriteLine("Corner: " + i + " : " + (corners[i] * scale));
            }
            sw.Close();
            fs.Close();
        }

        #endregion

        // These are the buttons for setting up the map - whether we generate a new one,
        // or load one from a file we've previously saved.
        #region Setup Buttons
        public void AddSetUpButtons()
        {
            Button returnButton = new Button("setupReturnButton")
            {
                Location = new Point(650, 650),
                Size = new Size(160, 40),
                Skin = MiyagiResources.Singleton.Skins["ButtonStandardSkin"],
                Text = "Return",
                TextStyle =
                {
                    Alignment = Alignment.MiddleCenter,
                    ForegroundColour = Colours.DarkBlue
                }
            };
            returnButton.MouseDown += ReturnButtonMouseDown;

            Panel newMapPanel = new Panel("setupNewMapPanel")
            {
                Location = new Point(32, 32),
                Size = new Size(400, 270),
                Skin = MiyagiResources.Singleton.Skins["PanelSkin"],
                Opacity = 0.5f,
                Enabled = false
            };

            TextBox mapWidthTextBox = new TextBox("setupMapWidthTextBox")
            {
                Size = new Size(64, 32),
                Location = new Point(50, 50),
                Padding = new Thickness(2,2,2,2),
                ToolTipText = "Map width in tiles.",
                ToolTipStyle = 
                {
                    HoverDuration = System.TimeSpan.Zero
                },
                Text = "30",
                TextStyle = 
                {
                    Alignment = Alignment.MiddleLeft,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                },
                TextBoxStyle = 
                {
                    DigitOnly = true
                },
                Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
            };

            Label mapWidthLabel = new Label("setupMapWidthLabel")
            {
                AutoSize = true,
                Text = "Width",
                Location = new Point(mapWidthTextBox.Location.X + mapWidthTextBox.Size.Width + 10, mapWidthTextBox.Location.Y + (mapWidthTextBox.Size.Height / 4)),
                TextStyle =
                {
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                }
            };

            TextBox mapHeightTextBox = new TextBox("setupMapHeightTextBox")
            {
                Size = new Size(64, 32),
                Location = new Point(50, 90),
                Padding = new Thickness(2,2,2,2),
                ToolTipText = "Map height in tiles.",
                Text = "30",
                ToolTipStyle =
                {
                    HoverDuration = System.TimeSpan.Zero
                },
                TextStyle = 
                {
                    Alignment = Alignment.MiddleLeft,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                },
                TextBoxStyle = 
                {
                    DigitOnly = true
                },
                Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
            };

            Label mapHeightLabel = new Label("setupMapHeightLabel")
            {
                AutoSize = true,
                Text = "Height",
                Location = new Point(mapHeightTextBox.Location.X + mapHeightTextBox.Size.Width + 10, mapHeightTextBox.Location.Y + (mapHeightTextBox.Size.Height / 4)),
                TextStyle = 
            {
                Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
            }
            };


            TextBox partitionSizeTextBox = new TextBox("setupPartitionSizeTextBox")
            {
                Size = new Size(64, 32),
                Location = new Point(50, 130),
                Padding = new Thickness(2,2,2,2),
                ToolTipText = "Partition size in tiles.",
                Text = "10",
                ToolTipStyle =
                {
                    HoverDuration = System.TimeSpan.Zero
                },
                TextStyle = 
                {
                    Alignment = Alignment.MiddleLeft,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                },
                TextBoxStyle = 
                {
                    DigitOnly = true
                },
                Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
            };

            Label partitionSizeLabel = new Label("setupPartitionSizeLabel")
            {
                AutoSize = true,
                Text = "Partition Size",
                Location = new Point(partitionSizeTextBox.Location.X + partitionSizeTextBox.Size.Width + 10, partitionSizeTextBox.Location.Y + (partitionSizeTextBox.Size.Height / 4)),
                TextStyle =
                {
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                }
            };

            CheckBox wallSurroundCheckBox = new CheckBox("setupWallSurroundCheckBox")
            {
                Size = new Size(32, 32),
                Location = new Point(50, 170),
                Skin = MiyagiResources.Singleton.Skins["CheckboxSkin"],
                ToolTipText = "Surround map with walls",
                ToolTipStyle = 
                {
                    HoverDuration = System.TimeSpan.Zero
                },
            };

            Label wallSurroundLabel = new Label("setupPartitionSizeLabel")
            {
                AutoSize = true,
                Text = "Wall Surround",
                Location = new Point(wallSurroundCheckBox.Location.X + wallSurroundCheckBox.Size.Width + 10, wallSurroundCheckBox.Location.Y + (wallSurroundCheckBox.Size.Height / 4)),
                TextStyle =
                {
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                }
            };

            CheckBox startBlankCheckBox = new CheckBox("setupStartBlankCheckBox")
            {
                Size = new Size(32, 32),
                Location = new Point(50, 210),
                Skin = MiyagiResources.Singleton.Skins["CheckboxSkin"],
                ToolTipText = "Map start blank",
                ToolTipStyle = 
                {
                    HoverDuration = System.TimeSpan.Zero
                },
            };

            Label startBlankLabel = new Label("setupStartBlankLabel")
            {
                AutoSize = true,
                Text = "Start Blank",
                Location = new Point(startBlankCheckBox.Location.X + startBlankCheckBox.Size.Width + 10, startBlankCheckBox.Location.Y + (startBlankCheckBox.Size.Height / 4)),
                TextStyle =
                {
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                }
            };

            Button newMapButton = new Button("setupNewMapButton")
            {
                Size = new Size(160, 40),
                Location = new Point(260, 130),
                Skin = MiyagiResources.Singleton.Skins["ButtonStandardSkin"],
                Text = "New Map",
                TextStyle =
                {
                    Alignment = Alignment.MiddleCenter,
                    ForegroundColour = Colours.DarkBlue
                }
            };
            newMapButton.MouseDown += NewMapButtonMouseDown;

            Panel loadMapPanel = new Panel("setupLoadMapPanel")
            {
                Location = new Point(32, 320),
                Size = new Size(400, 70),
                Skin = MiyagiResources.Singleton.Skins["PanelSkin"],
                Opacity = 0.5f,
                Enabled = false
            };

            TextBox mapNameTextBox = new TextBox("setupMapNameTextBox")
            {
                Size = new Size(160, 32),
                Location = new Point(50, 340),
                Padding = new Thickness(2, 2, 2, 2),
                ToolTipText = "Name of map to load.",
                Text = "Map Name",
                TextStyle =
                {
                    Alignment = Alignment.MiddleLeft,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                },
                Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
            };

            Button loadMapButton = new Button("setupLoadMapButton")
            {
                Size = new Size(160, 40),
                Location = new Point(230, 340),
                Skin = MiyagiResources.Singleton.Skins["ButtonStandardSkin"],
                Text = "Load Map",
                TextStyle =
                {
                    Alignment = Alignment.MiddleCenter,
                    ForegroundColour = Colours.DarkBlue
                }
            };
            loadMapButton.MouseDown += LoadMapButtonMouseDown;

            guiEditOptions = new GUI("guiEditOptions");
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(guiEditOptions);

            guiEditOptions.Controls.Add(returnButton);
            guiEditOptions.Controls.Add(mapWidthTextBox);
            guiEditOptions.Controls.Add(mapWidthLabel);
            guiEditOptions.Controls.Add(mapHeightTextBox);
            guiEditOptions.Controls.Add(mapHeightLabel);
            guiEditOptions.Controls.Add(partitionSizeTextBox);
            guiEditOptions.Controls.Add(partitionSizeLabel);
            guiEditOptions.Controls.Add(wallSurroundCheckBox);
            guiEditOptions.Controls.Add(wallSurroundLabel);
            guiEditOptions.Controls.Add(startBlankCheckBox);
            guiEditOptions.Controls.Add(startBlankLabel);
            guiEditOptions.Controls.Add(newMapButton);

            guiEditOptions.Controls.Add(mapNameTextBox);
            guiEditOptions.Controls.Add(loadMapButton);

            guiEditOptions.Controls.Add(newMapPanel);
            guiEditOptions.Controls.Add(loadMapPanel);

            newMapPanel.SendToBack();
            loadMapPanel.SendToBack();
        }

        private void ReturnButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            mStateMgr.RequestStateChange(typeof(MainMenu));
        }

        private void NewMapButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            int mapWidth = int.Parse(((TextBox)guiEditOptions.GetControl("setupMapWidthTextBox")).Text);
            int mapHeight = int.Parse(((TextBox)guiEditOptions.GetControl("setupMapHeightTextBox")).Text);
            int partitionSize = int.Parse(((TextBox)guiEditOptions.GetControl("setupPartitionSizeTextBox")).Text);
            bool wallSurround = ((CheckBox)guiEditOptions.GetControl("setupWallSurroundCheckBox")).Checked;
            bool startBlank = ((CheckBox)guiEditOptions.GetControl("setupStartBlankCheckBox")).Checked;

            if (mapWidth < 1 || mapHeight < 1)
                return;

            if (map.InitMap(mapWidth, mapHeight, wallSurround, startBlank, partitionSize))
            {
                guiEditOptions.Visible = false;
                // We're going to be editing the map, so lets remove all these as we wont
                // be using them for a while (we expect!).
                mEngine.mMiyagiSystem.GUIManager.DisposeAllGUIs();
                AddEditorButtons();
                inEditor = true;
                mEngine.Camera.Position = new Mogre.Vector3(0, 300, 0);
                mEngine.Camera.LookAt(new Mogre.Vector3(160, 64, 160));
            }
        }

        private void LoadMapButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mapSaver.Load(((TextBox)guiEditOptions.GetControl("setupMapNameTextBox")).Text))
            {
                guiEditOptions.Visible = false;
                map.LoadMapSaverMap(mapSaver.mapWidth, mapSaver.mapHeight, mapSaver.nameArray);
                // We're going to be editing the map, so lets remove all these as we wont
                // be using them for a while (we expect!).
                mEngine.mMiyagiSystem.GUIManager.DisposeAllGUIs();
                AddEditorButtons();
                inEditor = true;
                mEngine.Camera.Position = new Mogre.Vector3(0, 300, 0);
                mEngine.Camera.LookAt(new Mogre.Vector3(160, 64, 160));
            }
        }
        #endregion

        // These are the buttons once we're actually in the editor.
        #region Editor Buttons

        private void AddEditorButtons()
        {
            guiEditScreen = new GUI("guiEditScreen");
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(guiEditScreen);

            #region Panels
            Panel topPanel = new Panel("editTopPanel")
            {
                Location = new Point(0, 0),
                Size = new Size(1024, 50),
                Skin = MiyagiResources.Singleton.Skins["PanelSkin"],
                Opacity = 0.8f,
                Movable = false,
                Enabled = false
            };

            Panel optionsPanel = new Panel("editOptionsPanel")
            {
                Location = new Point(0, 0),
                Size = new Size(1024, 50),
                Skin = MiyagiResources.Singleton.Skins["PanelSkin"],
                Visible = false,
                Enabled = false,
                Movable = false,
                AlwaysOnTop = true
            };

            Panel infoPanel = new Panel("editInfoPanel")
            {
                Location = new Point(0, 50),
                Size = new Size(200, 200),
                Skin = MiyagiResources.Singleton.Skins["PanelSkin"],
                AlwaysOnTop = true,
                Movable = true,
                ResizeMode = ResizeModes.None,
                Opacity = 0.5f
            };
            #endregion

            #region Options panel controls
            Button hideButton = new Button("editHideButton")
            {
                Location = new Point(5, 5),
                Size = new Size(160, 40),
                Skin = MiyagiResources.Singleton.Skins["ButtonStandardSkin"],
                Text = "Hide",
                TextStyle =
                {
                    Alignment = Alignment.MiddleCenter,
                    ForegroundColour = Colours.DarkBlue
                },
                AlwaysOnTop = true
            };
            hideButton.MouseDown += HideButtonMouseDown;


            Button returnButton = new Button("editReturnButton")
            {
                Location = new Point(510, 5),
                Size = new Size(160, 40),
                Skin = MiyagiResources.Singleton.Skins["ButtonStandardSkin"],
                Text = "Main Menu",
                TextStyle =
                {
                    Alignment = Alignment.MiddleCenter,
                    ForegroundColour = Colours.DarkBlue
                },
                AlwaysOnTop = true
            };
            returnButton.MouseDown += ReturnButtonMouseDown;

            Button saveButton = new Button("editSaveButton")
            {
                Location = new Point(170, 5),
                Size = new Size(160, 40),
                Skin = MiyagiResources.Singleton.Skins["ButtonStandardSkin"],
                Text = "Save",
                TextStyle =
                {
                    Alignment = Alignment.MiddleCenter,
                    ForegroundColour = Colours.DarkBlue
                },
                AlwaysOnTop = true
            };
            saveButton.MouseDown += SaveButtonMouseDown;

            Button lightingButton = new Button("editLightingButton")
            {
                Location = new Point(340, 5),
                Size = new Size(160, 40),
                Skin = MiyagiResources.Singleton.Skins["ButtonStandardSkin"],
                Text = "Lighting",
                TextStyle =
                {
                    Alignment = Alignment.MiddleCenter,
                    ForegroundColour = Colours.DarkBlue
                },
                AlwaysOnTop = true
            };
            lightingButton.MouseDown += LightingButtonMouseDown;
            #endregion

            #region Top panel controls
            Button showButton = new Button("editShowButton")
            {
                Location = new Point(5, 5),
                Size = new Size(160, 40),
                Skin = MiyagiResources.Singleton.Skins["ButtonStandardSkin"],
                Text = "Options",
                TextStyle =
                {
                    Alignment = Alignment.MiddleCenter,
                    ForegroundColour = Colours.DarkBlue
                }
            };
            showButton.MouseDown += ShowButtonMouseDown;

            DropDownList tileDropdown = new DropDownList("editTileDropdown")
            {
                Location = new Point(200, 5),
                Size = new Size(175, 40),
                Skin = MiyagiResources.Singleton.Skins["DropDownListStandardSkin"],
                DropDownSize = new Size(175, 140),
                TextureFiltering = TextureFiltering.Anisotropic,
                Text = "LMB",
                ListStyle = 
                {
                    ItemOffset = new Point(10, 0),
                    Alignment = Alignment.MiddleLeft,
                    MaxVisibleItems = 4,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"],
                    MultiSelect = false,
                    ScrollBarStyle =
                    {
                        Extent = 15, //Width of the scrollbar, if youre wondering.
                        ThumbStyle =
                        {
                            BorderStyle =
                            {
                                Thickness = new Thickness(2, 2, 2, 2)
                            }
                        }
                    },
                },
                TextStyle =
                {
                    Offset = new Point(10, 0),
                    Alignment = Alignment.MiddleLeft,
                    ForegroundColour = Colours.DarkBlue,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                }
            };
            tileDropdown.SelectedIndexChanged += tileDropdown_SelectedIndexChanged;

            foreach (TileType type in Enum.GetValues(typeof(TileType)))
            {
                tileDropdown.Items.Add(type.ToString());
            }

            DropDownList itemDropDown = new DropDownList("editItemDropDown")
            {
                Location = new Point(380, 5),
                Size = new Size(175, 40),
                Skin = MiyagiResources.Singleton.Skins["DropDownListStandardSkin"],
                DropDownSize = new Size(175, 140),
                TextureFiltering = TextureFiltering.Anisotropic,
                Text = "RMB",
                ListStyle =
                {
                    ItemOffset = new Point(10, 0),
                    Alignment = Alignment.MiddleLeft,
                    MaxVisibleItems = 4,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"],
                    MultiSelect = false,
                    ScrollBarStyle =
                    {
                        Extent = 15, //Width of the scrollbar, if youre wondering.
                        ThumbStyle =
                        {
                            BorderStyle =
                            {
                                Thickness = new Thickness(2, 2, 2, 2)
                            }
                        }
                    },
                },
                TextStyle =
                {
                    Offset = new Point(10, 0),
                    Alignment = Alignment.MiddleLeft,
                    ForegroundColour = Colours.DarkBlue,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                }
            };
            itemDropDown.SelectedIndexChanged += itemDropDown_SelectedIndexChanged;

            foreach (ItemType type in Enum.GetValues(typeof(ItemType)))
            {
                itemDropDown.Items.Add(type.ToString());
            }

            #endregion

            #region Info panel control
            Label itemName = new Label("editItemName")
            {
                Location = new Point(5, 5),
                AutoSize = true,
                Text = "No selection",
                TextStyle =
                {
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                }
            };

            TextBox itemX = new TextBox("editItemX")
            {
                Size = new Size(60, 30),
                Location = new Point(5, 30),
                Padding = new Thickness(1, 1, 1, 1),
                ToolTipText = "X position",
                ToolTipStyle =
                {
                    HoverDuration = System.TimeSpan.Zero
                },
                Text = "X",
                TextStyle =
                {
                    Alignment = Alignment.MiddleLeft,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                },
                TextBoxStyle =
                {
                    DigitOnly = true
                },
                Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
            };
            itemX.LostFocus += itemXTextBoxChanged;

            TextBox itemY = new TextBox("editItemY")
            {
                Size = new Size(60, 30),
                Location = new Point(70, 30),
                Padding = new Thickness(1, 1, 1, 1),
                ToolTipText = "Y position",
                ToolTipStyle =
                {
                    HoverDuration = System.TimeSpan.Zero
                },
                Text = "Y",
                TextStyle =
                {
                    Alignment = Alignment.MiddleLeft,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                },
                TextBoxStyle =
                {
                    DigitOnly = true
                },
                Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
            };
            itemY.LostFocus += itemYTextBoxChanged;

            TextBox itemZ = new TextBox("editItemZ")
            {
                Size = new Size(60, 30),
                Location = new Point(135, 30),
                Padding = new Thickness(1, 1, 1, 1),
                ToolTipText = "Z position",
                ToolTipStyle =
                {
                    HoverDuration = System.TimeSpan.Zero
                },
                Text = "Z",
                TextStyle =
                {
                    Alignment = Alignment.MiddleLeft,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                },
                TextBoxStyle =
                {
                    DigitOnly = true
                },
                Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
            };
            itemZ.LostFocus += itemZTextBoxChanged;

            TextBox itemPitch = new TextBox("editItemPitch")
            {
                Size = new Size(60, 30),
                Location = new Point(5, 65),
                Padding = new Thickness(1, 1, 1, 1),
                ToolTipText = "Pitch",
                ToolTipStyle =
                {
                    HoverDuration = System.TimeSpan.Zero
                },
                Text = "P",
                TextStyle =
                {
                    Alignment = Alignment.MiddleLeft,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                },
                TextBoxStyle =
                {
                    DigitOnly = true
                },
                Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
            };
            itemPitch.LostFocus += itemPitchTextBoxChanged;

            TextBox itemYaw = new TextBox("editItemYaw")
            {
                Size = new Size(60, 30),
                Location = new Point(70, 65),
                Padding = new Thickness(1, 1, 1, 1),
                ToolTipText = "Yaw",
                ToolTipStyle =
                {
                    HoverDuration = System.TimeSpan.Zero
                },
                Text = "Y",
                TextStyle =
                {
                    Alignment = Alignment.MiddleLeft,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                },
                TextBoxStyle =
                {
                    DigitOnly = true
                },
                Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
            };
            itemYaw.LostFocus += itemYawTextBoxChanged;

            TextBox itemRoll = new TextBox("editItemRoll")
            {
                Size = new Size(60, 30),
                Location = new Point(135, 65),
                Padding = new Thickness(1, 1, 1, 1),
                ToolTipText = "Roll",
                ToolTipStyle =
                {
                    HoverDuration = System.TimeSpan.Zero
                },
                Text = "R",
                TextStyle =
                {
                    Alignment = Alignment.MiddleLeft,
                    Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
                },
                TextBoxStyle =
                {
                    DigitOnly = true
                },
                Skin = MiyagiResources.Singleton.Skins["TextBoxSkin"]
            };
            itemRoll.LostFocus += itemRollTextBoxChanged;

            Button deleteButton = new Button("editDeleteButton")
            {
                Location = new Point(5, 100),
                Size = new Size(160, 30),
                Skin = MiyagiResources.Singleton.Skins["ButtonStandardSkin"],
                Text = "Delete",
                TextStyle =
                {
                    Alignment = Alignment.MiddleCenter,
                    ForegroundColour = Colours.DarkBlue
                }
            };
            deleteButton.MouseDown += DeleteButtonMouseDown;
            #endregion

            #region Control adding
            guiEditScreen.Controls.Add(topPanel);
            guiEditScreen.Controls.Add(optionsPanel);
            guiEditScreen.Controls.Add(showButton);
            guiEditScreen.Controls.Add(tileDropdown);
            guiEditScreen.Controls.Add(itemDropDown);
            guiEditScreen.Controls.Add(infoPanel);

            optionsPanel.Controls.Add(hideButton);
            optionsPanel.Controls.Add(returnButton);
            optionsPanel.Controls.Add(saveButton);
            optionsPanel.Controls.Add(lightingButton);

            infoPanel.Controls.Add(itemName);
            infoPanel.Controls.Add(itemX);
            infoPanel.Controls.Add(itemY);
            infoPanel.Controls.Add(itemZ);
            infoPanel.Controls.Add(itemPitch);
            infoPanel.Controls.Add(itemYaw);
            infoPanel.Controls.Add(itemRoll);
            infoPanel.Controls.Add(deleteButton);
            #endregion
        }

        #region Button event handlers

        private void tileDropdown_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddList = (DropDownList)sender;
            newTileType = (TileType)ddList.SelectedIndex;
        }

        private void itemDropDown_SelectedIndexChanged(object sender, EventArgs e)
        {
            DropDownList ddList = (DropDownList)sender;
            newItemType = (ItemType)ddList.SelectedIndex;
        }

        private void SaveButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            mapSaver.Save("MapSave");
        }

        private void LightingButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (mEngine.SceneMgr.AmbientLight == ColourValue.White)
            {
                mEngine.SceneMgr.AmbientLight = new ColourValue(0.1f, 0.1f, 0.1f);
            }
            else
            {
                mEngine.SceneMgr.AmbientLight = ColourValue.White;
            }
        }

        private void ShowButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            guiEditScreen.GetControl("editOptionsPanel").Visible = true;
            guiEditScreen.GetControl("editOptionsPanel").Enabled = true;
        }

        private void HideButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            guiEditScreen.GetControl("editOptionsPanel").Visible = false;
            guiEditScreen.GetControl("editOptionsPanel").Enabled = false;
        }

        private void itemXTextBoxChanged(object sender, EventArgs e)
        {
            if (selectedItem != null && ((TextBox)sender).Text.Length > 0)
            {
                float x;
                if (float.TryParse(((TextBox)sender).Text, out x))
                {
                    Mogre.Vector3 pos = selectedItem.Node.Position;
                    selectedItem.Node.SetPosition(x, pos.y, pos.z);
                }
            }
        }

        private void itemYTextBoxChanged(object sender, EventArgs e)
        {
            if (selectedItem != null && ((TextBox)sender).Text.Length > 0)
            {
                float y;
                if (float.TryParse(((TextBox)sender).Text, out y))
                {
                    Mogre.Vector3 pos = selectedItem.Node.Position;
                    selectedItem.Node.SetPosition(pos.x, y, pos.z);
                }
            }
        }

        private void itemZTextBoxChanged(object sender, EventArgs e)
        {
            if (selectedItem != null && ((TextBox)sender).Text.Length > 0)
            {
                float z;
                if(float.TryParse(((TextBox)sender).Text, out z))
                {
                    Mogre.Vector3 pos = selectedItem.Node.Position;
                    selectedItem.Node.SetPosition(pos.x, pos.y, z);
                }
            }
        }

        private void itemPitchTextBoxChanged(object sender, EventArgs e)
        {
            if (selectedItem != null && ((TextBox)sender).Text.Length > 0)
            {
                float pitch;
                if (float.TryParse(((TextBox)sender).Text, out pitch))
                {
                    pitch = Mogre.Math.DegreesToRadians(pitch);
                    selectedItem.Node.Pitch(pitch);
                }
            }
        }

        private void itemYawTextBoxChanged(object sender, EventArgs e)
        {
            if (selectedItem != null && ((TextBox)sender).Text.Length > 0)
            {
                float yaw;
                if (float.TryParse(((TextBox)sender).Text, out yaw))
                {
                    yaw = Mogre.Math.DegreesToRadians(yaw);
                    selectedItem.Node.Yaw(yaw);
                }
            }
        }

        private void itemRollTextBoxChanged(object sender, EventArgs e)
        {
            if (selectedItem != null && ((TextBox)sender).Text.Length > 0)
            {
                float roll;
                if (float.TryParse(((TextBox)sender).Text, out roll))
                {
                    roll = Mogre.Math.DegreesToRadians(roll);
                    selectedItem.Node.Roll(roll);
                }
            }
        }

        private void DeleteButtonMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (selectedItem != null)
            {
                mEngine.SceneMgr.DestroySceneNode(selectedItem.Node);
                mEngine.SceneMgr.DestroyEntity(selectedItem.Entity);
                selectedItem = null;
            }
        }
        #endregion
        #endregion


        #region Input
        public override void UpdateInput(Mogre.FrameEvent evt, MOIS.Keyboard keyState, MOIS.Mouse mouseState)
        {
            if (mouseState.MouseState.ButtonDown(MOIS.MouseButtonID.MB_Right))
            {
                if (keyState.IsKeyDown(MOIS.KeyCode.KC_RIGHT) || keyState.IsKeyDown(MOIS.KeyCode.KC_D))
                {
                    mEngine.Camera.Yaw(-0.015f);
                }
                else if (keyState.IsKeyDown(MOIS.KeyCode.KC_LEFT) || keyState.IsKeyDown(MOIS.KeyCode.KC_A))
                {
                    mEngine.Camera.Yaw(0.015f);
                }
                if (keyState.IsKeyDown(MOIS.KeyCode.KC_UP) || keyState.IsKeyDown(MOIS.KeyCode.KC_W))
                {
                    mEngine.Camera.MoveRelative(new Mogre.Vector3(0, 0, -2f));
                }
                else if (keyState.IsKeyDown(MOIS.KeyCode.KC_DOWN) || keyState.IsKeyDown(MOIS.KeyCode.KC_S))
                {
                    mEngine.Camera.MoveRelative(new Mogre.Vector3(0, 0, 2f));
                }
            }
                
            else
            {
                if (keyState.IsKeyDown(MOIS.KeyCode.KC_RIGHT) || keyState.IsKeyDown(MOIS.KeyCode.KC_D))
                {
                    mEngine.Camera.MoveRelative(new Mogre.Vector3(3f, 0, 0));
                }
                else if (keyState.IsKeyDown(MOIS.KeyCode.KC_LEFT) || keyState.IsKeyDown(MOIS.KeyCode.KC_A))
                {
                    mEngine.Camera.MoveRelative(new Mogre.Vector3(-3f, 0, 0));
                }
                if (keyState.IsKeyDown(MOIS.KeyCode.KC_UP) || keyState.IsKeyDown(MOIS.KeyCode.KC_W))
                {
                    mEngine.Camera.MoveRelative(new Mogre.Vector3(0, 3f, -3f));
                }
                else if (keyState.IsKeyDown(MOIS.KeyCode.KC_DOWN)|| keyState.IsKeyDown(MOIS.KeyCode.KC_S))
                {
                    mEngine.Camera.MoveRelative(new Mogre.Vector3(0, -3f, 3f));
                }

            }

        }

        public override void KeyDown(MOIS.KeyEvent keyState)
        {
            if (keyState.key == MOIS.KeyCode.KC_RSHIFT)
            {
                guiEditScreen.Resize(0.5, 0.5);
                guiEditScreen.Update();
                mEngine.SceneMgr.ShowBoundingBoxes = !mEngine.SceneMgr.ShowBoundingBoxes;
            }
        }

        public override void KeyUp(MOIS.KeyEvent keyState)
        {
        }

        public override void MouseUp(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
        {
        }

        public override void MouseDown(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
        {
            // Don't affect the map if we're in the top of the screen as our buttons
            // are up there.
            Point mousePos = mEngine.mMiyagiSystem.InputManager.MouseLocation;
            Mogre.Vector2 mousePosAbs = new Vector2((float)mousePos.X / (float)mEngine.Window.Width, (float)mousePos.Y / (float)mEngine.Window.Height);
            if (mousePos.Y < 60)
            {
                return;
            }

            if (inEditor)
            {
                Mogre.Vector3 worldPos;
                AtomBaseClass SelectedObject = HelperClasses.AtomUtil.PickAtScreenPosition(mEngine, mousePosAbs, out worldPos); 

                if (SelectedObject == null)
                {
                    return;
                }

                if (button == MOIS.MouseButtonID.MB_Left)
                {
                    if (newTileType != TileType.None && SelectedObject.AtomType == AtomType.Tile)
                    {
                        Vector2 tilePos = map.GetTileArrayPositionFromWorldPosition(SelectedObject.Node.Position.x, SelectedObject.Node.Position.z);

                        if (!mEngine.mNetworkMgr.isConnected)
                        {
                            map.ChangeTile(tilePos, newTileType);
                        }
                        else
                        {
                            mEngine.mNetworkMgr.SendChangeTile((int)tilePos.x, (int)tilePos.y, newTileType);
                        }
                    }
                }
                else if (button == MOIS.MouseButtonID.MB_Right)
                {
                    if (newItemType != ItemType.None)
                    {
                        //Mogre.Vector3 position = new Mogre.Vector3(SelectedObject.Node.Position.x, 1, SelectedObject.Node.Position.z);
                        Mogre.Vector3 position = worldPos + new Mogre.Vector3(0, 1, 0);

                        if (mEngine.mNetworkMgr.isConnected)
                        {
                            itemManager.SendCreateItem(position);
                        }
                        else
                        {
                            SS3D_shared.Crowbar crowbar = new SS3D_shared.Crowbar(mEngine.SceneMgr, position, lastID);
                            lastID++;
                            crowbar.Node.CreateChildSceneNode().AttachObject(mEngine.SceneMgr.CreateParticleSystem("Gas" + lastID, "gasTemplate"));
                            ParticleSystem.DefaultNonVisibleUpdateTimeout = 5;
                        }
                    }
                    else
                    {
                        if (SelectedObject.AtomType == AtomType.Item || SelectedObject.AtomType == AtomType.Object)
                        {
                            if (selectedItem != null)
                            {
                                selectedItem.Node.ShowBoundingBox = false;
                            }
                            selectedItem = null;
                            Label itemName = (Label)guiEditScreen.GetControl("itemName");
                            itemName.Text = SelectedObject.AtomType.ToString();
                            TextBox itemPosition = (TextBox)guiEditScreen.GetControl("itemX");
                            itemPosition.Text = SelectedObject.Node.Position.x.ToString();
                            itemPosition = (TextBox)guiEditScreen.GetControl("itemY");
                            itemPosition.Text = SelectedObject.Node.Position.y.ToString();
                            itemPosition = (TextBox)guiEditScreen.GetControl("itemZ");
                            itemPosition.Text = SelectedObject.Node.Position.z.ToString();

                            itemPosition = (TextBox)guiEditScreen.GetControl("itemPitch");
                            itemPosition.Text = SelectedObject.Node.Orientation.Pitch.ValueDegrees.ToString();
                            itemPosition = (TextBox)guiEditScreen.GetControl("itemYaw");
                            itemPosition.Text = SelectedObject.Node.Orientation.Yaw.ValueDegrees.ToString();
                            itemPosition = (TextBox)guiEditScreen.GetControl("itemRoll");
                            itemPosition.Text = SelectedObject.Node.Orientation.Roll.ValueDegrees.ToString();

                            selectedItem = SelectedObject;
                            selectedItem.Node.ShowBoundingBox = true;
                        }
                    }
                }

            }
        }

        public override void MouseMove(MOIS.MouseEvent mouseState)
        {
        }

        #endregion

    }

}
