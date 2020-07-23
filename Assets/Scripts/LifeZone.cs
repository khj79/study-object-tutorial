using UnityEngine;

public class LifeZone : GameLevelObject
{
    [SerializeField] private float dyingDuration = 0;

    private void OnTriggerExit(Collider other)
    {
        var shape = other.GetComponent<Shape>();

        if (shape)
        {
            if (dyingDuration <= 0f)
            {
                shape.Die();
            }
            else if (!shape.IsMarkedAsDying)
            {
                shape.AddBehavior<DyingShapeBehavior>().Initialize(shape, dyingDuration);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        var collider = GetComponent<Collider>();
        
        var box = collider as BoxCollider;

        if (box != null)
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.DrawWireCube(box.center, box.size);
            return;
        }

        var sphere = collider as SphereCollider;

        if (sphere != null)
        {
            Vector3 scale = transform.lossyScale;
            scale = Vector3.one * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, scale);
            Gizmos.DrawWireSphere(sphere.center, sphere.radius);
            return;
        }
    }
}
