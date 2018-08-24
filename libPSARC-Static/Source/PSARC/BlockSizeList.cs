using System;
using System.Diagnostics;
using System.IO;

namespace libPSARC.PSARC {

    public class BlockSizeList {

        private uint[] sizes;

        public uint this[int index] {
            get => sizes[index];
            set => sizes[index] = value;
        }

        public int Length => sizes.Length;

        public BlockSizeList() : this( 0 ) { }

        public BlockSizeList( int numBlocks ) => this.sizes = new uint[numBlocks];

        public BlockSizeList( Stream streamIn, int numBytes, uint maxBlockSize ) {
            const int INT_SIZE = 4;

            int wordSize = (int) Math.Log( maxBlockSize - 1, 1 << 8 ) + 1;
            Debug.WriteLine( $"wordSize = {wordSize}\n" );

            int numBlocks = numBytes / wordSize;
            this.sizes = new uint[numBlocks];
            Debug.WriteLine( $"numBlocks = {numBlocks}\n" );

            byte[] buffer = new byte[INT_SIZE];
            int offset = INT_SIZE - wordSize;
            for (int i = 0; i < sizes.Length; i++) {
                streamIn.Read( buffer, 0, wordSize );
                Interop.ByteOrder.Swap( buffer, 0, wordSize );
                sizes[i] = BitConverter.ToUInt32( buffer, 0 );
            }

        }

    }

}
