#nullable disable
using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities;

public partial class PostImage : BaseEntity
{
    public long? PostId { get; set; }
    public string ImgUrl { get; set; }
    public virtual Post Post { get; set; }
}
