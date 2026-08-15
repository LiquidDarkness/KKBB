using System.Collections.Generic;
using UnityEngine;

public class Lotto<T>
{
    private List<T> tickets = new List<T>();

    public void FillBucket(Dictionary<T, int> data)
    {
        FillBucket((IEnumerable<KeyValuePair<T, int>>)data);
    }

    internal void FillBucket(IEnumerable<KeyValuePair<T, int>> collection)
    {
        tickets.Clear();
        foreach (var item in collection)
        {
            for (int i = 0; i < item.Value; i++)
            {
                tickets.Add(item.Key);
            }
        }
    }

    public bool HasTickets => tickets.Count > 0;

    public T GetRandomTicket()
    {
        // Random.Range(0, 0) hands back 0, so an empty bucket used to index straight out of the
        // list and throw - once per block destroyed, for as long as the pool stayed empty.
        if (tickets.Count == 0)
        {
            return default;
        }

        return tickets[UnityEngine.Random.Range(0, tickets.Count)];
    }
}
