
using System;
using System.Collections;
using System.Collections.Generic;
using SS14.Client.Graphics.VertexData;
using VertexFieldContext = SS14.Client.Graphics.VertexData.VertexEnums.VertexFieldContext;
using VertexFieldType = SS14.Client.Graphics.VertexData.VertexEnums.VertexFieldType;



namespace SS14.Client.Graphics.VertexData
{
    /// <summary>
    /// Vertex declaration system.
    /// From here we can set up our custom vertices.
    /// </summary>
    /// <remarks>
    /// Not all models are created equal.  Some will contain position data, and color information, some
    /// will contain texture coordinates, normals, etc...  This will allow us to manage different types
    /// of vertices so that we can efficiently use mesh data.
    /// </remarks>
    public class VertexType : IDisposable
    {
        #region Variables.
        private bool _changed;						// Flag to indicate that the vertex has changed.
        private bool _sizeChanged;					// Flag to indicate that the size has changed.
        private int _vertexSize;					// Size of the vertex in bytes.
      
        private List<VertexField> _fields;			// List of vertex fields.
        #endregion

        #region Properties.
        /// <summary>
        /// Property to create and return the Direct 3D vertex declaration.
        /// </summary>
      

        /// <summary>
        /// Property to return whether this type has changed or not.
        /// </summary>
        public bool NeedsUpdate
        {
            get
            {
                return _changed;
            }
        }

        /// <summary>
        /// Property to return the number of fields in the list.
        /// </summary>
        public int Count
        {
            get
            {
                return _fields.Count;
            }
        }
        #endregion

        #region Methods.
        /// <summary>
        /// Function to add a field to this vertex.
        /// </summary>
        /// <param name="stream">Stream to bind this field with.</param>
        /// <param name="fieldOffset">Offset of this field within the vertex type..</param>
        /// <param name="context">Context of this field.</param>
        /// <param name="fieldType">Data type of the field.</param>
        public void CreateField ( short stream , short fieldOffset , VertexFieldContext context , VertexFieldType fieldType )
        {
            CreateField(stream , fieldOffset , context , fieldType , 0);
        }

        /// <summary>
        /// Function to add a field to this vertex.
        /// </summary>
        /// <param name="stream">Stream to bind this field with.</param>
        /// <param name="fieldOffset">Offset of this field within the vertex type..</param>
        /// <param name="context">Context of this field.</param>
        /// <param name="fieldType">Data type of the field.</param>
        /// <param name="index">Index of the field, only required for certain types.</param>
        public void CreateField ( short stream , short fieldOffset , VertexFieldContext context , VertexFieldType fieldType , byte index )
        {
            VertexField newField;	// A new vertex field.

            newField = new VertexField(stream , fieldOffset , context , fieldType , index);
            _fields.Add(newField);

            _changed = true;
            _sizeChanged = true;
        }

        /// <summary>
        /// Function to remove a field from the vertex.
        /// </summary>
        /// <param name="index">Index of the field to remove.</param>
        public void Remove ( int index )
        {
            _fields.RemoveAt(index);
            _changed = true;
            _sizeChanged = true;
        }

        /// <summary>
        /// Function to remove a field from the vertex.
        /// </summary>
        /// <param name="stream">Stream to which the field is bound.</param>
        /// <param name="context">Context of the field we wish to remove.</param>
        public void Remove ( short stream , VertexFieldContext context )
        {
            int i;					// loop.
            for (i = _fields.Count - 1 ; i >= 0 ; i--)
            {
                if ((_fields[i].Context == context) && (_fields[i].Stream == stream))
                    Remove(i);
            }
        }

        /// <summary>
        /// Function to update a vertex _fields[i].
        /// </summary>
        /// <param name="stream">Stream to which the field is bound.</param>
        /// <param name="fieldIndex">Index of the field to update</param>
        /// <param name="fieldOffset">Offset within the buffer.</param>
        /// <param name="context">Context of this field.</param>
        /// <param name="fieldType">Data type of the field.</param>
        /// <param name="index">Index of the field, only required for certain types.</param>
        public void UpdateField ( short stream , int fieldIndex , short fieldOffset , VertexFieldContext context , VertexFieldType fieldType , byte index )
        {
            if ((fieldIndex < 0) || (fieldIndex >= _fields.Count))
                throw new IndexOutOfRangeException("The index " + index.ToString() + " is not valid for this collection.");

            _fields[fieldIndex] = new VertexField(stream , fieldOffset , context , fieldType , index);

            _changed = true;
            _sizeChanged = true;
        }

