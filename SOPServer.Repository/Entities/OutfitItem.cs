using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Entities {
    public partial class OutfitItem : BaseEntity
    {
        public long? OutfitId { get; set; }
        public long? ItemId { get; set; }
        public virtual Outfit Outfit { get; set; }
        public virtual Item Item { get; set; }
    }
}
