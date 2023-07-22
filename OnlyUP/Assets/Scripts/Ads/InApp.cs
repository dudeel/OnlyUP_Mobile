using UnityEngine;
using UnityEngine.Purchasing;

public class InApp : MonoBehaviour
{
    public void OnPurchaseCompleted(Product product)
    {
        switch(product.definition.id)
        {
            case "com.TeamWaves.OnlyUP.removeads":
                RemoveAds();
                break;
        }
    }

    private void RemoveAds()
    {
        PlayerPrefs.SetInt("removeads", 1);
        Debug.Log("Purchase: removeads");
    }
}