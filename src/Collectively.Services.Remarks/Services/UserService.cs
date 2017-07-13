﻿using System.Threading.Tasks;
using Collectively.Services.Remarks.Domain;
using Collectively.Services.Remarks.Extensions;
using Collectively.Services.Remarks.Repositories;
using NLog;

namespace Collectively.Services.Remarks.Services
{
    public class UserService : IUserService
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task CreateIfNotFoundAsync(string userId, string name, string role)
        {
            var user = await _userRepository.GetByUserIdAsync(userId);
            if (user.HasValue)
                return;

            Logger.Debug($"User not found, creating new one. userId: {userId}, name: {name}, role: {role}.");
            user = new User(userId, name, role);
            await _userRepository.AddAsync(user.Value);
        }

        public async Task UpdateNameAsync(string userId, string name)
        {
            Logger.Debug($"Update userName, userId:{userId}, name:{name}");
            var user = await _userRepository.GetOrFailAsync(userId);
            user.SetName(name);
            await _userRepository.UpdateAsync(user);
        }
    }
}