using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace System.Data.Fakes.QueryHandling.Impl
{
    sealed class ExecuteScalarQueryHandlerBuilder<T> : AbstractQueryHandlerBuilder<IExecuteScalarQueryHandlerBuilderArgChecker<T>, T>,
        IExecuteScalarQueryHandlerBuilder<T>, IExecuteScalarQueryHandlerBuilderArgChecker<T> where T : struct
    {
        protected override IQueryHandler CreateHandler<TArgs>(List<Regex> queryRegex, Dictionary<string, Func<object, bool>> argCheckers, Func<TArgs, T> getResult)
        {
            return new ExecuteScalarQueryHandler<TArgs, T>(queryRegex, argCheckers, getResult);
        }
    }

    sealed class ExecuteScalarObjectQueryHandlerBuilder : AbstractQueryHandlerBuilder<IExecuteScalarQueryHandlerBuilderArgChecker<object>, object>,
        IExecuteScalarQueryHandlerBuilder<object>, IExecuteScalarQueryHandlerBuilderArgChecker<object>
    {
        protected override IQueryHandler CreateHandler<TArgs>(List<Regex> queryRegex, Dictionary<string, Func<object, bool>> argCheckers, Func<TArgs, object> getResult)
        {
            return new ExecuteScalarQueryHandler<TArgs, object>(queryRegex, argCheckers, getResult);
        }
    }
}
