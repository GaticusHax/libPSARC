using System;
using System.Diagnostics;
using System.IO;

namespace libPSARC.PSARC {

    public class BlockSizeList {

        private UInt32[] sizes;

        public UInt32 this[int index] {
            get => sizes[index];
            set => sizes[index] = value;
        }

        public BlockSizeList() : this( 0 ) { }

        public BlockSizeList( int numBlocks ) {
            this.sizes = new UInt32[numBlocks];
        }

        public BlockSizeList( Stream streamIn, int numBytes, UInt32 maxBlockSize ) {
            const int INT_SIZE = 4;

            Int32 wordSize = (Int32) Math.Log( maxBlockSize - 1, 1 << 8 ) + 1;
            Debug.WriteLine( $"wordSize = {wordSize}\n" );

            int numBlocks = numBytes / wordSize;
            this.sizes = new UInt32[numBlocks];
            Debug.WriteLine( $"numBlocks = {numBlocks}\n" );

            Byte[] buffer = new Byte[INT_SIZE];
            int offset = INT_SIZE - wordSize;
            for (int i = 0; i < sizes.Length; i++) {
                streamIn.Read( buffer, 0, wordSize );
                Interop.ByteOrder.Swap( buffer, 0, wordSize );
                sizes[i] = BitConverter.ToUInt32( buffer, 0 );
            }

        }

    }

}
