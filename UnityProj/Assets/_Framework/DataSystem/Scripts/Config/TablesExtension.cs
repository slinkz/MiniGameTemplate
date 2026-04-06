// Hand-written partial extension for Luban-generated Tables class.
// Provides table name metadata for ConfigManager pre-loading.
// This file is NOT auto-generated and should be preserved across Luban regenerations.
//
// NOTE: The namespace "cfg" is lowercase because it must match the Luban-generated code
// (controlled by luban.conf topModule setting). This is intentional, not a style violation.

namespace cfg
{
    public partial class Tables
    {
        /// <summary>
        /// Returns all table file names that the Tables constructor will request from the loader.
        /// Must match the lowercase names used in the generated Tables constructor (e.g., "tbitem", "tbglobalconst").
        /// UPDATE THIS when adding/removing tables in Luban schema.
        /// </summary>
        public static string[] GetTableNames()
        {
            return new string[]
            {
                "tbitem",
                "tbglobalconst",
            };
        }
    }
}
