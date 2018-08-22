using System;
using System.Reflection;
using System.Text;

namespace libPSARC {

    public static class Utils {

        #region // Byte Conversions

        public static string BytesToHex( byte[] bytes, int length = 0 ) {
            if ( length == 0 ) length = bytes.Length;
            return BitConverter.ToString( bytes, 0, length ).Replace( '-', ' ' );
        }

        public static char[] BytesToChars( byte[] bytes, int length = 0 ) {
            if ( length == 0 ) length = bytes.Length;
            return Encoding.ASCII.GetChars( bytes, 0, length );
        }

        public static string BytesToString( byte[] bytes, int length = 0 ) {
            if ( length == 0 ) length = bytes.Length;
            return Encoding.ASCII.GetString( bytes, 0, length );
        }

        public static UInt32 BytesToUInt32( byte[] bytes ) => BitConverter.ToUInt32( bytes, 0 );

        #endregion

        #region // Attribute Helpers

        public static T GetAttribute<T>( MemberInfo member, bool inherit ) where T : Attribute {
            var attributes = (T[]) member.GetCustomAttributes( typeof( T ), inherit );
            if ( attributes.Length > 0 ) return attributes[0];
            return null;
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
