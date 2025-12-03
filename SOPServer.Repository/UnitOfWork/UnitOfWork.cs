using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using SOPServer.Repository.DBContext;
using SOPServer.Repository.Repositories.Implements;
using SOPServer.Repository.Repositories.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.UnitOfWork
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly SOPServerContext _context;
        private IDbContextTransaction _transaction;
        private IItemRepository _itemRepository;
        private ICategoryRepository _categoryRepository;
        private IOccasionRepository _occasionRepository;
        private ISeasonRepository _seasonRepository;
        private IItemOccasionRepository _itemOccasionRepository;
        private IItemSeasonRepository _itemSeasonRepository;
        private IItemStyleRepository _itemStyleRepository;
        private IUserRepository _userRepository;
        private IStyleRepository _styleRepository;
        private IPostRepository _postRepository;
        private IHashtagRepository _hashtagRepository;
        private IPostHashtagsRepository _postHashtagsRepository;
        private IPostImageRepository _postImageRepository;
        private IPostItemRepository _postItemRepository;
        private IPostOutfitRepository _postOutfitRepository;
        private IOutfitRepository _outfitRepository;
        private IAISettingRepository _aiSettingRepository;
        private IJobRepository _jobRepository;
        private ILikePostRepository _likePostRepository;
        private ICommentPostRepository _commentPostRepository;
        private IFollowerRepository _followerRepository;
        private IUserOccasionRepository _userOccasionRepository;
        private IOutfitItemRepository _outfitItemRepository;
        private IOutfitUsageHistoryRepository _outfitUsageHistoryRepository;
        private ICollectionRepository _collectionRepository;
        private ICollectionOutfitRepository _collectionOutfitRepository;
        private ICommentCollectionRepository _commentCollectionRepository;
        private ILikeCollectionRepository _likeCollectionRepository;
        private ISaveCollectionRepository _saveCollectionRepository;
        private IReportCommunityRepository _reportCommunityRepository;
        private IReportReporterRepository _reportReporterRepository;
        private IUserSuspensionRepository _userSuspensionRepository;
        private IUserViolationRepository _userViolationRepository;
        private IUserSubscriptionRepository _userSubscriptionRepository;
        private ISubscriptionPlanRepository _subscriptionPlanRepository;
        private IUserSubscriptionTransactionRepository _subscriptionTransactionRepository;
        private INotificationRepository _notificationRepository;
        private IUserNotificationRepository _userNotificationRepository;
        private IUserDeviceRepository _userDeviceRepository;
        private IItemWornAtHistoryRepository _itemWornAtHistoryRepository;

        public UnitOfWork(SOPServerContext context)
        {
            _context = context;
        }

        public IItemRepository ItemRepository
        {
            get
            {
                return _itemRepository ??= new ItemRepository(_context);
            }
        }

        public IUserRepository UserRepository
        {
            get
            {
                return _userRepository ??= new UserRepository(_context);
            }
        }

        public ICategoryRepository CategoryRepository
        {
            get
            {
                return _categoryRepository ??= new CategoryRepository(_context);
            }
        }

        public IOccasionRepository OccasionRepository
        {
            get
            {
                return _occasionRepository ??= new OccasionRepository(_context);
            }
        }

        public IItemOccasionRepository ItemOccasionRepository
        {
            get
            {
                return _itemOccasionRepository ??= new ItemOccasionRepository(_context);
            }
        }

        public IItemSeasonRepository ItemSeasonRepository
        {
            get
            {
                return _itemSeasonRepository ??= new ItemSeasonRepository(_context);
            }
        }

        public IItemStyleRepository ItemStyleRepository
        {
            get
            {
                return _itemStyleRepository ??= new ItemStyleRepository(_context);
            }
        }

        public IStyleRepository StyleRepository
        {
            get
            {
                return _styleRepository ??= new StyleRepository(_context);
            }
        }

        public IPostRepository PostRepository
        {
            get
            {
                return _postRepository ??= new PostRepository(_context);
            }
        }

        public ISeasonRepository SeasonRepository
        {
            get
            {
                return _seasonRepository ??= new SeasonRepository(_context);
            }
        }

        public IHashtagRepository HashtagRepository
        {
            get
            {
                return _hashtagRepository ??= new HashtagRepository(_context);
            }
        }

        public IPostHashtagsRepository PostHashtagsRepository
        {
            get
            {
                return _postHashtagsRepository ??= new PostHashtagsRepository(_context);
            }
        }

        public IPostImageRepository PostImageRepository
        {
            get
            {
                return _postImageRepository ??= new PostImageRepository(_context);
            }
        }

        public IPostItemRepository PostItemRepository
        {
            get
            {
                return _postItemRepository ??= new PostItemRepository(_context);
            }
        }

        public IPostOutfitRepository PostOutfitRepository
        {
            get
            {
                return _postOutfitRepository ??= new PostOutfitRepository(_context);
            }
        }

        public IOutfitRepository OutfitRepository
        {
            get
            {
                return _outfitRepository ??= new OutfitRepository(_context);
            }
        }

        public IAISettingRepository AISettingRepository
        {
            get
            {
                return _aiSettingRepository ??= new AISettingRepository(_context);
            }
        }

        public IJobRepository JobRepository
        {
            get
            {
                return _jobRepository ??= new JobRepository(_context);
            }
        }

        public ILikePostRepository LikePostRepository
        {
            get
            {
                return _likePostRepository ??= new LikePostRepository(_context);
            }
        }

        public ICommentPostRepository CommentPostRepository
        {
            get
            {
                return _commentPostRepository ??= new CommentPostRepository(_context);
            }
        }

        public IFollowerRepository FollowerRepository
        {
            get
            {
                return _followerRepository ??= new FollowerRepository(_context);
            }
        }

        public IUserOccasionRepository UserOccasionRepository
        {
            get
            {
                return _userOccasionRepository ??= new UserOccasionRepository(_context);
            }
        }

        public IOutfitItemRepository OutfitItemRepository
        {
            get
            {
                return _outfitItemRepository ??= new OutfitItemRepository(_context);
            }
        }

        public IOutfitUsageHistoryRepository OutfitUsageHistoryRepository
        {
            get
            {
                return _outfitUsageHistoryRepository ??= new OutfitUsageHistoryRepository(_context);
            }
        }

        public ICollectionRepository CollectionRepository
        {
            get
            {
                return _collectionRepository ??= new CollectionRepository(_context);
            }
        }

        public ICollectionOutfitRepository CollectionOutfitRepository
        {
            get
            {
                return _collectionOutfitRepository ??= new CollectionOutfitRepository(_context);
            }
        }

        public ICommentCollectionRepository CommentCollectionRepository
        {
            get
            {
                return _commentCollectionRepository ??= new CommentCollectionRepository(_context);
            }
        }

        public ILikeCollectionRepository LikeCollectionRepository
        {
            get
            {
                return _likeCollectionRepository ??= new LikeCollectionRepository(_context);
            }
        }

        public ISaveCollectionRepository SaveCollectionRepository
        {
            get
            {
                return _saveCollectionRepository ??= new SaveCollectionRepository(_context);
            }
        }

        public IReportCommunityRepository ReportCommunityRepository
        {
            get
            {
                return _reportCommunityRepository ??= new ReportCommunityRepository(_context);
            }
        }

        public IReportReporterRepository ReportReporterRepository
        {
            get
            {
                return _reportReporterRepository ??= new ReportReporterRepository(_context);
            }
        }

        public IUserSuspensionRepository UserSuspensionRepository
        {
            get
            {
                return _userSuspensionRepository ??= new UserSuspensionRepository(_context);
            }
        }

        public IUserViolationRepository UserViolationRepository
        {
            get
            {
                return _userViolationRepository ??= new UserViolationRepository(_context);
            }
        }

        public IUserSubscriptionRepository UserSubscriptionRepository
        {
            get
            {
                return _userSubscriptionRepository ??= new UserSubscriptionRepository(_context);
            }
        }

        public ISubscriptionPlanRepository SubscriptionPlanRepository
        {
            get
            {
                return _subscriptionPlanRepository ??= new SubscriptionPlanRepository(_context);
            }

        }

        public IUserSubscriptionTransactionRepository UserSubscriptionTransactionRepository
        {
            get
            {
                return _subscriptionTransactionRepository ??= new UserSubscriptionTransactionRepository(_context);
            }
        }

        public INotificationRepository NotificationRepository
        {
            get
            {
                return _notificationRepository ??= new NotificationRepository(_context);
            }
        }

        public IUserNotificationRepository UserNotificationRepository
        {
            get
            {
                return _userNotificationRepository ??= new UserNotificationRepository(_context);
            }
        }

        public IUserDeviceRepository UserDeviceRepository
        {
            get
            {
                return _userDeviceRepository ??= new UserDeviceRepository(_context);
            }
        }

        public IItemWornAtHistoryRepository ItemWornAtHistoryRepository
        {
            get
            {
                return _itemWornAtHistoryRepository ??= new ItemWornAtHistoryRepository(_context);
            }
        }

        public void Commit()
        {
            try
            {
                _context.SaveChanges();
                _transaction?.Commit();
            }
            catch (Exception)
            {
                _transaction?.Rollback();
                throw;
            }
            finally
            {
                _transaction?.Dispose();
            }
        }

        public void Dispose()
        {
            _context.Dispose();
        }

        public void Rollback()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
        }

        public int Save()
        {
            return _context.SaveChanges();
        }

        public Task SaveAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
