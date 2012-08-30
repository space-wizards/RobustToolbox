using System;
using System.Collections;
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
        public bool CanEdit;
        public bool IsListItem;
        public object ListItem;
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
                var fieldVal = field.GetValue(assigned);

                if (fieldVal != null && fieldVal is ICollection)
                {
                    newEntry.VarName = field.Name;
                    newEntry.LabelName = new Label(field.Name + " = " + (fieldVal == null ? "null" : fieldVal.ToString()), "CALIBRI", _resourceManager);
                    newEntry.CanEdit = true;
                    newEntry.IsListItem = true;
                    newEntry.CanEdit = !field.IsInitOnly;

                    newEntry.LabelName.Position = new Point(5, pos);
                    newEntry.LabelName.DrawBorder = true;
                    newEntry.LabelName.BorderColor = Color.Honeydew;
                    newEntry.LabelName.BackgroundColor = Color.Gray;
                    newEntry.LabelName.DrawBackground = true;
                    newEntry.LabelName.Update(0);

                    pos += 5 + newEntry.LabelName.ClientArea.Height;

                    this.components.Add(newEntry.LabelName);
                    ObjPropList.Add(newEntry);

                    newEntry = new PropWindowStruct();

                    foreach (var item in (ICollection)fieldVal)
                    {
                        newEntry.VarName = item.ToString();

                        newEntry.CanEdit = true;
                        newEntry.IsListItem = true;
                        newEntry.ListItem = item;

                        newEntry.LabelName = new Label(item.ToString(), "CALIBRI", _resourceManager);
                        newEntry.LabelName.Position = new Point(15, pos);
                        newEntry.LabelName.DrawBorder = true;
                        newEntry.LabelName.BorderColor = Color.DeepSkyBlue;
                        newEntry.LabelName.BackgroundColor = Color.Gray;
                        newEntry.LabelName.DrawBackground = true;
                        newEntry.LabelName.Clicked += new Label.LabelPressHandler(LabelName_Clicked);
                        newEntry.LabelName.Update(0);

                        pos += 5 + newEntry.LabelName.ClientArea.Height;

                        this.components.Add(newEntry.LabelName);
                        ObjPropList.Add(newEntry);
                    }
                }
                else
                {
                    newEntry.VarName = field.Name;
                    newEntry.LabelName = new Label(field.Name + " = " + (fieldVal == null ? "null" : fieldVal.ToString()), "CALIBRI", _resourceManager);
                    newEntry.CanEdit = !field.IsInitOnly;
                    newEntry.LabelName.Position = new Point(5, pos);
                    newEntry.LabelName.DrawBorder = true;
                    newEntry.LabelName.BorderColor = newEntry.CanEdit ? Color.Chartreuse : Color.IndianRed;
                    newEntry.LabelName.BackgroundColor = Color.Gray;
                    newEntry.LabelName.DrawBackground = true;
                    newEntry.LabelName.Clicked += new Label.LabelPressHandler(LabelName_Clicked);
                    this.components.Add(newEntry.LabelName);
                    newEntry.LabelName.Update(0);
                    pos += 5 + newEntry.LabelName.ClientArea.Height;
                }

                ObjPropList.Add(newEntry);
            }
        }


        void LabelName_Clicked(Label sender)
        {
            PropWindowStruct selected = new PropWindowStruct();
            bool found = false;

            foreach (PropWindowStruct struc in ObjPropList)
            {
                if (struc.LabelName == sender)
                {
                    selected = struc;
                    found = true;
                    break;
                }
            }

            if (found)
            {
                if (selected.IsListItem)
                {
                    if (selected.ListItem != null)
                    {
                        UserInterfaceManager uiMgr = (UserInterfaceManager)IoCManager.Resolve<IUserInterfaceManager>();
                        uiMgr.AddComponent(new PropEditWindow(new Size(400, 400), _resourceManager, selected.ListItem));
                    }
                }
                else if (fields.First(x => x.Name == selected.VarName) != null)
                {
                    FieldInfo field = fields.First(x => x.Name == selected.VarName);
                    var fieldVar = field.GetValue(assigned);
                    if (fieldVar == null) return;

                    UserInterfaceManager uiMgr = (UserInterfaceManager)IoCManager.Resolve<IUserInterfaceManager>();
                    uiMgr.AddComponent(new PropEditWindow(new Size(400, 400), _resourceManager, fieldVar));
                }

                this.Dispose();
            }
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
