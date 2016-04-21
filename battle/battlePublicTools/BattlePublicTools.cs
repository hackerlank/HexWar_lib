using System.Collections.Generic;
using HexWar;


public class BattlePublicTools
{
    public static List<Hero> GetAttackTargetHeroList(Dictionary<int, int[]> _neighbourPosMap, Dictionary<int, Hero> _heroDic, Hero _hero)
    {
        List<Hero> result = new List<Hero>();

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
                            Hero hero = _heroDic[pos];

                            if(hero.isMine != _hero.isMine)
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
}

