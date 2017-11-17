namespace System.Data.Fakes.QueryHandling
{
    public interface IQueryHandler
    {
        bool ForExecuteNonQuery { get; }
        bool ForExecuteScalar { get; }
        bool ForExecuteReader { get; }

        bool Handles(IDbCommand command);

        int GetResult(IDbCommand command);
        object GetScalar(IDbCommand command);
        IDataReader GetReader(IDbCommand command);
    }
}
