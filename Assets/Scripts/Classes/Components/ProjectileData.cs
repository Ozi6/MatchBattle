using UnityEngine;

[System.Serializable]
public class ProjectileData
{
    [Header("Basic Properties")]
    public float damage = 10f;
    public float speed = 5f;
    public float lifetime = 5f;

    [Header("Debuff Properties")]
    public DebuffType debuffType = DebuffType.None;
    public float debuffDuration = 0f;
    public float debuffIntensity = 0f;

    [Header("Special Properties")]
    public bool piercing = false;
    public int maxPierceCount = 1;
    public bool hasAreaEffect = false;
    public float areaRadius = 1f;

    [Header("Knockback Properties")]
    public bool hasKnockback = false;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.3f;

    [Header("Homing Properties")]
    public bool isHoming = true;
    public float homingStrength = 2f;
    public float homingRange = 3f;

    [Header("Trajectory Properties")]
    public bool usesArc = false;
    public float arcHeight = 2f;

    [Header("Explosion Properties")]
    public bool explodesOnContact = true;
    public float explosionTimer = 0f;

    public ProjectileData Clone()
    {
        return new ProjectileData
        {
            damage = this.damage,
            speed = this.speed,
            lifetime = this.lifetime,
            debuffType = this.debuffType,
            debuffDuration = this.debuffDuration,
            debuffIntensity = this.debuffIntensity,
            piercing = this.piercing,
            maxPierceCount = this.maxPierceCount,
            hasAreaEffect = this.hasAreaEffect,
            areaRadius = this.areaRadius,
            hasKnockback = this.hasKnockback,
            knockbackForce = this.knockbackForce,
            knockbackDuration = this.knockbackDuration,
            isHoming = this.isHoming,
            homingStrength = this.homingStrength,
            homingRange = this.homingRange,
            usesArc = this.usesArc,
            arcHeight = this.arcHeight,
            explodesOnContact = this.explodesOnContact,
            explosionTimer = this.explosionTimer
        };
    }
}