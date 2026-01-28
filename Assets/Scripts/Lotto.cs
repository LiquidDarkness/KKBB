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

    public T GetRandomTicket()
    {
        return tickets[UnityEngine.Random.Range(0, tickets.Count)];
    }
}
