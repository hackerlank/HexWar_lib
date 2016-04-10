using System;
using System.Collections.Generic;
using System.IO;

namespace HexWar
{
    public class Battle
    {
        private static Dictionary<int, IHeroSDS> heroDataDic;
        private static Dictionary<int, MapData> mapDataDic;

        private const int MAX_POWER = 4;
        private const int DEFAULT_HAND_CARD_NUM = 5;
        private const int MAX_HAND_CARD_NUM = 10;
        private const int DEFAULT_MONEY = 5;
        private const int ADD_MONEY = 1;
        private const int MAX_MONEY = 10;

        public MapData mapData;

        public Dictionary<int, bool> mapDic;
        public Dictionary<int, bool> mapBelongDic;
        public Dictionary<int, Hero> heroDic;
        public Dictionary<int, Hero> heroMapDic;

        private List<int> mCards;
        private List<int> oCards;

        public Dictionary<int,int> mHandCards;
        public Dictionary<int,int> oHandCards;

        public int mScore;
        public int oScore;

        public int mMoney;
        public int oMoney;

        private int cardUid;
        private int heroUid;

        public bool mOver;
        public bool oOver;

        private Random random;

        public static void Init(Dictionary<int, IHeroSDS> _heroDataDic, Dictionary<int, MapData> _mapDataDic)
        {
            heroDataDic = _heroDataDic;
            mapDataDic = _mapDataDic;
        }

        public void ServerStart(int _mapID,List<int> _mCards,List<int> _oCards)
        {
            random = new Random();

            mapData = mapDataDic[_mapID];

            heroDic = new Dictionary<int, Hero>();

            heroMapDic = new Dictionary<int, Hero>();

            mapDic = new Dictionary<int, bool>();

            mapBelongDic = new Dictionary<int, bool>();

            Dictionary<int, bool>.Enumerator enumerator = mapData.dic.GetEnumerator();

            while (enumerator.MoveNext())
            {
                mapDic.Add(enumerator.Current.Key, enumerator.Current.Value);
            }

            mScore = mapData.score1;
            oScore = mapData.score2;

            mMoney = oMoney = DEFAULT_MONEY;

            cardUid = heroUid = 1;

            mCards = _mCards;
            oCards = _oCards;

            mHandCards = new Dictionary<int, int>();
            oHandCards = new Dictionary<int, int>();

            for (int i = 0; i < DEFAULT_HAND_CARD_NUM; i++)
            {
                int index = (int)(random.NextDouble() * mCards.Count);

                mHandCards.Add(cardUid, mCards[index]);

                mCards.RemoveAt(index);

                cardUid++;

                index = (int)(random.NextDouble() * oCards.Count);

                oHandCards.Add(cardUid, oCards[index]);

                oCards.RemoveAt(index);

                cardUid++;
            }

            mOver = oOver = false;
        }

        public void ServerRefreshData(BinaryWriter _bw,bool _isMine)
        {
            _bw.Write(_isMine);

            _bw.Write(mapData.id);

            Dictionary<int, bool>.Enumerator enumerator = mapDic.GetEnumerator();

            while (enumerator.MoveNext())
            {
                _bw.Write(enumerator.Current.Key);

                _bw.Write(enumerator.Current.Value);
            }

            _bw.Write(mapBelongDic.Count);

            Dictionary<int, bool>.KeyCollection.Enumerator enumerator2 = mapBelongDic.Keys.GetEnumerator();

            while (enumerator2.MoveNext())
            {
                _bw.Write(enumerator2.Current);
            }

            _bw.Write(heroDic.Count);

            Dictionary<int, Hero>.ValueCollection.Enumerator enumerator3 = heroDic.Values.GetEnumerator();

            while (enumerator3.MoveNext())
            {
                Hero hero = enumerator3.Current;

                _bw.Write(hero.uid);

                _bw.Write(hero.id);

                _bw.Write(hero.isMine);

                _bw.Write(hero.pos);

                _bw.Write(hero.nowHp);

                _bw.Write(hero.nowPower);

                _bw.Write(hero.canMove);
            }

            Dictionary<int, int> handCards = _isMine ? mHandCards : oHandCards;

            _bw.Write(handCards.Count);

            Dictionary<int, int>.Enumerator enumerator4 = handCards.GetEnumerator();

            while (enumerator4.MoveNext())
            {
                _bw.Write(enumerator4.Current.Key);

                _bw.Write(enumerator4.Current.Value);
            }

            if (_isMine)
            {
                _bw.Write(mMoney);

                _bw.Write(mOver);
            }
            else
            {
                _bw.Write(oMoney);

                _bw.Write(oOver);
            }
        }

