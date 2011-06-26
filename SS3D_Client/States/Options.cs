using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using Mogre;

using SS3D.Modules;

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
  public class OptionsMenu : State
  {
    private StateManager mStateMgr;
    private Dictionary<string, KeyValuePair<uint, uint>> possible_resolutions;
    private GUI guiOptionsMenu;

    private uint res_width = 0;
    private uint res_heigth = 0;

    private bool changed = false;

    public OptionsMenu()
    {
      mEngine = null;
    }

    #region Startup, Shutdown, Update
    public override bool Startup(StateManager _mgr)
    {
        mEngine = _mgr.Engine;
        mStateMgr = _mgr;

        // Lets make sure the background is visible
        if (mEngine.mMiyagiSystem.GUIManager.GetGUI("guiBackground") != null)
        {
            mEngine.mMiyagiSystem.GUIManager.GetGUI("guiBackground").Visible = true;
        }
        // If we've been here before, lets just use that menu and not recreated it
        if (mEngine.mMiyagiSystem.GUIManager.GetGUI("guiOptionsMenu") == null)
        {
            possible_resolutions = GetResolutions();
            guiOptionsMenu = new GUI("guiOptionsMenu");
            guiOptionsMenu.ZOrder = 10;
            mEngine.mMiyagiSystem.GUIManager.GUIs.Add(guiOptionsMenu);
            CreateMenu();
            guiOptionsMenu.Resize(mEngine.ScalarX, mEngine.ScalarY);
        }
        else
        {
            guiOptionsMenu = mEngine.mMiyagiSystem.GUIManager.GetGUI("guiOptionsMenu");
            guiOptionsMenu.Fade(0, 1, 100);

            CheckBox cbFullscreen = (CheckBox)mEngine.mMiyagiSystem.GUIManager.GetControl("optionsFullscreenCheckbox");
            cbFullscreen.Checked = ConfigManager.Singleton.Configuration.Fullscreen;

            DropDownList resolutionsDropdown = (DropDownList)mEngine.mMiyagiSystem.GUIManager.GetControl("optionsResolutionsDropdown");
            resolutionsDropdown.Text = ConfigManager.Singleton.Configuration.DisplayWidth.ToString() + " x " + ConfigManager.Singleton.Configuration.DisplayHeight.ToString();

        }
        
        return true;
    }
    private void CreateMenu()
    {

        Panel passportPanel = new Panel("optionsPassportPanel")
        {
            Location = new Point(0, 0),
            Size = new Size(866, 768),
            ResizeMode = ResizeModes.None,
            Skin = MiyagiResources.Singleton.Skins["PassportOverlay"],
            AlwaysOnTop = false,
            TextureFiltering = TextureFiltering.Anisotropic,
            Enabled = false
        };

        Panel ticketPanel = new Panel("optionsTicketPanel")
        {
            Location = new Point(0, 256),
            Size = new Size(740, 356),
            ResizeMode = ResizeModes.None,
            Skin = MiyagiResources.Singleton.Skins["TicketOverlay"],
            AlwaysOnTop = false,
            TextureFiltering = TextureFiltering.Anisotropic,
            Enabled = false
        };

        Button returnButton = new Button("optionsReturnButton")
        {
            Location = new Point(580, 350),
            Size = new Size(120, 30),
            Skin = MiyagiResources.Singleton.Skins["ButtonSkinLogo"],
            Text = "Return",
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
                ForegroundColour = Colours.DarkBlue,
                Font = MiyagiResources.Singleton.Fonts["SpacedockStencil"]
            }
        };

        Button applyButton = new Button("optionsApplyButton")
        {
            Location = new Point(580, 300),
            Size = new Size(120, 30),
            Skin = MiyagiResources.Singleton.Skins["ButtonSkinLogo"],
            Text = "Save",
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
                ForegroundColour = Colours.DarkBlue,
                Font = MiyagiResources.Singleton.Fonts["SpacedockStencil"]
            }
        };

        CheckBox fullscreenCheckbox = new CheckBox("optionsFullscreenCheckbox")
        {
            Location = new Point(200, 410),
            Size = new Size(30, 30),
            Skin = MiyagiResources.Singleton.Skins["CheckboxSkin"],
            TextureFiltering = TextureFiltering.Anisotropic,
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
                ForegroundColour = Colours.DarkBlue,
            }
        };

        Label fullscreenCheckboxLabel = new Label("optionsFullscreenCheckboxLabel")
        {
            Location = new Point(fullscreenCheckbox.Location.X + fullscreenCheckbox.Size.Width + 10, fullscreenCheckbox.Location.Y + (fullscreenCheckbox.Size.Height / 4)),
            Text = "Fullscreen",
            AutoSize = true,
            TextStyle = 
            {
                Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
            }
        };

        DropDownList resolutionsDropdown = new DropDownList("optionsResolutionsDropdown")
        {
            Location = new Point(20, 410),
            Size = new Size(175, 30),
            Skin = MiyagiResources.Singleton.Skins["DropDownListRedSkin"],
            DropDownSize = new Size(175, 140),
            TextureFiltering =  TextureFiltering.Anisotropic, 
            ListStyle =
            {
                ItemOffset = new Point(10,0),
                Alignment= Alignment.MiddleLeft,
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

        DropDownList fsaaDropdown = new DropDownList("optionsFsaaDropdown")
        {
            Size = new Size(175, 30),
            Location = new Point(20, 450),
            Skin = MiyagiResources.Singleton.Skins["DropDownListRedSkin"],
            DropDownSize = new Size(175, 140),
            TextureFiltering = TextureFiltering.Anisotropic,
            ListStyle =
            {
                ItemOffset = new Point(10, 0),
                Alignment = Alignment.MiddleLeft,
                MaxVisibleItems = 4,
                Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"],
                MultiSelect = false,
                ScrollBarStyle =
                {
                    Extent = 15,
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
        Label fsaaDropdownLabel = new Label("optionsFsaaDropdownLabel")
        {
            Location = new Point(fsaaDropdown.Location.X + fsaaDropdown.Size.Width + 10, fsaaDropdown.Location.Y + (fsaaDropdown.Size.Height / 4)),
            Text = "FSAA",
            AutoSize = true,
            TextStyle =
            {
                Font = MiyagiResources.Singleton.Fonts["SpacedockStencilSmall"]
            }
        };

        string[] fsaaOptions = { "0", "2", "4", "8", "16"};
        fsaaDropdown.Items.AddRange(fsaaOptions);
        string[] selectedOpt = { mEngine.Window.FSAA.ToString() };
        fsaaDropdown.SelectItems(selectedOpt);
        fsaaDropdown.SelectedIndexChanged += new EventHandler(fsaaDropdown_SelectedIndexChanged);

        returnButton.MouseDown += ReturnButtonMouseDown;

        applyButton.MouseDown += ApplyButtonMouseDown;

        fullscreenCheckbox.CheckedChanged += fullscreenCheckboxChanged;
        fullscreenCheckbox.Checked = ConfigManager.Singleton.Configuration.Fullscreen;

        resolutionsDropdown.SelectedIndexChanged += new EventHandler(resolutionsDropdown_SelectedIndexChanged);
        resolutionsDropdown.Text = ConfigManager.Singleton.Configuration.DisplayWidth.ToString() + " x " + ConfigManager.Singleton.Configuration.DisplayHeight.ToString();

        resolutionsDropdown.Items.AddRange(possible_resolutions.Keys.ToArray<string>());
        guiOptionsMenu.Controls.Add(passportPanel);
        guiOptionsMenu.Controls.Add(ticketPanel);
        guiOptionsMenu.Controls.Add(applyButton);
        guiOptionsMenu.Controls.Add(fullscreenCheckbox);
        guiOptionsMenu.Controls.Add(fullscreenCheckboxLabel);
        guiOptionsMenu.Controls.Add(returnButton);
        guiOptionsMenu.Controls.Add(resolutionsDropdown);
        guiOptionsMenu.Controls.Add(fsaaDropdown);
        guiOptionsMenu.Controls.Add(fsaaDropdownLabel);
    }

    private Dictionary<string, KeyValuePair<uint, uint>> GetResolutions()
    {

        List<string> resolutions_raw = new List<string>();

        ConfigOptionMap conMap = mEngine.Root.RenderSystem.GetConfigOptions();

        foreach (KeyValuePair<string, ConfigOption_NativePtr> pair in conMap)
        {
            if (pair.Key.Equals("Video Mode"))
            {

                StringVector resolutions = pair.Value.possibleValues;

                foreach (string possible in resolutions)
                {
                    if (possible.Contains("@ 16")) continue;
                    resolutions_raw.Add(possible);
                }
            }
        }

        //This stuff is very ugly but i dont give a shit.
        resolutions_raw.Remove("640 x 480 @ 32-bit colour");
        resolutions_raw.Remove("720 x 480 @ 32-bit colour");
        resolutions_raw.Remove("720 x 576 @ 32-bit colour");
        resolutions_raw.Remove("800 x 600 @ 32-bit colour");

        Dictionary<string, KeyValuePair<uint, uint>> res_lookup = new Dictionary<string, KeyValuePair<uint, uint>>();

        foreach (string res in resolutions_raw)
        {
            string [] build;
            build = res.Split(new Char[] { ' ' });
            string refined = build[0] + " x " + build[2];
            res_lookup.Add(refined, new KeyValuePair<uint, uint>(uint.Parse(build[0]), uint.Parse(build[2])));
        }

        return res_lookup;
    }

    private void ApplyButtonMouseDown(object sender, MouseButtonEventArgs e)
    {
        DropDownList DDFsaa = (DropDownList)mEngine.mMiyagiSystem.GUIManager.GetControl("optionsFsaaDropdown");
        ConfigManager.Singleton.Configuration.FSAA = Convert.ToInt32(DDFsaa.SelectedItem.Text);

        CheckBox CBFullscreen = (CheckBox)mEngine.mMiyagiSystem.GUIManager.GetControl("optionsFullscreenCheckbox");
        ConfigManager.Singleton.Configuration.Fullscreen = CBFullscreen.Checked;

        if (res_heigth != 0 && res_width != 0)
        {
            ConfigManager.Singleton.Configuration.DisplayWidth = res_width;
            ConfigManager.Singleton.Configuration.DisplayHeight = res_heigth;
        }

        ConfigManager.Singleton.Save();

        if (changed)
        {
            DialogResult result; //Where DO THE GRAPHICS FOR THIS STUFF COME FROM. FUCK.
            result = DialogBox.Show("Some of the changes require a restart."+Environment.NewLine+"Would you like to restart now?", "Restart?", DialogBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                mEngine.Window.Destroy(); //This is really fucking ugly and unstable. But i really have no better idea.
                System.Windows.Forms.Application.Restart();
            }
        }

    }

    void resolutionsDropdown_SelectedIndexChanged(object sender, EventArgs e)
    {
        KeyValuePair<uint, uint> picked_res;
        DropDownList ddList = (DropDownList)sender;
        if (!possible_resolutions.TryGetValue(ddList.SelectedItem.Text, out picked_res)) mEngine.Window.Destroy();
        res_width = picked_res.Key;
        res_heigth = picked_res.Value;
        changed = true;
    }

    void fsaaDropdown_SelectedIndexChanged(object sender, EventArgs e)
    {
        changed = true;
    }

    private void fullscreenCheckboxChanged(object sender, EventArgs e)
    {
        changed = true;
    }

    private void ReturnButtonMouseDown(object sender, MouseButtonEventArgs e)
    {
        mStateMgr.RequestStateChange(typeof(MainMenu));
    }

    public override void Shutdown()
    {
        mEngine.mMiyagiSystem.GUIManager.GetGUI("guiOptionsMenu").Fade(1, 0, 100);
    }

    public override void Update(long _frameTime)
    {
    } 
    #endregion

    #region Input
    public override void UpdateInput(Mogre.FrameEvent evt, MOIS.Keyboard keyState, MOIS.Mouse mouseState)
    {
    }

    public override void KeyDown(MOIS.KeyEvent keyState)
    {
    }

    public override void KeyUp(MOIS.KeyEvent keyState)
    {
    }

    public override void MouseUp(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
    {
    }

    public override void MouseDown(MOIS.MouseEvent mouseState, MOIS.MouseButtonID button)
    {
    }

    public override void MouseMove(MOIS.MouseEvent mouseState)
    {
    }

    #endregion

  }

}
