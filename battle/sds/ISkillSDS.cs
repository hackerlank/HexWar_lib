public enum SkillEventName
{
    ATTACK1,
    ATTACK2,
    ATTACK3,
    ATTACKOVER,
    DIE
}

public enum SkillTrigger
{
    ALL,
    HERO,
    ALLY,
    ENEMY
}

public enum SkillConditionType
{
    NULL,
    DISTANCE_SMALLER,
    HP_BIGGER,
    HP_SMALLER,
    POWER_BIGGER
}

public enum SkillTargetType
{
    SELF,
    ALLY,
    ENEMY,
    ALL,
    TRIGGER
}

public enum SkillEffectType
{
    SILENT,
    DAMAGE_CHANGE,
    MAX_HP_CHANGE,
    POWER_CHANGE,
    DO_DAMAGE,
    DO_DAMAGE_FIX,
    RECOVER_HP
}

public interface ISkillSDS
{
    SkillEventName GetEventName();
    SkillTrigger GetTrigger();
    SkillConditionType GetConditionType();
    int GetConditionData();
    SkillTargetType GetTargetType();
    int GetTargetNum();
    SkillEffectType GetEffectType();
    int[] GetEffectData();
}

