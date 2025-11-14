#nullable disable
using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities;

public partial class CommentPost : BaseEntity
{
    public long PostId { get; set; }
    public long UserId { get; set; }
    public string Comment { get; set; }
    public long? ParentCommentId { get; set; }
    public bool IsHidden { get; set; } = false;

    public virtual Post Post { get; set; }
    public virtual User User { get; set; }
    public virtual CommentPost ParentComment { get; set; }
    public virtual ICollection<CommentPost> Replies { get; set; } = new List<CommentPost>();
    public virtual ICollection<ReportCommunity> ReportCommunities { get; set; } = new List<ReportCommunity>();
}
