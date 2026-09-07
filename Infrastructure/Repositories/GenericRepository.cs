using Application.Repositories;
using Infrastructure.Context;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        //Dependancy Injection
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public GenericRepository(AppDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public IQueryable<T> GetAll()
        {
            return _dbSet.AsQueryable();
        }

        public IQueryable<T> GetAllReadOnly()
        {
            return _dbSet.AsNoTracking();
        }

        public IEnumerable<T> GetAllList()
        {
            return _dbSet.ToList();
        }

        public void Delete(T input)
        {
            _dbSet.Remove(input);
        }

        public void DeleteRange(IEnumerable<T> input)
        {
            _dbSet.RemoveRange(input);
        }

        public void Update(T input)
        {
            _dbSet.Update(input);
        }

        public void UpdateRange(IEnumerable<T> input)
        {
            _dbSet.UpdateRange(input);
        }

        public T? GetById(int id)
        {
            return _dbSet.Find(id);
        }

        public async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public void Insert(T input)
        {
            _dbSet.Add(input);
        }

        public async Task InsertAsync(T input)
        {
            await _dbSet.AddAsync(input);
        }

        public void InsertRange(IEnumerable<T> input)
        {
            _dbSet.AddRange(input);
        }

        public async Task InsertRangeAsync(IEnumerable<T> input)
        {
            await _dbSet.AddRangeAsync(input);
        }

        public void SaveChanges()
        {
            _context.SaveChanges();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
