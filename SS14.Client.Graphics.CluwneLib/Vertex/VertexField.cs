using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using SS14.Client.Graphics.CluwneLib.Vertex;
using VertexFieldContext = SS14.Client.Graphics.CluwneLib.Vertex.VertexEnums.VertexFieldContext;
using VertexFieldType = SS14.Client.Graphics.CluwneLib.Vertex.VertexEnums.VertexFieldType;


namespace SS14.Client.Graphics.CluwneLib.Vertex
{
	/// <summary>
	/// VertexField
	/// </summary>
	public class VertexField
	{
		#region Variables.
		private short _stream;					                // Stream to which this element is bound.
		private short _offset;					                // Offset of the field within the field type.
        private VertexFieldContext _context;	                // The purpose of this field.
		private VertexFieldType   _type;			            // Data type of this field.
		private byte _index;					                // Index of the item, only applicable to certain types of fields. (i.e. textures).
		private short _size;					                // Size of this field in bytes.
		#endregion

		#region Properties.
		/// <summary>
		/// Property to return the offset of this field within the field type.
		/// </summary>
		public short Offset
		{
			get
			{
				return _offset;
			}
		}

		/// <summary>
		/// Property to return the context of this field.
		/// </summary>
		public VertexFieldContext Context
		{
			get
			{
				return _context;
			}
		}

		/// <summary>
		/// Property to return the data type of the field.
		/// </summary>
		public VertexFieldType Type
		{
			get
			{
				return _type;
			}
		}

		/// <summary>
		/// Property to return the index of this field.
		/// Only applicable to particular types of fields (i.e. textures).
		/// </summary>
		public byte Index
		{
			get
			{
				return _index;
			}
		}

		/// <summary>
		/// Property to return the number of bytes occupied by this field.
		/// </summary>
		public int Bytes
		{
			get
			{
				return _size;
			}
		}

		/// <summary>
		/// Property to return the stream to which this element is bound.
		/// </summary>
		public short Stream
		{
			get
			{
				return _stream;
			}
		}
		#endregion

		#region Methods.
		/// <summary>
		/// Function to return the size in bytes of a vertex field.
		/// </summary>
		/// <param name="type">Type of the field to evaluate.</param>
		/// <returns>Size of the field in bytes.</returns>
		static public int SizeOf(VertexFieldType type)
		{
			switch(type)
			{
				case VertexFieldType.Float1:
					return sizeof(float);
				case VertexFieldType.Float2:
					return sizeof(float) * 2;
				case VertexFieldType.Float3:
					return sizeof(float) * 3;
				case VertexFieldType.Float4:
					return sizeof(float) * 4;
				case VertexFieldType.Short1:
					return sizeof(short);
				case VertexFieldType.Short2:
					return sizeof(short) * 2;
				case VertexFieldType.Short3:
					return sizeof(short) * 3;
				case VertexFieldType.Short4:
					return sizeof(short) * 4;
				case VertexFieldType.Color:
					return sizeof(int);
				case VertexFieldType.UByte4:
					return sizeof(byte) * 4;
			}

			throw new Exception( "Vertex field type '" + type.ToString() + "' is not recognized.");
		}

		/// <summary>
		/// Function to return the number of elements within a field.
		/// </summary>
		/// <param name="type">Type of this field.</param>
		/// <returns>The number of elements within a field.</returns>
		static public int FieldElementCount(VertexFieldType type)
		{
			switch(type)
			{
				case VertexFieldType.Float1:
					return 1;
				case VertexFieldType.Float2:
					return 2;
				case VertexFieldType.Float3:
					return 3;
				case VertexFieldType.Float4:
					return 4;
				case VertexFieldType.Short1:
					return 1;
				case VertexFieldType.Short2:
					return 2;
				case VertexFieldType.Short3:
					return 3;
				case VertexFieldType.Short4:
					return 4;
				case VertexFieldType.Color:
					return 1;
				case VertexFieldType.UByte4:
					return 4;
			}

			throw new Exception( "Vertex field type '" + type.ToString() + "' is not recognized.");
		}

		/// <summary>
		/// Indicates whether this instance and a specified object are equal.
		/// </summary>
		/// <param name="obj">Another object to compare to.</param>
		/// <returns>
		/// true if obj and this instance are the same type and represent the same value; otherwise, false.
		/// </returns>
		public override bool Equals(object obj)
		{
			VertexField left = obj as VertexField;		// Comparison vertex field.

			if ((left != null) && ((left._context == _context) && (left._index == _index) && (left._offset == _offset) && (left._stream == _stream) && (left._type == _type)))
				return true;

			return false;
		}

		/// <summary>
		/// Returns the hash code for this instance.
		/// </summary>
		/// <returns>
		/// A 32-bit signed integer that is the hash code for this instance.
		/// </returns>
		public override int GetHashCode()
		{
			return ((_context.GetHashCode()) ^ (_index.GetHashCode()) ^ (_offset.GetHashCode()) ^ (_stream.GetHashCode()) ^ (_type.GetHashCode()));
		}
		#endregion

		#region Operators.
		/// <summary>
		/// Operator to test two vertex fields for equality.
		/// </summary>
		/// <param name="left">Left vertex field to compare.</param>
		/// <param name="right">Right vertex field to compare.</param>
		/// <returns>TRUE if left and right are equal, FALSE if not.</returns>
		public static bool operator ==(VertexField left, VertexField right)
		{
			if (left != null && right != null && (left._context == right._context) && (left._index == right._index) && (left._offset == right._offset) && (left._stream == right._stream) && (left._type == right._type))
				return true;

			return false;
		}

		/// <summary>
		/// Operator to test two vertex fields for inequality.
		/// </summary>
		/// <param name="left">Left vertex field to compare.</param>
		/// <param name="right">Right vertex field to compare.</param>
		/// <returns>TRUE if left and right are not equal, FALSE if they are.</returns>
		public static bool operator !=(VertexField left, VertexField right)
		{
			return !(left == right);
		}
		#endregion

		#region Constructors and Destructors.
		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="stream">Stream to bind this field with.</param>
		/// <param name="fieldOffset">Position of the field within the field type.</param>
		/// <param name="context">The purpose of this field.</param>
		/// <param name="fieldType">Data type of the field.</param>
		/// <param name="index">Index of the item, only applicable to certain types of fields.</param>
		internal VertexField(short stream,short fieldOffset, VertexFieldContext context, VertexFieldType fieldType, byte index)
		{
			_stream = stream;
			_offset = fieldOffset;
			_context = context;
			_type = fieldType;
			_index = index;
			_size = (short)SizeOf(fieldType);
		}
		#endregion
	}
}

