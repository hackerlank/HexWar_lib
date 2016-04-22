using System.Collections.Generic;
using HexWar;


public class BattlePublicTools2
{
    public static List<Hero2> GetAttackTargetHeroList(Dictionary<int, int[]> _neighbourPosMap, Dictionary<int, Hero2> _heroDic, Hero2 _hero)
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

                            if (hero.isMine != _hero.isMine)
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

