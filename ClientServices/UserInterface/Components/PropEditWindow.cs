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
        private Object assigned;
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

        private void RebuildPropList(object newObj)
        {
            this.components.Clear();
            this.components.Add(search);

            ObjPropList.Clear();
            assigned = newObj;
            BuildPropList();
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
                    newEntry.CanEdit = false;
                    newEntry.IsListItem = false;

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

                    newEntry.CanEdit = !field.IsInitOnly;
                    newEntry.IsListItem = false;

                    newEntry.LabelName = new Label(field.Name + " = " + (fieldVal == null ? "null" : fieldVal.ToString()), "CALIBRI", _resourceManager);
                    newEntry.LabelName.Position = new Point(5, pos);
                    newEntry.LabelName.DrawBorder = true;
                    newEntry.LabelName.BorderColor = newEntry.CanEdit ? Color.Chartreuse : Color.IndianRed;
                    newEntry.LabelName.BackgroundColor = Color.Gray;
                    newEntry.LabelName.DrawBackground = true;
                    newEntry.LabelName.Clicked += new Label.LabelPressHandler(LabelName_Clicked);
                    newEntry.LabelName.Update(0);

                    pos += 5 + newEntry.LabelName.ClientArea.Height;

                    this.components.Add(newEntry.LabelName);
                    ObjPropList.Add(newEntry);
                }   
            }
        }


        void LabelName_Clicked(Label sender)
        {
            PropWindowStruct? selected = null;

            if (ObjPropList.Any(x => x.LabelName == sender))
                selected = ObjPropList.First(x => x.LabelName == sender);

            if (selected.HasValue)
            {
                if (selected.Value.IsListItem)
                {
                    if (selected.Value.ListItem != null)
                        RebuildPropList(selected.Value.ListItem);
                }
                else if (fields.First(x => x.Name == selected.Value.VarName) != null)
                {
                    FieldInfo field = fields.First(x => x.Name == selected.Value.VarName);
                    var fieldVar = field.GetValue(assigned);
                    if (fieldVar == null) return;

                    RebuildPropList(fieldVar);
                }
            }
        }
    }
}
