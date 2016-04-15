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

        public Dictionary<int, int> mSummonAction = new Dictionary<int, int>();
        public Dictionary<int, int> oSummonAction = new Dictionary<int, int>();

        public Dictionary<int, int> mMoveAction = new Dictionary<int, int>();
        public Dictionary<int, int> oMoveAction = new Dictionary<int, int>();

        private int cardUid;
        private int heroUid;

        public bool mOver;
        public bool oOver;

        private Random random;

        private Action<bool, MemoryStream> serverSendDataCallBack;

        private Action<MemoryStream> clientSendDataCallBack;
        private Action<bool> clientRefreshDataCallBack;

        public static void Init(Dictionary<int, IHeroSDS> _heroDataDic, Dictionary<int, MapData> _mapDataDic)
        {
            heroDataDic = _heroDataDic;
            mapDataDic = _mapDataDic;
        }

        public void ServerSetCallBack(Action<bool, MemoryStream> _serverSendDataCallBack)
        {
            serverSendDataCallBack = _serverSendDataCallBack;
        }

        public void ClientSetCallBack(Action<MemoryStream> _clientSendDataCallBack, Action<bool> _clientRefreshDataCallBack)
        {
            clientSendDataCallBack = _clientSendDataCallBack;
            clientRefreshDataCallBack = _clientRefreshDataCallBack;
        }

        public void ServerStart(int _mapID,List<int> _mCards,List<int> _oCards)
        {
            Log.Write("Battle Start!");

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

            ServerRefreshData(true);

            ServerRefreshData(false);
        }

        public void ServerGetPackage(byte[] _bytes,bool _isMine)
        {
            using (MemoryStream ms = new MemoryStream(_bytes))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    byte tag = br.ReadByte();

                    switch (tag)
                    {
                        case PackageTag.C2S_REFRESH:

                            ServerRefreshData(_isMine);

                            break;

                        case PackageTag.C2S_DOACTION:

                            ServerDoAction(_isMine, br);
                            
                            break;
                    }
                }
            }
        }

        private void ServerRefreshData(bool _isMine)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    Log.Write("ServerRefreshData  isMine:" + _isMine);

                    bw.Write(PackageTag.S2C_REFRESH);

                    bw.Write(_isMine);

                    bw.Write(mapData.id);

                    bw.Write(mapDic.Count);

                    Dictionary<int, bool>.Enumerator enumerator = mapDic.GetEnumerator();

                    while (enumerator.MoveNext())
                    {
                        bw.Write(enumerator.Current.Key);

                        bw.Write(enumerator.Current.Value);
                    }

                    bw.Write(mapBelongDic.Count);

                    Dictionary<int, bool>.KeyCollection.Enumerator enumerator2 = mapBelongDic.Keys.GetEnumerator();

                    while (enumerator2.MoveNext())
                    {
                        bw.Write(enumerator2.Current);
                    }

                    bw.Write(heroDic.Count);

                    Dictionary<int, Hero>.ValueCollection.Enumerator enumerator3 = heroDic.Values.GetEnumerator();

                    while (enumerator3.MoveNext())
                    {
                        Hero hero = enumerator3.Current;

                        bw.Write(hero.uid);

                        bw.Write(hero.id);

                        bw.Write(hero.isMine);

                        bw.Write(hero.pos);

                        bw.Write(hero.nowHp);

                        bw.Write(hero.nowPower);

                        bw.Write(hero.canMove);
                    }

                    Dictionary<int, int> handCards = _isMine ? mHandCards : oHandCards;

                    bw.Write(handCards.Count);

                    Dictionary<int, int>.Enumerator enumerator4 = handCards.GetEnumerator();

                    while (enumerator4.MoveNext())
                    {
                        bw.Write(enumerator4.Current.Key);

                        bw.Write(enumerator4.Current.Value);
                    }

                    Dictionary<int, int> summonAction;

                    Dictionary<int, int> moveAction;

                    bool isOver;

                    if (_isMine)
                    {
                        bw.Write(mMoney);

                        bw.Write(mOver);

                        isOver = mOver;
                        
                        summonAction = mSummonAction;

                        moveAction = mMoveAction;
                    }
                    else
                    {
                        bw.Write(oMoney);

                        bw.Write(oOver);

                        isOver = oOver;
                        
                        summonAction = oSummonAction;

                        moveAction = oMoveAction;
                    }

                    if (isOver)
                    {
                        bw.Write(summonAction.Count);

                        enumerator4 = summonAction.GetEnumerator();

                        while (enumerator4.MoveNext())
                        {
                            bw.Write(enumerator4.Current.Key);

                            bw.Write(enumerator4.Current.Value);
                        }

                        bw.Write(moveAction.Count);

                        enumerator4 = moveAction.GetEnumerator();

                        while (enumerator4.MoveNext())
                        {
                            bw.Write(enumerator4.Current.Key);

                            bw.Write(enumerator4.Current.Value);
                        }
                    }

                    serverSendDataCallBack(_isMine, ms);
                }
            }
        }

        public void ClientGetPackage(byte[] _bytes)
        {
            using (MemoryStream ms = new MemoryStream(_bytes))
            {
                using (BinaryReader br = new BinaryReader(ms))
                {
                    byte tag = br.ReadByte();

                    switch (tag)
                    {
                        case PackageTag.S2C_REFRESH:

                            ClientRefreshData(br);

                            break;
                    }
                }
            }
        }

        private void ClientRefreshData(BinaryReader _br)
        {
            bool isMine = _br.ReadBoolean();

            Log.Write("ClientRefreshData  isMine:" + isMine);

            mapDic = new Dictionary<int, bool>();

            int mapID = _br.ReadInt32();

            mapData = mapDataDic[mapID];

            int num = _br.ReadInt32();

            for(int i = 0; i < num; i++)
            {
                int pos = _br.ReadInt32();

                bool mapIsMine = _br.ReadBoolean();

                mapDic.Add(pos, mapIsMine);
            }

            mapBelongDic = new Dictionary<int, bool>();

            num = _br.ReadInt32();

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

                bool heroIsMine = _br.ReadBoolean();

                int pos = _br.ReadInt32();

                int nowHp = _br.ReadInt32();

                int nowPower = _br.ReadInt32();

                bool canMove = _br.ReadBoolean();

                Hero hero = new Hero(uid, heroIsMine, id, heroDataDic[id], pos, nowHp, nowPower, canMove);

                heroDic.Add(uid, hero);

                heroMapDic.Add(pos, hero);
            }

            Dictionary<int, int> handCards;

            if (isMine)
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

            Dictionary<int, int> summonAction;

            Dictionary<int, int> moveAction;

            bool isOver;

            if (isMine)
            {
                mMoney = _br.ReadInt32();

                isOver = mOver = _br.ReadBoolean();

                summonAction = mSummonAction;

                moveAction = mMoveAction;
            }
            else
            {
                oMoney = _br.ReadInt32();

                isOver = oOver = _br.ReadBoolean();

                summonAction = oSummonAction;

                moveAction = oMoveAction;
            }

            summonAction.Clear();

            moveAction.Clear();

            if (isOver)
            {
                int actionNum = _br.ReadInt32();

                for (int i = 0; i < actionNum; i++)
                {
                    int uid = _br.ReadInt32();

                    int pos = _br.ReadInt32();

                    summonAction.Add(uid, pos);
                }

                actionNum = _br.ReadInt32();

                for (int i = 0; i < actionNum; i++)
                {
                    int uid = _br.ReadInt32();

                    int direction = _br.ReadInt32();

                    moveAction.Add(uid, direction);
                }
            }

            clientRefreshDataCallBack(isMine);
        }

        public void ClientRequestSummon(bool _isMine, int _uid, int _pos)
        {
            Dictionary<int, int> summonAction = _isMine ? mSummonAction : oSummonAction;

            summonAction.Add(_uid, _pos);
        }

        public void ClientRequestUnsummon(bool _isMine, int _uid)
        {
            Dictionary<int, int> summonAction = _isMine ? mSummonAction : oSummonAction;

            summonAction.Remove(_uid);
        }

        public void ClientRequestMove(bool _isMine, int _uid, int _direction)
        {
            Dictionary<int, int> moveAction = _isMine ? mMoveAction : oMoveAction;

            moveAction.Add(_uid, _direction);
        }

        public void ClientRequestUnmove(bool _isMine, int _uid)
        {
            Dictionary<int, int> moveAction = _isMine ? mMoveAction : oMoveAction;

            moveAction.Remove(_uid);
        }

        public void ClientRequestDoAction(bool _isMine)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.C2S_DOACTION);

                    Dictionary<int, int> summonAction;

                    Dictionary<int, int> moveAction;

                    if (_isMine)
                    {
                        summonAction = mSummonAction;

                        moveAction = mMoveAction;
                    }
                    else
                    {
                        summonAction = oSummonAction;

                        moveAction = oMoveAction;
                    }

                    bw.Write(summonAction.Count);

                    Dictionary<int, int>.Enumerator enumerator = summonAction.GetEnumerator();

                    while (enumerator.MoveNext())
                    {
                        bw.Write(enumerator.Current.Key);

                        bw.Write(enumerator.Current.Value);
                    }

                    bw.Write(moveAction.Count);

                    enumerator = moveAction.GetEnumerator();

                    while (enumerator.MoveNext())
                    {
                        bw.Write(enumerator.Current.Key);

                        bw.Write(enumerator.Current.Value);
                    }

                    summonAction.Clear();

                    moveAction.Clear();

                    if (_isMine)
                    {
                        mOver = true;
                    }
                    else
                    {
                        oOver = true;
                    }

                    clientSendDataCallBack(ms);
                }
            }
        }

        public void ClientRequestRefreshData()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.C2S_REFRESH);

                    clientSendDataCallBack(ms);
                }
            }
        }

        private void ServerDoAction(bool _isMine, BinaryReader _br)
        {
            Dictionary<int, int> cards;
            Dictionary<int, int> summonAction;
            Dictionary<int, int> moveAction;

            if (_isMine)
            {
                if (mOver)
                {
                    return;
                }
                else
                {
                    mOver = true;
                }

                cards = mHandCards;

                summonAction = mSummonAction;

                moveAction = mMoveAction;
            }
            else
            {
                if (oOver)
                {
                    return;
                }
                else
                {
                    oOver = true;
                }

                cards = oHandCards;

                summonAction = oSummonAction;

                moveAction = oMoveAction;
            }

            int summonActionNum = _br.ReadInt32();

            for(int i = 0; i < summonActionNum; i++)
            {
                int uid = _br.ReadInt32();

                int pos = _br.ReadInt32();

                if (cards.ContainsKey(uid))
                {
                    summonAction.Add(uid, pos);
                }
            }

            int moveActionNum = _br.ReadInt32();

            for (int i = 0; i < moveActionNum; i++)
            {
                int uid = _br.ReadInt32();

                int direction = _br.ReadInt32();

                if (heroDic.ContainsKey(uid) && heroDic[uid].isMine == _isMine)
                {
                    moveAction.Add(uid, direction);
                }
            }

            if(mOver && oOver)
            {
                StartBattle();

                ServerRefreshData(true);

                ServerRefreshData(false);
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
            DoSummonAction();

            DoMoveAction();

            DoAttack();

            HeroRecoverPower();

            HeroRecoverCanMove();

            ResetMapBelong();

            RecoverCards();

            RecoverMoney();

            mOver = oOver = false;
        }

        private void DoSummonAction()
        {
            Dictionary<int, int>.Enumerator enumerator = mSummonAction.GetEnumerator();

            while (enumerator.MoveNext())
            {
                int uid = enumerator.Current.Key;
                int pos = enumerator.Current.Value;

                if (mapDic.ContainsKey(pos) && mapDic[pos] && !mapBelongDic.ContainsKey(pos) && !heroMapDic.ContainsKey(pos))
                {
                    int heroID = mHandCards[uid];

                    IHeroSDS sds = heroDataDic[heroID];
                    
                    if (sds.GetCost() > mMoney)
                    {
                        throw new Exception("b");
                    }
                    else
                    {
                        mMoney -= sds.GetCost();
                    }

                    mHandCards.Remove(uid);

                    ServerAddHero(true, heroID, sds, pos);
                }
                else
                {
                    throw new Exception("d");
                }
            }

            mSummonAction.Clear();

            enumerator = oSummonAction.GetEnumerator();

            while (enumerator.MoveNext())
            {
                int uid = enumerator.Current.Key;
                int pos = enumerator.Current.Value;

                if (mapDic.ContainsKey(pos) && !mapDic[pos] && !mapBelongDic.ContainsKey(pos) && !heroMapDic.ContainsKey(pos))
                {
                    int heroID = oHandCards[uid];

                    IHeroSDS sds = heroDataDic[heroID];

                    if (sds.GetCost() > oMoney)
                    {
                        throw new Exception("b");
                    }
                    else
                    {
                        oMoney -= sds.GetCost();
                    }

                    oHandCards.Remove(uid);

                    ServerAddHero(false, heroID, sds, pos);
                }
                else
                {
                    throw new Exception("d");
                }
            }

            oSummonAction.Clear();
        }

        private void DoMoveAction()
        {
            List<int> oldPosList = new List<int>();

            Dictionary<int, Hero> newPosDic = new Dictionary<int, Hero>();

            Dictionary<int, int>.Enumerator enumerator = mMoveAction.GetEnumerator();

            while (enumerator.MoveNext())
            {
                int uid = enumerator.Current.Key;
                int direction = enumerator.Current.Value;
                
                if (direction < 0 || direction > 6)
                {
                    throw new Exception("e");
                }
                
                Hero hero = heroDic[uid];

                if (hero.canMove)
                {
                    int[] tmpArr = mapData.neighbourPosMap[hero.pos];

                    int pos = tmpArr[direction];
                    
                    if (pos != -1 && ((mapDic[pos] == hero.isMine && !mapBelongDic.ContainsKey(pos)) || (mapDic[pos] != hero.isMine && mapBelongDic.ContainsKey(pos))))
                    {
                        oldPosList.Add(hero.pos);
                        newPosDic.Add(pos, hero);
                    }
                }
            }

            mMoveAction.Clear();

            enumerator = oMoveAction.GetEnumerator();

            while (enumerator.MoveNext())
            {
                int uid = enumerator.Current.Key;
                int direction = enumerator.Current.Value;

                if (direction < 0 || direction > 6)
                {
                    throw new Exception("e");
                }
                
                Hero hero = heroDic[uid];

                if (hero.canMove)
                {
                    int[] tmpArr = mapData.neighbourPosMap[hero.pos];

                    int pos = tmpArr[direction];

                    if (pos != -1 && ((mapDic[pos] == hero.isMine && !mapBelongDic.ContainsKey(pos)) || (mapDic[pos] != hero.isMine && mapBelongDic.ContainsKey(pos))))
                    {
                        oldPosList.Add(hero.pos);
                        newPosDic.Add(pos, hero);
                    }
                }
            }

            oMoveAction.Clear();

            for(int i = 0; i < oldPosList.Count; i++)
            {
                heroMapDic.Remove(oldPosList[i]);
            }

            Dictionary<int, Hero>.Enumerator enumerator3 = newPosDic.GetEnumerator();

            while (enumerator3.MoveNext())
            {
                int pos = enumerator3.Current.Key;

                Hero hero = enumerator3.Current.Value;

                if (heroMapDic.ContainsKey(pos))
                {
                    heroDic.Remove(hero.uid);

                    throw new Exception("x");
                }
                else
                {
                    heroMapDic.Add(pos, hero);

                    hero.pos = pos;

                    if (mapDic[pos] != hero.isMine)
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

                if (hero.sds.GetHeroTypeSDS().GetCanMove())
                {
                    bool canMove = true;

                    int[] tmpPosArr = mapData.neighbourPosMap[hero.pos];

                    for (int i = 0; i < 6; i++)
                    {
                        int tmpPos = tmpPosArr[i];

                        if (tmpPos != -1)
                        {
                            if (heroMapDic.ContainsKey(tmpPos))
                            {
                                Hero tmpHero = heroMapDic[tmpPos];

                                if (tmpHero.isMine != hero.isMine && tmpHero.nowPower >= hero.nowPower)
                                {
                                    canMove = false;

                                    break;
                                }
                            }
                        }
                    }

                    hero.canMove = canMove;
                }

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

                                    tmpDic.Add(pos, tmpList);
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
