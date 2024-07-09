using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Audio;
using Robust.Client.ResourceManagement;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;
using Robust.Shared.ContentPack;
using Robust.Shared.Input;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.ViewVariables;

namespace Robust.Client.UserInterface.Controls
{

    /// <summary>
    /// A generic control that represents a clickable item slot with a sprite. Essentially a  ContainerButton with a SpriteView inside.
    /// The only way to add the SpriteView is through the SetEntity method.
    /// It also has a label that can be used to display any text.
    ///
    /// See <seealso cref="ContainerButton"/> and <seealso cref="SpriteView"/>
    ///
    /// </summary>
    ///
    [Virtual]
    public class ItemSlotButton : ContainerButton
    {
        protected SpriteView _sprite;

        /// <summary>
        ///   The BoxContainer that contains the sprite and the label. This is the only direct child of the ItemSlotButton.
        /// </summary>
        protected BoxContainer _box;

        public Label Label { get; }

        /// <summary>
        ///    The position of the label relative to the sprite.
        ///    If set to Top, the label will be above the sprite.
        ///    If set to Bottom, the label will be below the sprite.
        /// </summary>
        public LabelPositionMode LabelPosition { get; set; } = LabelPositionMode.Bottom;

        // this is a workaround to fix the label position as the label is usually added after the sprite
        public void SetLabelPosition(LabelPositionMode position)
        {
            if (Label.Parent == null)
            {
                return;
            }

            LabelPosition = position;
            if (position == LabelPositionMode.Top)
            {
                Label.SetPositionFirst();
            }
            else
            {
                Label.SetPositionLast();
            }
        }

        /// <summary>
        ///  The text displayed by the button.
        ///  If the text is null or empty, the label will be removed from the button.
        ///  <seealso cref="Label.Text"/>
        ///  <seealso cref="Label"/>
        public string? Text
        {
            get => Label.Text;
            set
            {
                if (Label.Text == value)
                    return;

                if (string.IsNullOrWhiteSpace(value) && Label.Parent != null)
                {
                    _box.RemoveChild(Label);
                }
                else if (Label.Parent != _box)
                {
                    _box.AddChild(Label);
                }
                Label.Text = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///     The padding of the button aka the Margin of the sprite inside it's root BoxContainer.
        ///     This forces the sprite's borders to have more space.
        ///     99% of the time It is recommended to use MinSize instead of this, but this can be useful for fine tuning how far the sprite is from the Label on some cases.
        ///     <seealso cref="Control.Margin">
        ///     <seealso cref="Control.MinSize">
        ///     <seealso cref="BoxContainer"/>
        /// </summary>
        public Thickness Padding
        {
            get => _sprite.Margin;
            set
            {
                _sprite.Margin = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///  The scale of the sprite.
        /// </summary>
        public Vector2 Scale
        {
            get => _sprite.Scale;
            set
            {
                _sprite.Scale = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///    The rotation of the sprite in radians.
        ///    <seealso cref="SpriteView.WorldRotation"/>
        /// </summary>
        public Angle? WorldRotation
        {
            get => _sprite.WorldRotation;
            set
            {
                _sprite.WorldRotation = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///    The rotation of the sprite in radians.
        ///    <seealso cref="SpriteView.EyeRotation"/>
        /// </summary>
        public Angle EyeRotation
        {
            get => _sprite.EyeRotation;
            set
            {
                _sprite.EyeRotation = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        ///    Overrides the direction the sprite is facing.
        ///    <seealso cref="SpriteView.OverrideDirection"/>
        /// </summary>
        public Direction? OverrideDirection
        {
            get => _sprite.OverrideDirection;
            set
            {
                _sprite.OverrideDirection = value;
                InvalidateMeasure();
            }
        }

        public ItemSlotButton()
        {
            AddStyleClass(StyleClassButton);

            _sprite = new SpriteView();
            _sprite.VerticalExpand = true;

            Label = new Label
            {
                StyleClasses = { StyleClassButton },
                VerticalExpand = true
            };
            _box = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 0
            };

            AddChild(_box);
        }

        public ItemSlotButton(IEntityManager entMan)
        {
            AddStyleClass(StyleClassButton);

            _sprite = new SpriteView(entMan);
            _sprite.VerticalExpand = true;

            Label = new Label
            {
                StyleClasses = { StyleClassButton }
            };
            _box = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 0
            };

            _box.AddChild(Label);

            SetLabelPosition(LabelPosition);
            AddChild(_box);
        }

        public ItemSlotButton(EntityUid? uid, IEntityManager entMan)
        {
            AddStyleClass(StyleClassButton);

            _sprite = new SpriteView(uid, entMan);
            _sprite.VerticalExpand = true;

            SetEntity(uid);

            Label = new Label
            {
                StyleClasses = { StyleClassButton }
            };
            _box = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 0
            };

            _box.AddChild(Label);

            SetLabelPosition(LabelPosition);
            AddChild(_box);
        }

        public ItemSlotButton(NetEntity uid, IEntityManager entMan)
        {
            AddStyleClass(StyleClassButton);
            _sprite = new SpriteView(uid, entMan);
            _sprite.VerticalExpand = true;
            SetEntity(uid);

            Label = new Label
            {
                StyleClasses = { StyleClassButton },
            };
            _box = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                SeparationOverride = 0
            };

            _box.AddChild(Label);

            SetLabelPosition(LabelPosition);
            AddChild(_box);
        }

        /// <summary>
        ///   Sets the entity of the sprite.
        ///
        ///   If the entity is null, the sprite will be removed from the button.
        ///   Also sets the Label's VerticalExpand accordingly
        ///   <seealso cref="SpriteView.SetEntity"/>
        /// </summary>
        /// <param name="uid"></param>
        public void SetEntity(EntityUid? uid)
        {
            if (uid == null && _sprite.Parent == _box)
            {
                _box.RemoveChild(_sprite);

                if (Label.Parent != null)
                    Label.VerticalExpand = true;

                return;
            }

            // if the sprite is already a child of the box, remove it, change it, then add it again
            // this works around the issue of the sprite not updating when the entity is changed from null to a valid entity
            if (_sprite.Parent == _box)
                _box.RemoveChild(_sprite);

            _sprite.SetEntity(uid);
            _box.AddChild(_sprite);

            if (Label.Parent != null)
                Label.VerticalExpand = false;

            SetLabelPosition(LabelPosition);
        }

        public void SetEntity(NetEntity uid)
        {
            if (uid == null && _sprite.Parent == _box)
            {
                _box.RemoveChild(_sprite);

                if (Label.Parent != null)
                    Label.VerticalExpand = true;

                return;
            }

            // if the sprite is already a child of the box, remove it, change it, then add it again
            // this works around the issue of the sprite not updating when the entity is changed from null to a valid entity
            if (_sprite.Parent == _box)
                _box.RemoveChild(_sprite);

            _sprite.SetEntity(uid);
            _box.AddChild(_sprite);

            if (Label.Parent != null)
                Label.VerticalExpand = false;

            SetLabelPosition(LabelPosition);
        }

        public enum LabelPositionMode
        {
            Bottom,
            Top
        }
    }

}
