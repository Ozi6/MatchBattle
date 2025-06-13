using UnityEngine;

public class DebuffComponent : IDebuffable
{
    private readonly DebuffType debuffType;
    private readonly float duration;
    private readonly float intensity;

    public DebuffComponent(DebuffType debuffType, float duration, float intensity)
    {
        this.debuffType = debuffType;
        this.duration = duration;
        this.intensity = intensity;
    }

    public void ApplyDebuff(Enemy enemy)
    {
        if (duration > 0f)
        {
            Debuff debuff = new Debuff(debuffType, duration, intensity);
            enemy.ApplyDebuff(debuff);
        }
    }
}

