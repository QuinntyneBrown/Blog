using Blog.Api.Core;
using System.Collections.Generic;
using System.Linq;

namespace Blog.Api.Data
{
    public static class SeedData
    {
        public static void Seed(BlogDbContext context)
        {
            UserConfiguration.Seed(context);
            ContentConfiguration.Seed(context);
        }

        internal static class ContentConfiguration
        {
            internal static void Seed(BlogDbContext context)
            {
                foreach (var name in new List<string> { "Landing", "About", "Shell" })
                {
                    var entity = context.Contents.SingleOrDefault(x => x.Name == name);

                    if (entity == null)
                    {
                        context.Contents.Add(new() { Name = name });

                        context.SaveChanges();
                    }
                }

            }
        }

        internal static class UserConfiguration
        {
            internal static void Seed(BlogDbContext context)
            {
                var user = context.Users.SingleOrDefault(x => x.Username == "User");

                if (user == null)
                {
                    context.Users.Add(new("user", "password", new PasswordHasher()));

                    context.SaveChanges();
                }
            }
        }

    }
}
