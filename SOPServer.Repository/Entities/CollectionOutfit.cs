using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities
{
    public partial class CollectionOutfit : BaseEntity
    {
        public long CollectionId { get; set; }
        public long OutfitId { get; set; }
        public string Description { get; set; }

        // Navigation properties
        public virtual Collection Collection { get; set; }
        public virtual Outfit Outfit { get; set; }
    }
}
