using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;
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
            InitializeFileDialog();
        }

        private void InitializeFileDialog()
        {
            var currentDir = Directory.GetCurrentDirectory() + @"..\..\..\..\Media\ParticleSystems";
            saveFileDialog1.InitialDirectory = Path.GetFullPath(currentDir);
            saveFileDialog1.RestoreDirectory = true;
            openFileDialog1.InitialDirectory = Path.GetFullPath(currentDir);
            openFileDialog1.RestoreDirectory = true;
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

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(saveFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                var particleSaver = new XmlSerializer(typeof(ParticleSettings));
                StreamWriter particleWriter = File.CreateText(saveFileDialog1.FileName);
                particleSaver.Serialize(particleWriter, particleConfigurator.ParticleSettings);
                particleWriter.Flush();
                particleWriter.Close();
            }
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                var particleLoader = new XmlSerializer(typeof(ParticleSettings));
                StreamReader particleReader = File.OpenText(openFileDialog1.FileName);
                var particleSettings = (ParticleSettings)particleLoader.Deserialize(particleReader);
                particleReader.Close();
                particleConfigurator.ParticleSettings.Load(particleSettings);
            }
        }
    }
}
