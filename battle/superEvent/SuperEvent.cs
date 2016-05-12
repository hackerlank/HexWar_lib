using System;
using System.Collections.Generic;

namespace HexWar_lib.battle.superEvent
{
    class SuperEventUnit
    {
        public int index;
        public string eventName;
        public Action callBack;

        public SuperEventUnit(int _index,string _eventName,Action _callBack)
        {
            index = _index;
            eventName = _eventName;
            callBack = _callBack;
        }
    }

    class SuperEvent
    {
        private Dictionary<int, SuperEventUnit> dicWithID = new Dictionary<int, SuperEventUnit>();
        private Dictionary<string, Dictionary<Action, SuperEventUnit>> dicWithEvent = new Dictionary<string, Dictionary<Action, SuperEventUnit>>();

        private int nowIndex;

        public int AddEvent(string _eventName,Action _callBack)
        {
            SuperEventUnit unit = new SuperEventUnit(nowIndex, _eventName, _callBack);

            nowIndex++;

            dicWithID.Add(unit.index, unit);

            Dictionary<Action, SuperEventUnit> dic;

            if (dicWithEvent.ContainsKey(_eventName))
            {
                dic = dicWithEvent[_eventName];
            }
            else
            {
                dic = new Dictionary<Action, SuperEventUnit>();
            }

            dic.Add(_callBack, unit);

            return unit.index;
        }

        public void RemoveEvent(int _index)
        {
            if (dicWithID.ContainsKey(_index))
            {
                SuperEventUnit unit = dicWithID[_index];

                dicWithID.Remove(_index);

                Dictionary<Action, SuperEventUnit> dic = dicWithEvent[unit.eventName];

                dic.Remove(unit.callBack);

                if(dic.Count == 0)
                {
                    dicWithEvent.Remove(unit.eventName);
                }
            }
        }

        public void RemoveEvent(string _eventName,Action _callBack)
        {
            if (dicWithEvent.ContainsKey(_eventName))
            {
                Dictionary<Action, SuperEventUnit> dic = dicWithEvent[_eventName];

                if (dic.ContainsKey(_callBack))
                {
                    SuperEventUnit unit = dic[_callBack];

                    dicWithID.Remove(unit.index);

                    dic.Remove(_callBack);

                    if(dic.Count == 0)
                    {
                        dicWithEvent.Remove(_eventName);
                    }
                }
            }
        }

        public void DispatchEvent(string _eventName)
        {
            if (dicWithEvent.ContainsKey(_eventName))
            {
                Dictionary<Action, SuperEventUnit> dic = dicWithEvent[_eventName];

                Action[] arr = new Action[dic.Count];

                Dictionary<Action, SuperEventUnit>.KeyCollection.Enumerator enumerator = dic.Keys.GetEnumerator();

                int i = 0;

                while (enumerator.MoveNext())
                {
                    arr[i] = enumerator.Current;
                }

                for(i = 0; i < arr.Length; i++)
                {
                    arr[i]();
                }
            }
        }
    }
}
