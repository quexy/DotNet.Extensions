using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Fakes;
using System.Data.Fakes.QueryHandling;
using System.Linq;
using System.Text.RegularExpressions;
using Moq;

namespace System.Data.Testing
{
    public class DbTestBase
    {
        protected readonly Mock<IDbTransaction> transactionMock = new Mock<IDbTransaction>();
        protected readonly Mock<IDbConnection> connectionMock = new Mock<IDbConnection>();
        protected readonly List<Mock<IDbCommand>> commandMockCollection = new List<Mock<IDbCommand>>();
        public DbTestBase()
        {
            transactionMock.SetupGet(m => m.Connection).Returns(connectionMock.Object);
            connectionMock.Setup(m => m.CreateCommand()).Returns(() => SetupCommandMock(new Mock<IDbCommand>()).Object);
        }

        public virtual void TestCleanup()
        {
            commandMockCollection.Clear();
            QueryHandlers.Clear();
        }

        private readonly List<IQueryHandler> QueryHandlers = new List<IQueryHandler>();
        protected IQueryHandlerBuilder AddQueryHandler()
        {
            var builder = new QueryHandlerBuilder();
            builder.Finished += obj => QueryHandlers.Add(obj);
            return builder;
        }

        protected Mock<IDbCommand> SetupCommandMock(Mock<IDbCommand> commandMock)
        {
            commandMockCollection.Add(commandMock);
            var parameterCollection = new DataParameterCollection();
            commandMock.SetupProperty(m => m.CommandText);
            commandMock.SetupGet(m => m.Parameters).Returns(parameterCollection);
            commandMock.Setup(m => m.CreateParameter()).Returns(() => new DataParameter());
            commandMock.Setup(m => m.ExecuteNonQuery())
                .Returns(() => HandleExecuteNonQuery(commandMock.Object));
            commandMock.Setup(m => m.ExecuteScalar())
                .Returns(() => HandleExecuteScalar(commandMock.Object));
            commandMock.Setup(m => m.ExecuteReader())
                .Returns(() => HandleExecuteReader(commandMock.Object));
            return commandMock;
        }

        protected static void VerifyCommandArgs(IDbCommand dbCommand)
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var parameters = dbCommand.Parameters.OfType<DataParameter>().Select(p => p.ParameterName);
            var missingArgs = Regex.Matches(dbCommand.CommandText, @"[^@](@[a-zA-Z0-9_]+)\b")
                .OfType<System.Text.RegularExpressions.Match>().Where(m => m.Success)
                .Select(m => m.Groups[1].Value).Distinct(comparer)
                .Except(parameters, comparer).ToArray();
            if (missingArgs.Length == 0) return;
            throw new InvalidOperationException(string.Format
            (
                "Required parameters missing: {0}",
                String.Join(", ", missingArgs)
            ));
        }

        private int HandleExecuteNonQuery(IDbCommand dbCommand)
        {
            VerifyCommandArgs(dbCommand);
            foreach (var handler in QueryHandlers.Where(h => h.ForExecuteNonQuery))
                if (handler.Handles(dbCommand)) return handler.GetResult(dbCommand);
            throw MakeQueryError("ExecuteNonQuery", dbCommand);
        }

        private object HandleExecuteScalar(IDbCommand dbCommand)
        {
            VerifyCommandArgs(dbCommand);
            foreach (var handler in QueryHandlers.Where(h => h.ForExecuteScalar))
                if (handler.Handles(dbCommand)) return handler.GetScalar(dbCommand);
            throw MakeQueryError("ExecuteScalar", dbCommand);
        }

        private IDataReader HandleExecuteReader(IDbCommand dbCommand)
        {
            VerifyCommandArgs(dbCommand);
            foreach (var handler in QueryHandlers.Where(h => h.ForExecuteReader))
                if (handler.Handles(dbCommand)) return handler.GetReader(dbCommand);
            throw MakeQueryError("ExecuteReader", dbCommand);
        }

        private Exception MakeQueryError(string method, IDbCommand dbCommand)
        {
            var commandString = dbCommand.CommandText.Replace("\r\n", " ")
                .Replace("\r", " ").Replace("\n", " ").Replace("  ", " ");
            var commandArgs = dbCommand.Parameters.OfType<IDbDataParameter>()
                .Select(p => $"{p.ParameterName}: {p.Value}");
            return new InvalidOperationException(string.Format
            (
                "Could not satisfy '{0}' for \"{1}\" with parameters [{2}]",
                method, commandString, String.Join(", ", commandArgs)
            ));
        }
    }
}
