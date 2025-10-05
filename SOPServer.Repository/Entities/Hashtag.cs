#nullable disable
using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities;

public partial class Hashtag : BaseEntity
{
    public string Name { get; set; }
    public virtual ICollection<PostHashtags> PostHashtags { get; set; } = new List<PostHashtags>();
}