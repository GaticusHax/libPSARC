using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace libPSARC.Interop {

    public enum Endian { Little, Big }

    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Field | AttributeTargets.Property, Inherited = true )]
    public class ByteOrderAttribute : Attribute {

        public Endian Endian { get; private set; }

        public bool IsLittleEndian => (Endian == Endian.Little);
        public bool IsBigEndian    => (Endian == Endian.Big);

        public ByteOrderAttribute( Endian byteOrder )   => this.Endian = byteOrder;
        public ByteOrderAttribute( bool isLittleEndian ) : this( isLittleEndian ? Endian.Little : Endian.Big ) { }

    }

    public static class ByteOrder {

        public static byte[] Swap( byte[] bytes, int offset, int size ) {
            //Debug.Assert( (size != 0) && ((size & (~size + 1)) == size) ); // size must be a power of two
 
            // reverse bytes directly in the byte buffer
            // process both ends of the byte range at the same time
            int i = offset - 1;
            int j = offset + size;
            while ( ++i < --j ) {
                byte swap = bytes[i];
                bytes[i] = bytes[j];
                bytes[j] = swap;
            }

            return bytes;
        }

        public static byte[] Convert( MemberInfo member, byte[] bytes, int baseOffset = 0 ) {
            Endian endian = BitConverter.IsLittleEndian ? Endian.Little : Endian.Big;
            return Convert( member, bytes, baseOffset, endian );
        }

        private static byte[] Convert( MemberInfo memberInfo, byte[] bytes, int baseOffset, Endian defaultEndian ) {
            var fieldInfo = memberInfo as FieldInfo;
            var typeInfo  = fieldInfo?.FieldType ?? memberInfo as TypeInfo;
            if ( typeInfo == null ) return bytes; // member is not a Field or a Type

            ByteOrderAttribute endianAttr = Utils.GetAttribute<ByteOrderAttribute>( memberInfo, true) ?? new ByteOrderAttribute( defaultEndian );
            bool isEndianSwapped = ( endianAttr.IsLittleEndian != BitConverter.IsLittleEndian );

            if ( typeInfo.IsPrimitive ) {
                if ( isEndianSwapped ) Swap( bytes, baseOffset, Marshal.SizeOf( typeInfo ) );

            } else if ( typeInfo.IsArray ) {
                var marshalAsAttr = Utils.GetAttribute<MarshalAsAttribute>( fieldInfo, true );
                int length = marshalAsAttr?.SizeConst ?? 0;
                Debug.Assert( length > 0 );

                typeInfo = typeInfo.GetElementType();

                int size = Marshal.SizeOf( typeInfo );
                if ( size <= 1 ) return bytes; // nothing to do

                for ( int i = 0; i < length; i++ ) Convert( typeInfo, bytes, baseOffset + (i * size), endianAttr.Endian );

            } else if ( typeInfo.IsEnum ) {
                typeInfo = typeInfo.GetEnumUnderlyingType();
                Convert( typeInfo, bytes, baseOffset, endianAttr.Endian );

            } else if ( typeInfo.IsValueType ) {
                var fields = typeInfo.GetFields();
                foreach ( var field in fields ) {
                    int offset = Marshal.OffsetOf( typeInfo, field.Name ).ToInt32();
                    Convert( field, bytes, baseOffset + offset, endianAttr.Endian );
                }

            } else {
                throw new NotImplementedException();

            }

            return bytes;
        }

    }

}