        public void ClientRefreshData(BinaryReader _br,bool _isMine)
        {
            mapDic = new Dictionary<int, bool>();

            int mapID = _br.ReadInt32();

            mapData = mapDataDic[mapID];

            for(int i = 0; i < mapData.size; i++)
            {
                int pos = _br.ReadInt32();

                bool isMine = _br.ReadBoolean();

                mapDic.Add(pos, isMine);
            }

            mapBelongDic = new Dictionary<int, bool>();

            int num = _br.ReadInt32();

            for(int i = 0; i < num; i++)
            {
                int pos = _br.ReadInt32();

                mapBelongDic.Add(pos, true);
            }

            heroDic = new Dictionary<int, Hero>();

            heroMapDic = new Dictionary<int, Hero>();

            num = _br.ReadInt32();

            for(int i = 0; i < num; i++)
            {
                int uid = _br.ReadInt32();

                int id = _br.ReadInt32();

                bool isMine = _br.ReadBoolean();

                int pos = _br.ReadInt32();

                int nowHp = _br.ReadInt32();

                int nowPower = _br.ReadInt32();

                bool canMove = _br.ReadBoolean();

                Hero hero = new Hero(uid, isMine, id, heroDataDic[id], pos, nowHp, nowPower, canMove);

                heroDic.Add(uid, hero);

                heroMapDic.Add(pos, hero);
            }

            Dictionary<int, int> handCards;

            if (_isMine)
            {
                mHandCards = new Dictionary<int, int>();

                handCards = mHandCards;
            }
            else
            {
                oHandCards = new Dictionary<int, int>();

                handCards = oHandCards;
            }

            num = _br.ReadInt32();

            for(int i = 0; i < num; i++)
            {
                int uid = _br.ReadInt32();

                int id = _br.ReadInt32();

                handCards.Add(uid, id);
            }

            if (_isMine)
            {
                mMoney = _br.ReadInt32();

                mOver = _br.ReadBoolean();
            }
            else
            {
                oMoney = _br.ReadInt32();

                oOver = _br.ReadBoolean();
            }
        }

