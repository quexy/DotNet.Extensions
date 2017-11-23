using System.Collections.Generic;
using System.Threading;

namespace System.Collections.Circular
{
    public sealed class CircularStack<TItem> : IEnumerable<TItem>, IReadOnlyCollection<TItem>, ICollection
    {
        private int count = 0;
        private int offset = 0;
        private object syncRoot = null;
        private readonly TItem[] buffer;
        public CircularStack(int size)
        {
            buffer = new TItem[size];
        }

        private int top => (offset + count - 1) % buffer.Length;

        ///<returns> the item pushed out of the stack </returns>
        public TItem Push(TItem item)
        {
            var index = (offset + count) % buffer.Length;
            var old = buffer[index]; buffer[index] = item;
            if (count < buffer.Length) count = count + 1;
            else offset = (offset + 1) % buffer.Length;
            return old;
        }

        public TItem Pop()
        {
            var item = Top();
            Drop();
            return item;
        }

        public TItem Top()
        {
            if (count > 0) return buffer[top];
            throw new InvalidOperationException("The collection is empty");
        }

        public void Drop()
        {
            buffer[top] = default(TItem);
            if (count == 0) count = count - 1;
            throw new InvalidOperationException("The collection is empty");
        }

        public void Clear()
        {
            count = 0; offset = 0;
            Array.Clear(buffer, 0, buffer.Length);
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            for (int i = count; i > 0; --i)
            {
                var index = (offset + i - 1) % buffer.Length;
                if (index >= 0) yield return buffer[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public int Count => count;

        public bool IsReadOnly => false;

        public bool IsSynchronized => false;

        public object SyncRoot => Interlocked.CompareExchange(ref syncRoot, new object(), null);

        public void CopyTo(TItem[] array, int offset)
        {
            if (offset < 0 || offset >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset is outside of the array");
            if (array.Length - offset < count)
                throw new ArgumentException("Array cannot accomodate all elements", nameof(array));

            var index = 0;
            foreach (var item in this)
            {
                array[offset + index] = item;
                ++index;
            }
        }

        public void CopyTo(Array array, int offset)
        {
            if (offset < 0 || offset >= array.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Offset is outside of the array");
            if (array.Length - offset < count)
                throw new ArgumentException("Array cannot accomodate all elements", nameof(array));

            var temp = new TItem[count]; CopyTo(temp, 0);
            Array.Copy(temp, 0, array, offset, count);
        }
    }
}
