[System.Serializable]
public class Debuff
{
    public DebuffType type;
    public float duration;
    public float intensity;
    public float tickInterval = 1f;
    public float timeRemaining;

    public Debuff(DebuffType debuffType, float debuffDuration, float debuffIntensity, float interval = 1f)
    {
        type = debuffType;
        duration = debuffDuration;
        intensity = debuffIntensity;
        tickInterval = interval;
        timeRemaining = duration;
    }
}

public enum DebuffType
{
    Bleed,
    Slow,
    Poison,
    Stun,
    Freeze,
    None
}