namespace HexWar
{
    public class Hero
    {
        public bool isMine;

        public int id;

        public IHeroSDS sds;

        public int pos;
        public int nowHp;
        public int nowPower;
        public bool canMove;

        public bool isSummon;

        public Hero(bool _isMine,int _id,IHeroSDS _sds,int _pos)
        {
            isMine = _isMine;
            id = _id;
            sds = _sds;
            pos = _pos;
            nowHp = sds.GetHp();
            nowPower = sds.GetPower();

            canMove = false;
            isSummon = true;
        }

        public Hero(bool _isMine,int _id,IHeroSDS _sds,int _pos,int _nowHp,int _nowPower,bool _canMove)
        {
            isMine = _isMine;
            id = _id;
            sds = _sds;
            pos = _pos;
            nowHp = _nowHp;
            nowPower = _nowPower;

            canMove = _canMove;
            isSummon = false;
        }
    }
}
