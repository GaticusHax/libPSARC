#if DEBUG
    #define DEBUG_LOG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using libPSARC;
using libPSARC.Interop;

using PSARC = libPSARC.PSARC;

namespace cliPSARC {

    internal class Program {

        internal static bool quiet = false;
        internal static bool overwrite = false;

        internal static string baseDir = "";
        internal static string archiveFile = "";

        internal static readonly string[] VERBS = new string[] { "list", "create", "extract" };
        internal static readonly Dictionary<string, string> OPTION_ALIASES = new Dictionary<string, string>() {
            { "h",  "help" },
            { "q",  "quiet" },
            { "y",  "overwrite" },
            { "x",  "file" },
        };

        private static string GetExecutableName() => Path.GetFileNameWithoutExtension( Environment.GetCommandLineArgs()[0] );

        internal static string GetHelpText() {
            string exe = GetExecutableName();
            return "\nusage:\n" +
                  $"    {exe} list [<Options>] <Archive>\n" +
                  $"    {exe} [create]  [<Options>] <BaseDir> [<NewArchive>]\n" +
                  $"    {exe} [extract] [<Options>] <Archive> [<OutputDir>]\n" +
                   "\n" +
                   "verbs:\n" +
                   "  list      List the contents of an existing archive.\n" +
                   "  create    Create an archive.\n" +
                   "            Default if the first file argument is a directory path.\n" +
                   "  extract   Extract the contents of an existing archive.\n" +
                   "            Default if the first file argument is a PSARC archive file.\n" +
                   "\n" +
                   "general options:\n" +
                   "  -h, --help            Show this help info.\n" +
                   "      --version         Show the version\n" +
                   "  -q, --quiet           Only show errors.\n" +
                   "  -y, --overwrite       Overwrite existing files when creating or extracting an archive.\n" +
                   "\n" +
                   "create options:\n" +
                   "      --input=FILE      A file containing a list of file paths to be archived.\n" +
                   "                        File paths should be relative to <BaseDir>\n" +
                   "                        If not specified, all files in <BaseDir> will be archived.\n" +
                   "      --output=FILE     Name of the archive file to create.\n" +
                   "                        If not specified, then <NewArchive> is used.\n" +
                   "                        If neither are specified, the name of <BaseDir> is used.\n" +
                   "      --exclude=GLOB    A file glob (wildcard pattern) of files to be excluded.\n" +
                   "                        This option can be used multiple times.\n" +
                   "\n" +
                   "extract options:\n" +
                   "      --input=FILE      A file containing a list of file paths to be extracted.\n" +
                   "                        If not specified, all files in the archive will be extracted.\n" +
                   "      --output=DIR      The directory where files will be extracted to.\n" +
                   "                        If not specified, then <OutputDir> is used.\n" +
                   "                        If neither are specified, the current directory is used.\n" +
                   "  -xFILE, --file=FILE   Extract a specific file. This option can be used multiple times.\n"
            ;
        }

        internal static int ShowHelp() {
            Console.WriteLine( GetHelpText() );
            return -1;
        }

        internal enum ErrorCode {
            Success,
            ArgumentRequired,
            InvalidArgument,
            FileNotFound,
            FileExists,
            DirectoryNotFound,
            DirectoryExists,
            InvalidArchive,
        }

        internal static readonly string[] ERROR_CODE_MESSAGES = new string[] {
            "",
            "No {0} argument specified!",
            "Invalid command line argument!",
            "File does not exist!\n\"{0}\"",
            "File already exists!\n\"{0}\"",
            "Directory does not exist!\n\"{0}\"",
            "Directory already exists!\n\"{0}\"",
            "Not a valid PSARC archive file!\n\"{0}\"",
        };

        internal static int ShowError( int code, string msg ) {
            Console.WriteLine( msg );
            return (int) code;
        }

        internal static int ShowError( ErrorCode code, params string[] args ) {
            Console.WriteLine( ERROR_CODE_MESSAGES[(int) code], args );
            return (int) code;
        }

        internal static int ShowVersion() {
            string version = libPSARC.Version.AssemblyVersion.ToString();
            string versionString = $"{GetExecutableName()} v{libPSARC.Version.GetString()}";
            Console.WriteLine( quiet ? version : versionString );
            return -1;
        }

        internal static void LogInfo( string msg ) { if ( !quiet ) Console.WriteLine( msg ); }
        internal static void LogOverwriteFile( string file ) => LogInfo( $"File already exists! Overwriting...\n\"{file}\"" );

        internal static void ArchiveListFiles( string archiveFile ) {
            using ( var fIn = new FileStream( archiveFile, FileMode.Open, FileAccess.Read ) ) {
                var archive = new PSARC.Archive( fIn );

                int count = Math.Min( 5, archive.filePaths.Count );
                for (int i = 0; i < count; i++) {
                    var hash = System.Security.Cryptography.MD5.Create().ComputeHash( Encoding.ASCII.GetBytes( archive.filePaths[i] ) );
                    Console.WriteLine( Utils.BytesToHex( hash ) );
                }

                foreach ( var path in archive.filePaths ) Console.WriteLine( path );

            }
        }

