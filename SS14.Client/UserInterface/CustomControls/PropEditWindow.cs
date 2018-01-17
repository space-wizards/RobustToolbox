using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using OpenTK.Graphics;
using SS14.Client.Graphics.Input;
using SS14.Client.UserInterface.Controls;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Maths;

namespace SS14.Client.UserInterface.CustomControls
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
        private object _entity;
        private FieldInfo[] fields;

        private readonly ListPanel _fieldList;

        public PropEditWindow(Vector2i size, IEntity entity)
            : base($"Entity Properties: [{entity.Uid}]{entity.Name}", size)
        {
            _entity = entity;

            search = new Textbox(150);
            search.LocalPosition = new Vector2i(5, 5);
            search.OnSubmit += search_OnSubmit;
            search.ClearOnSubmit = true;
            search.ClearFocusOnSubmit = false;
            Container.AddControl(search);

            _fieldList = new ListPanel();
            _fieldList.LocalPosition = new Vector2i(5, 0);
            _fieldList.Alignment = Align.Bottom;
            search.AddControl(_fieldList);

            BuildPropList(_fieldList);
        }

        private void search_OnSubmit(Textbox sender, string text)
        {
            foreach (var struc in ObjPropList)
            {
                if (struc.VarName.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                    struc.LabelName.BackgroundColor = new Color4(255, 228, 196, 255);
                else
                    struc.LabelName.BackgroundColor = Color4.Gray;
            }
        }

        private void RebuildPropList(object obj)
        {
            if (ScrollbarH.Visible) ScrollbarH.Value = 0;
            if (ScrollbarV.Visible) ScrollbarV.Value = 0;

            foreach (var control in _fieldList.Children.ToList())
            {
                _fieldList.RemoveControl(control);
            }

            ObjPropList.Clear();
            _entity = obj;
            title.Text = $"Entity Properties: {_entity}";
            BuildPropList(_fieldList);
            Container.DoLayout();
        }

        private Control CreateEditField(object obj, FieldInfo field)
        {
            switch (obj)
            {
                case string s:
                    var editStr = new Textbox(100);
                    editStr.ClearOnSubmit = false;
                    editStr.UserData = field;
                    editStr.Text = s;
                    editStr.OnSubmit += editStr_OnSubmit;
                    return editStr;

                case Enum _:
                    var editEnum = new Listbox(100, 100, Enum.GetNames(obj.GetType()).ToList());
                    editEnum.UserData = field;
                    editEnum.SelectItem(obj.ToString());
                    editEnum.ItemSelected += editEnum_ItemSelected;
                    return editEnum;

                default:
                    if (obj is float || obj is int || obj is short || obj is long || obj is double || obj is decimal)
                    {
                        var editNum = new Textbox(100);
                        editNum.ClearOnSubmit = false;
                        editNum.UserData = field;
                        editNum.Text = obj.ToString();
                        editNum.OnSubmit += editNum_OnSubmit;
                        return editNum;
                    }
                    else if (obj is bool)
                    {
                        var editBool = new Checkbox();
                        editBool.UserData = field;
                        editBool.Value = (bool) obj;
                        editBool.ValueChanged += editBool_ValueChanged;
                        return editBool;
                    }
                    break;
            }
            return null;
        }

        //Setting these does not work when inside a key value pair of a list item. fix.

        private void editBool_ValueChanged(bool newValue, Checkbox sender)
        {
            var field = (FieldInfo) sender.UserData;
            if (field.IsInitOnly || field.IsLiteral) return;
            field.SetValue(_entity, newValue);
        }

        private void editNum_OnSubmit(Textbox sender, string text)
        {
            var field = (FieldInfo) sender.UserData;
            object set = null;

            if (field.GetValue(_entity) is float)
                set = float.Parse(text);
            else if (field.GetValue(_entity) is int || field.GetValue(_entity) is int)
                set = int.Parse(text);
            else if (field.GetValue(_entity) is short)
                set = short.Parse(text);
            else if (field.GetValue(_entity) is long)
                set = long.Parse(text);
            else if (field.GetValue(_entity) is double || field.GetValue(_entity) is double)
                set = double.Parse(text);
            else if (field.GetValue(_entity) is decimal || field.GetValue(_entity) is decimal)
                set = decimal.Parse(text);
            else if (field.GetValue(_entity) is float)
                set = float.Parse(text);

            if (field.IsInitOnly || field.IsLiteral) return;
            field.SetValue(_entity, set);
        }

        private void editEnum_ItemSelected(Label item, Listbox sender)
        {
            var field = (FieldInfo) sender.UserData;
            var state = Enum.Parse(field.FieldType, item.Text, true);
            if (field.IsInitOnly || field.IsLiteral) return;
            field.SetValue(_entity, state);
        }

        private void editStr_OnSubmit(Textbox sender, string text)
        {
            var field = (FieldInfo) sender.UserData;
            if (field.IsInitOnly || field.IsLiteral) return;
            field.SetValue(_entity, text);
        }

        private void BuildPropList(ListPanel parent)
        {
            var entType = _entity.GetType();
            fields =
                entType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                  BindingFlags.Static);
            var pos = 25;

            foreach (var field in fields)
            {
                var newEntry = new PropWindowStruct();
                var fieldVal = field.GetValue(_entity);

                if (fieldVal != null && fieldVal is ICollection)
                {
                    newEntry.VarName = field.Name;
                    newEntry.LabelName = new Label(field.Name + " = " + (fieldVal == null ? "null" : ""), "CALIBRI");
                    newEntry.CanEdit = false;
                    newEntry.IsListItem = false;

                    newEntry.LabelName.Position = new Vector2i(5, pos);
                    newEntry.LabelName.DrawBorder = true;
                    newEntry.LabelName.BorderColor = new Color4(240, 255, 240, 255);
                    newEntry.LabelName.BackgroundColor = Color4.Gray;
                    newEntry.LabelName.DrawBackground = true;
                    newEntry.LabelName.Update(0);

                    pos += 5 + newEntry.LabelName.ClientArea.Height;

                    parent.AddControl(newEntry.LabelName);
                    ObjPropList.Add(newEntry);

                    newEntry = new PropWindowStruct();

                    foreach (var item in (ICollection) fieldVal)
                    {
                        newEntry.VarName = item.ToString();

                        newEntry.CanEdit = true;
                        newEntry.IsListItem = true;
                        newEntry.ListItem = item;

                        newEntry.LabelName = new Label(item.ToString(), "CALIBRI");
                        newEntry.LabelName.Position = new Vector2i(15, pos);
                        newEntry.LabelName.DrawBorder = true;
                        newEntry.LabelName.BorderColor = new Color4(0, 191, 255, 255);
                        newEntry.LabelName.BackgroundColor = Color4.Gray;
                        newEntry.LabelName.DrawBackground = true;
                        newEntry.LabelName.Clicked += LabelName_Clicked;
                        newEntry.LabelName.Update(0);

                        newEntry.LabelName.Text = item.ToString();
                        newEntry.LabelName.Update(0);
                        pos += 5 + newEntry.LabelName.ClientArea.Height;

                        parent.AddControl(newEntry.LabelName);
                        ObjPropList.Add(newEntry);
                    }
                }
                else
                {
                    newEntry.VarName = field.Name;

                    newEntry.CanEdit = !(field.IsInitOnly || field.IsLiteral);
                    newEntry.IsListItem = false;

                    newEntry.LabelName = new Label(field.Name + " = " + (fieldVal == null ? "null" : ""), "CALIBRI");
                    newEntry.LabelName.Position = new Vector2i(5, pos);
                    newEntry.LabelName.DrawBorder = true;
                    newEntry.LabelName.BorderColor = newEntry.CanEdit ? new Color4(127, 255, 0, 255) : new Color4(205, 92, 92, 255);
                    newEntry.LabelName.BackgroundColor = Color4.Gray;
                    newEntry.LabelName.DrawBackground = true;
                    newEntry.LabelName.Clicked += LabelName_Clicked;
                    newEntry.LabelName.Update(0);

                    var edit = CreateEditField(fieldVal, field);
                    if (edit != null && newEntry.CanEdit)
                    {
                        edit.Position = new Vector2i(newEntry.LabelName.ClientArea.Right + 5,
                            newEntry.LabelName.ClientArea.Top);
                        parent.AddControl(edit);
                        edit.Update(0);
                        pos += newEntry.LabelName.ClientArea.Height > edit.ClientArea.Height
                            ? 5 + newEntry.LabelName.ClientArea.Height
                            : 5 + edit.ClientArea.Height;
                    }
                    else
                    {
                        newEntry.LabelName.Text = field.Name + " = " +
                                                  (fieldVal == null ? "null" : fieldVal.ToString());
                        newEntry.LabelName.Update(0);
                        pos += 5 + newEntry.LabelName.ClientArea.Height;
                    }

                    parent.AddControl(newEntry.LabelName);
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
                        if (selected.Value.IsListItem)
                        {
                            if (selected.Value.ListItem != null)
                                RebuildPropList(selected.Value.ListItem);
                        }
                        else if (fields.First(x => x.Name == selected.Value.VarName) != null)
                        {
                            var field = fields.First(x => x.Name == selected.Value.VarName);
                            var fieldVar = field.GetValue(_entity);
                            if (fieldVar == null) return;

                            RebuildPropList(fieldVar);
                        }
                    break;
                }
            }
        }
    }
}
