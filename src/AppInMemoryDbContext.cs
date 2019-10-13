using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SteamOpenIdConnectProvider
{
    // This is completely in-memory, we do not need a persistent store.
    public class AppInMemoryDbContext : IdentityDbContext<IdentityUser>
    {
        public AppInMemoryDbContext(DbContextOptions<AppInMemoryDbContext> options)
            : base(options)
        {
        }
    }
}
