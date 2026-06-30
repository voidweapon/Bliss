using System;

namespace Bliss.Base.Collections
{
    public class FixedCircularArray<T>
    {
        private T[] array;
        private int head;
        private int tail;
        private int count;
        private int capacity;

        public T this[int index]
        {
            get => Get(index);
            set => Add(value, out _);
        }

        public FixedCircularArray(int capacity)
        {
            this.capacity = capacity;
            array = new T[capacity];
            head = 0;
            tail = 0;
            count = 0;
        }

        public void SetCapacity(int newCapacity)
        {
            if (this.capacity == newCapacity)
            {
                return;
            }
            
            T[] newArray = new T[newCapacity];
            int newSize = Math.Min(newCapacity, count);
            int newHead = 0;
            int newTail = 0;
            
            for (int i = 0; i < newSize; i++)
            {
                newArray[i] = Get(i);
                newTail++;
            }
            
            array = newArray;
            head = newHead;
            tail = newTail;
            count = newSize;
            this.capacity = newCapacity;
        }

        public void Add(T item, out T removedItem)
        {
            if (count == capacity)
            {
                removedItem = array[tail];
                head = (head + 1) % capacity;
            }
            else
            {
                removedItem = default(T);
            }
            
            array[tail] = item;
            tail = (tail + 1) % capacity;
            count++;
            count = Math.Min(count, capacity);
        }

        public T RemoveHead()
        {
            if (count == 0)
            {
                return default(T);
            }

            T item = array[head];
            head = (head + 1) % capacity;
            count--;
            return item;
        }
        
        public T RemoveTail()
        {
            if (count == 0)
            {
                return default(T);
            }

            T item = array[tail];
            tail = (tail - 1) % capacity;
            count--;
            return item;
        }

        public T Get(int index)
        {
            if (index < 0 || index >= count)
            {
                throw new IndexOutOfRangeException("Index out of range!");
            }

            return array[(head + index) % capacity];
        }
        
        public T HeadItem()
        {
            return array[head];
        }

        public T TailItem()
        {
            int index = (tail - 1 + capacity) % capacity;
            return array[index];
        }

        public T GetFromRawIndex(int index)
        {
            if (index < 0 || index >= count)
            {
                throw new IndexOutOfRangeException("Index out of range!");
            }
            return array[index];
        }

        public void Clear()
        {
            head = 0;
            tail = 0;
            count = 0;
            Array.Clear(array, 0, array.Length);
        }

        public int Count => count;
    }
}