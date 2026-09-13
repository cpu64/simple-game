using System;

public class RingBuffer<T>
{
    private readonly T[] buffer;
    private int start;
    private int count;

    public int Count => count;
    public int Capacity => buffer.Length;

    public RingBuffer(int capacity, T defaultValue = default(T))
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));

        buffer = new T[capacity];

        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = defaultValue;
        }

        count = 0;
    }

    public void Add(T item)
    {
        int index = (start + count) % buffer.Length;

        buffer[index] = item;

        if (count < buffer.Length)
        {
            count++;
        }
        else
        {
            start = (start + 1) % buffer.Length;
        }
    }

    public T Get(int index)
    {
        if (index < 0 || index >= count)
            throw new ArgumentOutOfRangeException(nameof(index));

        return buffer[(start + index) % buffer.Length];
    }
}
