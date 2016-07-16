using System.Collections.Generic;
using System;

namespace HexWar
{
    public class Hero2
    {
        private Battle2 battle;

        public bool isMine;

        public int id;

        public IHeroSDS sds;

        public int pos;
        public int nowHp;
        public int nowPower;
        

        //public bool isMoved;
        public bool isSummon;
        public bool isSilent;
        public int maxHp;
        public int damage;

        public int changeDamage;
        public int changeMaxHp;
        public int changeHp;
        public int changePower;
        public bool changeIsSilent;

        private List<int> eventIndexList;

        internal int uid;

        public Hero2(Battle2 _battle, int _uid, bool _isMine, int _id, IHeroSDS _sds, int _pos)
        {
            battle = _battle;

            uid = _uid;

            isMine = _isMine;
            id = _id;
            sds = _sds;
            pos = _pos;
            maxHp = nowHp = sds.GetHp();
            damage = sds.GetDamage();
            nowPower = sds.GetPower();

            //isMoved = false;

            isSummon = !sds.GetHeroTypeSDS().GetCanCharge();

            eventIndexList = new List<int>();

            for (int i = 0; i < sds.GetSkills().Length; i++)
            {
                ISkillSDS skillSDS = Battle2.skillDataDic[sds.GetSkills()[i]];

                int index = i;

                Action<SuperEvent> del = delegate (SuperEvent e)
                {
                    CastSkill(index,e);
                };

                switch (skillSDS.GetTrigger())
                {
                    case SkillTrigger.ALL:

                        int eventIndex = battle.superEventListener.AddListener(skillSDS.GetEventName().ToString(), del);

                        eventIndexList.Add(eventIndex);

                        break;

                    case SkillTrigger.HERO:

                        eventIndex = battle.superEventListener.AddListener(string.Format("{0}{1}", skillSDS.GetEventName(), uid), del);

                        eventIndexList.Add(eventIndex);

                        break;

                    case SkillTrigger.ALLY:

                        eventIndex = battle.superEventListener.AddListener(string.Format("{0}{1}", skillSDS.GetEventName(), isMine), del);

                        eventIndexList.Add(eventIndex);

                        break;

                    case SkillTrigger.ENEMY:

                        eventIndex = battle.superEventListener.AddListener(string.Format("{0}{1}", skillSDS.GetEventName(), !isMine), del);

                        eventIndexList.Add(eventIndex);

                        break;
                }
            }
        }

