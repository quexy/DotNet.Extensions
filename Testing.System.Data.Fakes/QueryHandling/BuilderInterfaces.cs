namespace System.Data.Fakes.QueryHandling
{
    public interface IQueryHandlerBuilder
    {
        IExecuteReaderQueryHandlerBuilder<T> SetupReader<T>() where T : class;
        IExecuteScalarQueryHandlerBuilder<T> SetupScalar<T>() where T : struct;
        IExecuteScalarQueryHandlerBuilder<object> SetupScalar();
        IExecuteNonQueryQueryHandlerBuilder SetupNonQuery();
    }

    public interface ICommandCheck<TArgChecker>
    {
        TArgChecker ForCommand(Action<IQueryString> setupChecker);
    }

    public interface IQueryString
    {
        IQueryString Having(string regex);
    }

    public interface IArgChecker<TArgChecker>
    {
        TArgChecker WithArgument<TArg>(string name, TArg value);
        TArgChecker WithArgument<TArg>(string name, Func<TArg, bool> isValue);
        TArgChecker WithArgument(string name, Func<object, bool> isValue);
    }

    public interface IQueryResult<TResult>
    {
        IQueryHandler Return(TResult result);
        IQueryHandler Return<TArgs>(TArgs args, Func<TArgs, TResult> getResult);
    }
}
