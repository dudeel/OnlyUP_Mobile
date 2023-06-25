using UnityEngine;

public class ParkourSystem : MonoBehaviour
{
    [SerializeField] Animator _animator;
    [SerializeField] CharacterController _controller;
    [SerializeField] private StarterAssetsInputs _input;
    [SerializeField] private bool _isClimbObject;
    [SerializeField] private ThirdPersonController _movement;
    public bool IsActionState;
    [SerializeField] Vector3 _objectPosition;
    
    private void OnTriggerStay(Collider other)
    {
        if (other.transform.gameObject.transform.tag == "ClimbTrigger")
        {
            _isClimbObject = true;
            _objectPosition = other.transform.position;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.gameObject.transform.tag =="ClimbTrigger")
        {
            _isClimbObject = false;
        }
    }

    private void Update()
    {
        if (!IsActionState && _isClimbObject && (_input.jump || !_controller.isGrounded))
        {
            _animator.CrossFade("Climb Up", 0f);

            IsActionState = true;
            _animator.applyRootMotion = true;
            _controller.enabled = false;
        }


        if (_animator.IsInTransition(0))
        {
            return;
        }

        if (_animator.GetCurrentAnimatorStateInfo(0).IsName("Climb Up"))
        {
            _animator.MatchTarget(_objectPosition, transform.rotation, AvatarTarget.RightFoot, new MatchTargetWeightMask(new Vector3(0, 1, 1), 0), 0.14f, 0.33f);
        }

        if (_isClimbObject && _animator.GetCurrentAnimatorStateInfo(0).IsName("Idle Walk Run Blend"))
        {
            _input.jump = false;
            IsActionState = false;
            _animator.applyRootMotion = false;
            _isClimbObject = false;
            _controller.enabled = true;
        }
    }
}
