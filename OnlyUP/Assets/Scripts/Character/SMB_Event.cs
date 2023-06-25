using UnityEngine;
using System.Collections.Generic;

public class SMB_Event : StateMachineBehaviour
{
    public enum SMBTiming
    {
        OnEnter,
        OnExit,
        OnUpdate,
        OnEnd
    }

    [System.Serializable]
    public class SMBEvent
    {
        public bool fired;
        public string eventName;
        public SMBTiming timing;
        public float onUpdateFrame;
    }

    [SerializeField] private int m_totalFrames;
    [SerializeField] private int m_currentFrame;
    [SerializeField] private float m_normilizedTime;
    [SerializeField] private float m_normilizedTimeUncapped;
    [SerializeField] private string m_motionTime = "";

    public List<SMBEvent> Events = new List<SMBEvent>();

    private bool m_hasParam;
    private SMBEventHandler m_eventHandler;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        m_hasParam = HasParameter(animator, m_motionTime);
        m_eventHandler = animator.GetComponent<SMBEventHandler>();
        m_totalFrames = GetTotalFrames(animator, layerIndex);

        m_normilizedTimeUncapped = stateInfo.normalizedTime;
        m_normilizedTime = m_hasParam ? animator.GetFloat(m_motionTime) : GetNormalizedTime(stateInfo);
        m_currentFrame = GetCurrentFrame(m_totalFrames, m_normilizedTime);

        if (m_eventHandler != null)
        {
            foreach (SMBEvent _smbEvent in Events)
            {
                _smbEvent.fired = false;
                if (_smbEvent.timing == SMBTiming.OnEnter)
                {
                    _smbEvent.fired = true;
                    m_eventHandler.Event.Invoke(_smbEvent.eventName);
                }
            }
        }
    }

    public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        m_normilizedTimeUncapped = stateInfo.normalizedTime;
        m_normilizedTime = m_hasParam ? animator.GetFloat(m_motionTime) : GetNormalizedTime(stateInfo);
        m_currentFrame = GetCurrentFrame(m_totalFrames, m_normilizedTime);


        if (m_eventHandler != null)
        {
            foreach (SMBEvent _smbEvent in Events)
            {
                if (!_smbEvent.fired)
                {
                    if (m_currentFrame >= _smbEvent.onUpdateFrame)
                    {
                        _smbEvent.fired = true;
                        m_eventHandler.Event.Invoke(_smbEvent.eventName);
                    }
                }
                else if (_smbEvent.timing == SMBTiming.OnEnd)
                {
                    if (m_currentFrame >= m_totalFrames)
                    {
                        _smbEvent.fired = true;
                        m_eventHandler.Event.Invoke(_smbEvent.eventName);
                    }
                }
            }
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (m_eventHandler != null)
        {
            foreach (SMBEvent _smbEvent in Events)
            {
                if (_smbEvent.timing == SMBTiming.OnExit)
                {
                    _smbEvent.fired = true;
                    m_eventHandler.Event.Invoke(_smbEvent.eventName);
                }
            }
        }
    }

    private bool HasParameter(Animator animator, string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName) || string.IsNullOrWhiteSpace(parameterName))
        {
            return false;
        }
        foreach (AnimatorControllerParameter parameter in animator.parameters)
            if (parameter.name == parameterName) return true;

        return false;
    }

    private int GetTotalFrames(Animator animator, int layerIndex)
    {
        AnimatorClipInfo[] _clipInfos = animator.GetNextAnimatorClipInfo(layerIndex);
        if (_clipInfos.Length == 0)
            _clipInfos = animator.GetCurrentAnimatorClipInfo(layerIndex);

        AnimationClip _clip = _clipInfos[0].clip;
        return Mathf.RoundToInt(_clip.length / _clip.frameRate);
    }

    private float GetNormalizedTime(AnimatorStateInfo stateInfo)
    {
        return stateInfo.normalizedTime > 1 ? 1 : stateInfo.normalizedTime;
    }

    private int GetCurrentFrame(int totalFrames, float normalizedTime)
    {
        return Mathf.RoundToInt(totalFrames * normalizedTime);
    }
}
