using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Trino.Client.Utils
{
    public class TrinoFunction
    {
        private readonly string catalog;
        private readonly string functionName;
        private readonly IList<object> Parameters;

        public TrinoFunction(string catalog, string functionName, IList<object> parameters)
        {
            this.catalog = catalog;
            this.functionName = functionName;
            this.Parameters = parameters;
        }

        public virtual Task<RecordExecutor> ExecuteAsync(ClientSession session)
        {
            string statement = BuildFunctionStatement();
            return RecordExecutor.Execute(session, statement);
        }

        protected virtual string BuildFunctionStatement()
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (!string.IsNullOrEmpty(catalog))
            {
                stringBuilder.Append(this.catalog);
                stringBuilder.Append(".");
            }
            stringBuilder.Append(this.functionName);
            stringBuilder.Append("(");

            for (int i = 0; i < Parameters.Count; i++)
            {
                if (i > 0)
                {
                    stringBuilder.Append(", ");
                }

                // if parameter is a digit, do not quote it
                if (Parameters[i] is int || Parameters[i] is long || Parameters[i] is float || Parameters[i] is double)
                {
                    stringBuilder.Append(Parameters[i]);
                }
                else
                {
                    stringBuilder.Append("'");
                    stringBuilder.Append(Parameters[i]);
                    stringBuilder.Append("'");
                }
            }
            stringBuilder.Append(")");

            return stringBuilder.ToString();
        }
    }
}
