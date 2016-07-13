using System;
using System.Collections.Generic;


class SuperEventListenerUnit
{
    public int index;
    public string eventName;
    public Action<SuperEvent> callBack;

    public SuperEventListenerUnit(int _index,string _eventName,Action<SuperEvent> _callBack)
    {
        index = _index;
        eventName = _eventName;
        callBack = _callBack;
    }
}

class SuperEvent
{
    public string eventName;
    public int index;
    public object[] datas;

    public SuperEvent(string _eventName)
    {
        eventName = _eventName;
    }

    public SuperEvent(string _eventName,params object[] _objs)
    {
        eventName = _eventName;
        datas = _objs;
    }

    public SuperEvent(string _eventName,int _index,object[] _datas)
    {
        eventName = _eventName;
        index = _index;
        datas = _datas;
    }
}

class SuperEventListener
{
    private Dictionary<int, SuperEventListenerUnit> dicWithID = new Dictionary<int, SuperEventListenerUnit>();
    private Dictionary<string, Dictionary<Action<SuperEvent>, SuperEventListenerUnit>> dicWithEvent = new Dictionary<string, Dictionary<Action<SuperEvent>, SuperEventListenerUnit>>();

    private int nowIndex;

    public int AddListener(string _eventName,Action<SuperEvent> _callBack)
    {
        SuperEventListenerUnit unit = new SuperEventListenerUnit(nowIndex, _eventName, _callBack);

        nowIndex++;

        dicWithID.Add(unit.index, unit);

        Dictionary<Action<SuperEvent>, SuperEventListenerUnit> dic;

        if (dicWithEvent.ContainsKey(_eventName))
        {
            dic = dicWithEvent[_eventName];
        }
        else
        {
            dic = new Dictionary<Action<SuperEvent>, SuperEventListenerUnit>();

            dicWithEvent.Add(_eventName, dic);
        }

        dic.Add(_callBack, unit);

        return unit.index;
    }

    public void RemoveListener(int _index)
    {
        if (dicWithID.ContainsKey(_index))
        {
            SuperEventListenerUnit unit = dicWithID[_index];

            dicWithID.Remove(_index);

            Dictionary<Action<SuperEvent>, SuperEventListenerUnit> dic = dicWithEvent[unit.eventName];

            dic.Remove(unit.callBack);

            if(dic.Count == 0)
            {
                dicWithEvent.Remove(unit.eventName);
            }
        }
    }

    public void RemoveListener(string _eventName, Action<SuperEvent> _callBack)
    {
        if (dicWithEvent.ContainsKey(_eventName))
        {
            Dictionary<Action<SuperEvent>, SuperEventListenerUnit> dic = dicWithEvent[_eventName];

            if (dic.ContainsKey(_callBack))
            {
                SuperEventListenerUnit unit = dic[_callBack];

                dicWithID.Remove(unit.index);

                dic.Remove(_callBack);

                if(dic.Count == 0)
                {
                    dicWithEvent.Remove(_eventName);
                }
            }
        }
    }

    public void DispatchEvent(SuperEvent e)
    {
        if (dicWithEvent.ContainsKey(e.eventName))
        {
            Dictionary<Action<SuperEvent>, SuperEventListenerUnit> dic = dicWithEvent[e.eventName];

            Action[] arr = new Action[dic.Count];

            Dictionary<Action<SuperEvent>, SuperEventListenerUnit>.Enumerator enumerator = dic.GetEnumerator();

            int i = 0;

            while (enumerator.MoveNext())
            {
                KeyValuePair<Action<SuperEvent>, SuperEventListenerUnit> pair = enumerator.Current;

                SuperEvent ev = new SuperEvent(e.eventName, pair.Value.index, e.datas);

                Action del = delegate ()
                {
                    pair.Key(ev);
                };

                arr[i] = del;

                i++;
            }

            for(i = 0; i < arr.Length; i++)
            {
                arr[i]();
            }
        }
    }
}

