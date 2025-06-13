using UnityEngine;

public static class ComponentExtensions
{
    public static ExplosionComponent Initialize(this ExplosionComponent component, float radius, GameObject explosionEffectPrefab)
    {
        component.GetType().GetField("radius", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(component, radius);
        component.GetType().GetField("explosionEffectPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(component, explosionEffectPrefab);
        return component;
    }

    public static ArcTrajectoryComponent Initialize(this ArcTrajectoryComponent component, Transform transform, Rigidbody2D rb)
    {
        component.GetType().GetField("projectileTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(component, transform);
        component.GetType().GetField("rb", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(component, rb);
        return component;
    }

    public static HomingComponent Initialize(this HomingComponent component, float homingStrength, float homingRange, Transform transform)
    {
        component.GetType().GetField("homingStrength", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(component, homingStrength);
        component.GetType().GetField("homingRange", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(component, homingRange);
        component.GetType().GetField("projectileTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(component, transform);
        return component;
    }
}