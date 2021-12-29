using System;
using Blog.Api.Models;

namespace Blog.Api.Features
{
    public static class UserExtensions
    {
        public static UserDto ToDto(this User user)
        {
            return new ()
            {
                UserId = user.UserId
            };
        }
        
    }
}
