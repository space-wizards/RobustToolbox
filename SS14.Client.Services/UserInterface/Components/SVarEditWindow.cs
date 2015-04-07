using SS14.Client.Interfaces.GOC;
using SS14.Client.Interfaces.Resource;
using SS14.Shared;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace SS14.Client.Services.UserInterface.Components
{
    internal sealed class SVarEditWindow : Window
    {
        private readonly Entity _owner;
        private List<MarshalComponentParameter> _sVars;

        public SVarEditWindow(Size size, Entity owner)
            : base("Entity SVars : " + owner.Name, size, IoCManager.Resolve<IResourceManager>())
        {
            _owner = owner;
        }

        public void GetSVarsCallback(object sender, GetSVarsEventArgs args)
        {
            _sVars = args.SVars;

            int off_y = 5;
            components.Clear();

            foreach (MarshalComponentParameter svar in _sVars)
            {
                var newLabel = new Label(svar.Family.ToString() + " : " + svar.Parameter.MemberName + " =  ", "CALIBRI",
                                         _resourceManager);
                newLabel.Update(0);

                newLabel.Position = new Point(5, off_y);
                newLabel.DrawBorder = true;
                newLabel.DrawBackground = true;

                GuiComponent newComp = CreateEditField(svar);
                newComp.Update(0);
                newComp.Position = new Point(newLabel.ClientArea.Right + 8, off_y);

                off_y += newLabel.ClientArea.Height + 5;

                components.Add(newLabel);
                components.Add(newComp);
            }
        }

        private GuiComponent CreateEditField(MarshalComponentParameter compPar)
        {
            if (compPar.Parameter.ParameterType == typeof (float) || compPar.Parameter.ParameterType == typeof (int) ||
                compPar.Parameter.ParameterType == typeof (String))
            {
                var editTxt = new Textbox(100, _resourceManager);
                editTxt.ClearOnSubmit = false;
                editTxt.UserData = compPar;
                editTxt.Text = compPar.Parameter.Parameter.ToString();
                editTxt.OnSubmit += editTxt_OnSubmit;
                return editTxt;
            }
            else if (compPar.Parameter.ParameterType == typeof (Boolean))
            {
                var editBool = new Checkbox(_resourceManager);
                editBool.UserData = compPar;
                editBool.Value = ((Boolean) compPar.Parameter.Parameter);
                editBool.ValueChanged += editBool_ValueChanged;
                return editBool;
            }
            return null;
        }

        private void editBool_ValueChanged(bool newValue, Checkbox sender)
        {
            var assigned = (MarshalComponentParameter) sender.UserData;
            assigned.Parameter.Parameter = newValue;
            _owner.GetComponent<ISVarsComponent>(ComponentFamily.SVars).DoSetSVar(assigned);
        }

        private void editTxt_OnSubmit(string text, Textbox sender)
        {
            var assigned = (MarshalComponentParameter) sender.UserData;

            if (assigned.Parameter.ParameterType == typeof (string))
            {
                assigned.Parameter.Parameter = text;
                _owner.GetComponent<ISVarsComponent>(ComponentFamily.SVars).DoSetSVar(assigned);
            }
            else if (assigned.Parameter.ParameterType == typeof (int))
            {
                assigned.Parameter.Parameter = int.Parse(text);
                _owner.GetComponent<ISVarsComponent>(ComponentFamily.SVars).DoSetSVar(assigned);
            }
            else if (assigned.Parameter.ParameterType == typeof (float))
            {
                assigned.Parameter.Parameter = float.Parse(text);
                _owner.GetComponent<ISVarsComponent>(ComponentFamily.SVars).DoSetSVar(assigned);
            }
        }
    }
}