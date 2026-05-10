using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BenLanSystem.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer("Server=localhost\\SQLEXPRESS;Database=BenLanDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;MultipleActiveResultSets=true");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
