using UnityEngine;

public class ShopItem : MonoBehaviour
{
    public GameObject item;
    private IDropReceiver iDrop;

     public void Buy()
    {
        iDrop = item.GetComponent<IDropReceiver>();
        iDrop.DigestDrop();
    }

    private void OnValidate()
    {
        if (item == null)
        {
            return;
        }

        iDrop = item.GetComponent<IDropReceiver>();

        if (iDrop != null)
        {
            return;
        }

        Debug.LogError("Item musi implementować IDropReceivera.");
    }
}