        public bool DoAction(bool _isMine, BinaryReader _br)
        {
            Dictionary<int, int> cards;

            if (_isMine)
            {
                if (mOver)
                {
                    return false;
                }
                else
                {
                    mOver = true;
                }

                cards = mHandCards;
            }
            else
            {
                if (oOver)
                {
                    return false;
                }
                else
                {
                    oOver = true;
                }

                cards = oHandCards;
            }

            int summonActionNum = _br.ReadInt32();

            for(int i = 0; i < summonActionNum; i++)
            {
                int uid = _br.ReadInt32();

                int pos = _br.ReadInt32();

                if (mapDic.ContainsKey(pos) && mapDic[pos] == _isMine && !mapBelongDic.ContainsKey(pos) && !heroMapDic.ContainsKey(pos) && cards.ContainsKey(uid))
                {
                    int heroID = cards[uid];

                    IHeroSDS sds = heroDataDic[heroID];

                    if (!sds.GetHeroTypeSDS().GetCanMove())
                    {
                        throw new Exception("a");
                    }

                    if (_isMine)
                    {
                        if (sds.GetCost() > mMoney)
                        {
                            throw new Exception("b");
                        }
                        else
                        {
                            mMoney -= sds.GetCost();
                        }
                    }
                    else
                    {
                        if (sds.GetCost() > oMoney)
                        {
                            throw new Exception("c");
                        }
                        else
                        {
                            oMoney -= sds.GetCost();
                        }
                    }

                    ServerAddHero(_isMine, heroID, sds, pos);
                }
                else
                {
                    throw new Exception("d");
                }
            }

            Dictionary<int, Hero> oldPosDic = new Dictionary<int, Hero>();
            Dictionary<int, Hero> newPosDic = new Dictionary<int, Hero>();

            int moveActionNum = _br.ReadInt32();

            for(int i = 0; i < moveActionNum; i++)
            {
                int uid = _br.ReadInt32();

                int direction = _br.ReadInt32();

                if (direction < 0 || direction > 6)
                {
                    throw new Exception("e");
                }

                if (heroDic.ContainsKey(uid))
                {
                    Hero hero = heroDic[uid];

                    if(hero.isMine == _isMine && hero.canMove)
                    {
                        int[] tmpArr = mapData.neighbourPosMap[hero.pos];

                        int pos = tmpArr[direction];

                        if(pos == -1)
                        {
                            throw new Exception("f");
                        }

                        if(mapDic[pos] == hero.isMine)
                        {
                            if (mapBelongDic.ContainsKey(pos))
                            {
                                throw new Exception("g");
                            }
                        }
                        else
                        {
                            if (!mapBelongDic.ContainsKey(pos))
                            {
                                throw new Exception("h");
                            }
                        }

                        oldPosDic.Add(hero.pos, hero);
                        newPosDic.Add(pos, hero);
                    }
                }
            }

            Dictionary<int, Hero>.KeyCollection.Enumerator enumerator = oldPosDic.Keys.GetEnumerator();

            while (enumerator.MoveNext())
            {
                heroMapDic.Remove(enumerator.Current);
            }

            Dictionary<int, Hero>.Enumerator enumerator2 = newPosDic.GetEnumerator();

            while (enumerator2.MoveNext())
            {
                int pos = enumerator2.Current.Key;

                Hero hero = enumerator2.Current.Value;

                if (heroMapDic.ContainsKey(pos))
                {
                    heroDic.Remove(hero.uid);
                }
                else
                {
                    heroMapDic.Add(pos, hero);

                    if(mapDic[pos] != hero.isMine)
                    {
                        mapDic[pos] = hero.isMine;

                        if (hero.isMine)
                        {
                            mScore++;
                            oScore--;
                        }
                        else
                        {
                            mScore--;
                            oScore++;
                        }
                    }

                    hero.nowPower--;
                }
            }

            if(mOver && oOver)
            {
                StartBattle();

                return true;
            }
            else
            {
                return false;
            }
        }

        private void ServerAddHero(bool _isMine,int _id,IHeroSDS _sds,int _pos)
        {
            Hero hero = new Hero(heroUid, _isMine, _id, _sds, _pos);

            heroDic.Add(hero.uid, hero);

            heroMapDic.Add(hero.pos, hero);

            heroUid++;
        }

        private void StartBattle()
        {
            DoAttack();

            HeroRecoverPower();

            HeroRecoverCanMove();

            ResetMapBelong();

            RecoverCards();

            RecoverMoney();
        }

        private void DoAttack()
        {
            Dictionary<int, int> attackedHeroDic = new Dictionary<int, int>();

            for (int i = MAX_POWER; i > 0; i--)
            {
                List<Hero> heros = new List<Hero>();

                Dictionary<int, Hero>.ValueCollection.Enumerator enumerator = heroDic.Values.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    Hero hero = enumerator.Current;

                    if ((!hero.isSummon || hero.sds.GetHeroTypeSDS().GetCanCharge()) && hero.nowPower >= i)
                    {
                        if (!attackedHeroDic.ContainsKey(hero.uid))
                        {
                            heros.Add(hero);

                            attackedHeroDic.Add(hero.uid, 1);
                        }
                        else
                        {
                            int attackTimes = attackedHeroDic[hero.uid];

                            if (hero.sds.GetAttackTimes() > attackTimes)
                            {
                                heros.Add(hero);

                                attackedHeroDic[hero.uid] = attackTimes + 1;
                            }
                        }
                    }
                }

                if (heros.Count > 0)
                {
                    DoAttackOnTurn(heros);
                }
            }
        }

