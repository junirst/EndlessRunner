using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmoothCameraFollow : MonoBehaviour
{
    [SerializeField] private Vector3 offset;
    [SerializeField] private float damping;

    public Transform target;

    private Vector3 vel = Vector3.zero;
    private Bounds movementBounds;
    private bool hasMovementBounds;

    public void SetMovementBounds(Bounds bounds)
    {
        movementBounds = bounds;
        hasMovementBounds = true;
        transform.position = ClampToMovementBounds(transform.position);
    }

    private void FixedUpdate()
    {   
        if (target == null) return;
    
        Vector3 targetPosition = target.position + offset;
        targetPosition.z = transform.position.z; 

        Vector3 smoothedPosition = Vector3.SmoothDamp(transform.position, targetPosition, ref vel, damping);
        transform.position = ClampToMovementBounds(smoothedPosition);

        if (hasMovementBounds)
        {
            GetCameraLimits(out float minimumX, out float maximumX, out float minimumY, out float maximumY);

            if (transform.position.x == minimumX || transform.position.x == maximumX)
            {
                vel.x = 0f;
            }

            if (transform.position.y == minimumY || transform.position.y == maximumY)
            {
                vel.y = 0f;
            }
        }
    }

    private Vector3 ClampToMovementBounds(Vector3 position)
    {
        if (!hasMovementBounds)
        {
            return position;
        }

        Camera camera = GetComponent<Camera>();
        if (!camera || !camera.orthographic)
        {
            return position;
        }

        GetCameraLimits(out float minimumX, out float maximumX, out float minimumY, out float maximumY);

        position.x = ClampAxis(position.x, minimumX, maximumX);
        position.y = ClampAxis(position.y, minimumY, maximumY);
        return position;
    }

    private void GetCameraLimits(out float minimumX, out float maximumX, out float minimumY, out float maximumY)
    {
        Camera camera = GetComponent<Camera>();
        float verticalExtent = camera.orthographicSize;
        float horizontalExtent = verticalExtent * camera.aspect;

        minimumX = movementBounds.min.x + horizontalExtent;
        maximumX = movementBounds.max.x - horizontalExtent;
        minimumY = movementBounds.min.y + verticalExtent;
        maximumY = movementBounds.max.y - verticalExtent;
    }

    private float ClampAxis(float value, float minimum, float maximum)
    {
        if (minimum > maximum)
        {
            return (minimum + maximum) * 0.5f;
        }

        return Mathf.Clamp(value, minimum, maximum);
    }
}
