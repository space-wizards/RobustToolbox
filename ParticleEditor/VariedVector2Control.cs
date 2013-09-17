using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ParticleEditor
{
    public partial class VariedVector2Control : UserControl, INotifyPropertyChanged
    {
        private float _variation;

        [Description("Vector"), Category("Data"),
        Bindable(true)]
        public PointF Point
        {
            get { return vector2Control1.Point; }
            set
            {
                vector2Control1.Point = value;
                OnPropertyChanged("Point");
                OnPropertyChanged("X");
                OnPropertyChanged("Y");
            }
        }

        [Description("X component"), Category("Data"),
        Bindable(true)]
        public float X
        {
            get { return Point.X; }
            set
            {
                Point = new PointF(value, Point.Y);
            }
        }

        [Description("Y Component"), Category("Data"),
        Bindable(true)]
        public float Y
        {
            get { return Point.Y; }
            set
            {
                Point = new PointF(Point.X, value);
            }
        }

        [Description("Variation"), Category("Data"),
        Bindable(true)]
        public float Variation
        {
            get { return _variation; }
            set
            {
                _variation = value;
                OnPropertyChanged("Variation");
            }
        }
        
        public VariedVector2Control()
        {
            InitializeComponent();
            vector2Control1.DataBindings.Add(new Binding("Point", this, "Point", true));
            numericUpDown1.DataBindings.Add(new Binding("Value", this, "Variation", true));
        }

        public void OnPropertyChanged(string property)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(property));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
