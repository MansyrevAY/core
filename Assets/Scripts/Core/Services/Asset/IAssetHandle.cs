namespace Core.Services.Asset
{
    public interface IAssetHandle
    {
        int Count { get; }
        bool UnloadAtZero { get; set; }
        bool UnloadRequested { get; }
        
        void AddInstance();
        void RemoveInstance();
        void Release();
    }
}