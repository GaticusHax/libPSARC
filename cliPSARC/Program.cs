#if DEBUG
    //#define DEBUG_LOG
#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using libPSARC;
using libPSARC.Interop;

using PSARC = libPSARC.PSARC;

namespace cliPSARC {

    public class Program {

        public enum ErrorCode {
            Success,
            ArgumentRequired,
            InvalidArgument,
            FileNotFound,
            FileExists,
            DirectoryNotFound,
            DirectoryExists,
            InvalidArchive,
        }

        private static string[] ERROR_CODE_MESSAGES = new string[] {
            "",
            "No {0} argument specified!",
            "Invalid command line argument!",
            "File does not exist!\n\"{0}\"",
            "File already exists!\n\"{0}\"",
            "Directory does not exist!\n\"{0}\"",
            "Directory already exists!\n\"{0}\"",
            "Not a valid PSARC archive file!\n\"{0}\"",
        };

        private static string[] VERBS = new string[] { "list", "create", "extract" };
        private static Dictionary<string, string> OPTION_ALIASES = new Dictionary<string, string>() {
            { "h",  "help" },
            { "q",  "quiet" },
            { "y",  "overwrite" },
            { "x",  "file" },
        };

        private static bool quiet = false;
        private static bool overwrite = false;

        private static string listFile = "";
        private static string baseDir = "";
        private static string archiveFile = "";
        private static List<string> files = new List<string>();

        public static string GetExecutableName() => Path.GetFileNameWithoutExtension( Environment.GetCommandLineArgs()[0] );

        public static string GetHelpText() {
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

        public static int ShowHelp() {
            Console.WriteLine( GetHelpText() );
            return -1;
        }

        public static int ShowError( ErrorCode code, params string[] args ) {
            Console.WriteLine( ERROR_CODE_MESSAGES[(int) code], args );
            return (int) code;
        }

        public static int ShowVersion() {
            string exe = GetExecutableName();
            Console.WriteLine( $"{exe} v{Assembly.GetExecutingAssembly().GetName().Version}" );
            return -1;
        }

        private static void LogInfo( string msg ) { if ( !quiet ) Console.WriteLine( msg ); }
        private static void LogOverwriteFile( string file ) => LogInfo( $"File already exists! Overwriting...\n\"{file}\"" );

        public static int Main( string[] args ) {
#if DEBUG_LOG
            Debug.Listeners.Add( new TextWriterTraceListener( Console.Out ) );
#endif

            if ( args.Length == 0 ) return ShowHelp();

            var options = new CommandLineOptions( args, VERBS, OPTION_ALIASES );

            if ( options.IsOption( "help"    ) ) return ShowHelp();
            if ( options.IsOption( "version" ) ) return ShowVersion();

            quiet     = options.IsOption( "quiet" );
            overwrite = options.IsOption( "overwrite" );

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
                    if ( !File.Exists( archiveFile ) ) return ShowError( ErrorCode.FileNotFound, archiveFile );

                    if ( options.optionKeys.Count != 0 ) return ShowError( ErrorCode.InvalidArgument );

                    using ( var fIn = new FileStream( archiveFile, FileMode.Open, FileAccess.Read ) ) {
                        var archive = new PSARC.Archive( fIn );
                        foreach ( var path in archive.filePaths ) Console.WriteLine( path );
                    }


                } else if ( options.verb == "create" ) {
                    baseDir = options.fileParams[0];
                    if ( !Directory.Exists( baseDir ) ) return ShowError( ErrorCode.DirectoryNotFound, baseDir );

                    archiveFile = (options.fileParams.Count > 1) ? options.fileParams[1] : Path.GetFileNameWithoutExtension( baseDir );
                    archiveFile = options.GetOption( "output" ) ?? archiveFile;
                    if ( File.Exists( archiveFile ) ) {
                        if ( !overwrite ) return ShowError( ErrorCode.FileExists, archiveFile );
                        LogOverwriteFile( archiveFile );
                    }

                    listFile = options.GetOption( "input" );
                    if ( (listFile != null) && !File.Exists( listFile ) ) return ShowError( ErrorCode.FileNotFound, listFile );

                    if ( options.IsOption( "exclude" ) ) throw new NotImplementedException();

                    if ( options.optionKeys.Count != 0 ) return ShowError( ErrorCode.InvalidArgument );

                    throw new NotImplementedException();

                } else if ( options.verb == "extract" ) {
                    archiveFile = options.fileParams[0];
                    if ( !File.Exists( archiveFile ) ) return ShowError( ErrorCode.FileNotFound, archiveFile );

                    baseDir = (options.fileParams.Count > 1) ? options.fileParams[1] : Directory.GetCurrentDirectory();
                    baseDir = options.GetOption( "output" ) ?? baseDir;

                    listFile = options.GetOption( "input" );
                    if ( listFile != null ) {
                        if ( !File.Exists( listFile ) ) return ShowError( ErrorCode.FileNotFound, listFile );
                        using ( var reader = new StreamReader( new FileStream( listFile, FileMode.Open, FileAccess.Read ) ) ) {
                            while ( !reader.EndOfStream ) files.Add( reader.ReadLine() );
                        }
                    }

                    var xFile = options.GetOption( "file" );
                    while ( xFile != null ) {
                        files.Add( xFile );
                        xFile = options.GetOption( "file" );
                    }

                    if ( options.optionKeys.Count != 0 ) return ShowError( ErrorCode.InvalidArgument );

                    using ( var fIn = new FileStream( archiveFile, FileMode.Open, FileAccess.Read ) ) {
                        var archive = new PSARC.Archive( fIn );

                        if ( files.Count == 0 ) foreach ( var path in archive.filePaths ) files.Add( path ); // extract all

                        foreach ( var file in files ) {
                            string fileName = Path.GetFileName( file );
                            string path = file.Remove( file.Length - fileName.Length );
                            Directory.CreateDirectory( Path.Combine( baseDir, path ) );
                            var filePath = Path.GetFullPath( Path.Combine( baseDir, file ) );
                            bool exists = File.Exists( filePath );
                            if ( !overwrite && exists ) return ShowError( ErrorCode.FileExists, filePath );
                            using ( var fOut = new FileStream( filePath, FileMode.Create, FileAccess.Write ) ) {
                                LogInfo( $"extracting {file}" );
                                archive.ExtractFile( fIn, file, fOut );
                                if ( exists ) LogOverwriteFile( filePath );
                            }
                        }
                    }
                }

            } catch (PSARC.InvalidArchiveException) {
                return ShowError( ErrorCode.InvalidArchive, archiveFile );
            }

            return 0;
        }

    }
}
