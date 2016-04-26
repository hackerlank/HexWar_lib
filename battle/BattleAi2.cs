using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HexWar
{
    class BattleAi2
    {
        public static void Action(Battle2 _battle)
        {
            Random random = new Random();

            bool canMove;

            bool canSummon = false;

            Dictionary<Hero2, List<int>> myHeros = new Dictionary<Hero2, List<int>>();

            Dictionary<int, Hero2>.ValueCollection.Enumerator enumerator = _battle.heroMapDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Hero2 hero = enumerator.Current;

                if(!hero.isMine && !hero.isMoved && !hero.isSummon)
                {
                    List<int> tmpList = new List<int>();

                    int[] tmpArr = _battle.mapData.neighbourPosMap[hero.pos];

                    for (int i = 0; i < 6; i++)
                    {
                        if(tmpArr[i] != -1)
                        {
                            if (!_battle.heroMapDic.ContainsKey(tmpArr[i]))
                            {
                                tmpList.Add(i);
                            }
                        }
                    }

                    if(tmpList.Count > 0)
                    {
                        myHeros.Add(hero,tmpList);
                    }
                }
            }

            canMove = myHeros.Count > 0;

            List<int> summonPosList = new List<int>();

            Dictionary<int, IHeroSDS> myCards = new Dictionary<int, IHeroSDS>();

            Dictionary<int, bool>.Enumerator enumerator3 = _battle.mapDic.GetEnumerator();

            while (enumerator3.MoveNext())
            {
                if (!enumerator3.Current.Value)
                {
                    int pos = enumerator3.Current.Key;

                    if (!_battle.heroMapDic.ContainsKey(pos))
                    {
                        summonPosList.Add(pos);
                    }
                }
            }

            if (summonPosList.Count > 0)
            {
                Dictionary<int, int>.Enumerator enumerator2 = _battle.oHandCards.GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    int uid = enumerator2.Current.Key;

                    int id = enumerator2.Current.Value;

                    IHeroSDS hero = Battle2.heroDataDic[id];

                    if (hero.GetCost() <= _battle.oMoney)
                    {
                        myCards.Add(uid, hero);
                    }
                }

                canSummon = myCards.Count > 0;
            }

            if(canSummon && canMove)
            {
                double r = random.NextDouble();

                if (r < 1 / 3)
                {
                    DoSummon(_battle, random, summonPosList, myCards);
                }
                else if( r < 2 * 3)
                {
                    DoMove(_battle, random, myHeros);
                }
                else
                {
                    _battle.ServerDoSkip();
                }
            }
            else if (canSummon)
            {
                double r = random.NextDouble();

                if(r < 0.5)
                {
                    DoSummon(_battle, random, summonPosList, myCards);
                }
                else
                {
                    _battle.ServerDoSkip();
                }
            }
            else if (canMove)
            {
                double r = random.NextDouble();

                if (r < 0.5)
                {
                    DoMove(_battle, random, myHeros);
                }
                else
                {
                    _battle.ServerDoSkip();
                }
            }
            else
            {
                _battle.ServerDoSkip();
            }
        }

        private static void DoSummon(Battle2 _battle, Random _random, List<int> _summonPosList, Dictionary<int, IHeroSDS> _myCards)
        {
            int cardIndex = (int)(_random.NextDouble() * _myCards.Count);

            Dictionary<int, IHeroSDS>.Enumerator enumerator = _myCards.GetEnumerator();

            for(int i = 0; i <= cardIndex; i++)
            {
                enumerator.MoveNext();
            }

            int cardUid = enumerator.Current.Key;

            int posIndex = (int)(_random.NextDouble() * _summonPosList.Count);

            int pos = _summonPosList[posIndex];

            _battle.ServerDoSummon(cardUid, pos);
        }

        private static void DoMove(Battle2 _battle, Random _random, Dictionary<Hero2, List<int>> _myHeros)
        {
            int index = (int)(_random.NextDouble() * _myHeros.Count);

            Dictionary<Hero2, List<int>>.Enumerator enumerator = _myHeros.GetEnumerator();

            for (int i = 0; i <= index; i++)
            {
                enumerator.MoveNext();
            }

            Hero2 hero = enumerator.Current.Key;

            List<int> directions = enumerator.Current.Value;

            index = (int)(_random.NextDouble() * directions.Count);

            int direction = directions[index];

            _battle.ServerDoMove(hero.pos, direction);
        }
    }
}
