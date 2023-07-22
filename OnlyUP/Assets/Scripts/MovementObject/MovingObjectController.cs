using UnityEngine;

public class MovingObjectController : MonoBehaviour
{
    [SerializeField] private GameObject _object;
    [SerializeField] private WaypointPath _path;

    private void Update() {
        if (transform.childCount < 2)
        {
            GameObject spawned = Instantiate(_object, transform.position, transform.rotation, transform);
            spawned.GetComponent<MovingObject>()._waypointPath = _path;
        }
    }
}
