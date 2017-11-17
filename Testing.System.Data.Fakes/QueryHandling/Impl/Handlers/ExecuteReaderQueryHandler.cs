namespace System.Data.Fakes.QueryHandling.Impl
{
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    sealed class ExecuteReaderQueryHandler<TArgs, T> : AbstractQueryHandler
    {
        private readonly Func<TArgs, T[]> getResult;
        public ExecuteReaderQueryHandler(List<Regex> queryRegex, Dictionary<string, Func<object, bool>> argCheckers, Func<TArgs, T[]> getResult) : base(queryRegex, argCheckers)
        {
            this.getResult = getResult;
        }

        protected override QueryType Type => QueryType.Reader;

        public override IDataReader GetReader(IDbCommand command) => DataReader.Create(getResult(GetArgs<TArgs>(command)));
    }
}
