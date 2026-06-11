namespace Lair.Meta
{
    //# 씬 전환 간 프로필 공유 static 홀더. Village 진입 시 Load, Battle 은 null 이면 직접 Load (에디터 Battle 직행 안전).
    public static class MetaSession
    {
        public static MetaProfile Profile;
        public static MetaProfileStore Store;

        public static MetaProfile GetOrLoad()
        {
            if (Store == null)
            {
                Store = new MetaProfileStore();
            }
            if (Profile == null)
            {
                Profile = Store.Load();
            }
            return Profile;
        }
    }
}
