using Application.Repositories;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace Application.Services.DemoUserSeederService
{
    // Adds one throwaway user with random data. Shared by the plain BackgroundService demo
    // and the Hangfire recurring job demo, so both trigger the exact same insert.
    public class DemoUserSeederService : IDemoUserSeederService
    {
        private readonly IGenericRepository<User> _userRepository;
        private readonly IPasswordHasher<User> _passwordHasher;

        public DemoUserSeederService(IGenericRepository<User> userRepository, IPasswordHasher<User> passwordHasher)
        {
            _userRepository = userRepository;
            _passwordHasher = passwordHasher;
        }

        public async Task AddRandomUserAsync()
        {
            var suffix = Guid.NewGuid().ToString("N")[..8];

            var user = new User
            {
                Name = $"Demo User {suffix}",
                Email = $"demo.{suffix}@tag.local",
                PhoneNumber = "0700000000",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            user.Password = _passwordHasher.HashPassword(user, "Demo@123");

            await _userRepository.InsertAsync(user);
            await _userRepository.SaveChangesAsync();
        }
    }
}
