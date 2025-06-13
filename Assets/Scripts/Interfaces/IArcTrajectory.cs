using UnityEngine;

public interface IArcTrajectory
{
    void SetupArc(Vector3 start, Vector3 target, float height, float speed);
    void UpdateArcMovement();
}
