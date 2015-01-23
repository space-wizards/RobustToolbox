using Cyotek.Windows.Forms;
using SS14.Shared.GameObjects;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Design;
using System.Windows.Forms;
using System.Windows.Forms.Design;

namespace SS14.Tools.ParticleEditor
{
    public delegate void ParticleConfigurationChangedHandler(object sender, EventArgs e);

    public delegate void ShowFpsChangedHandler(object sender, ShowFpsChangedEventArgs e);

    public delegate void BackgroundColorChangedHandler(object sender, BackgroundColorChangedEventArgs e);

    public partial class ParticleConfigurator : UserControl
    {
        public ParticleSettings ParticleSettings { get; private set; }

        public event ParticleConfigurationChangedHandler ConfigurationChanged;
        public event ShowFpsChangedHandler ShowFPSChanged;
        public event BackgroundColorChangedHandler BackgroundColorChanged;
        public event ParticleConfigurationChangedHandler ParticleRateChanged;
        public ParticleEditorMainForm MainForm { get; set; }
        private List<string> _sprites; 

        public ParticleConfigurator()
        {
            InitializeComponent();
            ParticleSettings = new ParticleSettings();
            InitializeBindings();
        }

        public void InitializeSpriteSelect()
        {
            _sprites = MainForm.ResourceManager.GetSpriteKeys();
            comboBoxSpriteSelect.DataBindings.Clear();
            comboBoxSpriteSelect.DataSource = _sprites;
            comboBoxSpriteSelect.SelectedValueChanged += ComboBoxSpriteSelectOnSelectedValueChanged;
        }

        private void ComboBoxSpriteSelectOnSelectedValueChanged(object sender, EventArgs eventArgs)
        {
            ParticleSettings.Sprite = (string)comboBoxSpriteSelect.SelectedValue;
        }

