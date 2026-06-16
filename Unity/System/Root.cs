namespace Hakjang
{
    public static class Root
    {
        public static DataManager sDataManager = new();
        public static ResourceManager sResourceManager = new();
        public static NetworkManager sNetworkManager = new();
        
#if UNITY_EDITOR
        public static DebugManager sDebugManager = new();
#endif
    }
}