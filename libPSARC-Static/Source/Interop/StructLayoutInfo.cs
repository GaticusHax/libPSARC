using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace libPSARC.Interop {

    [AttributeUsage( AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum
                   | AttributeTargets.Field | AttributeTargets.Property, Inherited = true )]
    public class StructMetaAttribute : Attribute {

        public string ToStringMethod { get; set; }

    }

    public static class StructMeta {

        public struct FieldData {

            public Type  type;
            public int offset;
            public int size;

        }

        public class FieldMap : Dictionary<string, FieldData> {
            public Type structType;
            public int structSize;

            public FieldMap( Type type ) {
                this.structType = type;
                this.structSize = Marshal.SizeOf( type );
            }

            public override string ToString() {
                StringBuilder sb = new StringBuilder( $"Layout of {this.structType.FullName} : 0x{this.structSize:X8}\n" );

                int nMaxStringLength = 0;
                int oMaxStringLength = 0;
                int sMaxStringLength = 0;

                string[] nStrings = new string[this.Count];
                string[] oStrings = new string[this.Count];
                string[] sStrings = new string[this.Count];
                string[] tStrings = new string[this.Count];

                // do some initialization for the next loop
                int i = 0;
                foreach ( var kvp in this ) {
                    nStrings[i] = $"{kvp.Key}";
                    oStrings[i] = $"{kvp.Value.offset:X}";
                    sStrings[i] = $"{kvp.Value.size:X}";
                    tStrings[i] = $"{kvp.Value.type.FullName}";

                    nMaxStringLength = (nMaxStringLength < nStrings[i].Length) ? nStrings[i].Length : nMaxStringLength;
                    oMaxStringLength = (oMaxStringLength < oStrings[i].Length) ? oStrings[i].Length : oMaxStringLength;
                    sMaxStringLength = (sMaxStringLength < sStrings[i].Length) ? sStrings[i].Length : sMaxStringLength;

                    i++;
                }

                // make sure the length of the offset and size strings are even
                if ( (oMaxStringLength & 1) == 1 ) oMaxStringLength += 1;
                if ( (sMaxStringLength & 1) == 1 ) sMaxStringLength += 1;

                for (i = 0; i < this.Count; i++) {
                    string nField = nStrings[i].PadRight( nMaxStringLength, ' ' );
                    string oField = oStrings[i].PadLeft ( oMaxStringLength, '0' );
                    string sField = sStrings[i].PadLeft ( sMaxStringLength, '0' );
                    string tField = tStrings[i];
                    sb.Append( $"    0x{oField} = {nField} : 0x{sField} {tField}\n" );
                }

                return sb.ToString();
            }
        }

        public static FieldMap GetLayout<T>() where T : struct => GetLayout( typeof( T ) );

        public static FieldMap GetLayout( Type type ) {
            var fieldMap = new FieldMap( type );

            var fields = type.GetFields();
            foreach ( var field in fields ) {
                fieldMap[field.Name] = new FieldData {
                    type   = field.FieldType,
                    offset = Unmanaged.OffsetOf( type, field.Name ),
                    size   = Unmanaged.SizeOf( field )
                };
            }

            return fieldMap;
        }

        public static string StructToString<T>( Dictionary<string, object> keyValuePairs ) where T : struct => StructToString<T>( null, keyValuePairs );
        public static string StructToString<T>( string heading, Dictionary<string, object> keyValuePairs ) where T : struct {
            Type type = typeof( T );
            StringBuilder sb = new StringBuilder( $"Value of {heading ?? type.FullName}\n" );

            int maxNameLength = 0;
            foreach ( var name in keyValuePairs.Keys.ToArray() ) maxNameLength = Math.Max( maxNameLength, name.Length );

            foreach (var kvp in keyValuePairs ) {
                sb.AppendLine( $"    {kvp.Key.PadRight( maxNameLength )} = {kvp.Value}" );
            }

            return sb.ToString();
        }

        public static string StructToString<T>( T data ) where T : struct => StructToString<T>( null, data );
        public static string StructToString<T>( string heading, T data ) where T : struct {
            Dictionary<String, Object> keyValuePairs = new Dictionary<string, object>();

            var type = data.GetType();
            var fields = type.GetFields();
            foreach (var field in fields ) {
                var value = field.GetValue( data );
                var toStringMethod = Utils.GetAttribute<StructMetaAttribute>( field, true )?.ToStringMethod;
                if (toStringMethod != null) {
                    const BindingFlags BINDING_FLAGS = BindingFlags.InvokeMethod
                                                    | BindingFlags.Public | BindingFlags.NonPublic
                                                    | BindingFlags.Instance | BindingFlags.Static;
                    value = type.InvokeMember( toStringMethod, BINDING_FLAGS, null, data, null );
                }
                keyValuePairs[field.Name] = value;
            }

            return StructToString<T>( heading, keyValuePairs );
        }

    }

}
