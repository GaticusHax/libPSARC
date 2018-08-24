using System;

namespace libPSARC {

    /// <summary>Version Utilities</summary>
    public static class Version {

        // THIS IS THE MASTER VERSION STRING. MAKE VERSION CHANGES HERE.
        //
        // The format is "Major.Minor.Patch.Prerelease"
        //
        // Semantic Versioning: https://semver.org/
        //
        // If the prerelease version is 0, then it is a master release. Otherwise
        // if the prerelease version is not 0, then the build is a (development) prerelease.
        //
        // First version is 0.1.0.1
        //
        // When the Major version is incremented:
        //      the Minor version should be reset to 0
        // When the Minor version is reset or incremented:
        //      the Patch version should be reset to 0
        // When the Patch version is reset or incremented:
        //      the Prerelease version should be reset to 0
        // When the Patch version is reset or incremented:
        //      the Prerelease version should be reset to 1
        //
        internal const string VERSION_STRING = "0.1.0.1";

        /// <summary>Gets AssemblyVersion.Major</summary>
        public static int Major      => AssemblyVersion.Major;

        /// <summary>Gets AssemblyVersion.Minor</summary>
        public static int Minor      => AssemblyVersion.Minor;

        /// <summary>Gets AssemblyVersion.Build</summary>
        public static int Patch      => AssemblyVersion.Build;

        /// <summary>Gets AssemblyVersion.Revision</summary>
        public static int Prerelease => AssemblyVersion.Revision;

        /// <summary>Gets the assembly version.</summary>
        public static System.Version AssemblyVersion => new System.Version( VERSION_STRING );

        /// <summary>
        ///     Returns a human-readable suffix indicating the <see cref="Prerelease"/> version.
        /// </summary>
        /// <returns>
        ///     If the current assembly version is a prerelease (Prerelease is not 0) then "-pre{Prerelease}" is returned.
        ///     Otherwise returns an emptry string.
        /// </returns>
        public static string GetSuffix() => (Prerelease != 0) ? $"-pre{Prerelease}" : string.Empty;

        /// <summary>
        ///     Returns the assembly version in a human-readable string format.
        ///     Eg. "1.1.0" (Release) or "1.1.0-pre1" (Pre-Release)
        /// </summary>
        /// <returns>"{<see cref="Major"/>}.{<see cref="Minor"/>}.{<see cref="Patch"/>}{<see cref="GetSuffix">Suffix</see>}"</returns>
        public static string GetString() => AssemblyVersion.ToString( 3 ) + GetSuffix();

    }

}
