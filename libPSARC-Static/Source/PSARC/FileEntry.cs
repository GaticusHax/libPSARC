using System;
using System.Runtime.InteropServices;

namespace libPSARC.PSARC {
    using System.Collections.Generic;
    using System.IO;
    using Interop;

    [ByteOrder( Endian.Big )]
    [StructLayout( LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 0x01, Size = 0x1E )]
    public struct FileEntry {

        [MarshalAs( UnmanagedType.ByValArray, SizeConst = 0x10 )]
        [StructMeta( ToStringMethod = "_nameDigestToString" )]
        /* 0x00 [0x10] */ public Byte[] nameDigest;
        private string _nameDigestToString() => Utils.BytesToHex( this.nameDigest );

        /* 0x10 [0x04] */ public Int32 blockIndex;

        /// <summary>The size of the uncompressed file.</summary>
        /* 0x14 [0x05] */ public UInt40 fileSize;

        /// <summary>The offset of the file data within the archive.</summary>
        /* 0x19 [0x05] */ public UInt40 dataOffset;

        #region // Methods

        public override string ToString() => ToString( null );
        public string ToString( string heading ) => StructMeta.StructToString( heading, this );

        #endregion

    } /* 0x1E */

    public class FileList {

        private FileEntry[] entries;

        public FileEntry this[int index] {
            get => entries[index];
            set => entries[index] = value;
        }

        public int Length => entries.Length;

        #region // Methods

        public FileList() : this( 0 ) { }

        public FileList( uint numFiles ) {
            entries = new FileEntry[numFiles];
        }

        public FileList( Stream streamIn, uint numFiles ) : this( numFiles ) {
            for ( int i = 0; i < numFiles; i++ ) {
                entries[i] = Unmanaged.BlitStruct<PSARC.FileEntry>( streamIn );
            }
        }

        #endregion
    }

}
