using GorgonLibrary;
using GorgonLibrary.Graphics;
using SS14.Client.GameObjects;
using SS14.Client.Services.Resources;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace SS14.Tools.ParticleEditor
{
    public partial class ParticleDisplay : UserControl
    {
        #region Properties
        public ParticleConfigurator ParticleConfigurator { get; set; }
        private ParticleSystem _particleSystem;
        private Sprite _particleSprite;
        private GorgonLibrary.Graphics.Image _particleImage;
        public ResourceManager ResourceManager { get; set; }
        public ParticleEditorMainForm MainForm { get; set; }
        #endregion

        #region Methods.

        /// <summary>
        /// Handles the OnFrameBegin event of the Screen control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="FrameEventArgs"/> instance containing the event data.</param>
        private void Screen_OnFrameBegin(object sender, FrameEventArgs e)
        {
            // Clear the screen.
            Gorgon.Screen.Clear();
            _particleSystem.Update(e.FrameDeltaTime);
            _particleSystem.Render();
        }

        /// <summary>
        /// Initializes the display control.
        /// </summary>
        public void InitDisplay()
        {
            if (!DesignMode)
            {
                // Initialize the library.
                Gorgon.Initialize();

                // Display the logo and frame stats.
                Gorgon.LogoVisible = false;
                Gorgon.FrameStatsVisible = false;
                Gorgon.AllowBackgroundRendering = true;

                // Set the video mode to match the form client area.
                Gorgon.SetMode(this);

                // Assign rendering event handler.
                Gorgon.Idle += new FrameEventHandler(Screen_OnFrameBegin);

                // Set the clear color to something ugly.
                Gorgon.Screen.BackgroundColor = Color.FromArgb(0, 0, 0);

                //Init Configuration and resource manager.
                MainForm.InitializeResourceManager();
                /*
                _particleImage = GorgonLibrary.Graphics.Image.FromFile("star1.png");
                _particleSprite = new Sprite("particlesprite", _particleImage);
                _particleSprite.Axis = new Vector2(_particleSprite.Width/2, _particleSprite.Height/2);*/
                _particleSprite = ResourceManager.GetSprite("star1");
                var settings = ParticleConfigurator.ParticleSettings;
                _particleSystem = new ParticleSystem(_particleSprite, new Vector2(0, 0));
                settings.ColorRange = new SS14.Shared.Utility.Range<Color>(Color.Blue, Color.Black);
                settings.EmitterPosition = new PointF(Gorgon.Screen.Width/2, Gorgon.Screen.Height/2);
                settings.EmissionRadiusRange = new PointF(10, 170);
                settings.EmitRate = 40;
                settings.Velocity = new Vector2(0, -20);
                settings.Acceleration = new Vector2(0, -30);
                settings.RadialAcceleration = 10;
                settings.TangentialAccelerationVariance = 0.2f;
                settings.TangentialVelocityVariance = 1;
                settings.RadialVelocityVariance = 1;
                //_particleSystem.TangentialAcceleration = 5;
                settings.Lifetime = 3;
                _particleSystem.Emit = true;
                settings.SpinVelocityVariance = 2;

                // Begin execution.
                Gorgon.Go();
            }
        }

        private Vector4 ToVector4Color(Color c)
        {
            return new Vector4(c.A, c.R, c.G, c.B);
        }

        public void ParticleSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var settings = ParticleConfigurator.ParticleSettings;
            switch (e.PropertyName)
            {
                case "Acceleration":
                    _particleSystem.Acceleration = settings.Acceleration;
                    break;
                case "AccelerationVariance":
                    _particleSystem.AccelerationVariance = settings.AccelerationVariance;
                    break;
                case "ColorRange":
                    _particleSystem.ColorRange =
                        new SS14.Shared.Utility.Range<Vector4>(ToVector4Color(settings.ColorRange.Start),
                                                                ToVector4Color(settings.ColorRange.End));
                    break;
                case "ColorVariance":
                    _particleSystem.ColorVariance = settings.ColorVariance;
                    break;
                case "EmissionOffset":
                    _particleSystem.EmissionOffset = settings.EmissionOffset;
                    break;
                case "EmissionRadiusRange":
                    _particleSystem.EmissionRadiusRange = new SS14.Shared.Utility.Range<float>(settings.EmissionRadiusRange.X, settings.EmissionRadiusRange.Y);
                    break;
                case "EmitRate":
                    _particleSystem.EmitRate = settings.EmitRate;
                    break;
                case "EmitterPosition":
                    _particleSystem.EmitterPosition = settings.EmitterPosition;
                    break;
                case "Lifetime":
                    _particleSystem.Lifetime = settings.Lifetime;
                    break;
                case "LifetimeVariance":
                    _particleSystem.LifetimeVariance = settings.LifetimeVariance;
                    break;
                case "MaximumParticleCount":
                    _particleSystem.MaximumParticleCount = settings.MaximumParticleCount;
                    break;
                case "RadialAcceleration":
                    _particleSystem.RadialAcceleration = settings.RadialAcceleration;
                    break;
                case "RadialAccelerationVariance":
                    _particleSystem.RadialAccelerationVariance = settings.RadialAccelerationVariance;
                    break;
                case "SizeRange":
                    _particleSystem.SizeRange = new SS14.Shared.Utility.Range<float>(settings.SizeRange.X, settings.SizeRange.Y);
                    break;
                case "SizeVariance":
                    _particleSystem.SizeVariance = settings.SizeVariance;
                    break;
                case "SpinVelocity":
                    _particleSystem.SpinVelocity = new SS14.Shared.Utility.Range<float>(settings.SpinVelocity.X, settings.SpinVelocity.Y);
                    break;
                case "SpinVelocityVariance":
                    _particleSystem.SpinVelocityVariance = settings.SpinVelocityVariance;
                    break;
                case "TangentialAcceleration":
                    _particleSystem.TangentialAcceleration = settings.TangentialAcceleration;
                    break;
                case "TangentialAccelerationVariance":
                    _particleSystem.TangentialAccelerationVariance = settings.TangentialAccelerationVariance;
                    break;
                case "TangentialVelocity":
                    _particleSystem.TangentialVelocity = settings.TangentialVelocity;
                    break;
                case "TangentialVelocityVariance":
                    _particleSystem.TangentialVelocityVariance = settings.TangentialVelocityVariance;
                    break;
                case "Velocity":
                    _particleSystem.Velocity = settings.Velocity;
                    break;
                case "VelocityVariance":
                    _particleSystem.VelocityVariance = settings.VelocityVariance;
                    break;
                case "Sprite":
                    _particleSystem.ParticleSprite = MainForm.ResourceManager.GetSprite(settings.Sprite);
                    break;
            }
        }

        public void ConfiguratorOnParticleRateChanged(object sender, EventArgs eventArgs)
        {
            _particleSystem.TangentialVelocity = Properties.Settings.Default.ParticleRate / 100;
        }

        public void ConfiguratorOnBackgroundColorChanged(object sender, BackgroundColorChangedEventArgs eventArgs)
        {
            Gorgon.Screen.BackgroundColor = eventArgs.Color;
        }

        public void ConfiguratorOnShowFpsChanged(object sender, ShowFpsChangedEventArgs eventArgs)
        {
            Gorgon.FrameStatsVisible = eventArgs.ShowFps;
        }

        public void ParticleConfiguration_OnChange(object sender, EventArgs e)
        {
            
        }
        #endregion

        #region Constructor/Destructor.
        /// <summary>
        /// Constructor.
        /// </summary>
        public ParticleDisplay()
        {
            InitializeComponent();

        }
        #endregion
    }
}
