﻿using SOPServer.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.UnitOfWork
{
    public interface IUnitOfWork : IDisposable
    {
        IItemRepository ItemRepository { get; }
        ICategoryRepository CategoryRepository { get; }
        IOccasionRepository OccasionRepository { get; }
        ISeasonRepository SeasonRepository { get; }
        IItemOccasionRepository ItemOccasionRepository { get; }
        IItemSeasonRepository ItemSeasonRepository { get; }
        IItemStyleRepository ItemStyleRepository { get; }
        IUserRepository UserRepository { get; }
        IStyleRepository StyleRepository { get; }
        IPostRepository PostRepository { get; }
        IHashtagRepository HashtagRepository { get; }
        IPostHashtagsRepository PostHashtagsRepository { get; }
        IPostImageRepository PostImageRepository { get; }
        IOutfitRepository OutfitRepository { get; }
        IAISettingRepository AISettingRepository { get; }
        IJobRepository JobRepository { get; }
        ILikePostRepository LikePostRepository { get; }
        ICommentPostRepository CommentPostRepository { get; }
        IFollowerRepository FollowerRepository { get; }


        int Save();
        void Commit();
        void Rollback();
        Task SaveAsync();
    }
}
