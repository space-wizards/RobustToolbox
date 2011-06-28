using System;

using Mogre;
using Lidgren.Network;

using SS3D.Modules;
using SS3D.Modules.Map;
using SS3D.Modules.Network;
using SS3D.Modules.UI;
using SS3D.Atom;

using SS3D_shared;

using Miyagi;
using Miyagi.UI;
using Miyagi.UI.Controls;
using Miyagi.Common;
using Miyagi.Common.Data;
using Miyagi.Common.Animation;
using Miyagi.Common.Resources;
using Miyagi.Common.Events;
using Miyagi.TwoD;


public class SpeechBubble : GUI
{
    MiyagiSystem mMiyagiMgr;
    OgreManager mEngine;
    MovableObject host;

    public Vector2 offset = new Vector2(0f, -40f);

    Panel mainBubble;
    Panel addBubble;
    Label Text;

    private const string defaultBubbleSkin = "ChatbubbleSkin";
    private const string defaultBubbleTailSkin = "ChatbubbleTailSkin";

    private float Opacity = 0;
    public float opacity
    {
        get
        {
            return Opacity;
        }

        private set
        {
            this.Opacity = value;
            this.mainBubble.Opacity = value;
            this.addBubble.Opacity = value;
            this.Text.Opacity = value;
        }
    }

    private DateTime removeAt;

    ~SpeechBubble()
    {
        mMiyagiMgr = null;
        mEngine = null;
        host = null;
        mainBubble = null;
        addBubble = null;
        Text = null;
    }

    public SpeechBubble(OgreManager ogremgr, MovableObject ihost)
    {
        mMiyagiMgr = ogremgr.mMiyagiSystem;
        mEngine = ogremgr;
        host = ihost;

        this.mainBubble = new Panel()
        {
            Location = new Point(0, 0),
            Size = new Size(0, 0),
            ResizeMode = ResizeModes.None,
            AutoSize = true,
            AutoSizeMode = Miyagi.UI.AutoSizeMode.GrowAndShrink,
            Padding = new Thickness(8, 15, 8, 15),
            HitTestVisible = false,
            Movable = false,
            TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
            Skin = MiyagiResources.Singleton.Skins[defaultBubbleSkin]
        };
        this.Controls.Add(mainBubble);

        this.addBubble = new Panel()
        {
            Location = new Point(0, 0),
            Size = new Size(11, 21),
            ResizeMode = ResizeModes.None,
            HitTestVisible = false,
            Movable = false,
            TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
            Skin = MiyagiResources.Singleton.Skins[defaultBubbleTailSkin]
        };
        this.Controls.Add(addBubble);

        this.Text = new Label()
        {
            Location = new Point(0, 0),
            Size = new Size(0, 0),
            AutoSize = true,
            AutoSizeMode = Miyagi.UI.AutoSizeMode.GrowAndShrink,
            Text = "",
            HitTestVisible = false,
            TextureFiltering = Miyagi.Common.TextureFiltering.Anisotropic,
            TextStyle =
            {
                Alignment = Alignment.MiddleCenter,
                ForegroundColour = Colours.Black
            }
        };
        mainBubble.Controls.Add(Text);

        Hide();

        this.Updating += new EventHandler(SpeechBubble_Updating);

        mMiyagiMgr.GUIManager.GUIs.Add(this);
    }

    public void Show(string text)
    {
        this.opacity = 100;
        //this.Visible = true;
        this.Text.Text = text;
        this.removeAt = DateTime.Now.AddMilliseconds(4000);
    }

    public void Show(string text, double durationMilli)
    {
        this.opacity = 100;
        //this.Visible = true;
        this.Text.Text = text;
        this.removeAt = DateTime.Now.AddMilliseconds(durationMilli);
    }

    public void Hide()
    {
        this.opacity = 0;
        //this.Visible = false;
    }

    public void ScheduleDispose() //Schedules removal after next update.
    {
        this.Updated += new EventHandler(SpeechBubble_DisposeNow);
    }

    private void SpeechBubble_DisposeNow(object sender, EventArgs e)
    {
        Dispose();
        mMiyagiMgr.GUIManager.GUIs.Remove(this);
    } //Used by ScheduleDispose()

    private void SpeechBubble_Updating(object sender, EventArgs e) //Updates position and visibility.
    {
        #region Host dispose check
        try
        {
            if (host.BoundingBox == null) return; //Since theres no good way of figuring out when the object is disposed. Ugly :(
        }
        catch (NullReferenceException)
        {
            ScheduleDispose();
            return;
        } 
        #endregion

        if ( (this.Visible && this.opacity > 0) && DateTime.Compare(DateTime.Now, removeAt) > 0)
        {
            Hide();
            return;
        }

        AxisAlignedBox objectAAB = host.GetWorldBoundingBox(true);
        Matrix4 viewMatrix = mEngine.Camera.ViewMatrix;
        Matrix4 projMatrix = mEngine.Camera.ProjectionMatrix;

        Mogre.Vector3 objPoint = (
            objectAAB.GetCorner(AxisAlignedBox.CornerEnum.FAR_LEFT_TOP) +
            objectAAB.GetCorner(AxisAlignedBox.CornerEnum.FAR_RIGHT_TOP) +
            objectAAB.GetCorner(AxisAlignedBox.CornerEnum.NEAR_LEFT_TOP) +
            objectAAB.GetCorner(AxisAlignedBox.CornerEnum.NEAR_RIGHT_TOP)
            ) / 4f; //Average of 4 TOP points of bounding box.

        if (this.Visible && this.opacity > 0)
        {
            Plane cameraPlane = new Plane(mEngine.Camera.DerivedOrientation.ZAxis, mEngine.Camera.DerivedPosition);
            Boolean isOutsideCam = (cameraPlane.GetSide(objPoint) != Plane.Side.NEGATIVE_SIDE);
            if (isOutsideCam)
            {
                Hide();
                return;
            }
        }

        objPoint = projMatrix * (viewMatrix * objPoint); //Transform to screen space.

        float result_x, result_y;
        result_x = (objPoint.x / 2) + 0.5f; //Normalize. (From -1 - 1 to 0 - 1)
        result_y = 1-((objPoint.y / 2) + 0.5f);

        int prop_x = (int)(result_x * (float)mEngine.Window.Width) - (int)(mainBubble.Width / 2f) + (int)offset.x;
        int prop_y = (int)(result_y * (float)mEngine.Window.Height) - (int)(mainBubble.Height / 2f) + (int)offset.y;

        this.mainBubble.Location = new Point(prop_x, prop_y);
        int tail_x = mainBubble.Location.X + (int)((mainBubble.Size.Width / 2f) - (addBubble.Size.Width / 2f));
        int tail_y = mainBubble.Location.Y + mainBubble.Size.Height - 3;
        this.addBubble.Location = new Point(tail_x, tail_y);
    }

}