using System.Collections.Generic;
using HexWar;

public enum TargetType
{
    ALLY,
    ENEMY,
    ALL
}

public class BattlePublicTools2
{
    public static List<Hero2> GetTargetHeroList(Dictionary<int, int[]> _neighbourPosMap, Dictionary<int, Hero2> _heroDic, Hero2 _hero, TargetType _targetType)
    {
        List<Hero2> result = new List<Hero2>();

        for (int i = 0; i < 6; i++)
        {
            int nowPos = _hero.pos;

            for (int m = 1; m <= _hero.sds.GetHeroTypeSDS().GetMaxRange(); m++)
            {
                int pos = _neighbourPosMap[nowPos][i];

                if (pos == -1)
                {
                    break;
                }
                else
                {
                    if (m < _hero.sds.GetHeroTypeSDS().GetMinRange())
                    {
                        nowPos = pos;
                    }
                    else
                    {
                        if (_heroDic.ContainsKey(pos))
                        {
                            Hero2 hero = _heroDic[pos];

                            if(_targetType == TargetType.ALL)
                            {
                                result.Add(_heroDic[pos]);
                            }
                            else if (_targetType == TargetType.ENEMY && hero.isMine != _hero.isMine)
                            {
                                result.Add(_heroDic[pos]);
                            }
                            else if (_targetType == TargetType.ALLY && hero.isMine == _hero.isMine)
                            {
                                result.Add(_heroDic[pos]);
                            }

                            break;
                        }
                        else
                        {
                            nowPos = pos;
                        }
                    }
                }
            }
        }

        return result;
    }

    public static List<Hero2> GetAttackTargetHeroList(Dictionary<int, int[]> _neighbourPosMap, Dictionary<int, Hero2> _heroDic, Hero2 _hero)
    {
        return GetTargetHeroList(_neighbourPosMap, _heroDic, _hero, TargetType.ENEMY);
    }
}

