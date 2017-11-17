namespace System.Data.Fakes.QueryHandling
{
    public interface IExecuteNonQueryQueryHandlerBuilder :
        ICommandCheck<IExecuteNonQueryQueryHandlerBuilderArgChecker>
    { }
    public interface IExecuteNonQueryQueryHandlerBuilderArgChecker :
        IArgChecker<IExecuteNonQueryQueryHandlerBuilderArgChecker>, IQueryResult<int>
    { }

    public interface IExecuteScalarQueryHandlerBuilder<T> :
        ICommandCheck<IExecuteScalarQueryHandlerBuilderArgChecker<T>>
    { }
    public interface IExecuteScalarQueryHandlerBuilderArgChecker<T> :
        IArgChecker<IExecuteScalarQueryHandlerBuilderArgChecker<T>>, IQueryResult<T>
    { }

    public interface IExecuteReaderQueryHandlerBuilder<T> :
        ICommandCheck<IExecuteReaderQueryHandlerBuilderArgChecker<T>>
    { }
    public interface IExecuteReaderQueryHandlerBuilderArgChecker<T> :
        IArgChecker<IExecuteReaderQueryHandlerBuilderArgChecker<T>>, IQueryResult<T[]>
    { }
}
