using System.Collections.Generic;
using System.IO;

public class MapData
{
    public int id;

    public int mapWidth;
    public int mapHeight;

    public int size;

    public int score1;
    public int score2;

    public Dictionary<int, bool> dic = new Dictionary<int, bool>();

    public Dictionary<int, int[]> neighbourPosMap = new Dictionary<int, int[]>();

    public MapData()
    {

    }

    public MapData(int _mapWidth, int _mapHeight)
    {
        mapWidth = _mapWidth;
        mapHeight = _mapHeight;

        size = mapWidth * mapHeight - mapHeight / 2;
    }

    public void SetData(BinaryWriter _bw)
    {
        _bw.Write(mapWidth);
        _bw.Write(mapHeight);

        _bw.Write(dic.Count);

        Dictionary<int, bool>.Enumerator enumerator = dic.GetEnumerator();

        while (enumerator.MoveNext())
        {
            _bw.Write(enumerator.Current.Key);

            _bw.Write(enumerator.Current.Value);
        }
    }

    public void GetData(BinaryReader _br)
    {
        mapWidth = _br.ReadInt32();
        mapHeight = _br.ReadInt32();

        size = mapWidth * mapHeight - mapHeight / 2;

        int num = _br.ReadInt32();

        for (int i = 0; i < num; i++)
        {
            int pos = _br.ReadInt32();

            bool isMine = _br.ReadBoolean();

            dic.Add(pos, isMine);
        }

        SetNeighbourPosMap();
    }

    public void SetNeighbourPosMap()
    {
        Dictionary<int, bool>.Enumerator enumerator = dic.GetEnumerator();

        while (enumerator.MoveNext())
        {
            int pos = enumerator.Current.Key;

            if (enumerator.Current.Value)
            {
                score1++;
            }
            else
            {
                score2++;
            }

            int[] vec = getNeighbourPosVec(pos);

            neighbourPosMap.Add(pos, vec);
        }
    }

    private int[] getNeighbourPosVec(int _pos)
    {
        int[] vec = new int[6];

        if (_pos % (mapWidth * 2 - 1) != 0)
        {
            if (_pos > mapWidth - 1)
            {
                int p = _pos - mapWidth;

                if (dic.ContainsKey(p))
                {
                    vec[5] = p;
                }
                else
                {
                    vec[5] = -1;
                }
            }
            else
            {
                vec[5] = -1;
            }

            if (_pos < size - mapWidth)
            {
                int p = _pos + mapWidth - 1;

                if (dic.ContainsKey(p))
                {
                    vec[3] = p;
                }
                else
                {
                    vec[3] = -1;
                }
            }
            else
            {
                vec[3] = -1;
            }

            if (_pos % (mapWidth * 2 - 1) != mapWidth)
            {
                int p = _pos - 1;

                if (dic.ContainsKey(p))
                {
                    vec[4] = p;
                }
                else
                {
                    vec[4] = -1;
                }
            }
            else
            {
                vec[4] = -1;
            }
        }
        else
        {
            vec[3] = -1;
            vec[4] = -1;
            vec[5] = -1;
        }

        if (_pos % (mapWidth * 2 - 1) != mapWidth - 1)
        {
            if (_pos > mapWidth - 1)
            {
                int p = _pos - mapWidth + 1;

                if (dic.ContainsKey(p))
                {
                    vec[0] = p;
                }
                else
                {
                    vec[0] = -1;
                }
            }
            else
            {
                vec[0] = -1;
            }

            if (_pos < size - mapWidth)
            {
                int p = _pos + mapWidth;

                if (dic.ContainsKey(p))
                {
                    vec[2] = p;
                }
                else
                {
                    vec[2] = -1;
                }
            }
            else
            {
                vec[2] = -1;
            }

            if (_pos % (mapWidth * 2 - 1) != mapWidth * 2 - 2)
            {
                int p = _pos + 1;

                if (dic.ContainsKey(p))
                {
                    vec[1] = p;
                }
                else
                {
                    vec[1] = -1;
                }
            }
            else
            {
                vec[1] = -1;
            }
        }
        else
        {
            vec[0] = -1;
            vec[1] = -1;
            vec[2] = -1;
        }

        return vec;
    }
}
