using System;
using System.Collections.Generic;
using System.Collections;
using System.IO;

namespace HexWar
{
    public enum ActionType
    {
        NONE,
        SUMMON,
        MOVE
    }

    public class Battle2
    {
        internal static Dictionary<int, IHeroSDS> heroDataDic;
        private static Dictionary<int, MapData> mapDataDic;
        internal static Dictionary<int, ISkillSDS> skillDataDic;

        private const int MAX_POWER = 4;
        private const int DEFAULT_HAND_CARD_NUM = 5;
        private const int MAX_HAND_CARD_NUM = 10;
        private const int DEFAULT_MONEY = 5;
        private const int ADD_MONEY = 1;
        private const int MAX_MONEY = 10;

        public MapData mapData;

        public Dictionary<int, bool> mapDic;
        public Dictionary<int, Hero2> heroMapDic;

        private List<int> mCards;
        private List<int> oCards;

        public Dictionary<int, int> mHandCards;
        public Dictionary<int, int> oHandCards;

        public int mScore;
        public int oScore;

        public int mMoney;
        public int oMoney;
        
        private int cardUid;

        public bool isSkip;
        public bool isMineAction;

        internal Random random;

        private Action<bool, MemoryStream> serverSendDataCallBack;

        private int heroUid;

        internal SuperEventListener superEventListener = new SuperEventListener();

        //client data
        public bool clientIsMine;

        public int clientOppHandCardsNum;

        public ActionType clientActionType = ActionType.NONE;

        public int clientActionData0;

        public int clientActionData1;

        private Action<MemoryStream> clientSendDataCallBack;
        private Action clientRefreshDataCallBack;
        private Action<int, int> clientDoSummonCallBack;
        private Action<int, int> clientDoMoveCallBack;
        private Action<BinaryReader> clientPlayBattleCallBack;

        //ai
        private bool isVsAi;

        public static void Init(Dictionary<int, IHeroSDS> _heroDataDic, Dictionary<int, MapData> _mapDataDic, Dictionary<int, ISkillSDS> _skillDataDic)
        {
            heroDataDic = _heroDataDic;
            mapDataDic = _mapDataDic;
            skillDataDic = _skillDataDic;
        }

        public void ServerSetCallBack(Action<bool, MemoryStream> _serverSendDataCallBack)
        {
            serverSendDataCallBack = _serverSendDataCallBack;
        }

        public void ClientSetCallBack(Action<MemoryStream> _clientSendDataCallBack, Action _clientRefreshDataCallBack, Action<int, int> _clientDoSummonCallBack, Action<int, int> _clientDoMoveCallBack, Action<BinaryReader> _clientPlayBattleCallBack)
        {
            clientSendDataCallBack = _clientSendDataCallBack;
            clientRefreshDataCallBack = _clientRefreshDataCallBack;
            clientDoSummonCallBack = _clientDoSummonCallBack;
            clientDoMoveCallBack = _clientDoMoveCallBack;
            clientPlayBattleCallBack = _clientPlayBattleCallBack;
        }

        public void ServerStart(int _mapID, List<int> _mCards, List<int> _oCards, bool _isVsAi)
        {
            Log.Write("Battle2 Start!");

            random = new Random();

            isVsAi = _isVsAi;

            heroUid = 1;

            mapData = mapDataDic[_mapID];

            heroMapDic = new Dictionary<int, Hero2>();

            mapDic = new Dictionary<int, bool>();

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

            isSkip = false;

            isMineAction = random.NextDouble() < 0.5;
                
            ServerRefreshData(true);

            if(!isVsAi)
            {
                ServerRefreshData(false);
            }
            else
            {
                if (!isMineAction)
                {
                    BattleAi2.Action(this);
                }
            }
        }

        public void ServerGetPackage(byte[] _bytes, bool _isMine)
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

                    bw.Write(isMineAction);

                    bw.Write(isSkip);

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

                    bw.Write(heroMapDic.Count);

