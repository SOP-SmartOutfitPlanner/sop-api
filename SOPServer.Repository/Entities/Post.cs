#nullable disable
using System;
using System.Collections.Generic;

namespace SOPServer.Repository.Entities;

public partial class Post : BaseEntity
{ 
    virtual public User User { get; set; }
    public long? UserId { get; set; }
    public string Body { get; set; }   
    public virtual ICollection<PostImage> PostImages { get; set; } = new List<PostImage>();
    public virtual ICollection<PostHashtags> PostHashtags { get; set; } = new List<PostHashtags>();
    public virtual ICollection<LikePost> LikePosts { get; set; } = new List<LikePost>();
    public virtual ICollection<CommentPost> CommentPosts { get; set; } = new List<CommentPost>();
}