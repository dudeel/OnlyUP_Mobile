using UnityEngine;
using UnityEngine.UI;
using Cinemachine;
using System;

public class UI_Settings : MonoBehaviour
{
    [SerializeField] private GameObject _mainHUD;
    [SerializeField] private GameObject _settings;
    [SerializeField] private GameObject _settingsBackground;

    [SerializeField] private Slider _sliderSensitivity;
    [SerializeField] private Slider _sliderFOV;

    [SerializeField] private InputField _textSensitivity;
    [SerializeField] private InputField _textFOV;

    [SerializeField] private UIVirtualTouchZone _sensivity;
    [SerializeField] private CinemachineVirtualCamera _camera;
    [SerializeField] private UI_Timer _timer;

    [SerializeField] private float _sensitivityData = 1;
    [SerializeField] private float _fovData = 60;

    private void Awake()
    {
        if (PlayerPrefs.HasKey("settingSensitivity")) _sensitivityData = PlayerPrefs.GetFloat("settingSensitivity");
        else PlayerPrefs.SetFloat("settingSensitivity", 1);

        if (PlayerPrefs.HasKey("settingSensitivity")) _fovData = PlayerPrefs.GetFloat("settingFOV");
        else PlayerPrefs.SetFloat("settingFOV", 60);
    }

    public void OpenSetting()
    {
        _mainHUD.SetActive(false);
        _settingsBackground.SetActive(true);
        _settings.SetActive(true);

        _sliderSensitivity.value = _sensitivityData;
        _textSensitivity.text = _sensitivityData.ToString();

        _sliderFOV.value = _fovData;
        _textFOV.text = _fovData.ToString();
    }
    public void CloseSetting()
    {
        _timer.onPaused = false;
        
        _settings.SetActive(false);
        _settingsBackground.SetActive(false);

        _mainHUD.SetActive(true);

        PlayerPrefs.SetFloat("settingSensitivity", _sensitivityData);
        PlayerPrefs.SetFloat("settingFOV", _fovData);
    }

    public void SetSensitivitySlider()
    {
        _sensitivityData = (float)System.Math.Round(_sliderSensitivity.value, 1);
        _textSensitivity.text = _sensitivityData.ToString();

        _sensivity.magnitudeMultiplier = _sensitivityData;
    }
    public void SetSensitivityInput()
    {
        if ((float)System.Math.Round(float.Parse(_textSensitivity.text), 1) < _sliderSensitivity.minValue)
            _sensitivityData = _sliderSensitivity.minValue;
        else if ((float)System.Math.Round(float.Parse(_textSensitivity.text), 1) > _sliderSensitivity.maxValue)
            _sensitivityData = _sliderSensitivity.maxValue;
        else
            _sensitivityData = (float)System.Math.Round(float.Parse(_textSensitivity.text), 1);

        _sliderSensitivity.value = _sensitivityData;

        _sensivity.magnitudeMultiplier = _sensitivityData;

    }
    public void SetFOVSlider()
    {
        _fovData = (float)System.Math.Round(_sliderFOV.value, 1);
        _textFOV.text = _fovData.ToString();

        _camera.m_Lens.FieldOfView = _fovData;
    }
    public void SetFOVInput()
    {
        if ((float)System.Math.Round(float.Parse(_textFOV.text), 1) < _sliderFOV.minValue)
            _fovData = _sliderFOV.minValue;
        else if ((float)System.Math.Round(float.Parse(_textFOV.text), 1) > _sliderFOV.maxValue)
            _fovData = _sliderFOV.maxValue;
        else
            _fovData = (float)System.Math.Round(float.Parse(_textFOV.text), 1);

        _sliderFOV.value = _fovData;

        _camera.m_Lens.FieldOfView = _fovData;
    }
}
