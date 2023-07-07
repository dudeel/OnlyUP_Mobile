using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Cinemachine;

public class UI_Settings : MonoBehaviour
{
    [SerializeField] private GameObject _mainHUD;
    [SerializeField] private GameObject _settings;
    [SerializeField] private GameObject _settingsBackground;

    [SerializeField] private Slider _sliderSensitivity;
    [SerializeField] private Slider _sliderFOV;

    [SerializeField] private TextMeshProUGUI _textSensitivity;
    [SerializeField] private TextMeshProUGUI _textFOV;

    [SerializeField] private UIVirtualTouchZone _sensivity;
    [SerializeField] private CinemachineVirtualCamera _camera;
    [SerializeField] private UI_Timer _timer;

    private void OnEnable()
    {
        _timer.onPaused = true;
        _sliderSensitivity.value = _sensivity.magnitudeMultiplier;
        _sliderFOV.value =_camera.m_Lens.FieldOfView;
    }

    public void OpenSetting()
    {
        _mainHUD.SetActive(false);

        _settingsBackground.SetActive(true);
        _settings.SetActive(true);

    }
    public void CloseSetting()
    {
        _timer.onPaused = false;
        
        _settings.SetActive(false);
        _settingsBackground.SetActive(false);

        _mainHUD.SetActive(true);
    }

    private void Update()
    {
        _textSensitivity.SetText(_sliderSensitivity.value.ToString());
        _sensivity.magnitudeMultiplier = _sliderSensitivity.value;

        _textFOV.SetText(_sliderFOV.value.ToString());
        _camera.m_Lens.FieldOfView = _sliderFOV.value;
    }
    
}
