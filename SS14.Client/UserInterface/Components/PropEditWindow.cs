using SFML.System;
using SFML.Window;
using SS14.Client.Graphics;
using SS14.Client.Interfaces.Resource;
using SS14.Shared.Maths;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SS14.Client.UserInterface.Components
{
    internal struct PropWindowStruct
    {
        public bool CanEdit;
        public bool IsListItem;
        public Label LabelName;
        public object ListItem;
        public string VarName;
    }

    internal class PropEditWindow : Window
    {
        private readonly List<PropWindowStruct> ObjPropList = new List<PropWindowStruct>();
        private readonly Textbox search;
        private Object assigned;
        private FieldInfo[] fields;

        public PropEditWindow(Vector2i size, IResourceCache resourceCache, Object obj)
            : base("Object Properties : " + obj, size, resourceCache)
        {
            Position = new Vector2i((int) (CluwneLib.CurrentRenderTarget.Size.X/2f) - (int) (ClientArea.Width/2f),
                                 (int) (CluwneLib.CurrentRenderTarget.Size.Y/2f) - (int) (ClientArea.Height/2f));

            search = new Textbox(150, resourceCache);
            search.Position = new Vector2i(5, 5);
            search.OnSubmit += search_OnSubmit;
            search.ClearOnSubmit = true;
            search.ClearFocusOnSubmit = false;
            components.Add(search);

            assigned = obj;
            BuildPropList();

            Update(0);
        }

        private void search_OnSubmit(string text, Textbox sender)
        {
            foreach (PropWindowStruct struc in ObjPropList)
            {
                if (struc.VarName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    struc.LabelName.BackgroundColor = new SFML.Graphics.Color(255, 228, 196);
                else
                    struc.LabelName.BackgroundColor = new SFML.Graphics.Color(128, 128, 128);
            }
        }

        private void RebuildPropList(object newObj)
        {
            if (scrollbarH.IsVisible()) scrollbarH.Value = 0;
            if (scrollbarV.IsVisible()) scrollbarV.Value = 0;

            components.Clear();
            components.Add(search);

            ObjPropList.Clear();
            assigned = newObj;
            title.Text.Text = "Object Properties : " + assigned;
            BuildPropList();
        }

        private GuiComponent CreateEditField(object o, FieldInfo field)
        {
            if (o is String || o is string)
            {
                var editStr = new Textbox(100, _resourceCache);
                editStr.ClearOnSubmit = false;
                editStr.UserData = field;
                editStr.Text = ((string) o);
                editStr.OnSubmit += editStr_OnSubmit;
                return editStr;
            }
            else if (o is Enum)
            {
                var editEnum = new Listbox(100, 100, _resourceCache, Enum.GetNames(o.GetType()).ToList());
                editEnum.UserData = field;
                editEnum.SelectItem(o.ToString());
                editEnum.ItemSelected += editEnum_ItemSelected;
                return editEnum;
            }
            else if (o is float || o is int || o is Int16 || o is Int32 || o is Int64 || o is double || o is Double ||
                     o is decimal || o is Decimal || o is Single)
            {
                var editNum = new Textbox(100, _resourceCache);
                editNum.ClearOnSubmit = false;
                editNum.UserData = field;
                editNum.Text = o.ToString();
                editNum.OnSubmit += editNum_OnSubmit;
                return editNum;
            }
            else if (o is bool || o is Boolean)
            {
                var editBool = new Checkbox(_resourceCache);
                editBool.UserData = field;
                editBool.Value = ((Boolean) o);
                editBool.ValueChanged += editBool_ValueChanged;
                return editBool;
            }
            return null;
        }

        //Setting these does not work when inside a key value pair of a list item. fix.

        private void editBool_ValueChanged(bool newValue, Checkbox sender)
        {
            var field = (FieldInfo) sender.UserData;
            if (field.IsInitOnly || field.IsLiteral) return;
            field.SetValue(assigned, newValue);
        }

        private void editNum_OnSubmit(string text, Textbox sender)
        {
            var field = (FieldInfo) sender.UserData;
            object set = null;

            if (field.GetValue(assigned) is float)
                set = float.Parse(text);
            else if (field.GetValue(assigned) is int || field.GetValue(assigned) is Int32)
                set = Int32.Parse(text);
            else if (field.GetValue(assigned) is Int16)
                set = Int16.Parse(text);
            else if (field.GetValue(assigned) is Int64)
                set = Int64.Parse(text);
            else if (field.GetValue(assigned) is double || field.GetValue(assigned) is Double)
                set = Double.Parse(text);
            else if (field.GetValue(assigned) is decimal || field.GetValue(assigned) is Decimal)
                set = Decimal.Parse(text);
            else if (field.GetValue(assigned) is Single)
                set = Single.Parse(text);

            if (field.IsInitOnly || field.IsLiteral) return;
            field.SetValue(assigned, set);
        }

        private void editEnum_ItemSelected(Label item, Listbox sender)
        {
            var field = (FieldInfo) sender.UserData;
            object state = Enum.Parse(field.FieldType, item.Text.Text, true);
            if (field.IsInitOnly || field.IsLiteral) return;
            field.SetValue(assigned, state);
        }

        private void editStr_OnSubmit(string text, Textbox sender)
        {
            var field = (FieldInfo) sender.UserData;
            if (field.IsInitOnly || field.IsLiteral) return;
            field.SetValue(assigned, text);
        }

        private void BuildPropList()
        {
            Type entType = assigned.GetType();
            fields =
                entType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                  BindingFlags.Static);
            int pos = 25;

            foreach (FieldInfo field in fields)
            {
                var newEntry = new PropWindowStruct();
                object fieldVal = field.GetValue(assigned);

                if (fieldVal != null && fieldVal is ICollection)
                {
                    newEntry.VarName = field.Name;
                    newEntry.LabelName = new Label(field.Name + " = " + (fieldVal == null ? "null" : ""), "CALIBRI",
                                                   _resourceCache);
                    newEntry.CanEdit = false;
                    newEntry.IsListItem = false;

                    newEntry.LabelName.Position = new Vector2i(5, pos);
                    newEntry.LabelName.DrawBorder = true;
                    newEntry.LabelName.BorderColor = new SFML.Graphics.Color(240, 255, 240);
                    newEntry.LabelName.BackgroundColor = new SFML.Graphics.Color(128, 128, 128);
                    newEntry.LabelName.DrawBackground = true;
                    newEntry.LabelName.Update(0);

                    pos += 5 + newEntry.LabelName.ClientArea.Height;

                    components.Add(newEntry.LabelName);
                    ObjPropList.Add(newEntry);

                    newEntry = new PropWindowStruct();

                    foreach (object item in (ICollection) fieldVal)
                    {
                        newEntry.VarName = item.ToString();

                        newEntry.CanEdit = true;
                        newEntry.IsListItem = true;
                        newEntry.ListItem = item;

                        newEntry.LabelName = new Label(item.ToString(), "CALIBRI", _resourceCache);
                        newEntry.LabelName.Position = new Vector2i(15, pos);
                        newEntry.LabelName.DrawBorder = true;
                        newEntry.LabelName.BorderColor = new SFML.Graphics.Color(0, 191, 255);
                        newEntry.LabelName.BackgroundColor = new SFML.Graphics.Color(128, 128, 128);
                        newEntry.LabelName.DrawBackground = true;
                        newEntry.LabelName.Clicked += LabelName_Clicked;
                        newEntry.LabelName.Update(0);

                        newEntry.LabelName.Text.Text = item.ToString();
                        newEntry.LabelName.Update(0);
                        pos += 5 + newEntry.LabelName.ClientArea.Height;

                        components.Add(newEntry.LabelName);
                        ObjPropList.Add(newEntry);
                    }
                }
                else
                {
                    newEntry.VarName = field.Name;

                    newEntry.CanEdit = !(field.IsInitOnly || field.IsLiteral);
                    newEntry.IsListItem = false;

                    newEntry.LabelName = new Label(field.Name + " = " + (fieldVal == null ? "null" : ""), "CALIBRI",
                                                   _resourceCache);
                    newEntry.LabelName.Position = new Vector2i(5, pos);
                    newEntry.LabelName.DrawBorder = true;
                    newEntry.LabelName.BorderColor = newEntry.CanEdit ? new SFML.Graphics.Color(127, 255, 0) : new SFML.Graphics.Color(205, 92, 92);
                    newEntry.LabelName.BackgroundColor = new SFML.Graphics.Color(128, 128, 128);
                    newEntry.LabelName.DrawBackground = true;
                    newEntry.LabelName.Clicked += LabelName_Clicked;
                    newEntry.LabelName.Update(0);

                    GuiComponent edit = CreateEditField(fieldVal, field);
                    if (edit != null && newEntry.CanEdit)
                    {
                        edit.Position = new Vector2i(newEntry.LabelName.ClientArea.Right() + 5,
                                                  newEntry.LabelName.ClientArea.Top);
                        components.Add(edit);
                        edit.Update(0);
                        pos += newEntry.LabelName.ClientArea.Height > edit.ClientArea.Height
                                   ? 5 + newEntry.LabelName.ClientArea.Height
                                   : 5 + edit.ClientArea.Height;
                    }
                    else
                    {
                        newEntry.LabelName.Text.Text = field.Name + " = " +
                                                       (fieldVal == null ? "null" : fieldVal.ToString());
                        newEntry.LabelName.Update(0);
                        pos += 5 + newEntry.LabelName.ClientArea.Height;
                    }

                    components.Add(newEntry.LabelName);
                    ObjPropList.Add(newEntry);
                }
            }
        }

        private void LabelName_Clicked(Label sender, MouseButtonEventArgs e)
        {
            switch (e.Button)
            {
                case Mouse.Button.Left:
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
                                object fieldVar = field.GetValue(assigned);
                                if (fieldVar == null) return;

                                RebuildPropList(fieldVar);
                            }
                        }
                        break;
                    }
            }
        }
    }
}
