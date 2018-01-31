using System;


namespace SS14.Client.Graphics.VertexData
{
    /// <summary>
    /// VertexField
    /// </summary>
    public class VertexField
	{
	    private readonly short _stream;					                // Stream to which this element is bound.
		private readonly short _offset;					                // Offset of the field within the field type.
        private readonly VertexFieldContext _context;	                // The purpose of this field.
		private readonly VertexFieldType   _type;			            // Data type of this field.
		private readonly byte _index;					                // Index of the item, only applicable to certain types of fields. (i.e. textures).
		private readonly short _size;					                // Size of this field in bytes.

	    /// <summary>
		/// Property to return the offset of this field within the field type.
		/// </summary>
		public short Offset => _offset;

	    /// <summary>
		/// Property to return the context of this field.
		/// </summary>
		public VertexFieldContext Context => _context;

	    /// <summary>
		/// Property to return the data type of the field.
		/// </summary>
		public VertexFieldType Type => _type;

	    /// <summary>
		/// Property to return the index of this field.
		/// Only applicable to particular types of fields (i.e. textures).
		/// </summary>
		public byte Index => _index;

	    /// <summary>
		/// Property to return the number of bytes occupied by this field.
		/// </summary>
		public int Bytes => _size;

	    /// <summary>
		/// Property to return the stream to which this element is bound.
		/// </summary>
		public short Stream => _stream;

	    /// <summary>
		/// Function to return the size in bytes of a vertex field.
		/// </summary>
		/// <param name="type">Type of the field to evaluate.</param>
		/// <returns>Size of the field in bytes.</returns>
		public static int SizeOf(VertexFieldType type)
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
			    default:
			        throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported value.");
            }
		}

		/// <summary>
		/// Function to return the number of elements within a field.
		/// </summary>
		/// <param name="type">Type of this field.</param>
		/// <returns>The number of elements within a field.</returns>
		public static int FieldElementCount(VertexFieldType type)
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
			    default:
			        throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported value.");
			}
		}

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

