using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace Trino.Data.ADO.Client
{
    /// <summary>
    /// Trino Parameter Collection, Trino only uses the parameter name.
    /// </summary>
    public class TrinoParameterCollection : DbParameterCollection
    {
        private readonly IList<IDataParameter> parameters = new List<IDataParameter>();

        public override bool IsFixedSize => throw new NotSupportedException();

        public override bool IsReadOnly => throw new NotSupportedException();

        public override int Count => parameters.Count;

        public override bool IsSynchronized => throw new NotSupportedException();

        public override object SyncRoot => throw new NotSupportedException();

        public override int Add(object value)
        {
            if (value == null) throw new ArgumentNullException("value");
            if (value is IDataParameter parameter)
            {
                parameters.Add(parameter);
                return 0;
            }
            return 1;
        }

        public override void AddRange(Array values)
        {
            throw new NotSupportedException();
        }

        public override void Clear()
        {
            throw new NotSupportedException();
        }

        public override bool Contains(string parameterName)
        {
            throw new NotSupportedException();
        }

        public override bool Contains(object value)
        {
            throw new NotSupportedException();
        }

        public override void CopyTo(Array array, int index)
        {
            throw new NotSupportedException();
        }

        public override IEnumerator GetEnumerator()
        {
            return parameters.GetEnumerator();
        }

        public override int IndexOf(string parameterName)
        {
            throw new NotSupportedException();
        }

        public override int IndexOf(object value)
        {
            throw new NotSupportedException();
        }

        public override void Insert(int index, object value)
        {
            throw new NotSupportedException();
        }

        public override void Remove(object value)
        {
            throw new NotSupportedException();
        }

        public override void RemoveAt(string parameterName)
        {
            throw new NotSupportedException();
        }

        public override void RemoveAt(int index)
        {
            throw new NotSupportedException();
        }

        protected override DbParameter GetParameter(int index)
        {
            throw new NotSupportedException();
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            throw new NotSupportedException();
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            throw new NotSupportedException();
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            throw new NotSupportedException();
        }
    }
}
