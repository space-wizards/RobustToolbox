using System;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace SFML.Graphics.Design
{
    abstract class MemberPropertyDescriptor : PropertyDescriptor
    {
        readonly MemberInfo _member;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberPropertyDescriptor"/> class.
        /// </summary>
        /// <param name="member">The member.</param>
        protected MemberPropertyDescriptor(MemberInfo member)
            : base(member.Name, (Attribute[])member.GetCustomAttributes(typeof(Attribute), true))
        {
            _member = member;
        }

        /// <summary>
        /// When overridden in a derived class, gets the type of the component this property is bound to.
        /// </summary>
        /// <value></value>
        /// <returns>A <see cref="T:System.Type"/> that represents the type of component this property is bound to.
        /// When the <see cref="M:System.ComponentModel.PropertyDescriptor.GetValue(System.Object)"/>
        /// or <see cref="M:System.ComponentModel.PropertyDescriptor.SetValue(System.Object,System.Object)"/>
        /// methods are invoked, the object specified might be an instance of this type.</returns>
        public override Type ComponentType
        {
            get { return _member.DeclaringType; }
        }

        /// <summary>
        /// When overridden in a derived class, gets a value indicating whether this property is read-only.
        /// </summary>
        /// <value></value>
        /// <returns>true if the property is read-only; otherwise, false.</returns>
        public override bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// When overridden in a derived class, returns whether resetting an object changes its value.
        /// </summary>
        /// <param name="component">The component to test for reset capability.</param>
        /// <returns>
        /// true if resetting the component changes its value; otherwise, false.
        /// </returns>
        public override bool CanResetValue(object component)
        {
            return false;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object"/> to compare with this instance.</param>
        /// <returns>
        /// 	<c>true</c> if the specified <see cref="System.Object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            var descriptor = obj as MemberPropertyDescriptor;
            return ((descriptor != null) && descriptor._member.Equals(_member));
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return _member.GetHashCode();
        }

        /// <summary>
        /// When overridden in a derived class, resets the value for this property of the component to the default value.
        /// </summary>
        /// <param name="component">The component with the property value that is to be reset to the default value.</param>
        public override void ResetValue(object component)
        {
        }

        /// <summary>
        /// When overridden in a derived class, determines a value indicating whether the value of this property needs
        /// to be persisted.
        /// </summary>
        /// <param name="component">The component with the property to be examined for persistence.</param>
        /// <returns>
        /// true if the property should be persisted; otherwise, false.
        /// </returns>
        public override bool ShouldSerializeValue(object component)
        {
            return true;
        }
    }
}