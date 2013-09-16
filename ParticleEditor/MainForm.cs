using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using CGO;
using Dialogs;
using GorgonLibrary;
using GorgonLibrary.Graphics;

namespace ParticleEditor
{
    /// <summary>
    /// Main application form.
    /// </summary>
    public partial class MainForm
        : Form
    {
        #region Properties
        private ParticleConfigurator _configurator;
        private ParticleSystem _particleSystem;
        private Sprite _particleSprite;
        private GorgonLibrary.Graphics.Image _particleImage;
        #endregion

        #region Methods.
        /// <summary>
        /// Handles the KeyDown event of the MainForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.KeyEventArgs"/> instance containing the event data.</param>
        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                Close();
            if (e.KeyCode == Keys.S)
                Gorgon.FrameStatsVisible = !Gorgon.FrameStatsVisible;
        }

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
        /// Handles the FormClosing event of the MainForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.FormClosingEventArgs"/> instance containing the event data.</param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Perform clean up.
            Gorgon.Terminate();
        }

        /// <summary>
        /// Handles the Load event of the MainForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void MainForm_Load(object sender, EventArgs e)
        {
            try
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
                
                _particleImage = GorgonLibrary.Graphics.Image.FromFile("star1.png");
                _particleSprite = new Sprite("particlesprite", _particleImage);
                _particleSprite.Axis = new Vector2D(_particleSprite.Width/2, _particleSprite.Height / 2);

                _particleSystem = new ParticleSystem(_particleSprite, new Vector2D(Gorgon.Screen.Width / 2, Gorgon.Screen.Height / 2));
                _particleSystem.EmissionRadiusRange = new SS13_Shared.Utility.Range<float>(10, 170);
                _particleSystem.EmitRate = 40;
                _particleSystem.Velocity = new Vector2D(0, -20);
                _particleSystem.Acceleration = new Vector2D(0, -30);
                _particleSystem.RadialAcceleration = 10;
                _particleSystem.TangentialAccelerationVariance = 0.2f;
                _particleSystem.TangentialVelocityVariance = 1;
                _particleSystem.RadialVelocityVariance = 1;
                //_particleSystem.TangentialAcceleration = 5;
                _particleSystem.Lifetime = 3;
                _particleSystem.Emit = true;
                _particleSystem.SpinVelocityVariance = 2;

                //Init Event Handlers
                InitEventHandlers();

                // Begin execution.
                Gorgon.Go();
            }
            catch (Exception ex)
            {
                UI.ErrorBox(this, "An unhandled error occured during execution, the program will now close.", ex.Message + "\n\n" + ex.StackTrace);
                Application.Exit();
            }
        }

        private void InitEventHandlers()
        {
            _configurator = new ParticleConfigurator();
            _configurator.ConfigurationChanged += ParticleConfiguration_OnChange;
            _configurator.ShowFPSChanged += ConfiguratorOnShowFpsChanged;
            _configurator.BackgroundColorChanged += ConfiguratorOnBackgroundColorChanged;
            _configurator.ParticleRateChanged += ConfiguratorOnParticleRateChanged;
            _configurator.Show();
            _configurator.ParticleSettings.PropertyChanged += ParticleSettings_PropertyChanged;
        }

        private Vector4D ToVector4DColor(Color c)
        {
            return new Vector4D(c.A, c.R, c.G, c.B);
        }

        void ParticleSettings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var settings = _configurator.ParticleSettings;
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
                        new SS13_Shared.Utility.Range<Vector4D>(ToVector4DColor(settings.ColorRange.Start),
                                                                ToVector4DColor(settings.ColorRange.End));
                    break;
                case "ColorVariance":
                    _particleSystem.ColorVariance = settings.ColorVariance;
                    break;
                case "EmissionOffset":
                    _particleSystem.EmissionOffset = settings.EmissionOffset;
                    break;
                case "EmissionRadiusRange":
                    _particleSystem.EmissionRadiusRange = new SS13_Shared.Utility.Range<float>(settings.EmissionRadiusRange.X, settings.EmissionRadiusRange.Y);
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
                    _particleSystem.SizeRange = new SS13_Shared.Utility.Range<float>(settings.SizeRange.X, settings.SizeRange.Y);
                    break;
                case "SizeVariance":
                    _particleSystem.SizeVariance = settings.SizeVariance;
                    break;
                case "SpinVelocity":
                    _particleSystem.SpinVelocity = new SS13_Shared.Utility.Range<float>(settings.SpinVelocity.X, settings.SpinVelocity.Y);
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

            }
        }

        private void ConfiguratorOnParticleRateChanged(object sender, EventArgs eventArgs)
        {
            _particleSystem.TangentialVelocity = Properties.Settings.Default.ParticleRate / 100;
        }

        private void ConfiguratorOnBackgroundColorChanged(object sender, EventArgs eventArgs)
        {
            Gorgon.Screen.BackgroundColor = Properties.Settings.Default.BackgroundColor;
        }

        private void ConfiguratorOnShowFpsChanged(object sender, EventArgs eventArgs)
        {
            Gorgon.FrameStatsVisible = Properties.Settings.Default.ShowFPS;
        }

        private void ParticleConfiguration_OnChange(object sender, EventArgs e)
        {
            
        }
        #endregion

        #region Constructor/Destructor.
        /// <summary>
        /// Constructor.
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

        }
        #endregion
    }
}