using System.Collections;
using UnityEngine;
using TMPro;

public class UI_Timer : MonoBehaviour
{
    [SerializeField] private int second = 0;
    [SerializeField] private int minutes = 0;
    [SerializeField] private TextMeshProUGUI textTimer;
    public bool onPaused = false;

    private void Awake()
    {
        textTimer = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        StartTimer();
    }

    private void OnEnable()
    {
        StartTimer();
    }

    private void StartTimer()
    {
        StartCoroutine(Timer());
    }

    public void RestartTimer()
    {
        second = 0;
        minutes = 0;
    }

    IEnumerator Timer()
    {
        while (true)
        {
            if (!onPaused)
            {
                if (second == 59)
                {
                    minutes++;
                    second = -1;
                }

                second++;
                
                if (second < 10)
                    textTimer.SetText($"{minutes}:0{second}");
                else
                    textTimer.SetText($"{minutes}:{second}");
            }

            yield return new WaitForSeconds(1f);
        }
    }
}
