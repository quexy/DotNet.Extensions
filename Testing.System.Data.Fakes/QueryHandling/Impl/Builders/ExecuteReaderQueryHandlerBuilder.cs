using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace System.Data.Fakes.QueryHandling.Impl
{
    sealed class ExecuteReaderQueryHandlerBuilder<T> : AbstractQueryHandlerBuilder<IExecuteReaderQueryHandlerBuilderArgChecker<T>, T[]>,
        IExecuteReaderQueryHandlerBuilder<T>, IExecuteReaderQueryHandlerBuilderArgChecker<T> where T : class
    {
        protected override IQueryHandler CreateHandler<TArgs>(List<Regex> queryRegex, Dictionary<string, Func<object, bool>> argCheckers, Func<TArgs, T[]> getResult)
        {
            return new ExecuteReaderQueryHandler<TArgs, T>(queryRegex, argCheckers, getResult);
        }
    }
}
