using SOPServer.Repository.UnitOfWork;
using SOPServer.Service.Services.Implements;
using SOPServer.Service.Services.Interfaces;
using SOPServer.Repository.Repositories.Interfaces;
using SOPServer.Repository.Repositories.Implements;

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

            services.AddScoped<ISeasonRepository, SeasonRepository>();
            services.AddScoped<ISeasonService, SeasonService>();

            services.AddScoped<IItemGoalRepository, ItemGoalRepository>();

            services.AddScoped<IItemOccasionRepository, ItemOccasionRepository>();

            services.AddScoped<IItemSeasonRepository, ItemSeasonRepository>();

            services.AddScoped<IItemStyleRepository, ItemStyleRepository>();

            services.AddScoped<IUserRepository, UserRepository>();

            // Services
            services.AddScoped<IGeminiService, GeminiService>();
            services.AddScoped<IItemService, ItemService>();

            services.AddScoped<IFirebaseStorageService, FirebaseStorageService>();
            return services;
        }
    }
}
