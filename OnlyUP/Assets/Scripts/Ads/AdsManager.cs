using UnityEngine;
using System.Collections;

public class AdsManager : MonoBehaviour
{

    [SerializeField] private CharacterController _controller;
    public int isRemoveAds = 0;

    private void Start()
    {
        if (PlayerPrefs.HasKey("removeads")) isRemoveAds = PlayerPrefs.GetInt("removeads");
        else isRemoveAds = 0;
        
        if (isRemoveAds == 1) return;

        InterstitialAd.S.LoadAd();
        StartCoroutine(TimerAds());
    }

    public void StartAds()
    {
        InterstitialAd.S.ShowAd();
    }

    IEnumerator TimerAds()
    {
        while (true)
        {
            yield return new WaitForSeconds(180f);
            StartCoroutine(Show());
        }
    }

    IEnumerator Show()
    {
        bool isShow = true;
        while (isShow)
        {
            if (_controller.isGrounded)
            {
                InterstitialAd.S.ShowAd();
                isShow = false;
            }
            yield return new WaitForSeconds(0.2f);
        }
    }
}
