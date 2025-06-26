using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SteamOpenIdConnectProvider.Domains.IdentityServer;

public sealed class AppInMemoryDbContext(DbContextOptions<AppInMemoryDbContext> options)
    : IdentityDbContext<IdentityUser>(options);
