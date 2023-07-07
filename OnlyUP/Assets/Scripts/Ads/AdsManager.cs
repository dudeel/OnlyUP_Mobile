using UnityEngine;
using System.Collections;

public class AdsManager : MonoBehaviour
{

    [SerializeField] private CharacterController _controller;

    private void Start()
    {
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
