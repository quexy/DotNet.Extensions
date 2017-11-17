namespace System.Data.Fakes.QueryHandling.Impl
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    abstract class AbstractQueryHandler : IQueryHandler
    {
        protected enum QueryType
        {
            NonQuery,
            Scalar,
            Reader
        }

        private readonly Regex[] queryRegex;
        private readonly IDictionary<string, Func<object, bool>> argCheckers;
        public AbstractQueryHandler(List<Regex> queryRegex, Dictionary<string, Func<object, bool>> argCheckers)
        {
            this.queryRegex = queryRegex.ToArray();
            this.argCheckers = argCheckers;
        }

        protected abstract QueryType Type { get; }

        public bool ForExecuteNonQuery => Type == QueryType.NonQuery;

        public bool ForExecuteScalar => Type == QueryType.Scalar;

        public bool ForExecuteReader => Type == QueryType.Reader;

        public bool Handles(IDbCommand command)
        {
            if (!queryRegex.All(r => r.IsMatch(command.CommandText))) return false;

            var cmp = StringComparer.OrdinalIgnoreCase;
            Func<string, IDbDataParameter> find = name =>
                command.Parameters.OfType<IDbDataParameter>()
                    .SingleOrDefault(p => cmp.Equals(p.ParameterName, name));

            return argCheckers.Select(e => new { p = find(e.Key), v = e.Value })
                .All(i => i.p != null && i.v(i.p.Value));
        }

        public virtual int GetResult(IDbCommand command)
        {
            throw new NotSupportedException();
        }

        public virtual object GetScalar(IDbCommand command)
        {
            throw new NotSupportedException();
        }

        public virtual IDataReader GetReader(IDbCommand command)
        {
            throw new NotSupportedException();
        }

        protected static TArgs GetArgs<TArgs>(IDbCommand command)
        {
            var cmp = StringComparer.OrdinalIgnoreCase;
            Func<string, IDbDataParameter> find =
                name => command.Parameters.OfType<IDbDataParameter>()
                    .SingleOrDefault(p => cmp.Equals(p.ParameterName, "@" + name));
            if (typeof(TArgs) == typeof(object)) return (TArgs)(new object());
            var ctor = typeof(TArgs).GetConstructors().SingleOrDefault(c => c.GetParameters().Length > 0);
            return (TArgs)ctor?.Invoke(ctor?.GetParameters().Select(p => find(p.Name).Value).ToArray());
        }
    }
}
