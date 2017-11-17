using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace System.Data.Fakes.QueryHandling.Impl
{
    sealed class ExecuteScalarQueryHandler<TArgs, T> : AbstractQueryHandler
    {
        private readonly Func<TArgs, T> getResult;
        public ExecuteScalarQueryHandler(List<Regex> queryRegex, Dictionary<string, Func<object, bool>> argCheckers, Func<TArgs, T> getResult) : base(queryRegex, argCheckers)
        {
            this.getResult = getResult;
        }

        protected override QueryType Type => QueryType.Scalar;

        public override object GetScalar(IDbCommand command) => getResult(GetArgs<TArgs>(command));
    }
}
