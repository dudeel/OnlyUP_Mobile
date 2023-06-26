using UnityEngine;

public class RespawnSystem : MonoBehaviour
{
    [SerializeField] private GameObject _character;
    [SerializeField] private Transform _spawnPosition;
    [SerializeField] private ParkourSystem _parkour;
    [SerializeField] private CharacterController _controller;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F)) Respawn();
    }
    public void Respawn()
    {
        Debug.Log("respawn");
        _parkour.IsActionState = true;
        _controller.enabled = false;
        _character.transform.position = new Vector3(_spawnPosition.position.x, _spawnPosition.position.y, _spawnPosition.position.z);
        _parkour.IsActionState = false;
        _controller.enabled = true;
    }
}