        private void CastSkill(int _index,SuperEvent e)
        {
            if (isSilent)
            {
                return;
            }

            List<Hero2> targets = null;

            ISkillSDS skillSDS = Battle2.skillDataDic[sds.GetSkills()[_index]];

            switch (skillSDS.GetTargetType())
            {
                case SkillTargetType.SELF:

                    targets = new List<Hero2>() { this };

                    break;

                case SkillTargetType.ALLY:

                    targets = BattlePublicTools2.GetTargetHeroList(battle.mapData.neighbourPosMap, battle.heroMapDic, this, TargetType.ALLY);

                    break;

                case SkillTargetType.ENEMY:

                    targets = BattlePublicTools2.GetTargetHeroList(battle.mapData.neighbourPosMap, battle.heroMapDic, this, TargetType.ENEMY);

                    break;

                case SkillTargetType.ALL:

                    targets = BattlePublicTools2.GetTargetHeroList(battle.mapData.neighbourPosMap, battle.heroMapDic, this, TargetType.ALL);

                    break;

                case SkillTargetType.TRIGGER:

                    Hero2 tmpHero = e.datas[0] as Hero2;

                    if(tmpHero == this)
                    {
                        return;
                    }

                    targets = new List<Hero2>() { tmpHero };

                    break;
            }

            if(targets.Count == 0)
            {
                return;
            }

            switch (skillSDS.GetConditionType())
            {
                case SkillConditionType.DISTANCE_SMALLER:

                    int dis = BattlePublicTools2.GetHerosDistance(battle.mapData.neighbourPosMap, pos, targets[0].pos);

                    if (dis == 0 || dis >= skillSDS.GetConditionData())
                    {
                        return;
                    }

                    break;

                case SkillConditionType.HP_BIGGER:

                    if(nowHp <= skillSDS.GetConditionData())
                    {
                        return;
                    }

                    break;

                case SkillConditionType.HP_SMALLER:

                    if(nowHp >= skillSDS.GetConditionData())
                    {
                        return;
                    }

                    break;

                case SkillConditionType.POWER_BIGGER:

                    if(nowPower <= skillSDS.GetConditionData())
                    {
                        return;
                    }

                    break;
            }

            while (targets.Count > skillSDS.GetTargetNum())
            {
                int index = (int)(battle.random.NextDouble() * targets.Count);

                targets.RemoveAt(index);
            }

            switch (skillSDS.GetEffectType())
            {
                case SkillEffectType.SILENT:

                    for(int i = 0; i < targets.Count; i++)
                    {
                        targets[i].ChangeSilent();
                    }

                    break;

                case SkillEffectType.MAX_HP_CHANGE:

                    for (int i = 0; i < targets.Count; i++)
                    {
                        targets[i].ChangeMaxHp(skillSDS.GetEffectData()[0]);
                    }

                    break;

                case SkillEffectType.DAMAGE_CHANGE:

                    for (int i = 0; i < targets.Count; i++)
                    {
                        targets[i].ChangeDamage(skillSDS.GetEffectData()[0]);
                    }

                    break;

                case SkillEffectType.POWER_CHANGE:

                    for (int i = 0; i < targets.Count; i++)
                    {
                        targets[i].ChangePower(skillSDS.GetEffectData()[0]);
                    }

                    break;

                case SkillEffectType.DO_DAMAGE:

                    for (int i = 0; i < targets.Count; i++)
                    {
                        targets[i].ChangeHp(skillSDS.GetEffectData()[0]);
                    }

                    break;

                case SkillEffectType.DO_DAMAGE_FIX:

                    for (int i = 0; i < targets.Count; i++)
                    {
                        int doDamage = skillSDS.GetEffectData()[0] + targets[i].sds.GetDamage() * skillSDS.GetEffectData()[1];

                        if(doDamage < 0)
                        {
                            targets[i].ChangeHp(doDamage);
                        }
                    }

                    break;

                case SkillEffectType.RECOVER_HP:

                    for (int i = 0; i < targets.Count; i++)
                    {
                        targets[i].ChangeHp(skillSDS.GetEffectData()[0]);
                    }

                    break;
            }

        }

        internal void ChangeSilent()
        {
            changeIsSilent = true;
        }

        internal void ChangeDamage(int _data)
        {
            changeDamage += _data;
        }

        internal void ChangeMaxHp(int _data)
        {
            changeMaxHp += _data;
        }

        internal void ChangePower(int _data)
        {
            changePower += _data;
        }

        internal void ChangeHp(int _data)
        {
            changeHp += _data;
        }

        public Hero2(bool _isMine, int _id, IHeroSDS _sds, int _pos, int _nowHp, int _nowPower, bool _isSummon)
        {
            isMine = _isMine;
            id = _id;
            sds = _sds;
            pos = _pos;
            nowHp = _nowHp;
            nowPower = _nowPower;

            //isMoved = _isMoved;
            isSummon = _isSummon;
        }

        internal void RefreshData()
        {
            if (!isSilent && changeIsSilent)
            {
                isSilent = true;

                changeIsSilent = false;
            }

            if(changeDamage != 0)
            {
                damage += changeDamage;

                changeDamage = 0;
            }

            if(changeMaxHp != 0)
            {
                nowHp += changeMaxHp;

                maxHp += changeMaxHp;

                changeMaxHp = 0;
            }

            if(changeHp != 0)
            {
                nowHp += changeHp;

                changeHp = 0;

                if (nowHp < 0)
                {
                    nowHp = 0;
                }
                else if(nowHp > maxHp)
                {
                    nowHp = maxHp;
                }
            }

            if(changePower != 0)
            {
                nowPower += changePower;

                changePower = 0;

                if(nowPower < 0)
                {
                    nowPower = 0;
                }
                else if(nowPower > sds.GetPower())
                {
                    nowPower = sds.GetPower();
                }
            }
        }

        internal void RefreshRoundOver()
        {
            if(maxHp > sds.GetHp())
            {
                nowHp -= maxHp - sds.GetHp();

                if(nowHp < 1)
                {
                    nowHp = 1;
                }

                maxHp = sds.GetHp();
            }

            if(damage != sds.GetDamage())
            {
                damage = sds.GetDamage();
            }

            if (isSilent)
            {
                isSilent = false;
            }
        }

        internal void Die()
        {
            for(int i = 0; i < eventIndexList.Count; i++)
            {
                battle.superEventListener.RemoveListener(eventIndexList[i]);
            }
        }
    }
}
