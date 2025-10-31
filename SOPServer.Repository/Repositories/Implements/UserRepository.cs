using SOPServer.Repository.DBContext;
using SOPServer.Repository.Entities;
using SOPServer.Repository.Repositories.Generic;
using SOPServer.Repository.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SOPServer.Repository.Repositories.Implements
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        private readonly SOPServerContext _context;

        public UserRepository(SOPServerContext context) : base(context)
        {
            _context = context;
        }
        
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users.Where(x => x.Email == email).FirstOrDefaultAsync();
        }

        public async Task<int> GetUserCountByMonth(int currentMonth, int currentYear)
        {
            return await _context.Users.Where(x => x.CreatedDate.Month == currentMonth && x.CreatedDate.Year == currentYear && !x.IsDeleted ).CountAsync();
        }

        public async Task<User?> GetUserProfileByIdAsync(long userId)
        {
            return await _context.Users
                .Include(u => u.Job)
                .Include(u => u.UserStyles)
                    .ThenInclude(us => us.Style)
                .Where(x => x.Id == userId && !x.IsDeleted)
                .FirstOrDefaultAsync();
        }

        public async Task<User?> GetByIdAsync(long id)
        {
            return await _context.Users
                .Include(u => u.Job)
                .Include(u => u.UserStyles)
                    .ThenInclude(us => us.Style)
                .Include(u => u.Items)
                    .ThenInclude(i => i.Category)
                .Include(u => u.Items)
                    .ThenInclude(i => i.ItemStyles)
                        .ThenInclude(ist => ist.Style)
                .Include(u => u.Items)
                    .ThenInclude(i => i.ItemOccasions)
                        .ThenInclude(io => io.Occasion)
                .Include(u => u.Items)
                    .ThenInclude(i => i.ItemSeasons)
                        .ThenInclude(isea => isea.Season)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.PostImages)
                .Include(u => u.Posts)
                    .ThenInclude(p => p.PostHashtags)
                        .ThenInclude(ph => ph.Hashtag)
                .Include(u => u.Outfits)
                    .ThenInclude(o => o.OutfitItems)
                        .ThenInclude(oi => oi.Item)
                .Include(u => u.OutfitUsageHistories)
                    .ThenInclude(ouh => ouh.Outfit)
                .Include(u => u.OutfitUsageHistories)
                    .ThenInclude(ouh => ouh.UserOccasion)
                .Include(u => u.UserOccasions)
                    .ThenInclude(uo => uo.Occasion)
                .Include(u => u.LikePosts)
                    .ThenInclude(lp => lp.Post)
                .Include(u => u.CommentPosts)
                    .ThenInclude(cp => cp.Post)
                .AsSplitQuery()
                .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
