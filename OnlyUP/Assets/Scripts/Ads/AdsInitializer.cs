using UnityEngine;
using UnityEngine.Advertisements;

public class AdsInitializer : MonoBehaviour, IUnityAdsInitializationListener
{
    [SerializeField] private string _androidGameId = "5336772";
    [SerializeField] private string _iOSGameId = "5336773";
    [SerializeField] private bool _testMode = false;
    public int isRemoveAds = 0;
    
    private string _gameId;

    void Awake()
    {
        if (PlayerPrefs.HasKey("removeads")) isRemoveAds = PlayerPrefs.GetInt("removeads");
        else isRemoveAds = 0;

        if (isRemoveAds == 1) return;

        InitializeAds();
    }

    public void InitializeAds()
    {
        _gameId = (Application.platform == RuntimePlatform.IPhonePlayer)
            ? _iOSGameId
            : _androidGameId;

        Advertisement.Initialize(_gameId, _testMode);
    }

    public void OnInitializationComplete()
    {
        Debug.Log("Unity Ads initialization complete.");
    }

    public void OnInitializationFailed(UnityAdsInitializationError error, string message)
    {
        Debug.Log($"Unity Ads Initialization Failed: {error.ToString()} - {message}");
    }
}