        private void DoAttackOnTurn(List<Hero> _heros)
        {
            int allDamage = 0;

            List<Hero> heroList = new List<Hero>();
            List<List<Hero>> heroTargetList = new List<List<Hero>>();
            List<int> heroDamageList = new List<int>();

            List<Hero> dieHeroList = null;

            for (int i = 0; i < _heros.Count; i++)
            {
                Hero hero = _heros[i];

                if(hero.sds.GetHeroTypeSDS().GetCanAttack() && hero.sds.GetDamage() > 0)
                {
                    List<Hero> targetHeroList = PublicTools.GetAttackTargetHeroList(mapData.neighbourPosMap, heroMapDic, hero);

                    if (targetHeroList.Count > 0)
                    {
                        heroList.Add(hero);
                        heroTargetList.Add(targetHeroList);
                        heroDamageList.Add(hero.sds.GetDamage());

                        allDamage += hero.sds.GetDamage();

                        if (targetHeroList.Count == 1)
                        {
                            targetHeroList[0].nowPower--;
                        }
                    }
                }
            }

            while(allDamage > 0)
            {
                int tmp = (int)(random.NextDouble() * allDamage);

                int add = 0;

                for(int i = 0; i < heroList.Count; i++)
                {
                    int damage = heroDamageList[i];

                    if(damage + add > tmp)
                    {
                        Hero hero = heroList[i];
                        List<Hero> targetHeroList = heroTargetList[i];

                        allDamage--;

                        heroDamageList[i]--;
                        
                        Hero dieHero = DoDamage(targetHeroList);

                        if(dieHero != null)
                        {
                            for(int m = heroList.Count - 1; m > -1 ; m--)
                            {
                                targetHeroList = heroTargetList[m];

                                int index = targetHeroList.IndexOf(dieHero);

                                if(index != -1)
                                {
                                    targetHeroList.RemoveAt(index);

                                    if(targetHeroList.Count == 0)
                                    {
                                        allDamage -= heroDamageList[m];

                                        heroList.RemoveAt(m);
                                        heroTargetList.RemoveAt(m);
                                        heroDamageList.RemoveAt(m);
                                    }
                                }
                            }

                            if(dieHeroList == null)
                            {
                                dieHeroList = new List<Hero>();
                            }

                            dieHeroList.Add(dieHero);
                        }

                        break;
                    }
                    else
                    {
                        add += damage;
                    }
                }
            }

            if(dieHeroList != null)
            {
                for(int i = 0; i < dieHeroList.Count; i++)
                {
                    Hero dieHero = dieHeroList[i];

                    heroDic.Remove(dieHero.uid);
                    heroMapDic.Remove(dieHero.pos);
                }
            }
        }

        private Hero DoDamage(List<Hero> _targetHeroList)
        {
            int allHp = 0;

            for(int i = 0; i < _targetHeroList.Count; i++)
            {
                allHp += _targetHeroList[i].nowHp;
            }

            int damage = (int)(random.NextDouble() * allHp);

            int add = 0;

            for(int i = 0; i < _targetHeroList.Count; i++)
            {
                Hero hero = _targetHeroList[i];

                if(hero.nowHp + add > damage)
                {
                    hero.nowHp--;

                    if(hero.nowHp == 0)
                    {
                        return hero;
                    }

                    break;
                }
                else
                {
                    add += hero.nowHp;
                }
            }

            return null;
        }

