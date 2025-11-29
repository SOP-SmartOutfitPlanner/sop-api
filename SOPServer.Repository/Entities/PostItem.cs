using SOPServer.Repository.Commons;

namespace SOPServer.Repository.Entities
{
    public partial class PostItem : BaseEntity
    {
        public long PostId { get; set; }
        public long ItemId { get; set; }

        public virtual Post Post { get; set; } = null!;
        public virtual Item Item { get; set; } = null!;
    }
}
