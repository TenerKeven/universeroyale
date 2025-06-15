using Godot;
using System;
using System.Dynamic;

public struct RingBuffer<T>
{
    private T[] buffer;
    private readonly int capacity;
    private int amount;

    public RingBuffer(int capacity)
    {
        amount = 0;
        buffer = new T[capacity];
        this.capacity = capacity;
    }

    public void Add(T item)
    {
        buffer[amount % capacity] = item;
        amount += 1;
    }

    public void Set(int index, T item)
    {
        buffer[index % capacity] = item;
    }

    public T Get(int index)
    {
        return buffer[index % capacity];
    }

    public void Clear()
    {
        amount = 0;
        buffer = new T[capacity];
    }
}
