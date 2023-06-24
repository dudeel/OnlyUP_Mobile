using UnityEngine;

public class JoystickMovement : JoystickHandler
{
    [SerializeField] private Movement _movement;
    private void Update() 
    {
       if (inputVector.x >= range || inputVector.x <= -range || inputVector.y >= range || inputVector.y <= -range)
        {
            _movement.Move(new Vector3(inputVector.x, 0, inputVector.y));
            //_movement.Rotation(new Vector3(inputVector.x, 0, inputVector.y));
        }
        else
        {
            //_movement.Move(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")));
            //_movement.Rotation(new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")));
        } 
    }
}
