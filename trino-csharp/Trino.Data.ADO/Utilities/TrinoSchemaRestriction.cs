namespace Trino.Data.ADO.Utilities
{
    /// <summary>
    /// Describes a restriction on a schema column used to recover Trino schema.
    /// </summary>
    internal class TrinoSchemaRestriction
    {
        internal string TrinoColumnName { get; private set; }
        internal SchemaRestrictionType RestrictionType { get; private set; }
        internal TrinoSchemaRestriction(string columnName, SchemaRestrictionType restrictionType)
        {
            TrinoColumnName = columnName;
            RestrictionType = restrictionType;
        }
    }
}
