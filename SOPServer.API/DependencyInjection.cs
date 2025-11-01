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

            // ========== POST MANAGEMENT ==========
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<IPostService, PostService>();

            services.AddScoped<IPostImageRepository, PostImageRepository>();

            services.AddScoped<IHashtagRepository, HashtagRepository>();
            services.AddScoped<IPostHashtagsRepository, PostHashtagsRepository>();

            // ========== POST INTERACTIONS ==========
            services.AddScoped<ILikePostRepository, LikePostRepository>();
            services.AddScoped<ILikePostService, LikePostService>();

            services.AddScoped<ICommentPostRepository, CommentPostRepository>();
            services.AddScoped<ICommentPostService, CommentPostService>();

            // ========== FOLLOWER MANAGEMENT ==========
            services.AddScoped<IFollowerRepository, FollowerRepository>();
            services.AddScoped<IFollowerService, FollowerService>();

            // ========== AI SETTINGS ==========
            services.AddScoped<IAISettingRepository, AISettingRepository>();
            services.AddScoped<IAISettingService, AISettingService>();

            // ========== EXTERNAL SERVICES ==========
            services.AddScoped<IGeminiService, GeminiService>();
            services.AddScoped<IRedisService, RedisService>();
            services.AddScoped<IMinioService, MinioService>();
            services.AddScoped<IQdrantService, QDrantService>();

            // ========== EMAIL SERVICES ==========
            services.AddScoped<IMailService, MailService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IOtpService, OtpService>();

            return services;
        }
    }
}
