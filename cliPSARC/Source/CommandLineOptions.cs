using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cliPSARC {

    public class CommandLineOptions {

        private static Dictionary<string, string> aliases;

        public string verb = null;

        public List<string> optionKeys = new List<string>();
        public List<string> optionVals = new List<string>();
        public List<string> fileParams = new List<string>();

        public int Count => optionKeys.Count;
        public int FileCount => fileParams.Count;

        public CommandLineOptions( string[] args, string[] verbs = null, Dictionary<string, string> optionAliases = null ) {
            if ( args.Length == 0 ) return;
            aliases = optionAliases ?? new Dictionary<string, string>();
            int i = ParseVerb( args[0], verbs ) ? 1 : 0;
            for ( ; i < args.Length; i++) ParseArg( args[i] );
        }

        private bool ParseVerb( string arg, string[] verbs ) {
            verb = null;
            if ( verbs == null ) return false;
            arg = arg.ToLower();
            for (int i = 0; i < verbs.Length; i++) {
                if ( arg != verbs[i] ) continue;
                verb = verbs[i];
                return true;
            }
            return false;
        }

        private void ParseArg( string arg ) {
            if ( (arg.Length > 1) && (arg[0] == '-') ) {
                char switchChar = arg[1];
                arg = arg.Substring( 2 );

                string key = "";
                string val = "";

                if ( switchChar != '-' ) {
                    key = switchChar.ToString();
                    if ( aliases.ContainsKey( key ) ) key = aliases[key];
                    arg = $"{key}={arg}";
                }

                var tokens = arg.Split( '=' );
                key = (tokens.Length > 0) ? tokens[0] : "";
                val = (tokens.Length > 1) ? tokens[1] : "";
                AddOption( key, val );
            } else {
                AddFile( arg );
            }
        }

        private void AddOption( string key, string val ) {
            optionKeys.Add( key.ToLower() );
            optionVals.Add( val );
        }

        private void AddFile( string file ) => fileParams.Add( file );

        public void RemoveOption( string key ) => RemoveOption( optionKeys.IndexOf( key ) );
        public void RemoveOption( int index ) {
            if ( index < 0 ) return;
            optionKeys.RemoveAt( index );
            optionVals.RemoveAt( index );
        }

        public string GetOption( string key ) {
            int index = optionKeys.IndexOf( key );
            if ( index < 0 ) return null;
            string val = optionVals[index];
            RemoveOption( index );
            return val;
        }

        public List<string> GetOptions( string key ) {
            var list = new List<string>();
            var param = GetOption( key );
            while (param != null) {
                list.Add( param );
                param = GetOption( key );
            }
            return list;
        }

        public bool IsOption( string key ) => GetOption( key ) != null;
    }

}
