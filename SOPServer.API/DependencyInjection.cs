using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Repository.Repositories.Interfaces;
using SOPServer.Repository.Repositories.Implements;
using SOPServer.Service.SettingModels; // added for Configure

namespace SOPServer.API
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfractstructure(this IServiceCollection services, IConfiguration config)
        {
            // ========== CONFIGURATION SETTINGS ==========
            services.Configure<NewsfeedSettings>(config.GetSection("NewsfeedSettings"));
            services.Configure<QDrantClientSettings>(config.GetSection("QDrantSettings"));

            // ========== UNIT OF WORK ==========
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ========== USER MANAGEMENT ==========
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IUserService, UserService>();

            services.AddScoped<IJobRepository, JobRepository>();
            services.AddScoped<IJobService, JobService>();

            // ========== STYLE MANAGEMENT ==========
            services.AddScoped<IStyleRepository, StyleRepository>();
            services.AddScoped<IStyleService, StyleService>();

            services.AddScoped<IItemStyleRepository, ItemStyleRepository>();

            // ========== CATEGORY MANAGEMENT ==========
            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICategoryService, CategoryService>();

            // ========== OCCASION MANAGEMENT ==========
            services.AddScoped<IOccasionRepository, OccasionRepository>();
            services.AddScoped<IOccasionService, OccasionService>();

            services.AddScoped<IItemOccasionRepository, ItemOccasionRepository>();

            // ========== USER OCCASION MANAGEMENT ==========
            services.AddScoped<IUserOccasionRepository, UserOccasionRepository>();
            services.AddScoped<IUserOccasionService, UserOccasionService>();

            // ========== SEASON MANAGEMENT ==========
            services.AddScoped<ISeasonRepository, SeasonRepository>();
            services.AddScoped<ISeasonService, SeasonService>();

            services.AddScoped<IItemSeasonRepository, ItemSeasonRepository>();

            // ========== ITEM MANAGEMENT ==========
            services.AddScoped<IItemRepository, ItemRepository>();
            services.AddScoped<IItemService, ItemService>();

            // ========== OUTFIT MANAGEMENT ==========
            services.AddScoped<IOutfitRepository, OutfitRepository>();
            services.AddScoped<IOutfitService, OutfitService>();

            services.AddScoped<IOutfitItemRepository, OutfitItemRepository>();
            services.AddScoped<IOutfitUsageHistoryRepository, OutfitUsageHistoryRepository>();

            // ========== COLLECTION MANAGEMENT ==========
            services.AddScoped<ICollectionRepository, CollectionRepository>();
            services.AddScoped<ICollectionService, CollectionService>();
            services.AddScoped<ICollectionOutfitRepository, CollectionOutfitRepository>();

            // ========== STYLIST DASHBOARD ==========
            services.AddScoped<IStylistDashboardService, StylistDashboardService>();

            // ========== ADMIN DASHBOARD ==========
            services.AddScoped<IAdminDashboardService, AdminDashboardService>();

            // ========== LIKE & SAVE COLLECTION ==========
            services.AddScoped<ILikeCollectionRepository, LikeCollectionRepository>();
            services.AddScoped<ILikeCollectionService, LikeCollectionService>();
            services.AddScoped<ICommentCollectionRepository, CommentCollectionRepository>();
            services.AddScoped<ICommentCollectionService, CommentCollectionService>();
            services.AddScoped<ISaveCollectionRepository, SaveCollectionRepository>();
            services.AddScoped<ISaveCollectionService, SaveCollectionService>();

            // ========== SAVE ITEMS AND OUTFITS ==========
            services.AddScoped<ISaveItemFromPostRepository, SaveItemFromPostRepository>();
            services.AddScoped<ISaveItemFromPostService, SaveItemFromPostService>();

            services.AddScoped<ISaveOutfitFromPostRepository, SaveOutfitFromPostRepository>();
            services.AddScoped<ISaveOutfitFromPostService, SaveOutfitFromPostService>();

            services.AddScoped<ISaveOutfitFromCollectionRepository, SaveOutfitFromCollectionRepository>();
            services.AddScoped<ISaveOutfitFromCollectionService, SaveOutfitFromCollectionService>();


            // Register comment collection dependencies
            services.AddScoped<ICommentCollectionRepository, CommentCollectionRepository>();
            services.AddScoped<ICommentCollectionService, CommentCollectionService>();

            // Register like collection dependencies
            services.AddScoped<ILikeCollectionRepository, LikeCollectionRepository>();
            services.AddScoped<ILikeCollectionService, LikeCollectionService>();

            // ========== POST MANAGEMENT ==========
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<IPostService, PostService>();

            services.AddScoped<IPostImageRepository, PostImageRepository>();
            services.AddScoped<IPostItemRepository, PostItemRepository>();
            services.AddScoped<IPostOutfitRepository, PostOutfitRepository>();

            services.AddScoped<IHashtagRepository, HashtagRepository>();
            services.AddScoped<IPostHashtagsRepository, PostHashtagsRepository>();

            // ========== POST INTERACTIONS ==========
            services.AddScoped<ILikePostRepository, LikePostRepository>();
            services.AddScoped<ILikePostService, LikePostService>();

            services.AddScoped<ICommentPostRepository, CommentPostRepository>();
            services.AddScoped<ICommentPostService, CommentPostService>();

            // ========== REPORT COMMUNITY ==========
            services.AddScoped<IReportCommunityRepository, ReportCommunityRepository>();
            services.AddScoped<IReportCommunityService, ReportCommunityService>();

            services.AddScoped<IUserSuspensionRepository, UserSuspensionRepository>();
            services.AddScoped<IUserViolationRepository, UserViolationRepository>();

            // ========== FOLLOWER MANAGEMENT ==========
            services.AddScoped<IFollowerRepository, FollowerRepository>();
            services.AddScoped<IFollowerService, FollowerService>();

            // ========== NOTIFICATION MANAGEMENT ==========
            services.AddScoped<INotificationRepository, NotificationRepository>();
            services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
            services.AddScoped<INotificationService, NotificationService>();

            services.AddScoped<IUserDeviceRepository, UserDeviceRepository>();
            services.AddScoped<IUserDeviceService, UserDeviceService>();

            // ========== AI SETTINGS ==========
            services.AddScoped<IAISettingRepository, AISettingRepository>();
            services.AddScoped<IAISettingService, AISettingService>();

            // ========== SUBSCRIPTION PLAN MANAGEMENT ==========
            services.AddScoped<ISubscriptionPlanRepository, SubscriptionPlanRepository>();
            services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();

            services.AddScoped<IUserSubscriptionRepository, UserSubscriptionRepository>();
            services.AddScoped<IUserSubscriptionService, UserSubscriptionService>();
            services.AddScoped<ISubscriptionLimitService, SubscriptionLimitService>();
            services.AddScoped<IBenefitUsageService, BenefitUsageService>();

            // Register the action filters for subscription management
            services.AddScoped<SOPServer.API.Attributes.SubscriptionLimitActionFilter>();
            services.AddScoped<SOPServer.API.Attributes.SubscriptionCreditRestoreFilter>();
            services.AddScoped<SOPServer.API.Attributes.ItemLimitActionFilter>();

            // ========== EXTERNAL SERVICES ==========
            services.AddScoped<IGeminiService, GeminiService>();
            services.AddScoped<IRedisService, RedisService>();
            services.AddScoped<IMinioService, MinioService>();
            services.AddScoped<IQdrantService, QDrantService>();
            services.AddScoped<IPayOSService, PayOSService>();

            // ========== LAZY SERVICES (for circular dependency resolution) ==========
            services.AddScoped<Lazy<IQdrantService>>(provider => new Lazy<IQdrantService>(() => provider.GetRequiredService<IQdrantService>()));

            // ========== EMAIL SERVICES ==========
            services.AddScoped<IMailService, MailService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IOtpService, OtpService>();

            // ========== WEATHER SERVICES ==========
            services.AddScoped<IWeatherService, WeatherService>();

            // ========== UTILITY SERVICES ==========
            services.AddScoped<SOPServer.Service.Utils.ContentVisibilityHelper>();

            return services;
        }
    }
}
