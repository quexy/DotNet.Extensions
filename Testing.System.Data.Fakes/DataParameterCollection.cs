namespace System.Data.Fakes
{
    using System.Collections;
    using System.Collections.Generic;

    public class DataParameterCollection : IDataParameterCollection
    {
        private readonly object syncRoot = new object();
        private readonly System.Collections.ArrayList Parameters = new System.Collections.ArrayList();
        public List<System.Collections.ArrayList> History { get; } = new List<System.Collections.ArrayList>();

        public object this[int index]
        {
            get => Parameters[index];
            set => Parameters[index] = value;
        }

        public bool IsFixedSize => Parameters.IsFixedSize;

        public bool IsReadOnly => Parameters.IsReadOnly;

        public int Count => Parameters.Count;

        public bool IsSynchronized => Parameters.IsSynchronized;

        public object SyncRoot => Parameters.SyncRoot;

        public int Add(object value)
        {
            return Parameters.Add(value);
        }

        public void Clear()
        {
            var hist = new System.Collections.ArrayList();
            hist.AddRange(Parameters);
            History.Add(hist);
            Parameters.Clear();
        }

        public bool Contains(object value)
        {
            return Parameters.Contains(value);
        }

        public void CopyTo(Array array, int index)
        {
            Parameters.CopyTo(array, index);
        }

        public IEnumerator GetEnumerator()
        {
            return Parameters.GetEnumerator();
        }

        public int IndexOf(object value)
        {
            return Parameters.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            Parameters.Insert(index, value);
        }

        public void Remove(object value)
        {
            Parameters.Remove(value);
        }

        public void RemoveAt(int index)
        {
            Parameters.RemoveAt(index);
        }

        public object this[string parameterName]
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public bool Contains(string parameterName)
        {
            throw new NotSupportedException();
        }

        public int IndexOf(string parameterName)
        {
            throw new NotSupportedException();
        }

        public void RemoveAt(string parameterName)
        {
            throw new NotSupportedException();
        }
    }
}
