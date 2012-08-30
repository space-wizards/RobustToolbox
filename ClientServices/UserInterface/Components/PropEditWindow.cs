using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Reflection;
using ClientInterfaces;
using ClientInterfaces.Network;
using ClientInterfaces.Player;
using ClientInterfaces.Resource;
using ClientInterfaces.GOC;
using ClientInterfaces.UserInterface;
using GorgonLibrary.InputDevices;
using GorgonLibrary.Graphics;
using GorgonLibrary;
using CGO;
using Lidgren.Network;
using SS13_Shared;
using SS13_Shared.GO;
using SS13.IoC;

namespace ClientServices.UserInterface.Components
{
    struct PropWindowStruct
    {
        public string VarName;
        public Label LabelName;
        public Textbox VarEdit;
        public bool CanEdit;
    }

    class PropEditWindow : Window
    {
        private readonly Object assigned;
        private Textbox search;
        private FieldInfo[] fields;

        List<PropWindowStruct> ObjPropList = new List<PropWindowStruct>();

        public PropEditWindow(Size size, IResourceManager resourceManager, Object obj)
            : base("Object Properties : " + obj.ToString(), size, resourceManager)
        {
            Position = new Point((int)(Gorgon.CurrentRenderTarget.Width / 2f) - (int)(ClientArea.Width / 2f), (int)(Gorgon.CurrentRenderTarget.Height / 2f) - (int)(ClientArea.Height / 2f));
            search = new Textbox(150, resourceManager);
            search.Position = new Point(5, 5);
            search.OnSubmit += new Textbox.TextSubmitHandler(search_OnSubmit);
            search.ClearOnSubmit = true;
            search.ClearFocusOnSubmit = false;
            components.Add(search);
            assigned = obj;
            BuildPropList();
            Update(0);
        }

        void search_OnSubmit(string text)
        {
            foreach (PropWindowStruct struc in ObjPropList)
            {
                if (struc.VarName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    struc.LabelName.BackgroundColor = Color.Bisque;
                else
                    struc.LabelName.BackgroundColor = Color.Gray;
            }
        }

        private void BuildPropList()
        {
            Type entType = assigned.GetType();
            fields = entType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            int pos = 25;

            foreach (FieldInfo field in fields)
            {
                PropWindowStruct newEntry = new PropWindowStruct();
                newEntry.VarName = field.Name;
                var fieldVal = field.GetValue(assigned);
                newEntry.LabelName = new Label(field.Name + " = " + (fieldVal == null ? "null" : fieldVal.ToString()), "CALIBRI", _resourceManager);
                newEntry.CanEdit = !field.IsInitOnly;
                newEntry.LabelName.Position = new Point(5, pos);
                newEntry.LabelName.DrawBorder = true;
                newEntry.LabelName.BorderColor = newEntry.CanEdit ? Color.DarkGreen : Color.DarkRed;
                newEntry.LabelName.BackgroundColor = Color.Gray;
                newEntry.LabelName.DrawBackground = true;
                newEntry.LabelName.Clicked += new Label.LabelPressHandler(LabelName_Clicked);
                this.components.Add(newEntry.LabelName);
                newEntry.LabelName.Update(0);
                pos += 5 + newEntry.LabelName.ClientArea.Height;

                ObjPropList.Add(newEntry);
            }
        }

        void LabelName_Clicked(Label sender)
        {
            string selected = null;
            foreach (PropWindowStruct struc in ObjPropList)
            {
                if (struc.LabelName == sender)
                {
                    selected = struc.VarName;
                    break;
                }
            }

            if (selected != null)
            {
                if (fields.First(x => x.Name == selected) != null)
                {
                    FieldInfo field = fields.First(x => x.Name == selected);
                    var fieldVar = field.GetValue(assigned);
                    if (fieldVar == null) return;
                    UserInterfaceManager uiMgr = (UserInterfaceManager)IoCManager.Resolve<IUserInterfaceManager>();
                    uiMgr.AddComponent(new PropEditWindow(new Size(400, 400), _resourceManager, fieldVar));
                }
            }
            this.Dispose();
        }

        public override sealed void Update(float frameTime)
        {
            base.Update(frameTime);
        }

        public override void Render()
        {
            base.Render();
        }

        public override void Dispose()
        {
            base.Dispose();
            GC.SuppressFinalize(this);
        }

    }
}
