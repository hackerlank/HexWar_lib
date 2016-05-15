public enum SkillEventName
{
    ATTACK1,
    ATTACK2,
    ATTACK3,
    ATTACKOVER,
    DIE
}

public enum SkillAddType
{
    NULL,
    UID,
    ISMINE
}

public enum SkillTargetType
{
    SELF,
    ALLY,
    ENEMY,
    ALL
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
    SkillAddType GetAddType();
    SkillTargetType GetTargetType();
    int GetTargetNum();
    SkillEffectType GetEffectType();
    int[] GetEffectData();
}

