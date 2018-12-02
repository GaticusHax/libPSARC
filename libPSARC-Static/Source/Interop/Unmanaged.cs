using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace libPSARC.Interop {

    internal static class Unmanaged {

        public unsafe static T BlitStruct<T>( byte[] bytes, int offset = 0 ) where T : struct {
            T value = default( T );
            Type type = typeof( T );

            bytes = ByteOrder.Convert( type, bytes, offset ); // swap endian if needed

            fixed ( byte* lpBytes = &bytes[offset] ) {
                value = (T) Marshal.PtrToStructure( new IntPtr( lpBytes ), type );
            }

            return value;
        }

        public unsafe static T BlitStruct<T>( Stream streamIn ) where T : struct {
            byte[] bytes = new byte[Marshal.SizeOf<T>()];
            streamIn.Read( bytes, 0, bytes.Length );
            return BlitStruct<T>( bytes, 0 );
        }

        public unsafe static void BlitBytes<T>( T value, byte[] bytes = null, int offset = 0 ) where T : struct {
            if ( bytes == null ) bytes = new byte[offset + Marshal.SizeOf( value )];

            fixed ( byte* lpBytes = &bytes[offset] ) {
                Marshal.StructureToPtr( value, new IntPtr( lpBytes ), false );
            }

            ByteOrder.Convert( typeof( T ), bytes, offset ); // swap endian if needed
        }

        public unsafe static void BlitBytes<T>( T value, Stream streamOut ) where T : struct {
            byte[] bytes = new byte[Marshal.SizeOf<T>()];
            BlitBytes<T>( value, bytes );
            streamOut.Write( bytes, 0, bytes.Length );
        }

        public static int OffsetOf( Type type, string fieldName, bool inherit = true )
                => Marshal.OffsetOf( type, fieldName ).ToInt32();

        public static int SizeOf( MemberInfo member ) {
            var fieldInfo = member as FieldInfo;
            var typeInfo = fieldInfo?.FieldType ?? member;
            var type = (Type) typeInfo;

            if (type.IsArray) {
                int length = Utils.GetAttribute<MarshalAsAttribute>( member, true )?.SizeConst ?? 1;
                return SizeOf( type.GetElementType() ) * length;
            } else if ( type.IsEnum ) {
                return SizeOf( type.GetEnumUnderlyingType() );
            }
            return Marshal.SizeOf( type );
        }

    }

}
