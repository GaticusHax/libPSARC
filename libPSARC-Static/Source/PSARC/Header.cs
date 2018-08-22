using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using libPSARC.Interop;

namespace libPSARC {

    /// <summary>The valid compression types for file assets in the archive.</summary>
    public enum CompressionType : UInt32 {
        none = 0x00000000,
        zlib = 0x7A6C6962, // "zlib"
        lzma = 0x6C7A6D61, // "lzma"
    }

    /// <summary>
    ///     Bit flags. Can be combined.
    ///     Note:
    ///        If the Absolute bit is set then Relative is ignored.
    ///        If the Absolute bit is NOT set, then Relative is implied.
    /// </summary>
    /// <remarks>Default is case-sensitive, relative paths.</remarks>
    [Flags] public enum ArchiveFlags : UInt32 {
        Relative = 0x00, // if Absolute bit not set
        IgnoreCase = 0x01,
        Absolute = 0x02,
        Encrypted = 0x04, // TODO: Rijndael? (RijndaelEncryptor)
    }

    internal static class ArchiveFlagsExtensions {

        public static string FlagsToString( this ArchiveFlags flags ) {
            bool isArchiveRelative = (flags & ArchiveFlags.Absolute) == 0;
            return (isArchiveRelative ? $"{ArchiveFlags.Relative} | " : "") + flags.ToString().Replace( ", ", " | " );
        }

    }

}

namespace libPSARC.PSARC {

    /// <summary>Header struct for PSARC (PlayStation ARChive) file format.</summary>
    [ByteOrder( Endian.Big )]
    [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 0x04, Size = 0x20 )]
    public struct Header {
        internal static readonly UInt32 MAGIC = BitConverter.ToUInt32( Encoding.ASCII.GetBytes( "PSAR" ), 0 );
        internal const UInt32 VERSION = 1 << 16 + 4;

        /// <summary>Should always be "PSAR".</summary>
        //[MarshalAs( UnmanagedType.ByValArray, SizeConst = 4 )]
        [StructMeta( ToStringMethod = "_magicToString" ), ByteOrder( Endian.Little )]
        /* 0x00 [0x04] */ public readonly UInt32 magic;
        private string _magicToString() => Encoding.ASCII.GetString( BitConverter.GetBytes( magic ) );

        /// <summary>PSARC format version. High short is Major. Low short is Minor.</summary>
        [StructMeta( ToStringMethod = "_versionToString" )]
        /* 0x04 [0x04] */ public readonly UInt32 version;
        private string _versionToString() => $"{Utils.BytesToHex( BitConverter.GetBytes( version ) )} ( Major: {VersionMajor}, Minor: {VersionMinor} )";

        // properties are not serialized
        /// <summary>PSARC file format version major.</summary>
        /* 0x04 [0x02] */ public UInt16 VersionMajor => (UInt16) ((version & 0xFFFF0000) >> 16);
        /// <summary>PSARC file format version minor.</summary>
        /* 0x06 [0x02] */ public UInt16 VersionMinor => (UInt16) (version & 0x0000FFFF);

        /// <summary>The compression type used for data blocks in the archive.</summary>
        /* 0x08 [0x04] */ public CompressionType compression;

        /// <summary>Offset to the start of the data blocks.</summary>
        /* 0x0C [0x04] */ public UInt32 dataOffset;

        /// <summary>The size in bytes of each entry in the table of contents array.</summary>
        /* 0x10 [0x04] */ public UInt32 tocEntrySize;

        /// <summary>The number of entries in the table of contents array.</summary>
        /* 0x14 [0x04] */ public UInt32 numFiles;

        /// <summary>The maximum size in bytes of each data block.</summary>
        /* 0x18 [0x04] */ public UInt32 maxBlockSize;

        /// <summary></summary>
        /// <remarks>Default is case-sensitive, relative paths.</remarks>
        [StructMeta( ToStringMethod = "ArchiveFlagsToString" )]
        /* 0x1C [0x04] */ public ArchiveFlags flags;
        private string ArchiveFlagsToString() => flags.FlagsToString();

        #region // Methods

        public Header( CompressionType compression = CompressionType.zlib,
                       UInt32 dataOffset = 0, UInt32 tocEntrySize = 0x20, UInt32 numFiles = 0, UInt32 maxBlockSize = (1 << 16),
                       ArchiveFlags flags = ArchiveFlags.Relative
        ) {
            this.magic = MAGIC;
            this.version = VERSION;
            this.compression = compression;
            this.dataOffset = dataOffset;
            this.tocEntrySize = tocEntrySize;
            this.numFiles = numFiles;
            this.maxBlockSize = maxBlockSize;
            this.flags = flags;
        }

        public Header( byte[] bytes, int offset = 0 ) : this() => this = Unmanaged.BlitStruct<Header>( bytes, offset );
        public Header( Stream streamIn ) : this() => this = Unmanaged.BlitStruct<Header>( streamIn );

        public override string ToString() => ToString( null );
        public string ToString( string heading ) => StructMeta.StructToString( heading, this );

        public static bool IsValidMagicID( UInt32 magic ) => (magic == MAGIC);
        public static bool IsValidMagicID( byte[] bytes ) => IsValidMagicID( BitConverter.ToUInt32( bytes, 0 ) );

        public static bool IsValid( Stream streamIn ) {
            var position = streamIn.Position;
            if ( streamIn.Length < Marshal.SizeOf<Header>() ) return false;
            var magic = new byte[sizeof( UInt32 )];
            streamIn.Read( magic, 0, magic.Length );
            streamIn.Position = position;
            return IsValidMagicID( magic );
        }

        #endregion

    } /* 0x20 */

}
