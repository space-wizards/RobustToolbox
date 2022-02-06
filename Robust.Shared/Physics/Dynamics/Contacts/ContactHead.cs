// Copyright (c) 2017 Kastellanos Nikolaos (Aether2D)

/*
*
* This software is provided 'as-is', without any express or implied
* warranty.  In no event will the authors be held liable for any damages
* arising from the use of this software.
* Permission is granted to anyone to use this software for any purpose,
* including commercial applications, and to alter it and redistribute it
* freely, subject to the following restrictions:
* 1. The origin of this software must not be misrepresented; you must not
* claim that you wrote the original software. If you use this software
* in a product, an acknowledgment in the product documentation would be
* appreciated but is not required.
* 2. Altered source versions must be plainly marked as such, and must not be
* misrepresented as being the original software.
* 3. This notice may not be removed or altered from any source distribution.
*/

using System.Collections;
using System.Collections.Generic;

namespace Robust.Shared.Physics.Dynamics.Contacts
{
    // So Farseer uses List<T> but box2d and aether both use a linkedlist for world contacts presumably because you need to frequently
    // and remove in the middle of enumeration so this is probably more brrrrrt
    public sealed class ContactHead : Contact, IEnumerable<Contact>
    {
        internal ContactHead(): base(null, 0, null, 0)
        {
            Prev = this;
            Next = this;
        }

        IEnumerator<Contact> IEnumerable<Contact>.GetEnumerator()
        {
            return new ContactEnumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new ContactEnumerator(this);
        }


        #region Nested type: ContactEnumerator

        private struct ContactEnumerator : IEnumerator<Contact>
        {
            private ContactHead _head;
            private Contact _current;

            public Contact Current => _current;
            object IEnumerator.Current => _current;

            public ContactEnumerator(ContactHead contact)
            {
                _head = contact;
                _current = _head;
            }

            public void Reset()
            {
                _current = _head;
            }

            public bool MoveNext()
            {
                _current = _current.Next!;
                return _current != _head;
            }

            public void Dispose()
            {
                _head = null!;
                _current = null!;
            }
        }

        #endregion
    }
}
