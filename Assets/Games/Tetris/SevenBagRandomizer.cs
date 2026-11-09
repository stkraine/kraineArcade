using System;
using System.Collections.Generic;

/// <summary>
/// Guideline 7-bag random generator: shuffles all 7 tetrominoes, deals one by one, refills when empty.
/// </summary>
public class SevenBagRandomizer
{
    private readonly Random _random;
    private readonly Queue<TetrominoType> _queue = new Queue<TetrominoType>();

    public SevenBagRandomizer(int? seed = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        Refill();
    }

    public TetrominoType Next()
    {
        if (_queue.Count == 0)
            Refill();
        return _queue.Dequeue();
    }

    public TetrominoType Peek(int index)
    {
        while (_queue.Count <= index)
            Refill();
        int i = 0;
        foreach (var item in _queue)
        {
            if (i == index) return item;
            i++;
        }
        return _queue.Peek();
    }

    private void Refill()
    {
        var bag = new List<TetrominoType>
        {
            TetrominoType.I, TetrominoType.O, TetrominoType.T,
            TetrominoType.S, TetrominoType.Z, TetrominoType.J, TetrominoType.L
        };
        // Fisher-Yates shuffle
        for (int i = bag.Count - 1; i > 0; i--)
        {
            int j = _random.Next(i + 1);
            var tmp = bag[i];
            bag[i] = bag[j];
            bag[j] = tmp;
        }
        foreach (var t in bag)
            _queue.Enqueue(t);
    }
}
