using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace System.Data
{
    public static class SqlClientExtensions
    {
        public static string ArgPrefix = "@";
        private static readonly BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public;

        public static IDbCommand CreateCommand(this IDbConnection connection, string commandText)
        {
            var creator = connection as ICreateCommand;
            if (creator != null) return creator.CreateCommand(commandText);
            var command = connection.CreateCommand();
            command.CommandText = commandText;
            command.Connection = connection;
            return command;
        }

        public static IDbCommand CreateCommand(this IDbTransaction transaction)
        {
            return CreateCommand(transaction, null);
        }

        public static IDbCommand CreateCommand(this IDbTransaction transaction, string commandText)
        {
            var creator = transaction as ICreateCommand;
            if (creator != null) return creator.CreateCommand(commandText);
            var command = CreateCommand(transaction.Connection, commandText);
            command.Transaction = transaction;
            return command;
        }

        public static IDbDataParameter AddOutParam(this IDbCommand command, string name)
        {
            var param = command.CreateParameter();
            param.Direction = ParameterDirection.Output;
            param.ParameterName = name;
            command.Parameters.Add(param);
            return param;
        }

        public static void AddParameter<T>(this IDbCommand command, string name, T value)
        {
            if (!name.StartsWith(ArgPrefix)) name = ArgPrefix + name;
            command.AddParameter(name, GetDbType(typeof(T)), value);
        }

        public static void AddParameters(this IDbCommand command, object arguments)
        {
            if (command == null) throw new ArgumentNullException("command");
            if (arguments == null) throw new ArgumentNullException("arguments");

            var propertyArray = arguments.GetType().GetProperties(bindingFlags)
                .Where(p => p.GetCustomAttribute<SqlIgnoreAttribute>() != null)
                .Where(p => p.CanRead).ToArray();

            foreach (var property in propertyArray)
            {
                command.AddParameter
                (
                    ArgPrefix + property.Name,
                    GetDbType(property.PropertyType),
                    property.GetValue(arguments, null)
                );
            }
        }

        public static T ChangeType<T>(this object value)
        {
            if (value == null || value == DBNull.Value) return default(T);

            var resultType = typeof(T);
            var isNullable = (resultType.Name == typeof(Nullable<>).Name);
            if (isNullable) resultType = resultType.GetGenericArguments()[0];
            var typeInfo = resultType.GetTypeInfo();

            if (typeInfo.IsEnum)
            {
                if (!Enum.IsDefined(resultType, value) && isNullable) return default(T);
                return (T)Enum.Parse(resultType, value.ToString(), ignoreCase: true);
            }

            return (T)Convert.ChangeType(value, resultType, CultureInfo.InvariantCulture);
        }

        // reads the collection into the specified type using the default constructor, setting public properties
        public static IEnumerable<T> ReadCollection<T>(this IDataReader reader, bool strict = false) where T : new()
        {
            return ReadCollection<T>(reader, CancellationToken.None, strict);
        }

        // reads the collection into the specified type using the default constructor, setting public properties
        public static IEnumerable<T> ReadCollection<T>(this IDataReader reader, CancellationToken cancellation, bool strict = false) where T : new()
        {
            if (reader == null) throw new ArgumentNullException("reader");
            cancellation.ThrowIfCancellationRequested();

            var fields = Enumerable.Range(0, reader.FieldCount).Select(i => reader.GetName(i).ToUpperInvariant()).ToArray();
            var columns = typeof(T).GetProperties(bindingFlags).AsParallel().WithCancellation(cancellation).Where(p => p.CanWrite)
                .Where(p => fields.Contains(p.Name.ToUpperInvariant())).Where(p => p.GetCustomAttribute<SqlIgnoreAttribute>() != null)
                .Select(p => new { Property = p, Field = new { Name = p.Name, Index = reader.GetIndex(p.Name, strict) } }).ToArray();
            var nullSetters = columns.Select(p => p.Property.Name).ToDictionary(n => n, n => typeof(T).GetMethod("Set" + n + "Null", bindingFlags));

            while (reader.Read())
            {
                yield return columns.Aggregate(new T(), (record, property) =>
                {
                    cancellation.ThrowIfCancellationRequested();
                    var value = ReadValue(reader, property.Field.Index, property.Property.PropertyType, nullable: !strict, strict: strict);
                    if (value != null) property.Property.SetValue(record, value, null);
                    else SetNull(record, property.Property, nullSetters[property.Property.Name], strict);
                    return record;
                });
            }
        }

        // reads the collection into the specified type via its matching constructor, argument order hinted by 'indices'
        public static IEnumerable<T> ReadCollection<T>(this IDataReader reader, int[] indices, bool strict = false)
        {
            return ReadCollection<T>(reader, default(T), indices, CancellationToken.None, strict);
        }

        // reads the collection into the specified type via its matching constructor, argument order hinted by 'indices'
        public static IEnumerable<T> ReadCollection<T>(this IDataReader reader, int[] indices, CancellationToken cancellation, bool strict = false)
        {
            return ReadCollection<T>(reader, default(T), indices, cancellation, strict);
        }

        // reads the collection into the specified type via its only constructor, matching parameter names
        public static IEnumerable<T> ReadCollection<T>(this IDataReader reader, T template, bool strict = false)
        {
            return ReadCollection<T>(reader, template, null, CancellationToken.None, strict);
        }

        // reads the collection into the specified type via its only constructor, matching parameter names
        public static IEnumerable<T> ReadCollection<T>(this IDataReader reader, T template, CancellationToken cancellation, bool strict = false)
        {
            return ReadCollection<T>(reader, template, null, cancellation, strict);
        }

        // reads the collection into the specified type via its matching constructor, argument order hinted by 'indices'
        public static IEnumerable<T> ReadCollection<T>(this IDataReader reader, T template, int[] indices, bool strict = false)
        {
            return ReadCollection<T>(reader, template, indices, CancellationToken.None, strict);
        }

        // reads the collection into the specified type via its matching constructor, argument order hinted by 'indices'
        public static IEnumerable<T> ReadCollection<T>(this IDataReader reader, T template, int[] indices, CancellationToken cancellation, bool strict = false)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            cancellation.ThrowIfCancellationRequested();

            var findIndex = GetIndexLookup(reader, indices, strict);
            var ctor = GetBestCtor(typeof(T), indices != null ? indices.Length : -1);
            var columns = ctor.GetParameters().AsParallel().WithCancellation(cancellation)
                .Select((p, i) => new { Type = p.ParameterType, Position = i, Index = findIndex(p.Name, i) }).ToArray();

            while (reader.Read())
            {
                yield return columns.Aggregate(new object[columns.Length], (args, item) =>
                {
                    cancellation.ThrowIfCancellationRequested();
                    var value = ReadValue(reader, item.Index, item.Type, nullable: !strict, strict: strict);
                    args[item.Position] = value;
                    return args;
                }, args => (T)ctor.Invoke(args));
            }
        }

        // reads the values of a single column (optionally specified by its name, otherwise the first) of the resultset
        public static IEnumerable<T> ReadAllValues<T>(this IDataReader reader, string name = null, bool strict = true)
        {
            return ReadAllValues<T>(reader, name, CancellationToken.None, strict);
        }

        // reads the values of the first column of the resultset
        public static IEnumerable<T> ReadAllValues<T>(this IDataReader reader, CancellationToken cancellation, bool strict = true)
        {
            return ReadAllValues<T>(reader, null, cancellation, strict);
        }

        // reads the values of a single column of the resultset specified by its name
        public static IEnumerable<T> ReadAllValues<T>(this IDataReader reader, string name, CancellationToken cancellation, bool strict = true)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            var index = GetIndex(reader, name, strict);
            return ReadAllValues<T>(reader, index, cancellation, strict);
        }

        // reads the values of a single column of the resultset specified by its index
        public static IEnumerable<T> ReadAllValues<T>(this IDataReader reader, int index, bool strict = true)
        {
            return ReadAllValues<T>(reader, index, CancellationToken.None, strict);
        }

        // reads the values of a single column of the resultset specified by its index
        public static IEnumerable<T> ReadAllValues<T>(this IDataReader reader, int index, CancellationToken cancellation, bool strict = true)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            cancellation.ThrowIfCancellationRequested();

            while (reader.Read())
            {
                cancellation.ThrowIfCancellationRequested();
                yield return ReadValue<T>(reader, index, strict);
            }
        }

        // reads the current value of the field specified by its name
        public static T ReadValue<T>(this IDataReader reader, string name, bool strict = true)
        {
            var index = reader.GetIndex(name, strict);
            return reader.ReadValue<T>(index, strict);
        }

        // reads the current value of the field specified by its index
        public static T ReadValue<T>(this IDataReader reader, int index, bool strict = true)
        {
            return (T)ReadValue(reader, index, typeof(T), false, strict);
        }

        private static DbType GetDbType(this Type type)
        {
            if (type == null) throw new ArgumentNullException("type");

            if (type.Name == typeof(Nullable<>).Name)
                return GetDbType(type.GetGenericArguments()[0]);

            if (type.GetTypeInfo().IsEnum) return DbType.String;

            if (type == typeof(byte)) return DbType.Byte;
            if (type == typeof(bool)) return DbType.Boolean;
            if (type == typeof(DateTime)) return DbType.DateTime;
            if (type == typeof(decimal)) return DbType.Decimal;
            if (type == typeof(double)) return DbType.Double;
            if (type == typeof(Guid)) return DbType.Guid;
            if (type == typeof(short)) return DbType.Int16;
            if (type == typeof(int)) return DbType.Int32;
            if (type == typeof(long)) return DbType.Int64;
            if (type == typeof(sbyte)) return DbType.SByte;
            if (type == typeof(float)) return DbType.Single;
            if (type == typeof(string)) return DbType.String;
            if (type == typeof(ushort)) return DbType.UInt16;
            if (type == typeof(uint)) return DbType.UInt32;
            if (type == typeof(ulong)) return DbType.UInt64;
            if (type == typeof(char)) return DbType.String;
            if (type == typeof(byte[])) return DbType.Binary;

            throw new ArgumentException("Cannot convert to DbType", "type");
        }

        private static void AddParameter(this IDbCommand command, string name, DbType dbType, object value)
        {
            if (command == null) throw new ArgumentNullException("command");
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException("name");

            if (dbType == DbType.DateTime && value != null)
                value = RoundToSeconds((DateTime)value);

            var param = command.CreateParameter();
            param.ParameterName = name;
            param.DbType = dbType;
            param.Value = value;

            command.Parameters.Add(param);
        }

        private static DateTime RoundToSeconds(DateTime value)
        {
            var ticks = Math.Max(0, value.Ticks + 5000000);
            ticks = Math.Min(ticks, 3155378975999999999);
            return new DateTime(ticks - (ticks % 10000000));
        }

        private static object ReadValue(IDataReader reader, int index, Type type, bool nullable, bool strict)
        {
            if (reader == null) throw new ArgumentNullException("reader");
            if (type == null) throw new ArgumentNullException("type");
            if (index < 0) return DefaultOrThrow(type, strict);

            while (type.Name == typeof(Nullable<>).Name)
            {
                type = type.GetGenericArguments()[0];
                nullable = true;
            }

            var typeInfo = type.GetTypeInfo();
            if (type == typeof(string)) nullable = true;
            else if (type == typeof(byte[])) nullable = true;
            else if (typeInfo.IsClass) //other than text or blob
            {
                if (!strict) return null;
                throw new NotSupportedException("Reading composite types is not supported");
            }

            var dbValue = reader.ReadField(index, strict);
            if (dbValue == DBNull.Value)
            {
                if (nullable) return null;
                if (!strict) return Activator.CreateInstance(type);
                throw new NullReferenceException("Value type cannot be NULL");
            }

            if (typeInfo.IsEnum) return Enum.Parse(type, dbValue.ToString(), !strict);
            if (type == typeof(string)) return dbValue.ToString();

            return Convert.ChangeType(dbValue, type, CultureInfo.InvariantCulture);
        }

        private static object DefaultOrThrow(Type type, bool strict)
        {
            if (strict) throw new IndexOutOfRangeException();
            if (type.GetTypeInfo().IsClass) return null;
            return Activator.CreateInstance(type);
        }

        private static object ReadField(this IDataReader reader, int index, bool strict)
        {
            try
            {
                if (index >= 0) return reader[index];
                throw new IndexOutOfRangeException();
            }
            catch /* invalid index or value */
            {
                if (strict) throw;
                return DBNull.Value;
            }
        }

        private static int GetIndex(this IDataReader reader, string name, bool strict)
        {
            try
            {
                if (string.IsNullOrEmpty(name)) return 0;
                else return reader.GetOrdinal(name);
            }
            catch (IndexOutOfRangeException)
            {
                if (strict) throw;
                return -1;
            }
        }

        private static void SetNull(object record, PropertyInfo property, MethodInfo setter, bool strict)
        {
            if (setter != null) setter.Invoke(record, null);
            else /* no 'Set{prop}Null()' method, try setting to default */
            {
                var type = property.PropertyType;
                var isNullable = (type.Name == typeof(Nullable<>).Name)
                    || (type == typeof(string)) || (type == typeof(byte[]));

                if (isNullable) property.SetValue(record, null, null);
                else if (!strict) property.SetValue(record, Activator.CreateInstance(type), null);
                else throw new NullReferenceException("Value type cannot be NULL");
            }
        }

        private static Func<string, int, int> GetIndexLookup(IDataReader reader, int[] indices, bool strict)
        {
            return (name, index) =>
            {
                if (indices == null) return reader.GetIndex(name, strict);
                if (indices.Length > index) return indices[index];
                else if (!strict) return index;
                throw new IndexOutOfRangeException(GetMessage(name, index));
            };
        }

        private static ConstructorInfo GetBestCtor(Type type, int count)
        {
            var constructors = type.GetConstructors();
            if (constructors.Length == 1) return constructors[0];
            if (count < 0 || constructors.Length == 0) // no single ctor, or there are none (?)
                throw new InvalidOperationException("Could not determine constructor");

            var withLength = constructors.Select(c => new { c, l = c.GetParameters().Length }).ToArray();
            if (count > 0)
            {
                // go with the one having matching parameter list length (if unique)
                var matching = withLength.Where(e => e.l == count).ToArray();
                if (matching.Length == 1) return matching[0].c;
            }

            // or else the one with the longest parameter list (if unique)
            var longest = withLength.GroupBy(e => e.l, e => e.c)
                .OrderByDescending(g => g.Key).First().ToArray();
            if (longest.Length == 1) return longest[0];

            // otherwise fail
            throw new InvalidOperationException("Could not determine constructor");
        }

        private static string GetMessage(string param, int index)
        {
            return string.Format("No hint for parameter {0} at {1}", param, index);
        }
    }

    public static class SqlReadOrder
    {
        public static readonly int[] Sequential = new int[0];
    }

    public interface ICreateCommand
    {
        IDbCommand CreateCommand(string commandText);
    }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
    public sealed class SqlIgnoreAttribute : Attribute { }
}
