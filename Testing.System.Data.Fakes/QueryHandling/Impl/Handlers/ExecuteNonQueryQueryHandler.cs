using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace System.Data.Fakes.QueryHandling.Impl
{
    sealed class ExecuteNonQueryQueryHandler<TArgs> : AbstractQueryHandler
    {
        private readonly Func<TArgs, int> getResult;
        public ExecuteNonQueryQueryHandler(List<Regex> queryRegex, Dictionary<string, Func<object, bool>> argCheckers, Func<TArgs, int> getResult) : base(queryRegex, argCheckers)
        {
            this.getResult = getResult;
        }

        protected override QueryType Type => QueryType.NonQuery;

        public override int GetResult(IDbCommand command) => getResult(GetArgs<TArgs>(command));
    }
}
