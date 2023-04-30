using Microsoft.EntityFrameworkCore;
using ZNetCS.AspNetCore.Logging.EntityFrameworkCore;

namespace Garage.Door.Opener
{
    public sealed class MyDbContext : DbContext
    {
        public MyDbContext(DbContextOptions options)
            : base(options)
        {
        }

        public DbSet<Log> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // build default model.
            LogModelBuilderHelper.Build(modelBuilder.Entity<Log>());

            // real relation database can map table:
            modelBuilder.Entity<Log>().ToTable("Log");
        }
    }
}