using System.ComponentModel;
using System.Windows.Forms;

namespace SS14.Tools.ParticleEditor
{
    public partial class VariedFloatControl : UserControl
    {
        private float _variation;
        private float _value;

        [Description("Value"), Category("Data"),
        Bindable(true)]
        public float Value
        {
            get { return _value; }
            set
            {
                _value = value; 
                OnPropertyChanged("Value");
            }
        }

        [Description("Variation"), Category("Data"),
        Bindable(true)]
        public float Variation
        {
            get { return _variation; }
            set { 
                _variation = value;
                OnPropertyChanged("Variation");
            }
        }
        
        public VariedFloatControl()
        {
            InitializeComponent();
            numericUpDownValue.DataBindings.Add(new Binding("Value", this, "Value", true));
            numericUpDownVariation.DataBindings.Add(new Binding("Value", this, "Variation", true));
        }

        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