        private void HeroRecoverPower()
        {
            Dictionary<int, Hero>.ValueCollection.Enumerator enumerator = heroDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Hero hero = enumerator.Current;

                if(hero.nowPower < hero.sds.GetPower())
                {
                    hero.nowPower++;
                }
                else
                {
                    hero.nowHp += hero.nowPower;

                    if(hero.nowHp > hero.sds.GetHp())
                    {
                        hero.nowHp = hero.sds.GetHp();
                    }
                }
            }
        }

        private void HeroRecoverCanMove()
        {
            Dictionary<int, Hero>.ValueCollection.Enumerator enumerator = heroDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Hero hero = enumerator.Current;

                bool canMove = true;

                int[] tmpPosArr = mapData.neighbourPosMap[hero.pos];

                for(int i = 0; i < 6; i++)
                {
                    int tmpPos = tmpPosArr[i];

                    if(tmpPos != -1)
                    {
                        if (heroMapDic.ContainsKey(tmpPos))
                        {
                            Hero tmpHero = heroMapDic[tmpPos];

                            if(tmpHero.isMine != hero.isMine && tmpHero.nowPower >= hero.nowPower)
                            {
                                canMove = false;

                                break;
                            }
                        }
                    }
                }

                hero.canMove = canMove;

                if (hero.isSummon)
                {
                    hero.isSummon = false;
                }
            }
        }

        private void ResetMapBelong()
        {
            mapBelongDic.Clear();

            Dictionary<int, List<Hero>> tmpDic = new Dictionary<int, List<Hero>>();

            Dictionary<int, Hero>.ValueCollection.Enumerator enumerator = heroDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Hero hero = enumerator.Current;

                if (hero.canMove)
                {
                    int[] tmpPosArr = mapData.neighbourPosMap[hero.pos];

                    for (int i = 0; i < 6; i++)
                    {
                        int pos = tmpPosArr[i];

                        if(pos != -1)
                        {
                            if (!heroMapDic.ContainsKey(pos))
                            {
                                List<Hero> tmpList;

                                if (!tmpDic.ContainsKey(pos))
                                {
                                    tmpList = new List<Hero>();
                                }
                                else
                                {
                                    tmpList = tmpDic[pos];
                                }

                                tmpList.Add(hero);
                            }
                        }
                    }
                }
            }

            Dictionary<int, List<Hero>>.Enumerator enumerator2 = tmpDic.GetEnumerator();

            while (enumerator2.MoveNext())
            {
                int pos = enumerator2.Current.Key;

                List<Hero> heros = enumerator2.Current.Value;

                bool result = GetMapBelong(heros);
                
                if (mapDic[pos] != result)
                {
                    mapBelongDic.Add(pos, true);
                }
            }
        }

        private bool GetMapBelong(List<Hero> _heros)
        {
            int power = 0;

            for(int i = 0; i < _heros.Count; i++)
            {
                power += _heros[i].nowPower;
            }

            int r = (int)(random.NextDouble() * power);

            int add = 0;

            for(int i = 0; i < _heros.Count; i++)
            {
                Hero hero = _heros[i];

                if(hero.nowPower + add > r)
                {
                    return hero.isMine;
                }
                else
                {
                    add += hero.nowPower;
                }
            }

            return true;
        }

        private void RecoverCards()
        {
            if(mCards.Count > 0)
            {
                int index = (int)(random.NextDouble() * mCards.Count);

                int id = mCards[index];

                mCards.RemoveAt(index);

                if(mHandCards.Count < MAX_HAND_CARD_NUM)
                {
                    mHandCards.Add(cardUid, id);

                    cardUid++;
                }
            }

            if (oCards.Count > 0)
            {
                int index = (int)(random.NextDouble() * oCards.Count);

                int id = oCards[index];

                oCards.RemoveAt(index);

                if (oHandCards.Count < MAX_HAND_CARD_NUM)
                {
                    oHandCards.Add(cardUid, id);

                    cardUid++;
                }
            }
        }

        private void RecoverMoney()
        {
            mMoney += ADD_MONEY;

            if(mMoney > MAX_MONEY)
            {
                mMoney = MAX_MONEY;
            }

            oMoney += ADD_MONEY;

            if(oMoney > MAX_MONEY)
            {
                oMoney = MAX_MONEY;
            }
        }
    }
}
