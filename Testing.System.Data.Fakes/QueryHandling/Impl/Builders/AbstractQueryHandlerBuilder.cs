using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace System.Data.Fakes.QueryHandling.Impl
{
    abstract class AbstractQueryHandlerBuilder<TArgChecker, TResult> : IArgChecker<TArgChecker>, IQueryString
    {
        private readonly List<Regex> QueryRegex = new List<Regex>();
        private readonly Dictionary<string, Func<object, bool>> ArgumentCheckers = new Dictionary<string, Func<object, bool>>();

        public event BuilderFinished Finished = delegate { };

        public TArgChecker WithArgument<TArg>(string name, TArg value)
        {
            var comparer = EqualityComparer<TArg>.Default;
            return WithArgument<TArg>(name, obj => comparer.Equals(obj, value));
        }

        public TArgChecker WithArgument<TArg>(string name, Func<TArg, bool> isValue)
        {
            ArgumentCheckers.Add(Prefix(name), o => isValue((TArg)o));
            return (TArgChecker)(object)this;
        }

        public TArgChecker WithArgument(string name, Func<object, bool> isValue)
        {
            ArgumentCheckers.Add(Prefix(name), isValue);
            return (TArgChecker)(object)this;
        }

        private static string Prefix(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));

            if (name.StartsWith("@")) return name;
            return "@" + name;
        }

        public TArgChecker ForCommand(Action<IQueryString> setupChecker)
        {
            setupChecker(this);
            return (TArgChecker)(object)this;
        }

        public IQueryString Having(string regex)
        {
            QueryRegex.Add(new Regex(regex, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase));
            return this;
        }

        public IQueryHandler Return(TResult result)
        {
            return CallFinished(CreateHandler(QueryRegex, ArgumentCheckers, (object o) => result));
        }

        public IQueryHandler Return<TArgs>(TArgs args, Func<TArgs, TResult> getResult)
        {
            return CallFinished(CreateHandler(QueryRegex, ArgumentCheckers, getResult));
        }

        private IQueryHandler CallFinished(IQueryHandler handler)
        {
            Finished(handler);
            return handler;
        }

        protected abstract IQueryHandler CreateHandler<TArgs>(List<Regex> queryRegex, Dictionary<string, Func<object, bool>> argCheckers, Func<TArgs, TResult> result);
    }
}
