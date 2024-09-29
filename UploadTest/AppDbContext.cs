using Microsoft.EntityFrameworkCore;

namespace UploadTest
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Text> Texts { get; set; }
        public DbSet<Image> Images { get; set; }
    }
}
