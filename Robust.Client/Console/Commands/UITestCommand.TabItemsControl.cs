using System;
using System.Collections.ObjectModel;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Robust.Client.Console.Commands;

internal sealed partial class UITestControl
{
    private sealed class TabItemsControl : Control
    {
        private readonly Random _random = new();
        private readonly ObservableCollection<int> _collection = [];

        private int _counter = 0;

        public TabItemsControl()
        {
            var addButton = new Button
            {
                Text = "Add Item",
            };
            addButton.OnPressed += AddButtonOnOnPressed;

            var removeButton = new Button
            {
                Text = "Remove Item"
            };
            removeButton.OnPressed += RemoveButtonOnOnChildAdded;

            var replaceButton = new Button
            {
                Text = "Replace Item"
            };
            replaceButton.OnPressed += ReplaceButtonOnOnPressed;

            var moveButton = new Button
            {
                Text = "Move Item"
            };
            moveButton.OnPressed += MoveButtonOnOnPressed;

            var resetButton = new Button
            {
                Text = "Reset Item"
            };
            resetButton.OnPressed += ResetButtonOnOnPressed;

            AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                Children =
                {
                    new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        HorizontalExpand = true,
                        Children =
                        {
                            addButton,
                            removeButton,
                            replaceButton,
                            moveButton,
                            resetButton
                        }
                    },
                    new ScrollContainer
                    {
                        HorizontalExpand = true,
                        Children =
                        {
                            new ItemsControl
                            {
                                ItemsSource = _collection,
                            }
                        }
                    }
                }
            });
        }

        private void AddButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            var value = _counter++;
            var index = _random.Next(_collection.Count + 1);
            Log.Info($"Inserted {value} at {index}");
            _collection.Insert(index, value);
        }

        private void RemoveButtonOnOnChildAdded(BaseButton.ButtonEventArgs obj)
        {
            if (_collection.Count == 0)
                return;

            var index = _random.Next(_collection.Count);
            Log.Info($"Removed {index} ({_collection[index]})");
            _collection.RemoveAt(index);
        }

        private void ReplaceButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            if (_collection.Count == 0)
                return;

            var index = _random.Next(_collection.Count);
            var value = _counter++;

            Log.Info($"Replaced {index} ({_collection[index]}) with {value}");
            _collection[index] = value;
        }

        private void MoveButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            if (_collection.Count == 0)
                return;

            var oldIndex = _random.Next(_collection.Count);
            var newIndex = _random.Next(_collection.Count);

            Log.Info($"Moved {oldIndex} ({_collection[oldIndex]}) -> {newIndex}");
            _collection.Move(oldIndex, newIndex);
        }

        private void ResetButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            Log.Info("Reset list");
            _collection.Clear();
        }
    }
}
