using System;
using System.Collections.Generic;
using System.Collections;
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

        public bool mOver;
        public bool oOver;

        private Random random;

        private Action<bool, MemoryStream> serverSendDataCallBack;

        public bool clientIsMine;

        private Action<MemoryStream> clientSendDataCallBack;
        private Action clientRefreshDataCallBack;
        private Action<BinaryReader> clientDoActionCallBack;

        public static void Init(Dictionary<int, IHeroSDS> _heroDataDic, Dictionary<int, MapData> _mapDataDic)
        {
            heroDataDic = _heroDataDic;
            mapDataDic = _mapDataDic;
        }

        public void ServerSetCallBack(Action<bool, MemoryStream> _serverSendDataCallBack)
        {
            serverSendDataCallBack = _serverSendDataCallBack;
        }

        public void ClientSetCallBack(Action<MemoryStream> _clientSendDataCallBack, Action _clientRefreshDataCallBack, Action<BinaryReader> _clientDoActionCallBack)
        {
            clientSendDataCallBack = _clientSendDataCallBack;
            clientRefreshDataCallBack = _clientRefreshDataCallBack;
            clientDoActionCallBack = _clientDoActionCallBack;
        }

        public void ServerStart(int _mapID,List<int> _mCards,List<int> _oCards)
        {
            Log.Write("Battle Start!");

            random = new Random();

            mapData = mapDataDic[_mapID];

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

            cardUid = 1;

            mCards = _mCards;
            oCards = _oCards;

            mHandCards = new Dictionary<int, int>();
            oHandCards = new Dictionary<int, int>();

            for (int i = 0; i < DEFAULT_HAND_CARD_NUM; i++)
            {
                int index = (int)(random.NextDouble() * mCards.Count);

                mHandCards.Add(GetCardUid(), mCards[index]);

                mCards.RemoveAt(index);

                index = (int)(random.NextDouble() * oCards.Count);

                oHandCards.Add(GetCardUid(), oCards[index]);

                oCards.RemoveAt(index);
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

                    bw.Write(mScore);

                    bw.Write(oScore);

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

                    bw.Write(heroMapDic.Count);

                    Dictionary<int, Hero>.ValueCollection.Enumerator enumerator3 = heroMapDic.Values.GetEnumerator();

                    while (enumerator3.MoveNext())
                    {
                        Hero hero = enumerator3.Current;

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
            MemoryStream ms = new MemoryStream(_bytes);
            BinaryReader br = new BinaryReader(ms);
                
            byte tag = br.ReadByte();

            switch (tag)
            {
                case PackageTag.S2C_REFRESH:

                    ClientRefreshData(br);

                    br.Close();

                    ms.Dispose();

                    break;

                case PackageTag.S2C_DOACTION:

                    clientDoActionCallBack(br);

                    break;
            }
        }

        private void ClientRefreshData(BinaryReader _br)
        {
            clientIsMine = _br.ReadBoolean();

            Log.Write("ClientRefreshData  isMine:" + clientIsMine);

            mScore = _br.ReadInt32();

            oScore = _br.ReadInt32();

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

            heroMapDic = new Dictionary<int, Hero>();

            num = _br.ReadInt32();

            for(int i = 0; i < num; i++)
            {
                int id = _br.ReadInt32();

                bool heroIsMine = _br.ReadBoolean();

                int pos = _br.ReadInt32();

                int nowHp = _br.ReadInt32();

                int nowPower = _br.ReadInt32();

                bool canMove = _br.ReadBoolean();

                AddHero(heroIsMine, id, pos, nowHp, nowPower, canMove);
            }

            Dictionary<int, int> handCards;

            if (clientIsMine)
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

            if (clientIsMine)
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

            clientRefreshDataCallBack();
        }

        public void ClientRequestSummon(int _cardUid, int _pos)
        {
            Dictionary<int, int> summonAction = clientIsMine ? mSummonAction : oSummonAction;

            summonAction.Add(_cardUid, _pos);
        }

        public void ClientRequestUnsummon(int _cardUid)
        {
            Dictionary<int, int> summonAction = clientIsMine ? mSummonAction : oSummonAction;

            summonAction.Remove(_cardUid);
        }

        public void ClientRequestMove(int _pos, int _direction)
        {
            Dictionary<int, int> moveAction = clientIsMine ? mMoveAction : oMoveAction;

            moveAction.Add(_pos, _direction);
        }

        public void ClientRequestUnmove(int _pos)
        {
            Dictionary<int, int> moveAction = clientIsMine ? mMoveAction : oMoveAction;

            moveAction.Remove(_pos);
        }

        public void ClientRequestDoAction()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.C2S_DOACTION);

                    Dictionary<int, int> summonAction;

                    Dictionary<int, int> moveAction;

                    if (clientIsMine)
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

                    //summonAction.Clear();

                    //moveAction.Clear();

                    if (clientIsMine)
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
                int pos = _br.ReadInt32();

                int direction = _br.ReadInt32();

                if (heroMapDic.ContainsKey(pos) && heroMapDic[pos].isMine == _isMine)
                {
                    moveAction.Add(pos, direction);
                }
            }

            if(mOver && oOver)
            {
                ServerStartBattle();

                //ServerRefreshData(true);

                //ServerRefreshData(false);
            }
        }

        private void AddHero(bool _isMine,int _id,IHeroSDS _sds,int _pos)
        {
            Hero hero = new Hero(_isMine, _id, _sds, _pos);

            heroMapDic.Add(hero.pos, hero);
        }

        private void AddHero(bool _isMine,int _id,int _pos,int _nowHp,int _nowPower,bool _canMove)
        {
            Hero hero = new Hero(_isMine, _id, heroDataDic[_id], _pos, _nowHp, _nowPower, _canMove);

            heroMapDic.Add(_pos, hero);
        }

        private void ServerStartBattle()
        {
            using (MemoryStream mMs = new MemoryStream(), oMs = new MemoryStream())
            {
                using (BinaryWriter mBw = new BinaryWriter(mMs), oBw = new BinaryWriter(oMs))
                {
                    mBw.Write(PackageTag.S2C_DOACTION);

                    oBw.Write(PackageTag.S2C_DOACTION);

                    DoSummonAction(mBw, oBw);

                    DoMoveAction(mBw, oBw);

                    DoAttack(mBw, oBw);

                    HeroRecoverPower();

                    HeroRecoverCanMove();

                    ResetMapBelong(mBw, oBw);

                    RecoverCards(mBw, oBw);

                    RecoverMoney();

                    RecoverOver();

                    serverSendDataCallBack(true, mMs);

                    serverSendDataCallBack(false, oMs);
                }
            }
        }

        private void DoSummonAction(BinaryWriter _mBw,BinaryWriter _oBw)
        {
            _oBw.Write(mSummonAction.Count);

            Dictionary<int, int>.Enumerator enumerator = mSummonAction.GetEnumerator();

            while (enumerator.MoveNext())
            {
                int tmpCardUid = enumerator.Current.Key;
                int pos = enumerator.Current.Value;

                _oBw.Write(pos);

                if (mapDic.ContainsKey(pos) && mapDic[pos] && !mapBelongDic.ContainsKey(pos) && !heroMapDic.ContainsKey(pos))
                {
                    int heroID = mHandCards[tmpCardUid];

                    _oBw.Write(heroID);

                    IHeroSDS sds = heroDataDic[heroID];
                    
                    if (sds.GetCost() > mMoney)
                    {
                        throw new Exception("b");
                    }
                    else
                    {
                        mMoney -= sds.GetCost();
                    }

                    mHandCards.Remove(tmpCardUid);

                    AddHero(true, heroID, sds, pos);
                }
                else
                {
                    throw new Exception("d");
                }
            }

            mSummonAction.Clear();

            _mBw.Write(oSummonAction.Count);

            enumerator = oSummonAction.GetEnumerator();

            while (enumerator.MoveNext())
            {
                int uid = enumerator.Current.Key;
                int pos = enumerator.Current.Value;

                _mBw.Write(pos);

                if (mapDic.ContainsKey(pos) && !mapDic[pos] && !mapBelongDic.ContainsKey(pos) && !heroMapDic.ContainsKey(pos))
                {
                    int heroID = oHandCards[uid];

                    _mBw.Write(heroID);

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

                    AddHero(false, heroID, sds, pos);
                }
                else
                {
                    throw new Exception("d");
                }
            }

            oSummonAction.Clear();
        }

        private void DoMoveAction(BinaryWriter _mBw,BinaryWriter _oBw)
        {
            Dictionary<int, Hero> newPosDic = new Dictionary<int, Hero>();

            _oBw.Write(mMoveAction.Count);

            Dictionary<int, int>.Enumerator enumerator = mMoveAction.GetEnumerator();

            while (enumerator.MoveNext())
            {
                int pos = enumerator.Current.Key;
                int direction = enumerator.Current.Value;

                _oBw.Write(pos);

                _oBw.Write(direction);
                
                if (direction < 0 || direction > 6)
                {
                    throw new Exception("e");
                }
                
                Hero hero = heroMapDic[pos];

                if (hero.canMove)
                {
                    int[] tmpArr = mapData.neighbourPosMap[hero.pos];

                    int targetPos = tmpArr[direction];
                    
                    if (targetPos != -1 && ((mapDic[targetPos] == hero.isMine && !mapBelongDic.ContainsKey(targetPos)) || (mapDic[targetPos] != hero.isMine && mapBelongDic.ContainsKey(targetPos))))
                    {
                        newPosDic.Add(targetPos, hero);
                    }
                }
            }

            mMoveAction.Clear();

            _mBw.Write(oMoveAction.Count);

            enumerator = oMoveAction.GetEnumerator();

            while (enumerator.MoveNext())
            {
                int pos = enumerator.Current.Key;
                int direction = enumerator.Current.Value;

                _mBw.Write(pos);

                _mBw.Write(direction);

                if (direction < 0 || direction > 6)
                {
                    throw new Exception("e");
                }
                
                Hero hero = heroMapDic[pos];

                if (hero.canMove)
                {
                    int[] tmpArr = mapData.neighbourPosMap[hero.pos];

                    int targetPos = tmpArr[direction];

                    if (targetPos != -1 && ((mapDic[targetPos] == hero.isMine && !mapBelongDic.ContainsKey(targetPos)) || (mapDic[targetPos] != hero.isMine && mapBelongDic.ContainsKey(targetPos))))
                    {
                        newPosDic.Add(targetPos, hero);
                    }
                }
            }

            oMoveAction.Clear();

            Dictionary<int, Hero>.ValueCollection.Enumerator enumerator4 = newPosDic.Values.GetEnumerator();

            while (enumerator4.MoveNext())
            {
                heroMapDic.Remove(enumerator4.Current.pos);
            }
            
            Dictionary<int, Hero>.Enumerator enumerator3 = newPosDic.GetEnumerator();

            while (enumerator3.MoveNext())
            {
                int pos = enumerator3.Current.Key;

                Hero hero = enumerator3.Current.Value;

                if (heroMapDic.ContainsKey(pos))
                {
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

        private void DoAttack(BinaryWriter _mBw,BinaryWriter _oBw)
        {
            Dictionary<int, int> attackedHeroDic = new Dictionary<int, int>();

            for (int i = MAX_POWER; i > 0; i--)
            {
                List<Hero> heros = new List<Hero>();

                Dictionary<int, Hero>.ValueCollection.Enumerator enumerator = heroMapDic.Values.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    Hero hero = enumerator.Current;

                    if ((!hero.isSummon || hero.sds.GetHeroTypeSDS().GetCanCharge()) && hero.nowPower >= i)
                    {
                        if (!attackedHeroDic.ContainsKey(hero.pos))
                        {
                            heros.Add(hero);

                            attackedHeroDic.Add(hero.pos, 1);
                        }
                        else
                        {
                            int attackTimes = attackedHeroDic[hero.pos];

                            if (hero.sds.GetAttackTimes() > attackTimes)
                            {
                                heros.Add(hero);

                                attackedHeroDic[hero.pos] = attackTimes + 1;
                            }
                        }
                    }
                }

                if (heros.Count > 0)
                {
                    _mBw.Write(true);

                    _oBw.Write(true);

                    DoAttackOnTurn(heros, _mBw, _oBw);
                }
                else
                {
                    _mBw.Write(false);

                    _oBw.Write(false);
                }
            }
        }

        private void DoAttackOnTurn(List<Hero> _heros, BinaryWriter _mBw, BinaryWriter _oBw)
        {
            int allDamage = 0;

            List<Hero> heroList = new List<Hero>();
            List<List<Hero>> heroTargetList = new List<List<Hero>>();
            List<int> heroDamageList = new List<int>();

            Dictionary<int, int> doRushDic = new Dictionary<int, int>();
            Dictionary<int, Dictionary<int, int>> doDamageDic = new Dictionary<int, Dictionary<int, int>>();

            List<Hero> dieHeroList = null;

            for (int i = 0; i < _heros.Count; i++)
            {
                Hero hero = _heros[i];

                if(hero.sds.GetHeroTypeSDS().GetCanAttack() && hero.sds.GetDamage() > 0)
                {
                    List<Hero> targetHeroList = BattlePublicTools.GetAttackTargetHeroList(mapData.neighbourPosMap, heroMapDic, hero);

                    if (targetHeroList.Count > 0)
                    {
                        heroList.Add(hero);
                        heroTargetList.Add(targetHeroList);
                        heroDamageList.Add(hero.sds.GetDamage());

                        Dictionary<int, int> tmpDamageDic = new Dictionary<int, int>();
                        
                        doDamageDic.Add(hero.pos, tmpDamageDic);

                        allDamage += hero.sds.GetDamage();

                        if (targetHeroList.Count == 1)
                        {
                            Hero targetHero = targetHeroList[0];

                            if(targetHero.nowPower > 0)
                            {
                                doRushDic.Add(hero.pos, targetHero.pos);

                                targetHero.nowPower--;
                            }
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
                        
                        Hero beDamageHero = GetBeDamageHero(targetHeroList);

                        Dictionary<int, int> tmpDic = doDamageDic[hero.pos];

                        if (tmpDic.ContainsKey(beDamageHero.pos))
                        {
                            tmpDic[beDamageHero.pos]++;
                        }
                        else
                        {
                            tmpDic.Add(beDamageHero.pos, 1);
                        }

                        beDamageHero.nowHp--;

                        if(beDamageHero.nowHp == 0)
                        {
                            for(int m = heroList.Count - 1; m > -1 ; m--)
                            {
                                targetHeroList = heroTargetList[m];

                                int index = targetHeroList.IndexOf(beDamageHero);

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

                            dieHeroList.Add(beDamageHero);
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

                    heroMapDic.Remove(dieHero.pos);
                }
            }

            _mBw.Write(doRushDic.Count);

            _oBw.Write(doRushDic.Count);

            Dictionary<int, int>.Enumerator enumerator3 = doRushDic.GetEnumerator();

            while (enumerator3.MoveNext())
            {
                _mBw.Write(enumerator3.Current.Key);

                _oBw.Write(enumerator3.Current.Key);

                _mBw.Write(enumerator3.Current.Value);

                _oBw.Write(enumerator3.Current.Value);
            }

            _mBw.Write(doDamageDic.Count);

            _oBw.Write(doDamageDic.Count);

            Dictionary<int, Dictionary<int, int>>.Enumerator enumerator = doDamageDic.GetEnumerator();

            while (enumerator.MoveNext())
            {
                _mBw.Write(enumerator.Current.Key);

                _oBw.Write(enumerator.Current.Key);

                Dictionary<int, int> tmpDic = enumerator.Current.Value;

                _mBw.Write(tmpDic.Count);

                _oBw.Write(tmpDic.Count);

                Dictionary<int, int>.Enumerator enumerator2 = tmpDic.GetEnumerator();

                while (enumerator2.MoveNext())
                {
                    _mBw.Write(enumerator2.Current.Key);

                    _oBw.Write(enumerator2.Current.Key);

                    _mBw.Write(enumerator2.Current.Value);

                    _oBw.Write(enumerator2.Current.Value);
                }
            }
        }

        private Hero GetBeDamageHero(List<Hero> _targetHeroList)
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
                    return hero;
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
            Dictionary<int, Hero>.ValueCollection.Enumerator enumerator = heroMapDic.Values.GetEnumerator();

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
            Dictionary<int, Hero>.ValueCollection.Enumerator enumerator = heroMapDic.Values.GetEnumerator();

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

        private void ResetMapBelong(BinaryWriter _mBw, BinaryWriter _oBw)
        {
            mapBelongDic.Clear();

            Dictionary<int, List<Hero>> tmpDic = new Dictionary<int, List<Hero>>();

            Dictionary<int, Hero>.ValueCollection.Enumerator enumerator = heroMapDic.Values.GetEnumerator();

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

            _mBw.Write(mapBelongDic.Count);

            _oBw.Write(mapBelongDic.Count);

            Dictionary<int, bool>.KeyCollection.Enumerator enumerator3 = mapBelongDic.Keys.GetEnumerator();

            while (enumerator3.MoveNext())
            {
                _mBw.Write(enumerator3.Current);

                _oBw.Write(enumerator3.Current);
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

                if (hero.nowPower + add > r)
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

        private void RecoverCards(BinaryWriter _mBw, BinaryWriter _oBw)
        {
            if(mCards.Count > 0)
            {
                int index = (int)(random.NextDouble() * mCards.Count);

                int id = mCards[index];

                mCards.RemoveAt(index);

                if(mHandCards.Count < MAX_HAND_CARD_NUM)
                {
                    int tmpCardUid = GetCardUid();

                    mHandCards.Add(tmpCardUid, id);

                    _mBw.Write(true);

                    _mBw.Write(tmpCardUid);

                    _mBw.Write(id);
                }
                else
                {
                    _mBw.Write(false);
                }
            }
            else
            {
                _mBw.Write(false);
            }

            if (oCards.Count > 0)
            {
                int index = (int)(random.NextDouble() * oCards.Count);

                int id = oCards[index];

                oCards.RemoveAt(index);

                if (oHandCards.Count < MAX_HAND_CARD_NUM)
                {
                    int tmpCardUid = GetCardUid();

                    oHandCards.Add(tmpCardUid, id);

                    _oBw.Write(true);

                    _oBw.Write(tmpCardUid);

                    _oBw.Write(id);
                }
                else
                {
                    _oBw.Write(false);
                }
            }
            else
            {
                _oBw.Write(false);
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

        private void RecoverOver()
        {
            mOver = oOver = false;
        }

        private int GetCardUid()
        {
            int result = cardUid;

            cardUid++;

            return result;
        }

        public int ClientDoSummonMyHero(BinaryReader _br)
        {
            Dictionary<int, int> summonAction = clientIsMine ? mSummonAction : oSummonAction;

            int summonNum = summonAction.Count;

            if(summonNum > 0)
            {
                Dictionary<int, int> tmpCards = clientIsMine ? mHandCards : oHandCards;

                Dictionary<int, int>.Enumerator enumerator = summonAction.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    int tmpCardUid = enumerator.Current.Key;

                    int pos = enumerator.Current.Value;

                    int heroID = tmpCards[tmpCardUid];

                    IHeroSDS sds = heroDataDic[heroID];

                    if (clientIsMine)
                    {
                        mMoney -= sds.GetCost();
                    }
                    else
                    {
                        oMoney -= sds.GetCost();
                    }

                    tmpCards.Remove(tmpCardUid);

                    AddHero(clientIsMine, heroID, sds, pos);
                }

                summonAction.Clear();
            }

            return summonNum;
        }

        public int ClientDoSummonOppHero(BinaryReader _br)
        {
            int summonNum = _br.ReadInt32();

            if(summonNum > 0)
            {
                for (int i = 0; i < summonNum; i++)
                {
                    int pos = _br.ReadInt32();

                    int heroID = _br.ReadInt32();

                    IHeroSDS sds = heroDataDic[heroID];

                    AddHero(!clientIsMine, heroID, sds, pos);
                }
            }

            return summonNum;
        }

        public Dictionary<int, int> ClientDoMove(BinaryReader _br)
        {
            Dictionary<int, int> clientMoveDic = new Dictionary<int, int>();

            Dictionary<int, Hero> moveDic = new Dictionary<int, Hero>();

            Dictionary<int, int> moveAction = clientIsMine ? mMoveAction : oMoveAction;

            Dictionary<int, int>.Enumerator enumerator = moveAction.GetEnumerator();

            while (enumerator.MoveNext())
            {
                int pos = enumerator.Current.Key;

                int direction = enumerator.Current.Value;

                int targetPos = mapData.neighbourPosMap[pos][direction];

                moveDic.Add(targetPos, heroMapDic[pos]);

                clientMoveDic.Add(pos, targetPos);
            }

            moveAction.Clear();

            int moveNum = _br.ReadInt32();

            for (int i = 0; i < moveNum; i++)
            {
                int pos = _br.ReadInt32();

                int direction = _br.ReadInt32();

                int targetPos = mapData.neighbourPosMap[pos][direction];

                moveDic.Add(targetPos, heroMapDic[pos]);

                clientMoveDic.Add(pos, targetPos);
            }

            Dictionary<int, Hero>.ValueCollection.Enumerator enumerator2 = moveDic.Values.GetEnumerator();

            while (enumerator2.MoveNext())
            {
                heroMapDic.Remove(enumerator2.Current.pos);
            }

            Dictionary<int, Hero>.Enumerator enumerator3 = moveDic.GetEnumerator();

            while (enumerator3.MoveNext())
            {
                Hero hero = enumerator3.Current.Value;

                int pos = enumerator3.Current.Key;

                heroMapDic.Add(pos, hero);

                hero.pos = pos;

                if (mapDic[pos] != hero.isMine)
                {
                    mapBelongDic.Remove(pos);

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

            return clientMoveDic;
        }

        public IEnumerator ClientDoAttack(BinaryReader _br)
        {
            for (int i = MAX_POWER; i > 0; i--)
            {
                bool b = _br.ReadBoolean();

                if (b)
                {
                    yield return i;

                    int num = _br.ReadInt32();

                    for (int m = 0; m < num; m++)
                    {
                        int pos = _br.ReadInt32();

                        int targetPos = _br.ReadInt32();

                        Hero targetHero = heroMapDic[targetPos];

                        if (targetHero.nowPower > 0)
                        {
                            targetHero.nowPower--;
                        }

                        yield return new KeyValuePair<int, int>(pos, targetPos);
                    }

                    num = _br.ReadInt32();

                    for (int m = 0; m < num; m++)
                    {
                        int pos = _br.ReadInt32();

                        int targetNum = _br.ReadInt32();

                        if(targetNum > 0)
                        {
                            KeyValuePair<int, int>[] pair2 = new KeyValuePair<int, int>[targetNum];

                            KeyValuePair<int, KeyValuePair<int, int>[]> pair = new KeyValuePair<int, KeyValuePair<int, int>[]>(pos,pair2);

                            for (int n = 0; n < targetNum; n++)
                            {
                                int targetPos = _br.ReadInt32();

                                int damage = _br.ReadInt32();

                                heroMapDic[targetPos].nowHp -= damage;

                                pair2[n] = new KeyValuePair<int, int>(targetPos, damage);
                            }

                            yield return pair;
                        }
                    }

                    List<int> delList = null;

                    Dictionary<int, Hero>.Enumerator enumerator3 = heroMapDic.GetEnumerator();

                    while (enumerator3.MoveNext())
                    {
                        if (enumerator3.Current.Value.nowHp == 0)
                        {
                            if (delList == null)
                            {
                                delList = new List<int>();
                            }

                            delList.Add(enumerator3.Current.Key);
                        }
                    }

                    if (delList != null)
                    {
                        for (int m = 0; m < delList.Count; m++)
                        {
                            heroMapDic.Remove(delList[m]);
                        }
                    }
                }
            }
        }

        public void ClientDoRecover(BinaryReader _br)
        { 
            HeroRecoverPower();

            HeroRecoverCanMove();

            mapBelongDic.Clear();

            int mapBelongNum = _br.ReadInt32();

            for(int i = 0; i < mapBelongNum; i++)
            {
                int pos = _br.ReadInt32();

                mapBelongDic.Add(pos, true);
            }

            bool addCard = _br.ReadBoolean();

            if (addCard)
            {
                Dictionary<int, int> tmpCards = clientIsMine ? mHandCards : oHandCards;

                int tmpCardUid = _br.ReadInt32();

                int id = _br.ReadInt32();

                tmpCards.Add(tmpCardUid, id);
            }

            RecoverMoney();

            if (clientIsMine)
            {
                mOver = false;
            }
            else
            {
                oOver = false;
            }

            clientRefreshDataCallBack();
        }
    }
}
