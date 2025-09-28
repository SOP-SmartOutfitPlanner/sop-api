﻿using SOPServer.Repository.DBContext;
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
            return await _context.Users.Include(x => x.Role).Where(x => x.Email == email).FirstOrDefaultAsync();
        }

        public async Task<int> GetUserCountByMonth(int currentMonth, int currentYear)
        {
            return await _context.Users.Where(x => x.CreatedDate.Month == currentMonth && x.CreatedDate.Year == currentYear && !x.IsDeleted ).CountAsync();
        }
    }
}
