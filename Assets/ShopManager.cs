using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopManager : MonoBehaviour
{
    public List<ShopEntry> shopEntries;
    public TypeDistinguisher balance;

    public void OnEnable()
    {
        int currentBalance = balance.IntValue;

        foreach (ShopEntry entry in shopEntries)
        {
            entry.button.interactable = entry.price < currentBalance;
        }
    }

    public void BuyItem(ShopEntry shopEntry)
    {
        balance.SetIntValue(balance.IntValue - shopEntry.price);
    }
}
