using System.Collections.Generic;

public sealed class DuplicateSequenceCache
{
    private const int Capacity = 64;
    private readonly HashSet<string> keys = new HashSet<string>();
    private readonly Queue<string> order = new Queue<string>();

    public bool HasSeen(ProtocolCommand command, uint sequence)
    {
        return keys.Contains(BuildKey(command, sequence));
    }

    public bool TryRemember(ProtocolCommand command, uint sequence)
    {
        string key = BuildKey(command, sequence);
        if (keys.Contains(key))
        {
            return false;
        }

        keys.Add(key);
        order.Enqueue(key);
        while (order.Count > Capacity)
        {
            keys.Remove(order.Dequeue());
        }
        return true;
    }

    public void Clear()
    {
        keys.Clear();
        order.Clear();
    }

    private static string BuildKey(ProtocolCommand command, uint sequence)
    {
        return ((int)command).ToString() + ":" + sequence;
    }
}
