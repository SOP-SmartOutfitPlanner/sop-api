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
        private IItemGoalRepository _itemGoalRepository;
        private IItemOccasionRepository _itemOccasionRepository;
        private IItemSeasonRepository _itemSeasonRepository;
        private IItemStyleRepository _itemStyleRepository;
        private IUserRepository _userRepository;

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

        public IItemGoalRepository ItemGoalRepository
        {
            get
            {
                return _itemGoalRepository ??= new ItemGoalRepository(_context);
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
