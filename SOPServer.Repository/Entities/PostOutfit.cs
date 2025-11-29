using SOPServer.Repository.Commons;

namespace SOPServer.Repository.Entities
{
    public partial class PostOutfit : BaseEntity
    {
        public long PostId { get; set; }
        public long OutfitId { get; set; }

        public virtual Post Post { get; set; } = null!;
        public virtual Outfit Outfit { get; set; } = null!;
    }
}
