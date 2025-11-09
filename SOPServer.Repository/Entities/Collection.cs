using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities
{
    public partial class Collection : BaseEntity
    {
        public long? UserId { get; set; }
        public string Title { get; set; }
        public string ShortDescription { get; set; }

        // Navigation properties
        public virtual User User { get; set; }
        public virtual ICollection<CollectionOutfit> CollectionOutfits { get; set; } = new HashSet<CollectionOutfit>();
    }
}
