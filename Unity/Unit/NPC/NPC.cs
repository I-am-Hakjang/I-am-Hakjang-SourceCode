namespace Hakjang
{
    public class NPC : BaseUnit
    {
        public override void OnNetworkStart(string id, bool is_owner)
        {
            this.Id = id;
            IsOwner = is_owner;
        }
    }
}