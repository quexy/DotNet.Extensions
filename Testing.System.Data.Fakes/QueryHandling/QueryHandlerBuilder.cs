namespace System.Data.Fakes.QueryHandling
{
    public delegate void BuilderFinished(IQueryHandler handler);

    public class QueryHandlerBuilder : IQueryHandlerBuilder
    {
        public event BuilderFinished Finished = delegate { };

        public static IExecuteNonQueryQueryHandlerBuilder ForNonQuery(BuilderFinished callback = null)
        {
            return new QueryHandlerBuilder().SetupNonQuery(callback);
        }

        public static IExecuteScalarQueryHandlerBuilder<T> ForScalar<T>(BuilderFinished callback = null) where T : struct
        {
            return new QueryHandlerBuilder().SetupScalar<T>(callback);
        }

        public static IExecuteReaderQueryHandlerBuilder<T> ForReader<T>(BuilderFinished callback = null) where T : class
        {
            return new QueryHandlerBuilder().SetupReader<T>(callback);
        }

        public IExecuteNonQueryQueryHandlerBuilder SetupNonQuery() { return SetupNonQuery(null); }
        public IExecuteNonQueryQueryHandlerBuilder SetupNonQuery(BuilderFinished callback)
        {
            if (callback != null) Finished += callback;
            var builder = new Impl.ExecuteNonQueryQueryHandlerBuilder();
            builder.Finished += obj => Finished(obj);
            return builder;
        }

        public IExecuteScalarQueryHandlerBuilder<T> SetupScalar<T>() where T : struct { return SetupScalar<T>(null); }
        public IExecuteScalarQueryHandlerBuilder<T> SetupScalar<T>(BuilderFinished callback) where T : struct
        {
            if (callback != null) Finished += callback;
            var builder = new Impl.ExecuteScalarQueryHandlerBuilder<T>();
            builder.Finished += obj => Finished(obj);
            return builder;
        }

        public IExecuteReaderQueryHandlerBuilder<T> SetupReader<T>() where T : class { return SetupReader<T>(null); }
        public IExecuteReaderQueryHandlerBuilder<T> SetupReader<T>(BuilderFinished callback) where T : class
        {
            if (callback != null) Finished += callback;
            var builder = new Impl.ExecuteReaderQueryHandlerBuilder<T>();
            builder.Finished += obj => Finished(obj);
            return builder;
        }
    }
}
