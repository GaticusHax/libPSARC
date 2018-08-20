using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using libPSARC.Interop;

namespace libPSARC.PSARC {

    public class Archive {

        public Header header;

        public FileList fileEntries;

        public BlockSizeList blockSizes;

        public List<string> filePaths;

        #region // Methods

        public Archive() {
            this.header = new Header();
            this.fileEntries = null;
            this.blockSizes = null;
            this.filePaths = null;
        }

        public Archive( Stream streamIn ) {
            header = new Header( streamIn );
            Debug.WriteLine( header );

            Debug.WriteLine( String.Format( "\n0x{0:X8} ({0})\n", streamIn.Position ) );

            fileEntries = new FileList( streamIn, header.numFiles );
            for ( int i = 0; i < 5; i++ ) Debug.WriteLine( fileEntries[i].ToString( $"{typeof( FileEntry )}[{i}]" ) );

            Debug.WriteLine( String.Format( "\n0x{0:X8} ({0})\n", streamIn.Position ) );

            int length = (int) (header.dataOffset - streamIn.Position);
            length -= (header.flags.HasFlag( ArchiveFlags.Encrypted ) ? 0x20 : 0x00);

            blockSizes = new BlockSizeList( streamIn, length, header.maxBlockSize );
            for ( int i = 0; i < 5; i++ ) Debug.WriteLine( $"blockSizes[{i}] = {blockSizes[i]}" );

            Debug.WriteLine( String.Format( "\n0x{0:X8} ({0})\n", streamIn.Position ) );

            filePaths = new List<string>();
            filePaths.Add( "|MANIFEST|" );
            using ( var reader = new StreamReader( ExtractFile( streamIn, fileEntries[0] ) ) ) {
                while ( !reader.EndOfStream ) filePaths.Add( reader.ReadLine() );
            }
        }

        public int GetFileIndex( string filePath ) {
            return (filePaths.Contains( filePath )) ? filePaths.IndexOf( filePath ) : -1;
        }

        public Stream ExtractFile( Stream streamIn, string filePath, Stream streamOut = null ) {
            return ExtractFile( streamIn, GetFileIndex( filePath ), streamOut );
        }

        public Stream ExtractFile( Stream streamIn, int fileIndex, Stream streamOut = null ) {
            return ExtractFile( streamIn, fileEntries[fileIndex], streamOut );
        }

        public Stream ExtractFile( Stream streamIn, FileEntry fileEntry, Stream streamOut = null) {
            var buffer = new byte[header.maxBlockSize];

            int index = fileEntry.blockIndex;
            long size = (long) fileEntry.fileSize.Value;
            long total = 0;

            streamIn.Seek( (long) fileEntry.dataOffset.Value, SeekOrigin.Begin );

            streamOut = streamOut ?? new MemoryStream( (int) (UInt64) fileEntry.fileSize );
            long startPosition = streamOut.Position;

            // loop until all blocks have been read
            while (total < size) {
                uint blockSize = blockSizes[index];
                streamIn.Read( buffer, 0, (int) blockSize );
                var zOut = new zlib.ZOutputStream( streamOut );
                zOut.Write( buffer, 0, (int) blockSize );
                zOut.Flush();
                total += zOut.TotalOut;
                index++;
            }

            streamOut.Flush();
            streamOut.Position = startPosition;
            return streamOut;
        }

        #endregion
    }

}
