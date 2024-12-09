using System;
using System.Data;
using System.Data.Common;

namespace Trino.Data.ADO
{
    /// <summary>
    /// Definition of a Trino parameter. Only the ParameterName is used by Trino.
    /// </summary>
    public class TrinoParameter : DbParameter
    {
        public override int Size { get; set; }
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string ParameterName { get; set; }
        public override string SourceColumn { get; set; }
        public override DataRowVersion SourceVersion { get; set; }
        public override object Value { get; set; }
        public override bool SourceColumnNullMapping { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

        public override void ResetDbType()
        {
            throw new NotSupportedException();
        }
    }
}