        /// <summary>
        /// Function to update a vertex _fields[i].
        /// </summary>
        /// <param name="stream">Stream to which the field is bound.</param>
        /// <param name="fieldIndex">Index of the field to update</param>
        /// <param name="fieldOffset">Offset within the buffer.</param>
        /// <param name="context">Context of this field.</param>
        /// <param name="fieldType">Data type of the field.</param>
        public void UpdateField ( short stream , int fieldIndex , short fieldOffset , VertexFieldContext context , VertexFieldType fieldType )
        {
            UpdateField(stream , fieldIndex , fieldOffset , context , fieldType , 0);
        }


        /// <summary>
        /// Function to insert a vertex field at a specified index.
        /// </summary>
        /// <param name="stream">Stream to which the field will be bound.</param>
        /// <param name="fieldIndex">Index after which to insert.</param>
        /// <param name="fieldOffset">Offset of the field in the buffer.</param>
        /// <param name="context">Context of this field.</param>
        /// <param name="fieldType">Data type of this field.</param>
        public void InsertField ( short stream , int fieldIndex , short fieldOffset , VertexFieldContext context , VertexFieldType fieldType )
        {
            InsertField(stream , fieldIndex , fieldOffset , context , fieldType , 0);
        }

        /// <summary>
        /// Function to insert a vertex field at a specified index.
        /// </summary>
        /// <param name="stream">Stream to which the field will be bound.</param>
        /// <param name="fieldIndex">Index after which to insert.</param>
        /// <param name="fieldOffset">Offset of the field in the buffer.</param>
        /// <param name="context">Context of this field.</param>
        /// <param name="fieldType">Data type of this field.</param>
        /// <param name="index">Index of the vertex field, required for certain fields.</param>
        public void InsertField ( short stream , int fieldIndex , short fieldOffset , VertexFieldContext context , VertexFieldType fieldType , byte index )
        {
            VertexField newField;	// New _fields[i].

            newField = new VertexField(stream , fieldOffset , context , fieldType , index);
            _fields.Insert(fieldIndex , newField);

            _changed = true;
            _sizeChanged = true;
        }

