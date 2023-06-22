using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class MobileDisableAutoSwitchControls : MonoBehaviour
{
    
#if ENABLE_INPUT_SYSTEM && (UNITY_IOS || UNITY_ANDROID)

    [Header("Target")]
    public PlayerInput playerInput;

    void Start()
    {
        DisableAutoSwitchControls();
    }

    void DisableAutoSwitchControls()
    {
        playerInput.neverAutoSwitchControlSchemes = true;
    }

#endif
    
}
