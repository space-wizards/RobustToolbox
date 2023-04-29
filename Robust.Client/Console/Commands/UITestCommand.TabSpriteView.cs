using System;
using System.Collections.Generic;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Client.Console.Commands;

internal sealed partial class UITestControl
{
    private sealed class TabSpriteView : Control
    {
        private const string EntityId = "debugRotation4";

        private readonly IEntityManager _entMan;
        private readonly BoxContainer _box;

        private record Entry(EntityUid Uid, SpriteComponent Sprite, TransformComponent Transform,
            SpriteView View, Action<Entry, float>? Update);

        private List<Entry> _entries = new();

        private float _degreesPerSecond = 45;

        public TabSpriteView()
        {
            IoCManager.Resolve(ref _entMan);
            SetValue(TabContainer.TabTitleProperty, nameof(SpriteView));
            _box = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Margin = new Thickness(10),
            };
            AddChild(new ScrollContainer() { Children = { _box }});

            var level = IoCManager.Resolve<IBaseClient>().RunLevel;
            if (level != ClientRunLevel.InGame && level != ClientRunLevel.SinglePlayerGame)
            {
                _box.AddChild(new Label
                {
                    Text = "You need to be in-game for this UI to work."
                });
                return;
            }

            _box.AddChild(new Label
            {
                Text = "============================\n" +
                       "Default SpriteView arguments\n" +// override sprite offset, world rotation, fit to control
                       "============================"
            });
            AddEntries();

            _box.AddChild(new Label
            {
                Text = "============================\n" +
                       " Respect entity properties  \n" +// no overrides for offset, rotation, but still fit to control.
                       "============================"
            });
            var added = AddEntries();
            foreach (var e in added)
            {
                e.View.SpriteOffset = true;
                e.View.WorldRotation = null;
            }

            _box.AddChild(new Label
            {
                Text = "============================\n" +
                       "   Transformed SpriteView   \n" +// Default + view transform
                       "============================"
            });
            added = AddEntries();
            foreach (var e in added)
            {
                e.View.Scale = (1,2);
                e.View.EyeRotation = Angle.FromDegrees(45);
                e.View.Offset = (40, 10);
                e.View.WorldRotation = null;
            }

            _box.AddChild(new Label
            {
                Text = "============================\n" +
                       "   64 x 64   - No Stretch   \n" +// as above, but with a fixed UI size & different stretch modes.
                       "==========================="
            });
            added = AddEntries();
            foreach (var e in added)
            {
                e.View.SetSize = (64, 64);
                e.View.Stretch = SpriteView.StretchMode.None;
                e.View.Scale = (1,2);
                e.View.EyeRotation = Angle.FromDegrees(45);
                e.View.Offset = (40, 10);
                e.View.WorldRotation = null;
            }

            _box.AddChild(new Label
            {
                Text = "============================\n" +
                       "  32 x 32   -  Fit to view  \n" +
                       "==========================="
            });
            added = AddEntries();
            foreach (var e in added)
            {
                e.View.SetSize = (32, 32);
                e.View.Stretch = SpriteView.StretchMode.Fit;
                e.View.Scale = (1,2);
                e.View.EyeRotation = Angle.FromDegrees(45);
                e.View.Offset = (40, 10);
                e.View.WorldRotation = null;
            }

            _box.AddChild(new Label
            {
                Text = "============================\n" +
                       "  128 x 128   -   Fill view \n" +
                       "==========================="
            });
            added = AddEntries();
            foreach (var e in added)
            {
                e.View.SetSize = (96, 96);
                e.View.Stretch = SpriteView.StretchMode.Fill;
                e.View.Scale = (1,2);
                e.View.EyeRotation = Angle.FromDegrees(45);
                e.View.Offset = (40, 10);
                e.View.WorldRotation = null;
            }
        }

        public void OnClosed()
        {
            foreach (var e in _entries)
            {
                _entMan.DeleteEntity(e.Uid);
            }
        }

        private List<Entry> AddEntries()
        {
            var added = new List<Entry>();

            var entry = AddEntry("Default", null);
            added.Add(entry);

            entry = AddEntry("World Rotated", (e, time) =>
            {
                e.Transform.LocalRotation += time;
                e.View.InvalidateMeasure();
            });
            added.Add(entry);

            entry = AddEntry("World Rotated (NoRot)", (e, time) =>
            {
                e.Transform.LocalRotation = Angle.FromDegrees(time * _degreesPerSecond);
                e.View.InvalidateMeasure();
            });
            entry.Sprite.NoRotation = true;
            added.Add(entry);

            entry = AddEntry("Offset", (e, time) =>
            {
                e.Sprite.Offset = Angle.FromDegrees(time * _degreesPerSecond).RotateVec(Vector2.One);
                e.View.InvalidateMeasure();
            });
            added.Add(entry);

            entry = AddEntry("Scaled", (e, time) =>
            {
                var theta = (float) Angle.FromDegrees(_degreesPerSecond * time).Theta;
                e.Sprite.Scale = Vector2.One * 1.5f + (MathF.Sin(theta), MathF.Cos(theta));
                e.View.InvalidateMeasure();
            });
            added.Add(entry);

            entry = AddEntry("Rotated", (e, time) =>
            {
                e.Sprite.Rotation = Angle.FromDegrees(time * _degreesPerSecond);
            });
            added.Add(entry);

            entry = AddEntry("Combination", (e, time) =>
            {
                var theta = (float) Angle.FromDegrees(_degreesPerSecond * time * 2).Theta;
                e.Sprite.Scale = Vector2.One * 1.5f + (MathF.Sin(theta), MathF.Cos(theta));
                e.Sprite.Offset = Angle.FromDegrees(time * _degreesPerSecond).RotateVec(Vector2.One);
                e.Sprite.Rotation = Angle.FromDegrees(0.5 * time * _degreesPerSecond);
                e.Transform.LocalRotation = Angle.FromDegrees(0.25 * time * _degreesPerSecond);
                e.View.InvalidateMeasure();
            });
            added.Add(entry);

            return added;
        }

        protected override void FrameUpdate(FrameEventArgs args)
        {
            base.FrameUpdate(args);
            foreach (var entry in _entries)
            {
                entry.Update?.Invoke(entry, args.DeltaSeconds);
            }
        }

        private Entry AddEntry(string text, Action<Entry, float>? onUpdate)
        {
            var box = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                Margin = new Thickness(10)
            };

            var label = new Label()
            {
                MinWidth = 200,
                Text = text
            };

            var ent = _entMan.SpawnEntity(EntityId, MapCoordinates.Nullspace);
            var view = new SpriteView();
            view.SetEntity(ent);

            var viewBox = new PanelContainer()
            {
                Children = { view },
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = Color.White,
                    BorderColor = Color.Black,
                    BorderThickness = new(4),
                }
            };

            box.AddChild(label);
            box.AddChild(viewBox);
            box.Name = text;
            _box.AddChild(box);

            var sprite = _entMan.GetComponent<SpriteComponent>(ent);
            var xform = _entMan.GetComponent<TransformComponent>(ent);
            var entry = new Entry(ent, sprite, xform, view, onUpdate);
            _entries.Add(entry);
            return entry;
        }
    }
}
