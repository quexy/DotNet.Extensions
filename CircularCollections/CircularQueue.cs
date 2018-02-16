namespace System.Collections.Circular
{
    using System.Collections.Generic;

#if NET40
    public sealed class CircularQueue<TItem> : IEnumerable<TItem>, ICollection
#else
    public sealed class CircularQueue<TItem> : IEnumerable<TItem>, IReadOnlyCollection<TItem>, ICollection
#endif
    {
        private int count = 0;
        private int offset = 0;
        private readonly TItem[] buffer;
        public CircularQueue(int size)
        {
            buffer = new TItem[size];
        }

        public int Size => buffer.Length;

        /// <returns> the item pushed out of the queue </returns>
        public TItem Enqueue(TItem item)
        {
            var index = (count + offset) % buffer.Length;
            var old = buffer[index]; buffer[index] = item;
            if (count < buffer.Length) count = count + 1;
            else offset = (offset + 1) % buffer.Length;
            return old;
        }

        public TItem Dequeue()
        {
            var item = Peek();
            Skip();
            return item;
        }

        public TItem Peek()
        {
            if (count > 0) return buffer[offset];
            throw new InvalidOperationException("The collection is empty");
        }

        public void Skip()
        {
            buffer[offset] = default(TItem);
            if (count == 0) throw new InvalidOperationException("The collection is empty");
            offset = (offset + 1) % buffer.Length; count = count - 1;
        }

        public void Clear()
        {
            count = 0; offset = 0;
            Array.Clear(buffer, 0, buffer.Length);
        }

        public int Count => count;

        public bool IsReadOnly => false;

        public bool IsSynchronized => false;

        public object SyncRoot => this;

        public IEnumerator<TItem> GetEnumerator()
        {
            for (int i = 0; i < count; ++i)
            {
                var index = (offset + i) % buffer.Length;
                if (index >= 0) yield return buffer[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void CopyTo(TItem[] array, int offset)
        {
            var index = 0;
            foreach (var item in this)
            {
                array[offset + index] = item;
                ++index;
            }
        }

        public void CopyTo(Array array, int offset)
        {
            var temp = new TItem[count]; CopyTo(temp, 0);
            Array.Copy(temp, 0, array, offset, count);
        }
    }
}
