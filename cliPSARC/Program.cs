using System;
using System.Diagnostics;
using System.IO;

using libPSARC;
using libPSARC.Interop;

using PSARC = libPSARC.PSARC;

namespace cliPSARC {

    public class Program {

        public static void Main( string[] args ) {
#if DEBUG
            Debug.Listeners.Add( new TextWriterTraceListener( Console.Out ) );
#endif

            Debug.WriteLine( StructMeta.GetLayout<PSARC.Header>().ToString() );
            Debug.WriteLine( StructMeta.GetLayout<PSARC.FileEntry>().ToString() );

            using ( var fIn = new FileStream( args[0], FileMode.Open, FileAccess.Read ) ) {

                var archive = new PSARC.Archive( fIn );

                Debug.WriteLine( $"length = {archive.filePaths.Count}" );

                using ( var fOut = new FileStream( "ALIENEYE.BASE.1.DDS", FileMode.Create ) ) {
                    archive.ExtractFile( fIn, "TEXTURES/PLANETS/CREATURES/SHARED/ALIENEYE.BASE.1.DDS", fOut );
                }
            }

        }

    }
}
