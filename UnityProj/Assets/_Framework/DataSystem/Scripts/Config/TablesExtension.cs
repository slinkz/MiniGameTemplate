// Hand-written partial extension for Luban-generated Tables class.
// Provides table name metadata for ConfigManager to pre-load binary data.
// Tables use lazy deserialization: bytes are pre-loaded at startup, but each table
// is only deserialized on first property access.
// This file is NOT auto-generated and should be preserved across Luban regenerations.
//
// NOTE: The namespace "cfg" is lowercase because it must match the Luban-generated code
// (controlled by luban.conf topModule setting). This is intentional, not a style violation.
//
// AUTO-UPDATED by update_tables_extension.py — do not manually edit the GetTableNames() array.

namespace cfg
{
    public partial class Tables
    {
        /// <summary>
        /// Returns all table file names used by ConfigManager to pre-load binary data.
        /// Must match the lowercase names used in the generated Tables lazy properties.
        /// </summary>
        public static string[] GetTableNames()
        {
            return new string[]
            {
                "tbglobalconst",
                "tbitem",
            };
        }
    }
}
