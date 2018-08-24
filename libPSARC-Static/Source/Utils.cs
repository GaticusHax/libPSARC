using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace libPSARC {

    public static class Utils {

        #region // Byte Conversions

        public static unsafe byte[] HexToBytes( string hex, uint stride=3 ) {
            if ( stride < 2 ) throw new ArgumentOutOfRangeException( "stride must be >= 2" );
            int len = hex?.Length ?? 0;
            byte[] bytes = new byte[len / stride + (((len % stride) > 1) ? 1 : 0)];
            fixed( byte* lpbytes = bytes)
            fixed( char* lphex = hex) {
                if ( lphex == null ) return bytes;
                char* eol = lphex + len;
                byte* b = lpbytes;
                for ( char* c = lphex; c < eol; c += stride, b++ ) {
                    int hi = *c; int lo = *(c + 1);
                    hi -= ( ( hi < 'A' ) ? '0' : ( ( ( hi < 'a' ) ? 'A' : 'a' ) - 0xA ) );
                    lo -= ( ( lo < 'A' ) ? '0' : ( ( ( lo < 'a' ) ? 'A' : 'a' ) - 0xA ) );
                    *b = (byte) ((hi << 4) + lo);
                };
            }
            return bytes;
        }

        [ExcludeFromCodeCoverage] public static string BytesToHex( byte[] bytes ) => BytesToHex( bytes, 0 );
        [ExcludeFromCodeCoverage] public static string BytesToHex( byte[] bytes, int startIndex ) => BytesToHex( bytes, startIndex, bytes.Length );
        public static string BytesToHex( byte[] bytes, int startIndex, int length ) {
            return BitConverter.ToString( bytes, startIndex, length ).Replace( '-', ' ' );
        }

        public static unsafe string BytesToHex( byte* lpbytes, int startIndex, int length ) {
            byte[] bytes = new byte[length];
            Marshal.Copy( new IntPtr( lpbytes + startIndex ), bytes, startIndex, length );
            return BytesToHex( bytes, startIndex, length );
        }

        #endregion

        #region // Attribute Helpers

        public static T GetAttribute<T>( MemberInfo member, bool inherit ) where T : Attribute {
            var attributes = (T[]) member.GetCustomAttributes( typeof( T ), inherit );
            return attributes.Length > 0 ? attributes[0] : null;
        }

        #endregion

        #region //



        #endregion

        #region // Extensions

        // Generic
        public static string FlagsToString( this Enum flags ) => flags.ToString().Replace( ", ", " | " );

        #endregion

    }

}
