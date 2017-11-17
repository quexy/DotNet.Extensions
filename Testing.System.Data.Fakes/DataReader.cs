namespace System.Data.Fakes
{
    using System.Linq;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Globalization;

    public class DataReader : IDataReader
    {
        private static readonly StringComparer comparer = StringComparer.OrdinalIgnoreCase;
        public static DataReader Create<T>(IEnumerable<T> data)
        {
            return new DataReader(typeof(T).GetProperties(), data.OfType<object>().ToArray());
        }

        private int index = -1;
        private readonly PropertyInfo[] columns;
        private readonly object[] rows;
        public DataReader(PropertyInfo[] columns, object[] rows)
        {
            IsClosed = false;
            this.columns = columns;
            this.rows = rows;
        }

        public object this[int i] => columns[i].GetValue(rows[index]) ?? DBNull.Value;

        public object this[string name] => columns.Where(p => comparer.Equals(p.Name, name))
            .Select(p => p.GetValue(rows[index]) ?? DBNull.Value) ?? throw new IndexOutOfRangeException();

        public string GetName(int i) => columns[i].Name;

        public int GetOrdinal(string name) => columns.Select((p, i) => new { p, i })
            .SingleOrDefault(e => comparer.Equals(e.p.Name, name))?.i ?? throw new IndexOutOfRangeException();

        public int Depth => 0;

        public bool IsClosed { get; set; }

        public int RecordsAffected => rows.Length;

        public int FieldCount => columns.Length;

        public void Close() => IsClosed = true;

        public void Dispose() => Close();

        public bool NextResult() => false;

        public bool Read()
        {
            if (IsClosed) return false;
            ++index;
            return index < rows.Length;
        }

        public bool IsDBNull(int i) => this[i] == DBNull.Value;

        public object GetValue(int i) => this[i];

        public string GetString(int i)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0}", this[i]);
        }

        public int GetValues(object[] values)
        {
            if (values == null) throw new ArgumentNullException(nameof(GetValues));
            var length = Math.Min(values.Length, columns.Length);
            var buffer = columns.Select(c => c.GetValue(rows[index]) ?? DBNull.Value).ToArray();
            Array.Copy(buffer, values, length);
            return length;
        }

        public IDataReader GetData(int i)
        {
            return new DataReader(new[] { columns[i] }, rows);
        }

        public Type GetFieldType(int i)
        {
            return columns[i].PropertyType;
        }

        #region not supported methods
        public string GetDataTypeName(int i)
        {
            throw new NotSupportedException();
        }

        public DataTable GetSchemaTable()
        {
            throw new NotSupportedException();
        }

        public bool GetBoolean(int i)
        {
            throw new NotSupportedException();
        }

        public byte GetByte(int i)
        {
            throw new NotSupportedException();
        }

        public long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public char GetChar(int i)
        {
            throw new NotSupportedException();
        }

        public long GetChars(int i, long fieldoffset, char[] buffer, int bufferoffset, int length)
        {
            throw new NotSupportedException();
        }

        public DateTime GetDateTime(int i)
        {
            throw new NotSupportedException();
        }

        public decimal GetDecimal(int i)
        {
            throw new NotSupportedException();
        }

        public double GetDouble(int i)
        {
            throw new NotSupportedException();
        }

        public float GetFloat(int i)
        {
            throw new NotSupportedException();
        }

        public Guid GetGuid(int i)
        {
            throw new NotSupportedException();
        }

        public short GetInt16(int i)
        {
            throw new NotSupportedException();
        }

        public int GetInt32(int i)
        {
            throw new NotSupportedException();
        }

        public long GetInt64(int i)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}
