using UnityEngine;

public class ArcTrajectoryComponent : MonoBehaviour, IArcTrajectory
{
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private float arcHeight;
    private float arcSpeed;
    private float totalDistance;
    private float arcProgress;
    private readonly Transform projectileTransform;
    private readonly Rigidbody2D rb;

    public ArcTrajectoryComponent(Transform transform, Rigidbody2D rb)
    {
        this.projectileTransform = transform;
        this.rb = rb;
    }

    public void SetupArc(Vector3 start, Vector3 target, float height, float speed)
    {
        startPosition = start;
        targetPosition = target;
        arcHeight = height;
        arcSpeed = speed;
        totalDistance = Vector3.Distance(startPosition, targetPosition);
        arcProgress = 0f;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        Debug.Log($"Arc trajectory setup: Start={startPosition}, Target={targetPosition}, Distance={totalDistance}");
    }

    public void UpdateArcMovement()
    {
        arcProgress += (arcSpeed / totalDistance) * Time.deltaTime;

        Vector3 currentPos = Vector3.Lerp(startPosition, targetPosition, arcProgress);
        float arcHeightMultiplier = 4f * arcProgress * (1f - arcProgress);
        currentPos.y += arcHeight * arcHeightMultiplier;

        projectileTransform.position = currentPos;

        if (arcProgress > 0.01f)
        {
            Vector3 lastPos = Vector3.Lerp(startPosition, targetPosition, arcProgress - 0.01f);
            lastPos.y += arcHeight * 4f * (arcProgress - 0.01f) * (1f - (arcProgress - 0.01f));

            Vector3 moveDirection = (currentPos - lastPos).normalized;
            if (moveDirection != Vector3.zero)
            {
                float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg;
                projectileTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }
    }

    public bool IsArcComplete() => arcProgress >= 0.85f;
}