                    Dictionary<int, Hero2>.ValueCollection.Enumerator enumerator3 = heroMapDic.Values.GetEnumerator();

                    while (enumerator3.MoveNext())
                    {
                        Hero2 hero = enumerator3.Current;

                        bw.Write(hero.id);

                        bw.Write(hero.isMine);

                        bw.Write(hero.pos);

                        bw.Write(hero.nowHp);

                        bw.Write(hero.nowPower);

                        //bw.Write(hero.isMoved);

                        bw.Write(hero.isSummon);
                    }

                    Dictionary<int, int> handCards = _isMine ? mHandCards : oHandCards;

                    bw.Write(handCards.Count);

                    Dictionary<int, int>.Enumerator enumerator4 = handCards.GetEnumerator();

                    while (enumerator4.MoveNext())
                    {
                        bw.Write(enumerator4.Current.Key);

                        bw.Write(enumerator4.Current.Value);
                    }

                    int oppHandCardsNum = _isMine ? oHandCards.Count : mHandCards.Count;

                    bw.Write(oppHandCardsNum);

                    bw.Write(mMoney);

                    bw.Write(oMoney);
                    
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

                    ClientDoAction(br);

                    br.Close();

                    ms.Dispose();

                    break;

                case PackageTag.S2C_PLAYBATTLE:

                    clientPlayBattleCallBack(br);

