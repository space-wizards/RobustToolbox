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

            int off_y = 5;
            this.components.Clear();

            foreach (MarshalComponentParameter svar in _sVars)
            {
                Label newLabel = new Label(svar.Family.ToString() + " : " + svar.Parameter.MemberName + " =  ", "CALIBRI", _resourceManager);
                newLabel.Update(0);

                newLabel.Position = new Point(5, off_y);
                newLabel.DrawBorder = true;
                newLabel.DrawBackground = true;

                GuiComponent newComp = CreateEditField(svar);
                newComp.Update(0);
                newComp.Position = new Point(newLabel.ClientArea.Right + 8, off_y);

                off_y += newLabel.ClientArea.Height + 5;

                this.components.Add(newLabel);
                this.components.Add(newComp);
            }
        }

        private GuiComponent CreateEditField(MarshalComponentParameter compPar)
        {
            if (compPar.Parameter.ParameterType == typeof(float) || compPar.Parameter.ParameterType == typeof(int) || compPar.Parameter.ParameterType == typeof(String))
            {
                Textbox editTxt = new Textbox(100, _resourceManager);
                editTxt.ClearOnSubmit = false;
                editTxt.UserData = compPar;
                editTxt.Text = compPar.Parameter.Parameter.ToString();
                editTxt.OnSubmit += new Textbox.TextSubmitHandler(editTxt_OnSubmit);
                return editTxt;
            }
            else if (compPar.Parameter.ParameterType == typeof(Boolean))
            {
                Checkbox editBool = new Checkbox(_resourceManager);
                editBool.UserData = compPar;
                editBool.Value = ((Boolean)compPar.Parameter.Parameter);
                editBool.ValueChanged += new Checkbox.CheckboxChangedHandler(editBool_ValueChanged);
                return editBool;
            }
            return null;
        }

        void editBool_ValueChanged(bool newValue, Checkbox sender)
        {
            MarshalComponentParameter assigned = (MarshalComponentParameter)sender.UserData;
            assigned.Parameter.Parameter = newValue;
            _owner.SetSVar(assigned);
        }

        void editTxt_OnSubmit(string text, Textbox sender)
        {
            MarshalComponentParameter assigned = (MarshalComponentParameter)sender.UserData;

            if (assigned.Parameter.ParameterType == typeof(string))
            {
                assigned.Parameter.Parameter = text;
                _owner.SetSVar(assigned);
            }
            else if (assigned.Parameter.ParameterType == typeof(int))
            {
                assigned.Parameter.Parameter = int.Parse(text);
                _owner.SetSVar(assigned);
            }
            else if (assigned.Parameter.ParameterType == typeof(float))
            {
                assigned.Parameter.Parameter = float.Parse(text);
                _owner.SetSVar(assigned);
            }
        }
    }
}
