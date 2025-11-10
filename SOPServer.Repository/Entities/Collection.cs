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
        virtual public User User { get; set; }
        public virtual ICollection<CollectionOutfit> CollectionOutfits { get; set; } = new HashSet<CollectionOutfit>();
        public virtual ICollection<CommentCollection> CommentCollections { get; set; } = new HashSet<CommentCollection>();
        public virtual ICollection<LikeCollection> LikeCollections { get; set; } = new HashSet<LikeCollection>();
        public virtual ICollection<SaveCollection> SaveCollections { get; set; } = new HashSet<SaveCollection>();
    }
}