                    break;
            }
        }

        private void ClientRefreshData(BinaryReader _br)
        {
            clientIsMine = _br.ReadBoolean();

            isMineAction = _br.ReadBoolean();

            isSkip = _br.ReadBoolean();
            
            mScore = _br.ReadInt32();

            oScore = _br.ReadInt32();

            mapDic = new Dictionary<int, bool>();

            int mapID = _br.ReadInt32();

            mapData = mapDataDic[mapID];

            int num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                int pos = _br.ReadInt32();

                bool mapIsMine = _br.ReadBoolean();

                mapDic.Add(pos, mapIsMine);
            }

            heroMapDic = new Dictionary<int, Hero2>();

            num = _br.ReadInt32();

            for (int i = 0; i < num; i++)
            {
                int id = _br.ReadInt32();

                bool heroIsMine = _br.ReadBoolean();

                int pos = _br.ReadInt32();

                int nowHp = _br.ReadInt32();

                int nowPower = _br.ReadInt32();

                bool isSummon = _br.ReadBoolean();

                AddHero(heroIsMine, id, pos, nowHp, nowPower, isSummon);
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

            for (int i = 0; i < num; i++)
            {
                int uid = _br.ReadInt32();

                int id = _br.ReadInt32();

                handCards.Add(uid, id);
            }

            clientOppHandCardsNum = _br.ReadInt32();

            mMoney = _br.ReadInt32();

            oMoney = _br.ReadInt32();

            clientRefreshDataCallBack();
        }

        public void ClientRequestSummon(int _cardUid, int _pos)
        {
            clientActionType = ActionType.SUMMON;

            clientActionData0 = _cardUid;

            clientActionData1 = _pos;
        }

        public void ClientRequestMove(int _pos, int _direction)
        {
            clientActionType = ActionType.MOVE;

            clientActionData0 = _pos;

            clientActionData1 = _direction;
        }

        public void ClientRequestUndoAction()
        {
            clientActionType = ActionType.NONE;
        }

        public void ClientRequestDoAction()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.C2S_DOACTION);

                    bw.Write((int)clientActionType);

                    if(clientActionType != ActionType.NONE)
                    {
                        bw.Write(clientActionData0);

                        bw.Write(clientActionData1);

                        if(clientActionType == ActionType.SUMMON)
                        {
                            Dictionary<int, int> handCards = clientIsMine ? mHandCards : oHandCards;

                            handCards.Remove(clientActionData0);
                        }
                    }

                    clientSendDataCallBack(ms);

                    clientActionType = ActionType.NONE;
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
            if(isMineAction != _isMine)
            {
                return;
            }
            
            ActionType actionType = (ActionType)_br.ReadInt32();

            if (actionType == ActionType.SUMMON)
            {
                int tmpCardUid = _br.ReadInt32();

                int pos = _br.ReadInt32();

                ServerDoSummon(tmpCardUid, pos);
            }
            else if(actionType == ActionType.MOVE)
            {
                int pos = _br.ReadInt32();

                int direction = _br.ReadInt32();

                ServerDoMove(pos, direction);
            }
            else
            {
                ServerDoSkip();
            }
        }

        internal void ServerDoSkip()
        {
            if (isSkip)
            {
                Log.Write("ServerStartBattle");

                ServerStartBattle();

                Log.Write("ServerEndBattle");
            }
            else
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        isSkip = true;

                        isMineAction = !isMineAction;

                        bw.Write(PackageTag.S2C_DOACTION);

                        bw.Write((int)ActionType.NONE);

                        serverSendDataCallBack(true, ms);

                        if (!isVsAi)
                        {
                            serverSendDataCallBack(false, ms);
                        }
                        else
                        {
                            if (!isMineAction)
                            {
                                BattleAi2.Action(this);
                            }
                        }
                    }
                }
            }
        }

        internal void ServerDoSummon(int _tmpCardUid,int _pos)
        {
            Dictionary<int, int> handCards = isMineAction ? mHandCards : oHandCards;

            if(!mapDic.ContainsKey(_pos) || mapDic[_pos] != isMineAction || !handCards.ContainsKey(_tmpCardUid))
            {
                Log.Write("ServerDoSummon  违规操作  uid:" + _tmpCardUid + "  pos:" + _pos);

                return;
            }

            int cardID = handCards[_tmpCardUid];

            IHeroSDS heroSDS = heroDataDic[cardID];

            if(heroSDS.GetCost() > (isMineAction ? mMoney : oMoney))
            {
                Log.Write("ServerDoSummon  违规操作  oMoney:" + oMoney);

                return;
            }

            handCards.Remove(_tmpCardUid);

            DoSummon(cardID, _pos);

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.S2C_DOACTION);

                    bw.Write((int)ActionType.SUMMON);

                    bw.Write(cardID);

                    bw.Write(_pos);

                    serverSendDataCallBack(true, ms);

                    if(!isVsAi)
                    {
                        serverSendDataCallBack(false, ms);
                    }
                    else
                    {
                        if (!isMineAction)
                        {
                            BattleAi2.Action(this);
                        }
                    }
                }
            }
        }

        private void DoSummon(int _cardID, int _pos)
        {
            IHeroSDS heroSDS = heroDataDic[_cardID];

            if (isMineAction)
            {
                mMoney -= heroSDS.GetCost();
            }
            else
            {
                oMoney -= heroSDS.GetCost();
            }

            AddHero(heroUid, isMineAction, _cardID, heroSDS, _pos);

            heroUid++;

            isSkip = false;

            isMineAction = !isMineAction;
        }
        
        internal void ServerDoMove(int _pos, int _direction)
        {
            if (!heroMapDic.ContainsKey(_pos))
            {
                return;
            }

            Hero2 hero = heroMapDic[_pos];

            if(hero.isMine != isMineAction)
            {
                Log.Write("ServerDoMove  违规操作1");

                return;
            }

            if(hero.nowPower < 1 || hero.isSummon)
            {
                Log.Write("ServerDoMove  违规操作2");

                return;
            }

            if(_direction < 0 || _direction > 6)
            {
                Log.Write("ServerDoMove  违规操作3");

                return;
            }

            int targetPos = mapData.neighbourPosMap[_pos][_direction];

            if (targetPos == -1)
            {
                Log.Write("ServerDoMove  违规操作4");

                return;
            }

            if (heroMapDic.ContainsKey(targetPos))
            {
                Log.Write("ServerDoMove  违规操作5");

                return;
            }

            DoMove(_pos, _direction);

            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter bw = new BinaryWriter(ms))
                {
                    bw.Write(PackageTag.S2C_DOACTION);

                    bw.Write((int)ActionType.MOVE);

                    bw.Write(_pos);

                    bw.Write(_direction);

                    serverSendDataCallBack(true, ms);

                    if (!isVsAi)
                    {
                        serverSendDataCallBack(false, ms);
                    }
                    else
                    {
                        if (!isMineAction)
                        {
                            BattleAi2.Action(this);
                        }
                    }
                }
            }
        }

        private void DoMove(int _pos, int _direction)
        {
            Hero2 hero = heroMapDic[_pos];
            
            int targetPos = mapData.neighbourPosMap[_pos][_direction];
            
            heroMapDic.Remove(_pos);

            heroMapDic.Add(targetPos, hero);

            hero.pos = targetPos;

            hero.nowPower--;

            //hero.isMoved = true;

            if (mapDic[targetPos] != hero.isMine)
            {
                mapDic[targetPos] = hero.isMine;

                if (hero.isMine)
                {
                    mScore++;
                    oScore--;
                }
                else
                {
                    oScore++;
                    mScore--;
                }
            }

            isSkip = false;

            isMineAction = !isMineAction;
        }

        private void AddHero(int _uid, bool _isMine, int _id, IHeroSDS _sds, int _pos)
        {
            Hero2 hero = new Hero2(this, _uid, _isMine, _id, _sds, _pos);

            heroMapDic.Add(hero.pos, hero);
        }

        private void AddHero(bool _isMine, int _id, int _pos, int _nowHp, int _nowPower, bool _isSummon)
        {
            Hero2 hero = new Hero2(_isMine, _id, heroDataDic[_id], _pos, _nowHp, _nowPower, _isSummon);

            heroMapDic.Add(_pos, hero);
        }

        private void ServerStartBattle()
        {
            using (MemoryStream mMs = new MemoryStream(), oMs = new MemoryStream())
            {
                using (BinaryWriter mBw = new BinaryWriter(mMs), oBw = new BinaryWriter(oMs))
                {
                    mBw.Write(PackageTag.S2C_PLAYBATTLE);

                    oBw.Write(PackageTag.S2C_PLAYBATTLE);

                    DoAttack(mBw, oBw);

                    HeroRecoverPower();
                    
                    RecoverCards(mBw, oBw);

                    RecoverMoney();

                    isSkip = false;

                    //isMineAction = !isMineAction;//设置行动顺序

                    serverSendDataCallBack(true, mMs);

                    if (!isVsAi)
                    {
                        serverSendDataCallBack(false, oMs);
                    }
                    else
                    {
                        if (!isMineAction)
                        {
                            BattleAi2.Action(this);
                        }
                    }
                }
            }
        }
        
        private void DoAttack(BinaryWriter _mBw, BinaryWriter _oBw)
        {
            Dictionary<int, int> attackedHeroDic = new Dictionary<int, int>();

            for (int i = MAX_POWER; i > 0; i--)
            {
                List<Hero2> heros = new List<Hero2>();

                Dictionary<int, Hero2>.ValueCollection.Enumerator enumerator = heroMapDic.Values.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    Hero2 hero = enumerator.Current;

                    if (!hero.isSummon && hero.nowPower >= i)
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

                    CastSkill(heros);

                    DoAttackOnTurn(heros, _mBw, _oBw);

                    AttackOver(heros);
                }
                else
                {
                    _mBw.Write(false);

                    _oBw.Write(false);
                }
            }
        }

        private void CastSkill(List<Hero2> _heros)
        {
            for(int i = 0; i < _heros.Count; i++)
            {
                string eventName = string.Format("{0}{1}", SkillEventName.ATTACK1, _heros[i].uid);

                SuperEvent e = new SuperEvent(eventName);

                superEventListener.DispatchEvent(e);

                eventName = string.Format("{0}{1}", SkillEventName.ATTACK1, _heros[i].isMine);

                e = new SuperEvent(eventName, _heros[i]);

                superEventListener.DispatchEvent(e);

                eventName = SkillEventName.ATTACK1.ToString();

                e = new SuperEvent(eventName, _heros[i]);

                superEventListener.DispatchEvent(e);
            }

            Dictionary<int, Hero2>.ValueCollection.Enumerator enumerator = heroMapDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                enumerator.Current.RefreshData();
            }

            for (int i = 0; i < _heros.Count; i++)
            {
                string eventName = string.Format("{0}{1}", SkillEventName.ATTACK2, _heros[i].uid);

                SuperEvent e = new SuperEvent(eventName);

                superEventListener.DispatchEvent(e);

                eventName = string.Format("{0}{1}", SkillEventName.ATTACK2, _heros[i].isMine);

                e = new SuperEvent(eventName, _heros[i]);

                superEventListener.DispatchEvent(e);

                eventName = SkillEventName.ATTACK2.ToString();

                e = new SuperEvent(eventName, _heros[i]);

                superEventListener.DispatchEvent(e);
            }

            enumerator = heroMapDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                enumerator.Current.RefreshData();
            }

            for (int i = 0; i < _heros.Count; i++)
            {
                string eventName = string.Format("{0}{1}", SkillEventName.ATTACK3, _heros[i].uid);

                SuperEvent e = new SuperEvent(eventName);

                superEventListener.DispatchEvent(e);

                eventName = string.Format("{0}{1}", SkillEventName.ATTACK3, _heros[i].isMine);

                e = new SuperEvent(eventName, _heros[i]);

                superEventListener.DispatchEvent(e);

                eventName = SkillEventName.ATTACK3.ToString();

                e = new SuperEvent(eventName, _heros[i]);

                superEventListener.DispatchEvent(e);
            }

            List<Hero2> dieHeroList = null;

            enumerator = heroMapDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                enumerator.Current.RefreshData();

                if (enumerator.Current.nowHp < 1)
                {
                    if (dieHeroList == null)
                    {
                        dieHeroList = new List<Hero2>();
                    }

                    dieHeroList.Add(enumerator.Current);
                }
            }

            if(dieHeroList != null)
            {
                for(int i = 0; i < dieHeroList.Count; i++)
                {
                    Hero2 dieHero = dieHeroList[i];

                    dieHero.Die();

                    _heros.Remove(dieHero);

                    heroMapDic.Remove(dieHero.pos);
                }
            }
        }

        private void DoAttackOnTurn(List<Hero2> _heros, BinaryWriter _mBw, BinaryWriter _oBw)
        {
            int allDamage = 0;

            List<Hero2> heroList = new List<Hero2>();
            List<List<Hero2>> heroTargetList = new List<List<Hero2>>();
            List<int> heroDamageList = new List<int>();

            Dictionary<int, int> doRushDic = new Dictionary<int, int>();
            Dictionary<int, Dictionary<int, int>> doDamageDic = new Dictionary<int, Dictionary<int, int>>();

            //List<Hero2> dieHeroList = null;

            for (int i = 0; i < _heros.Count; i++)
            {
                Hero2 hero = _heros[i];

                if (hero.sds.GetHeroTypeSDS().GetCanAttack() && hero.damage > 0)
                {
                    List<Hero2> targetHeroList = BattlePublicTools2.GetAttackTargetHeroList(mapData.neighbourPosMap, heroMapDic, hero);

                    if (targetHeroList.Count > 0)
                    {
                        heroList.Add(hero);
                        heroTargetList.Add(targetHeroList);
                        heroDamageList.Add(hero.damage);

                        Dictionary<int, int> tmpDamageDic = new Dictionary<int, int>();

                        doDamageDic.Add(hero.pos, tmpDamageDic);

                        allDamage += hero.damage;

                        if (targetHeroList.Count == 1)
                        {
                            Hero2 targetHero = targetHeroList[0];

                            if (targetHero.nowPower > 0)
                            {
                                doRushDic.Add(hero.pos, targetHero.pos);

                                targetHero.nowPower--;
                            }
                        }
                    }
                }
            }

            while (allDamage > 0)
            {
                int tmp = (int)(random.NextDouble() * allDamage);

                int add = 0;

                for (int i = 0; i < heroList.Count; i++)
                {
                    int damage = heroDamageList[i];

                    if (damage + add > tmp)
                    {
                        Hero2 hero = heroList[i];
                        List<Hero2> targetHeroList = heroTargetList[i];

                        allDamage--;

                        heroDamageList[i]--;

                        Hero2 beDamageHero = GetBeDamageHero(targetHeroList);

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

                        if (beDamageHero.nowHp == 0)
                        {
                            for (int m = heroList.Count - 1; m > -1; m--)
                            {
                                targetHeroList = heroTargetList[m];

                                int index = targetHeroList.IndexOf(beDamageHero);

                                if (index != -1)
                                {
                                    targetHeroList.RemoveAt(index);

                                    if (targetHeroList.Count == 0)
                                    {
                                        allDamage -= heroDamageList[m];

                                        heroList.RemoveAt(m);
                                        heroTargetList.RemoveAt(m);
                                        heroDamageList.RemoveAt(m);
                                    }
                                }
                            }

                            //if (dieHeroList == null)
                            //{
                            //    dieHeroList = new List<Hero2>();
                            //}

                            //dieHeroList.Add(beDamageHero);
                        }

                        break;
                    }
                    else
                    {
                        add += damage;
                    }
                }
            }

            //if (dieHeroList != null)
            //{
            //    for (int i = 0; i < dieHeroList.Count; i++)
            //    {
            //        Hero2 dieHero = dieHeroList[i];

            //        dieHero.Die();

            //        heroMapDic.Remove(dieHero.pos);
            //    }
            //}

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

        private void AttackOver(List<Hero2> _heros)
        {
            for (int i = 0; i < _heros.Count; i++)
            {
                string eventName = string.Format("{0}{1}", SkillEventName.ATTACKOVER, _heros[i].uid);

                SuperEvent e = new SuperEvent(eventName);

                superEventListener.DispatchEvent(e);

                eventName = string.Format("{0}{1}", SkillEventName.ATTACKOVER, _heros[i].isMine);

                e = new SuperEvent(eventName, _heros[i]);

                superEventListener.DispatchEvent(e);

                eventName = SkillEventName.ATTACKOVER.ToString();

                e = new SuperEvent(eventName, _heros[i]);

                superEventListener.DispatchEvent(e);
            }

            List<Hero2> dieHeroList = null;

            Dictionary<int, Hero2>.ValueCollection.Enumerator enumerator = heroMapDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                enumerator.Current.RefreshData();

                if (enumerator.Current.nowHp < 1)
                {
                    if (dieHeroList == null)
                    {
                        dieHeroList = new List<Hero2>();
                    }

                    dieHeroList.Add(enumerator.Current);
                }
            }

            if (dieHeroList != null)
            {
                for (int i = 0; i < dieHeroList.Count; i++)
                {
                    Hero2 dieHero = dieHeroList[i];

                    dieHero.Die();

                    _heros.Remove(dieHero);

                    heroMapDic.Remove(dieHero.pos);
                }
            }
        }

        private Hero2 GetBeDamageHero(List<Hero2> _targetHeroList)
        {
            int allHp = 0;

            for (int i = 0; i < _targetHeroList.Count; i++)
            {
                allHp += _targetHeroList[i].nowHp;
            }

            int damage = (int)(random.NextDouble() * allHp);

            int add = 0;

            for (int i = 0; i < _targetHeroList.Count; i++)
            {
                Hero2 hero = _targetHeroList[i];

                if (hero.nowHp + add > damage)
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
            Dictionary<int, Hero2>.ValueCollection.Enumerator enumerator = heroMapDic.Values.GetEnumerator();

            while (enumerator.MoveNext())
            {
                Hero2 hero = enumerator.Current;

                hero.RefreshRoundOver();

                if (hero.nowPower < hero.sds.GetPower())
                {
                    hero.nowPower++;
                }
                else
                {
                    hero.nowHp += hero.nowPower;

                    if (hero.nowHp > hero.maxHp)
                    {
                        hero.nowHp = hero.maxHp;
                    }
                }

                //hero.isMoved = false;

                hero.isSummon = false;
            }
        }
        
        private void RecoverCards(BinaryWriter _mBw, BinaryWriter _oBw)
        {
            if (mCards.Count > 0)
            {
                int index = (int)(random.NextDouble() * mCards.Count);

                int id = mCards[index];

                mCards.RemoveAt(index);

                if (mHandCards.Count < MAX_HAND_CARD_NUM)
                {
                    int tmpCardUid = GetCardUid();

                    mHandCards.Add(tmpCardUid, id);

                    _mBw.Write(true);

                    _oBw.Write(true);

                    _mBw.Write(tmpCardUid);

                    _mBw.Write(id);
                }
                else
                {
                    _mBw.Write(false);

                    _oBw.Write(false);
                }
            }
            else
            {
                _mBw.Write(false);

                _oBw.Write(false);
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

                    _mBw.Write(true);

                    _oBw.Write(tmpCardUid);

                    _oBw.Write(id);
                }
                else
                {
                    _oBw.Write(false);

                    _mBw.Write(false);
                }
            }
            else
            {
                _oBw.Write(false);

                _mBw.Write(false);
            }
        }

        private void RecoverMoney()
        {
            mMoney += ADD_MONEY;

            if (mMoney > MAX_MONEY)
            {
                mMoney = MAX_MONEY;
            }

            oMoney += ADD_MONEY;

            if (oMoney > MAX_MONEY)
            {
                oMoney = MAX_MONEY;
            }
        }
        
        private int GetCardUid()
        {
            int result = cardUid;

            cardUid++;

            return result;
        }

        private void ClientDoAction(BinaryReader _br)
        {
            ActionType actionType = (ActionType)_br.ReadInt32();

            if(actionType == ActionType.SUMMON)
            {
                if (isMineAction != clientIsMine)
                {
                    clientOppHandCardsNum--;
                }

                int cardID = _br.ReadInt32();

                int pos = _br.ReadInt32();

                DoSummon(cardID, pos);

                clientDoSummonCallBack(cardID, pos);
            }
            else if(actionType == ActionType.MOVE)
            {
                int pos = _br.ReadInt32();

                int direction = _br.ReadInt32();

                DoMove(pos, direction);

                clientDoMoveCallBack(pos, direction);
            }
            else
            {
                isSkip = true;

                isMineAction = !isMineAction;

                clientRefreshDataCallBack();
            }
        }
        
        public IEnumerator ClientDoAttack(BinaryReader _br)
        {
            isSkip = false;

            //isMineAction = !isMineAction;//设置行动顺序

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

                        Hero2 targetHero = heroMapDic[targetPos];

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

                        if (targetNum > 0)
                        {
                            KeyValuePair<int, int>[] pair2 = new KeyValuePair<int, int>[targetNum];

                            KeyValuePair<int, KeyValuePair<int, int>[]> pair = new KeyValuePair<int, KeyValuePair<int, int>[]>(pos, pair2);

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

                    Dictionary<int, Hero2>.Enumerator enumerator3 = heroMapDic.GetEnumerator();

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

            bool mAddCard = _br.ReadBoolean();

            if (mAddCard)
            {
                if (clientIsMine)
                {
                    int tmpCardUid = _br.ReadInt32();

                    int cardID = _br.ReadInt32();

                    mHandCards.Add(tmpCardUid, cardID);
                }
                else
                {
                    clientOppHandCardsNum++;
                }
            }

            bool oAddCard = _br.ReadBoolean();

            if (oAddCard)
            {
                if (!clientIsMine)
                {
                    int tmpCardUid = _br.ReadInt32();

                    int cardID = _br.ReadInt32();

                    oHandCards.Add(tmpCardUid, cardID);
                }
                else
                {
                    clientOppHandCardsNum++;
                }
            }
            
            RecoverMoney();
            
            clientRefreshDataCallBack();
        }
    }
}
