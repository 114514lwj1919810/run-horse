using System.Collections.Generic;
using UnityEngine;

public sealed class MainThreadMessageQueue : MonoBehaviour
{
    private readonly Queue<string> pending = new Queue<string>();

    public int Count
    {
        get
        {
            lock (pending)
            {
                return pending.Count;
            }
        }
    }

    public void Enqueue(string message)
    {
        lock (pending)
        {
            pending.Enqueue(message ?? string.Empty);
        }
    }

    public bool TryDequeue(out string message)
    {
        lock (pending)
        {
            if (pending.Count == 0)
            {
                message = null;
                return false;
            }
            message = pending.Dequeue();
            return true;
        }
    }

    public void Clear()
    {
        lock (pending)
        {
            pending.Clear();
        }
    }
}