        /// <summary>
        /// Function to retrieve whether this vertex definition contains a field of a particular context type.
        /// </summary>
        /// <param name="stream">Stream to check.</param>
        /// <param name="context">Context to check for.</param>
        /// <returns>TRUE if context exists, FALSE if not.</returns>
        public bool HasFieldContext ( short stream , VertexFieldContext context )
        {
            int i;	// loop.

            for (i = 0 ; i < _fields.Count ; i++)
            {
                if ((_fields[i].Context == context) && (_fields[i].Stream == stream))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve whether this vertex definition contains a field of a particular context type.
        /// </summary>
        /// <param name="context">Context to check for.</param>
        /// <returns>TRUE if context exists, FALSE if not.</returns>
        public bool HasFieldContext ( VertexFieldContext context )
        {
            int i;	// loop.

            for (i = 0 ; i < _fields.Count ; i++)
            {
                if (_fields[i].Context == context)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Function to retrieve whether this vertex definition has a particular field type.
        /// </summary>
        /// <param name="stream">Stream to check.</param>
        /// <param name="fieldType">Type of data to check for.</param>
        /// <returns>TRUE if the type is present, FALSE if not.</returns>
        public bool HasFieldType ( short stream , VertexFieldType fieldType )
        {
            int i;					// loop.

            for (i = 0 ; i < _fields.Count ; i++)
            {
                if ((_fields[i].Type == fieldType) && (_fields[i].Stream == stream))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Function to return the size of the vertex in bytes, _fieldsd on stream.
        /// </summary>
        /// <param name="stream">Stream index to check.</param>
        /// <returns>Size of the vertex in bytes.</returns>
        public int VertexSize ( short stream )
        {
            int i;				// Loop.

            // Loop through and get sizes.
            if (_sizeChanged)
            {
                _vertexSize = 0;
                for (i = 0 ; i < _fields.Count ; i++)
                {
                    if (_fields[i].Stream == stream)
                        _vertexSize += _fields[i].Bytes;
                }
                _sizeChanged = false;
            }

            return _vertexSize;
        }

        /// <summary>
        /// Function to invalidate the declaration.
        /// </summary>
        public void Invalidate ( )
        {
            _changed = true;
            _sizeChanged = true;
        }

        /// <summary>
        /// Function to clone this vertex type.
        /// </summary>
        /// <returns>A copy of this vertex type.</returns>
        public VertexType Clone ( )
        {
            VertexType newVertexType = new VertexType();	// New vertex type.

            // Copy.
            for (int i = 0 ; i < _fields.Count ; i++)
                newVertexType.CreateField(_fields[i].Stream , _fields[i].Offset , _fields[i].Context , _fields[i].Type , _fields[i].Index);

            return newVertexType;
        }

        /// <summary>
        /// Determines whether the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>.
        /// </summary>
        /// <param name="obj">The <see cref="T:System.Object"></see> to compare with the current <see cref="T:System.Object"></see>.</param>
        /// <returns>
        /// true if the specified <see cref="T:System.Object"></see> is equal to the current <see cref="T:System.Object"></see>; otherwise, false.
        /// </returns>
        public override bool Equals ( object obj )
        {
            VertexType comparer = obj as VertexType;		// Comparison object.

            if ((comparer != null) && (comparer.Count == Count))
            {
                // Compare fields.
                for (int i = 0 ; i < _fields.Count ; i++)
                {
                    if (!comparer.Contains(_fields[i]))
                        return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Property to return whether a field exists within this type or not.
        /// </summary>
        /// <param name="field">Field value.</param>
        /// <returns>TRUE if it exists, FALSE if not.</returns>
        public bool Contains ( VertexField field )
        {
            return _fields.Contains(field);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing algorithms and data structures like a hash table.
        /// </summary>
        /// <returns>
        /// A hash code for the current <see cref="T:System.Object"></see>.
        /// </returns>
        public override int GetHashCode ( )
        {
            int code = 0;		// Hash code.

            for (int i = 0 ; i < _fields.Count ; i++)
                code ^= _fields[i].GetHashCode();

            return code;
        }
        #endregion

        #region Properties.
        /// <summary>
        /// Property to return a vertex field by its index.
        /// </summary>
        public VertexField this[int index]
        {
            get
            {
                return this[index , 0];
            }
        }

        /// <summary>
        /// Property to return a vertex field by its context.
        /// </summary>
        public VertexField this[VertexFieldContext context]
        {
            get
            {
                return this[context , 0];
            }
        }

        /// <summary>
        /// Property to return a vertex field by its index.
        /// </summary>
        public VertexField this[int index , int fieldindex]
        {
            get
            {
                VertexField field;		// Field in question

                for (int i = 0 ; i < _fields.Count ; i++)
                {
                    field = _fields[i];
                    if (field.Index == fieldindex)
                        return field;
                }

                throw new IndexOutOfRangeException("The index " + index.ToString() + " is not valid for this collection.");
            }
        }

        /// <summary>
        /// Property to return a vertex field by its context.
        /// </summary>
        public VertexField this[VertexFieldContext context , int fieldindex]
        {
            get
            {
                VertexField field;		// Field in question

                for (int i = 0 ; i < _fields.Count ; i++)
                {
                    field = _fields[i];
                    if ((field.Context == context) && (field.Index == fieldindex))
                        return field;
                }

                throw new ArgumentException("There is no vertex field context '" + context.ToString() + "' in this vertex type." , "context");
            }
        }
        #endregion

        #region Constructors and Destructors.
        /// <summary>
        /// Constructor.
        /// </summary>
        internal VertexType ( )
        {
            _fields = new List<VertexField>();
            _changed = true;
            _sizeChanged = true;
        }
        #endregion

        #region IDisposable Members
        /// <summary>
        /// Function to perform clean up.
        /// </summary>
        /// <param name="disposing">TRUE to dispose all resources, FALSE to only release unmanaged.</param>
        private void Dispose ( bool disposing )
        {
            if (disposing)
            {

                _fields.Clear();
                _fields = null;
              
            }
        }

        /// <summary>
        /// Function to perform clean up.
        /// </summary>
        public void Dispose ( )
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}