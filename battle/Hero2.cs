namespace HexWar
{
    public class Hero2
    {
        public bool isMine;

        public int id;

        public IHeroSDS sds;

        public int pos;
        public int nowHp;
        public int nowPower;

        //public bool isMoved;
        public bool isSummon;

        internal int uid;

        public Hero2(int _uid, bool _isMine, int _id, IHeroSDS _sds, int _pos)
        {
            uid = _uid;

            isMine = _isMine;
            id = _id;
            sds = _sds;
            pos = _pos;
            nowHp = sds.GetHp();
            nowPower = sds.GetPower();

            //isMoved = false;

            isSummon = !sds.GetHeroTypeSDS().GetCanCharge();
        }

        public Hero2(bool _isMine, int _id, IHeroSDS _sds, int _pos, int _nowHp, int _nowPower, bool _isSummon)
        {
            isMine = _isMine;
            id = _id;
            sds = _sds;
            pos = _pos;
            nowHp = _nowHp;
            nowPower = _nowPower;

            //isMoved = _isMoved;
            isSummon = _isSummon;
        }
    }
}
