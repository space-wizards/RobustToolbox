using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using GorgonLibrary;

namespace ParticleEditor
{
    public partial class ParticleEditorMainForm : Form
    {
        public ParticleEditorMainForm()
        {
            InitializeComponent();
            InitializeEvents();
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