        public void InitializeBindings()
        {
            colorRangeControl1.DataBindings.Add(new Binding("ColorRange", ParticleSettings, "ColorRange", true, DataSourceUpdateMode.OnPropertyChanged));
            numericUpDownColorVariation.DataBindings.Add(new Binding("Value", ParticleSettings,
                                                                     "ColorVariance", true, DataSourceUpdateMode.OnPropertyChanged));
            textBoxParticleEmitRate.DataBindings.Add(new Binding("Text", ParticleSettings, "EmitRate",
                                                                 true, DataSourceUpdateMode.OnPropertyChanged));
            trackBar1.DataBindings.Add(new Binding("Value", ParticleSettings, "EmitRate",
                                                                 true, DataSourceUpdateMode.OnPropertyChanged));
            textBoxMaxDisplayedParticles.DataBindings.Add(new Binding("Text", ParticleSettings, "MaximumParticleCount",
                                                                 true, DataSourceUpdateMode.OnPropertyChanged));
            trackBarMaxDisplayedParticles.DataBindings.Add(new Binding("Value", ParticleSettings, "MaximumParticleCount",
                                                                 true, DataSourceUpdateMode.OnPropertyChanged));
            vector2ControlEmitterPosition.DataBindings.Add(new Binding("Point", ParticleSettings,
                                                                       "EmitterPosition",
                                                                       true, DataSourceUpdateMode.OnPropertyChanged));
            vector2ControlEmitOffset.DataBindings.Add(new Binding("Point", ParticleSettings,
                                                                       "EmissionOffset",
                                                                       true, DataSourceUpdateMode.OnPropertyChanged));
            floatRangeControlEmitRadius.DataBindings.Add(new Binding("Range", ParticleSettings,
                                                                       "EmissionRadiusRange",
                                                                       true, DataSourceUpdateMode.OnPropertyChanged));
            variedVector2Velocity.DataBindings.Add(new Binding("Point", ParticleSettings, "Velocity", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedVector2Acceleration.DataBindings.Add(new Binding("Point", ParticleSettings, "Acceleration", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedFloatControlRadialVelocity.DataBindings.Add(new Binding("Value", ParticleSettings, "RadialVelocity", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedFloatControlRadialAcceleration.DataBindings.Add(new Binding("Value", ParticleSettings, "RadialAcceleration", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedFloatControlTangentialVelocity.DataBindings.Add(new Binding("Value", ParticleSettings, "TangentialVelocity", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedFloatControlTangentialAcceleration.DataBindings.Add(new Binding("Value", ParticleSettings, "TangentialAcceleration", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedVector2Velocity.DataBindings.Add(new Binding("Variation", ParticleSettings, "VelocityVariance", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedVector2Acceleration.DataBindings.Add(new Binding("Variation", ParticleSettings, "AccelerationVariance", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedFloatControlRadialVelocity.DataBindings.Add(new Binding("Variation", ParticleSettings, "RadialVelocityVariance", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedFloatControlRadialAcceleration.DataBindings.Add(new Binding("Variation", ParticleSettings, "RadialAccelerationVariance", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedFloatControlTangentialVelocity.DataBindings.Add(new Binding("Variation", ParticleSettings, "TangentialVelocityVariance", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedFloatControlTangentialAcceleration.DataBindings.Add(new Binding("Variation", ParticleSettings, "TangentialAccelerationVariance", true,
                                                               DataSourceUpdateMode.OnPropertyChanged));
            variedFloatControlLifetime.DataBindings.Add(new Binding("Value", ParticleSettings, "Lifetime", true,
                                                                    DataSourceUpdateMode.OnPropertyChanged));
            variedFloatControlLifetime.DataBindings.Add(new Binding("Variation", ParticleSettings, "LifetimeVariance",
                                                                    true, DataSourceUpdateMode.OnPropertyChanged));
            variedFloatRangeControlSize.DataBindings.Add(new Binding("Range", ParticleSettings, "SizeRange", true,
                                                                     DataSourceUpdateMode.OnPropertyChanged));
            variedFloatRangeControlSize.DataBindings.Add(new Binding("Variation", ParticleSettings, "SizeVariance", true,
                                                                     DataSourceUpdateMode.OnPropertyChanged));
            variedFloatRangeControlSpin.DataBindings.Add(new Binding("Range", ParticleSettings, "SpinVelocity", true,
                                                                     DataSourceUpdateMode.OnPropertyChanged));
            variedFloatRangeControlSpin.DataBindings.Add(new Binding("Variation", ParticleSettings, "SpinVelocityVariance", true,
                                                                     DataSourceUpdateMode.OnPropertyChanged));
        }

        private void colorSwatchBackgroundColor_Click(object sender, EventArgs e)
        {
            var dialog = new ColorPickerDialog();
            dialog.Color = colorSwatchBackgroundColor.SelectedColor;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                colorSwatchBackgroundColor.SelectedColor = dialog.Color;
                if(BackgroundColorChanged != null)
                    BackgroundColorChanged(sender, new BackgroundColorChangedEventArgs(dialog.Color));

            }
        }

        private void checkShowFPS_CheckStateChanged(object sender, EventArgs e)
        {
            if (ShowFPSChanged != null)
                ShowFPSChanged(sender, new ShowFpsChangedEventArgs(checkShowFPS.Checked));
        }
    }
    
    public class MyColorConverter : ColorConverter
    {
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return false;
        }
    }

    public class ColorPickerDoohickey : UITypeEditor
    {
        public override UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            // Indicates that this editor can display a Form-based interface. 
            return UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(
            ITypeDescriptorContext context, 
            IServiceProvider provider, 
            object value)
        {
            // Attempts to obtain an IWindowsFormsEditorService.
            IWindowsFormsEditorService edSvc = 
                (IWindowsFormsEditorService)provider.GetService(typeof(IWindowsFormsEditorService));
            if (edSvc == null)
            {
                return null;
            }

            // Displays a StringInputDialog Form to get a user-adjustable  
            // string value. 
            using (ColorPickerDialog dialog = new ColorPickerDialog())
            {
                dialog.Color = (Color) value;
                if (edSvc.ShowDialog(dialog) == DialogResult.OK)
                {
                    return dialog.Color;
                }
            }

            // If OK was not pressed, return the original value 
            return (Color)value;
        }    
    }

    public class ShowFpsChangedEventArgs : EventArgs
    {
        public bool ShowFps { get; set; }
        public ShowFpsChangedEventArgs(bool showFps)
        {
            ShowFps = showFps;
        }
    }

    public class BackgroundColorChangedEventArgs : EventArgs
    {
        public Color Color { get; set; }
        public BackgroundColorChangedEventArgs(Color c)
        {
            Color = c;
        }
    }
}
