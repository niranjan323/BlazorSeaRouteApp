//using SeaRouteModel.Models;
//using System.Collections.Generic;
//using System.Reflection.Emit;

//namespace SeaRouteWebApis.Context
//{
//    public class ApplicationDbContext : DbContext
//    {
//        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
//        {
//        }

//        public DbSet<CPorts> Ports { get; set; }
//        public DbSet<Country> Countries { get; set; }
//        public DbSet<GeoPoints> GeoPoints { get; set; }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            base.OnModelCreating(modelBuilder);

//            modelBuilder.Entity<CPorts>()
//                .HasOne(p => p.Country)
//                .WithMany(c => c.Ports)
//                .HasForeignKey(p => p.CountryCode)
//                .HasPrincipalKey(c => c.CountryCode);

//            modelBuilder.Entity<CPorts>()
//                .HasOne(p => p.GeoPoint)
//                .WithOne()
//                .HasForeignKey<CPorts>(p => p.PointId);

//            // Additional configuration as needed
//        }
//    }
//}
