using UnityEngine;


public class HomingComponent : MonoBehaviour, IHoming
{
    private readonly float homingStrength;
    private readonly float homingRange;
    private readonly string enemyTag = "Enemy";
    private Enemy targetEnemy;
    private Vector2 direction;
    private readonly Transform projectileTransform;

    public HomingComponent(float homingStrength, float homingRange, Transform transform)
    {
        this.homingStrength = homingStrength;
        this.homingRange = homingRange;
        this.projectileTransform = transform;
    }

    public void SetTarget(Enemy target)
    {
        targetEnemy = target;
    }

    public void UpdateHoming()
    {
        if (targetEnemy == null || targetEnemy.IsDead())
            targetEnemy = FindClosestEnemy();

        if (targetEnemy != null)
        {
            Vector2 targetDirection = ((Vector2)targetEnemy.transform.position - (Vector2)projectileTransform.position).normalized;
            direction = Vector2.Lerp(direction, targetDirection, homingStrength * Time.deltaTime).normalized;
        }
    }

    private Enemy FindClosestEnemy()
    {
        Collider2D[] nearbyColliders = Physics2D.OverlapCircleAll(projectileTransform.position, homingRange);
        Enemy closestEnemy = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider2D col in nearbyColliders)
        {
            if (col.CompareTag(enemyTag))
            {
                Enemy enemy = col.GetComponent<Enemy>();
                if (enemy != null && !enemy.IsDead())
                {
                    float distance = Vector2.Distance(projectileTransform.position, enemy.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestEnemy = enemy;
                    }
                }
            }
        }

        return closestEnemy;
    }

    public Vector2 GetDirection() => direction;
}

