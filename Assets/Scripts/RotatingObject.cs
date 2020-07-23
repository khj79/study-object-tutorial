using UnityEngine;

public class RotatingObject : GameLevelObject
{
    [SerializeField] private Vector3 angularVelocity = Vector3.zero;

    public override void GameUpdate()
    {
        transform.Rotate(angularVelocity * Time.deltaTime);
    }
}
