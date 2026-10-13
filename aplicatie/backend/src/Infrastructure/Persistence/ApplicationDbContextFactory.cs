using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace NutriScan.Infrastructure.Persistence;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        // Design-time connection string for generating migrations
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=nutriscan_db;Username=nutriscan_user;Password=nutriscan_secret_password",
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)
        );

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
