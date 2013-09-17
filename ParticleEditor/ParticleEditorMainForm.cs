using System;
using System.Windows.Forms;
using ClientServices.Configuration;
using ClientServices.Resources;
using GorgonLibrary;

namespace ParticleEditor
{
    public partial class ParticleEditorMainForm : Form
    {
        private ConfigurationManager _configurationManager;
        public ResourceManager ResourceManager { get; private set; }

        public ParticleEditorMainForm()
        {
            InitializeComponent();
            InitializeEvents();
            particleConfigurator.MainForm = this;
            particleDisplay.MainForm = this;
            particleDisplay.InitDisplay();
        }

        public void InitializeResourceManager()
        {
            _configurationManager = new ConfigurationManager();
            _configurationManager.Initialize("config.xml");
            ResourceManager = new ResourceManager(_configurationManager);
            particleDisplay.ResourceManager = ResourceManager;
            particleConfigurator.InitializeSpriteSelect();
        }

        public void InitializeEvents()
        {
            particleDisplay.ParticleConfigurator = particleConfigurator;
            particleConfigurator.ConfigurationChanged += particleDisplay.ParticleConfiguration_OnChange;
            particleConfigurator.ShowFPSChanged += particleDisplay.ConfiguratorOnShowFpsChanged;
            particleConfigurator.BackgroundColorChanged += particleDisplay.ConfiguratorOnBackgroundColorChanged;
            particleConfigurator.ParticleRateChanged += particleDisplay.ConfiguratorOnParticleRateChanged;
            particleConfigurator.ParticleSettings.PropertyChanged += particleDisplay.ParticleSettings_PropertyChanged;
        }

        /// <summary>
        /// Handles the FormClosing event of the MainForm control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.FormClosingEventArgs"/> instance containing the event data.</param>
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!DesignMode)
            {
                // Perform clean up.
                Gorgon.Terminate();
            }
        }
    }
}
