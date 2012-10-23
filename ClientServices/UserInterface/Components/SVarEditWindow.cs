using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using ClientInterfaces.GOC;
using ClientInterfaces.Resource;
using SS13.IoC;
using SS13_Shared.GO;

namespace ClientServices.UserInterface.Components
{
    struct SVarWindowStruct
    {
        public string ComponentFamily;
        public Label LabelComponentFamily;
        public string ParameterName;
        public Label LabelParameterName;
        public GuiComponent EditField;
    }
    sealed class SVarEditWindow : Window
    {
        private IEntity _owner;
        private List<MarshalComponentParameter> _sVars; 
        
        public SVarEditWindow(Size size, IEntity owner)
            :base("Entity SVars : " + owner.Name, size, IoCManager.Resolve<IResourceManager>())
        {
            _owner = owner;
        }


        public void GetSVarsCallback(object sender, GetSVarsEventArgs args)
        {
            _sVars = args.SVars;
        }
    }
}