        internal static void ArchiveExtractFiles( string archiveFile, string baseDir, List<string> files ) {
            using ( var fIn = new FileStream( archiveFile, FileMode.Open, FileAccess.Read ) ) {
                var archive = new PSARC.Archive( fIn );

                // if no files were specified then extract all
                if ( files.Count == 0 ) foreach ( var path in archive.filePaths ) files.Add( path );

                foreach ( var file in files ) ArchiveExtractFile( archive, fIn, baseDir, file );
            }
        }

        internal static void ArchiveExtractFile( PSARC.Archive archive, Stream streamIn, string baseDir, string file ) {
            CreateDirectory( baseDir, file );
            var filePath = Path.GetFullPath( Path.Combine( baseDir, file ) );
            bool exists = File.Exists( filePath );
            FileMode fileMode = overwrite ? FileMode.Create : FileMode.CreateNew;
            using ( var fOut = new FileStream( filePath, fileMode, FileAccess.Write ) ) {
                LogInfo( $"extracting {file}" );
                archive.ExtractFile( streamIn, file, fOut );
                if ( exists ) LogOverwriteFile( filePath );
            }
        }

        internal static List<string> ReadFileList( string listFile ) {
            var files = new List<string>();
            if ( listFile == null ) return files;
            using ( var reader = new StreamReader( new FileStream( listFile, FileMode.Open, FileAccess.Read ) ) ) {
                while ( !reader.EndOfStream ) files.Add( reader.ReadLine() );
            }
            return files;
        }

        // Make sure the path to file exists.
        internal static void CreateDirectory( string baseDir, string path ) {
            string fileName = Path.GetFileName( path );
            path = path.Remove( path.Length - fileName.Length );
            Directory.CreateDirectory( Path.Combine( baseDir, path ) );
        }

        internal static int Main( string[] args ) {
#if DEBUG_LOG
            Debug.Listeners.Add( new TextWriterTraceListener( Console.Out ) );
#endif

            if ( args.Length == 0 ) return ShowHelp();

            var options = new CommandLineOptions( args, VERBS, OPTION_ALIASES );

            quiet     = options.IsOption( "quiet" );
            overwrite = options.IsOption( "overwrite" );

            if ( options.IsOption( "help"    ) ) return ShowHelp();
            if ( options.IsOption( "version" ) ) return ShowVersion();

            if ( options.fileParams.Count < 1 ) {
                string arg = "<Archive> or <BaseDir>";                 // execution order matters!
                arg = (options.verb != null     ) ? "<Archive>" : arg; // execution order matters!
                arg = (options.verb == "create" ) ? "<BaseDir>" : arg; // execution order matters!
                return ShowError( ErrorCode.ArgumentRequired, arg );
            }

            if ( options.verb == null ) { // auto-detect what verb mode should be used
                options.verb = Directory.Exists( options.fileParams[0] ) ? "create" : "extract";
            }

            try {

                if ( options.verb == "list" ) {
                    archiveFile = options.fileParams[0];

                    if ( options.Count != 0 ) return ShowError( ErrorCode.InvalidArgument );

                    ArchiveListFiles( archiveFile );

                } else if ( options.verb == "create" ) {
                    baseDir = options.fileParams[0];

                    archiveFile = (options.fileParams.Count > 1) ? options.fileParams[1] : Path.GetFileNameWithoutExtension( baseDir );
                    archiveFile = options.GetOption( "output" ) ?? archiveFile;

                    var listFile = options.GetOption( "input" );

                    if ( options.IsOption( "exclude" ) ) throw new NotImplementedException();

                    if ( options.Count != 0 ) return ShowError( ErrorCode.InvalidArgument );

                    bool exists = File.Exists( archiveFile );
                    FileMode fileMode = overwrite ? FileMode.Create : FileMode.CreateNew;

                    throw new NotImplementedException();

                    //if (exists) LogOverwriteFile( archiveFile );

                } else if ( options.verb == "extract" ) {
                    archiveFile = options.fileParams[0];

                    baseDir = (options.fileParams.Count > 1) ? options.fileParams[1] : Directory.GetCurrentDirectory();
                    baseDir = options.GetOption( "output" ) ?? baseDir;

                    var files = ReadFileList( options.GetOption( "input" ) );
                    files.AddRange( options.GetOptions( "file" ) );

                    if ( options.Count != 0 ) return ShowError( ErrorCode.InvalidArgument );

                    ArchiveExtractFiles( archiveFile, baseDir, files );
                }

            } catch ( PSARC.InvalidArchiveException ) {
                return ShowError( ErrorCode.InvalidArchive, archiveFile );
            } catch ( IOException e ) {
                return ShowError( (int) ErrorCode.FileExists, e.Message );
            }

            return 0;
        }

    }
}
