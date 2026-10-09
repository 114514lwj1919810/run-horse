using System.Collections.Generic;
using System.Text;

public sealed class SerialLineBuffer
{
    private readonly StringBuilder pending = new StringBuilder();
    private readonly Queue<string> completedQueue = new Queue<string>();
    private bool droppingOversizeLine;

    public SerialLineBuffer()
    {
        MaximumLineLength = ProtocolValidation.FullStateFrameLimit;
    }

    public int MaximumLineLength { get; set; }
    public int DroppedLineCount { get; private set; }
    public string PendingText { get { return pending.ToString(); } }

    public IList<string> Append(string chunk)
    {
        return AppendInternal(chunk, false);
    }

    public IList<string> AppendCallbackLine(string line)
    {
        return AppendInternal(line, true);
    }

    public IList<string> Append(byte[] bytes, int count)
    {
        if (bytes == null || count <= 0)
        {
            return new List<string>();
        }
        int safeCount = count > bytes.Length ? bytes.Length : count;
        return Append(Encoding.ASCII.GetString(bytes, 0, safeCount));
    }

    public bool TryReadLine(out string line)
    {
        if (completedQueue.Count == 0)
        {
            line = null;
            return false;
        }
        line = completedQueue.Dequeue();
        return true;
    }

    public void Clear()
    {
        pending.Length = 0;
        completedQueue.Clear();
        droppingOversizeLine = false;
    }

    private IList<string> AppendInternal(string chunk, bool completeAtEnd)
    {
        var completed = new List<string>();
        if (string.IsNullOrEmpty(chunk))
        {
            return completed;
        }

        for (int index = 0; index < chunk.Length; index++)
        {
            char character = chunk[index];
            if (character == '\n')
            {
                CompleteLine(completed);
            }
            else if (character != '\r' && !droppingOversizeLine)
            {
                if (pending.Length >= MaximumLineLength)
                {
                    pending.Length = 0;
                    droppingOversizeLine = true;
                    DroppedLineCount++;
                }
                else
                {
                    pending.Append(character);
                }
            }
        }

        if (completeAtEnd)
        {
            CompleteLine(completed);
        }
        return completed;
    }

    private void CompleteLine(List<string> completed)
    {
        if (droppingOversizeLine)
        {
            droppingOversizeLine = false;
            pending.Length = 0;
            return;
        }
        if (pending.Length == 0)
        {
            return;
        }
        string line = pending.ToString();
        pending.Length = 0;
        completed.Add(line);
        completedQueue.Enqueue(line);
    }
}
