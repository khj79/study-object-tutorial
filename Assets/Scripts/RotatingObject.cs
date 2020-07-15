using UnityEngine;

public class RotatingObject : PersistableObject
{
    [SerializeField] private Vector3 angularVelocity = Vector3.zero;

    private void FixedUpdate()
    {
        transform.Rotate(angularVelocity * Time.deltaTime);
    }
}
