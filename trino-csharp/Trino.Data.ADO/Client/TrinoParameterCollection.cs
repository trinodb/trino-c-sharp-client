using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Trino.Data.ADO.Client
{
    /// <summary>
    /// Trino Parameter Collection, Trino only uses the parameter name.
    /// </summary>
    public class TrinoParameterCollection : DbParameterCollection
    {
        private readonly List<IDataParameter> parameters = new List<IDataParameter>();

        public override bool IsFixedSize => false;

        public override bool IsReadOnly => false;

        public override int Count => parameters.Count;

        public override bool IsSynchronized => false;

        public override object SyncRoot => parameters;

        public override int Add(object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value is IDataParameter parameter)
            {
                parameters.Add(parameter);
                return parameters.Count - 1;
            }

            throw new ArgumentException("Value must be an IDataParameter", nameof(value));
        }

        public override void AddRange(Array values)
        {
            if (values == null) throw new ArgumentNullException(nameof(values));
            foreach (var value in values)
            {
                Add(value);
            }
        }

        public override void Clear()
        {
            parameters.Clear();
        }

        public override bool Contains(string parameterName)
        {
            return parameters.Any(
                p => string.Equals(p.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase));
        }

        public override bool Contains(object value)
        {
            return parameters.Contains(value as IDataParameter);
        }

        public override void CopyTo(Array array, int index)
        {
            if (array == null) throw new ArgumentNullException(nameof(array));
            ((IList)parameters).CopyTo(array, index);
        }

        public override IEnumerator GetEnumerator()
        {
            return parameters.GetEnumerator();
        }

        public override int IndexOf(string parameterName)
        {
            return parameters.Select((p, i) => new { Parameter = p, Index = i })
                .FirstOrDefault(x => string.Equals(x.Parameter.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase))
                ?.Index ?? -1;
        }

        public override int IndexOf(object value)
        {
            if (value is IDataParameter parameter)
            {
                return parameters.IndexOf(parameter);
            }

            return -1;
        }

        public override void Insert(int index, object value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (value is IDataParameter parameter)
            {
                parameters.Insert(index, parameter);
                return;
            }

            throw new ArgumentException("Value must be an IDataParameter", nameof(value));
        }

        public override void Remove(object value)
        {
            if (value is IDataParameter parameter)
            {
                parameters.Remove(parameter);
            }
        }

        public override void RemoveAt(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index >= 0)
            {
                parameters.RemoveAt(index);
            }
        }

        public override void RemoveAt(int index)
        {
            parameters.RemoveAt(index);
        }

        protected override DbParameter GetParameter(int index)
        {
            return parameters[index] as DbParameter;
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            return parameters.FirstOrDefault(p => string.Equals(p.ParameterName, parameterName, StringComparison.OrdinalIgnoreCase)) as DbParameter;
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            parameters[index] = value ?? throw new ArgumentNullException(nameof(value));
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            var index = IndexOf(parameterName);
            if (index == -1)
            {
                throw new ArgumentException($"Parameter {parameterName} not found", nameof(parameterName));
            }

            parameters[index] = value;
        }
    }
}