using System;

public struct RingBuffer<T>
{
    private readonly int capacity;
    private T[] buffer;
    public RingBuffer(int capacity)
    {
        this.capacity = capacity;
        buffer = new T[capacity];
    }

    // Associa um valor a um tick
    public void Set(int tick, T value)
    {
        int index = tick % capacity;
        buffer[index] = value;
    }

    // Retorna o valor de um tick, se existir
    public T Get(int tick)
    {
        int index = tick % capacity;
        return buffer[index];
    }

    public void Clear()
    {
        buffer = new T[capacity];
    }

    public int Capacity => capacity;
}
