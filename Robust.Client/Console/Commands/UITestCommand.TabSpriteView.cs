using System;
using System.Collections.Generic;
using System.Numerics;
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
        private readonly IGameTiming _timing;
        private readonly BoxContainer _box;

        private record Entry(EntityUid Uid, SpriteComponent Sprite, TransformComponent Transform,
            SpriteView View, Action<Entry, float>? Update);

        private List<Entry> _entries = new();

        private float _degreesPerSecond = 45;

        public TabSpriteView()
        {
            IoCManager.Resolve(ref _entMan, ref _timing);
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
                Text = "This control contains a bunch of sprite views. They are grouped based on SpriteView properties.\n" +
                       "Each group has views with different rotation and sprite transformations applied.\n" +
                       "Except for the fixed-size no-stretch view, the sprites should never leave the boxes.\n"
            });

            _box.AddChild(new Label
            {
                Text = "=============================\n" +
                       "Default SpriteView. Ignore sprite offset and world rotation\n" +
                       "============================="
            });
            AddEntries();

            _box.AddChild(new Label
            {
                Text = "============================\n" +
                       "No overrides (Show entity rotation & offset)\n" +
                       "============================="
            });

            foreach (var e in AddEntries())
            {
                e.View.SpriteOffset = true;
                e.View.WorldRotation = null;
            }

            _box.AddChild(new Label
            {
                Text = "=============================\n" +
                       "No override + Transformed view\n" +
                       "============================="
            });

            foreach (var e in AddEntries())
            {
                e.View.Scale = new(1, 0.75f);
                e.View.EyeRotation = Angle.FromDegrees(45);
                e.View.SpriteOffset = true;
                e.View.WorldRotation = null;
            }

            _box.AddChild(new Label
            {
                Text = "============================\n" +
                       "No override + Transform + Fixed Size + No Stretch (sprites can leave).\n" +
                       "============================"
            });

            foreach (var e in AddEntries())
            {
                e.View.SetSize = new(64, 64);
                e.View.Stretch = SpriteView.StretchMode.None;
                e.View.Scale = new(1, 0.75f);
                e.View.EyeRotation = Angle.FromDegrees(45);
                e.View.SpriteOffset = true;
                e.View.WorldRotation = null;
            }

            _box.AddChild(new Label
            {
                Text = "============================\n" +
                       "No override + Transform + Fixed Size + Shrink to view\n" +
                       "============================"
            });

            foreach (var e in AddEntries())
            {
                e.View.SetSize = new(64, 64);
                e.View.Stretch = SpriteView.StretchMode.Fit;
                e.View.Scale = new(1, 0.75f);
                e.View.EyeRotation = Angle.FromDegrees(45);
                e.View.SpriteOffset = true;
                e.View.WorldRotation = null;
            }

            _box.AddChild(new Label
            {
                Text = "============================\n" +
                       "No override + Transform + Fixed Size + Scale to fill view\n" +
                       "============================"
            });

            foreach (var e in AddEntries())
            {
                e.View.SetSize = new(300, 300);
                e.View.Stretch = SpriteView.StretchMode.Fill;
                e.View.Scale = new(1, 0.75f);
                e.View.EyeRotation = Angle.FromDegrees(45);
                e.View.SpriteOffset = true;
                e.View.WorldRotation = null;
            }

            _box.AddChild(new Label
            {
                Text = "============================\n" +
                       "With override + Transform + Fixed Size + Scale to fill view\n" +
                       "============================"
            });

            foreach (var e in AddEntries())
            {
                e.View.SetSize = new(300, 300);
                e.View.Stretch = SpriteView.StretchMode.Fill;
                e.View.Scale = new(1, 0.75f);
                e.View.EyeRotation = Angle.FromDegrees(45);
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

            entry = AddEntry("Local Rotation", (e, time) =>
            {
                e.Transform.LocalRotation = Angle.FromDegrees(time * _degreesPerSecond);
                e.View.InvalidateMeasure();
            });
            added.Add(entry);

            entry = AddEntry("Local Rotation (NoRot)", (e, time) =>
            {
                e.Transform.LocalRotation = Angle.FromDegrees(time * _degreesPerSecond);
                e.View.InvalidateMeasure();
            });
            entry.Sprite.NoRotation = true;
            added.Add(entry);

            entry = AddEntry("Offset", (e, time) =>
            {
                e.Sprite.Offset = new Vector2(MathF.Sin((float) Angle.FromDegrees(time * _degreesPerSecond)), 0);
                e.View.InvalidateMeasure();
            });
            added.Add(entry);

            entry = AddEntry("Scaled", (e, time) =>
            {
                var theta = (float) Angle.FromDegrees(_degreesPerSecond * time).Theta;
                e.Sprite.Scale = Vector2.One + new Vector2(0.5f * MathF.Sin(theta), 0.5f * MathF.Cos(theta));
                e.View.InvalidateMeasure();
            });
            added.Add(entry);

            entry = AddEntry("Sprite Rotation", (e, time) =>
            {
                e.Sprite.Rotation = Angle.FromDegrees(time * _degreesPerSecond);
            });
            added.Add(entry);

            entry = AddEntry("Combination", (e, time) =>
            {
                var theta = (float) Angle.FromDegrees(_degreesPerSecond * time * 2).Theta;
                e.Sprite.Scale = Vector2.One + new Vector2(0.5f * MathF.Sin(theta), 0.5f * MathF.Cos(theta));
                e.Sprite.Offset = new(MathF.Sin((float) Angle.FromDegrees(time * _degreesPerSecond)), 0);
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
            var time = (float) _timing.CurTime.TotalSeconds;
            foreach (var entry in _entries)
            {
                entry.Update?.Invoke(entry, time);
            }
        }

        private Entry AddEntry(string text, Action<Entry, float>? onUpdate)
        {
            var label = new Label
            {
                MinWidth = 200,
                Text = text,
                VerticalAlignment = VAlignment.Center
            };

            var ent = _entMan.SpawnEntity(EntityId, MapCoordinates.Nullspace);
            var view = new SpriteView();
            view.SetEntity(ent);

            var viewBox = new PanelContainer()
            {
                VerticalAlignment = VAlignment.Center,
                Children = { view },
                PanelOverride = new StyleBoxFlat
                {
                    BackgroundColor = Color.White,
                    BorderColor = Color.Black,
                    BorderThickness = new(4),
                }
            };

            _box.AddChild(new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal,
                Margin = new Thickness(10),
                MinHeight = 150,
                Children = { label, viewBox},
                Name = text,
            });

            var sprite = _entMan.GetComponent<SpriteComponent>(ent);
            var xform = _entMan.GetComponent<TransformComponent>(ent);
            var entry = new Entry(ent, sprite, xform, view, onUpdate);
            _entries.Add(entry);
            return entry;
        }
    }
}
