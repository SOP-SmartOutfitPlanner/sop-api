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
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            services.AddScoped<IItemRepository, ItemRepository>();

            services.AddScoped<ICategoryRepository, CategoryRepository>();
            services.AddScoped<ICategoryService, CategoryService>();

            services.AddScoped<IItemOccasionRepository, ItemOccasionRepository>();

            services.AddScoped<IItemSeasonRepository, ItemSeasonRepository>();

            services.AddScoped<IItemStyleRepository, ItemStyleRepository>();
            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IPostImageRepository, PostImageRepository>();
            services.AddScoped<IPostRepository, PostRepository>();
            services.AddScoped<IHashtagRepository, HashtagRepository>();
            services.AddScoped<IPostHashtagsRepository, PostHashtagsRepository>();

            services.AddScoped<IOutfitRepository, OutfitRepository>();

            // Services
            services.AddScoped<IGeminiService, GeminiService>();
            services.AddScoped<IItemService, ItemService>();
            services.AddScoped<IPostService, PostService>();
            services.AddScoped<IOutfitService, OutfitService>();

            services.AddScoped<IRedisService, RedisService>();
            services.AddScoped<IMailService, MailService>();
            services.AddScoped<IEmailTemplateService, EmailTemplateService>();
            services.AddScoped<IOtpService, OtpService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IMinioService, MinioService>();

            return services;
        }
    }
}
