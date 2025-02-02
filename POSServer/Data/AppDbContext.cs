using Microsoft.EntityFrameworkCore;
using POSServer.Models;

namespace POSServer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Users> Users { get; set; } = null!;
        public DbSet<Products> Products { get; set; } = null!;
        public DbSet<Category> Category { get; set; } = null!;
        public DbSet<Inventory> Inventory { get; set; } = null!;
        public DbSet<Locations> Locations { get; set; } = null!;
        public DbSet<Orders> Orders { get; set; } = null!;
        public DbSet<OrderProducts> OrderProducts { get; set; } = null!;
        public DbSet<CashDrawer> CashDrawer { get; set; } = null!;
        public DbSet<Discounts> Discounts { get; set; } = null!;
        public DbSet<Suppliers> Suppliers { get; set; } = null!;
        public DbSet<StockIn> StockIn { get; set; } = null!;
        public DbSet<StockAdjustments> StockAdjustments { get; set; } = null!;
        public DbSet<Customers> Customers { get; set; } = null!;

        public DbSet<Expense> Expense { get; set; } = null!;
        public DbSet<Withdrawals> Withdrawals { get; set; } = null!;
        public DbSet<InitialCash> InitialCash { get; set; } = null!;


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Users>(entity =>
            {
                entity.Property(u => u.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<Products>(entity =>
            {
                // Set decimal precision to 18 digits with 2 after the decimal point
                entity.Property(p => p.SupplierPrice).HasPrecision(18, 2);
                entity.Property(p => p.RetailPrice).HasPrecision(18, 2);
                entity.Property(p => p.WholesalePrice).HasPrecision(18, 2);
                entity.Property(n => n.Name)
                      .HasMaxLength(100)
                      .IsRequired();
                entity.Property(c => c.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<Category>(entity =>
            {
                // Define the primary key
                entity.HasKey(c => c.CategoryId);

                // Set decimal precision to 18 digits with 2 after the decimal point
                entity.Property(c => c.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Additional configurations can be added here as needed
                // Example: Max length for Name
                entity.Property(c => c.Name)
                      .HasMaxLength(100)
                      .IsRequired();

                // If there are relationships, configure them here
                // Example: One-to-Many relationship with Products
                entity.HasMany(c => c.Products)
                      .WithOne(p => p.Category)
                      .HasForeignKey(p => p.CategoryId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Inventory>(entity =>
            {
                // Define the primary key
                entity.HasKey(i => i.InventoryId);

                // Set decimal precision to 18 digits with 2 after the decimal point
                entity.Property(i => i.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

            });


            modelBuilder.Entity<Locations>(entity =>
            {
                // Define the primary key
                entity.HasKey(l => l.LocationId);

                entity.Property(c => c.Name)
                    .HasMaxLength(100)
                    .IsRequired();

                // Set decimal precision to 18 digits with 2 after the decimal point
                entity.Property(l => l.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasMany(c => c.Inventory)
                      .WithOne(p => p.Locations)
                      .HasForeignKey(p => p.LocationId)
                      .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<Orders>(entity =>
            {
                // Define the primary key for the Orders entity
                entity.HasKey(o => o.OrderId);
                entity.Property(p => p.TotalAmount).HasPrecision(18, 2);
                // Add other configurations as needed, e.g., default values, relationships, etc.
                entity.Property(o => o.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(u => u.Users)
                    .WithMany(o => o.Orders)
                    .HasForeignKey(u => u.UserId);

                entity.HasOne(l => l.Location)
                  .WithMany(o => o.Orders)
                  .HasForeignKey(l => l.LocationId);

                entity.HasOne(c => c.Customers)
                 .WithMany(o => o.Orders)
                 .HasForeignKey(c => c.CustomerId)
                 .OnDelete(DeleteBehavior.SetNull);

                //entity.HasOne(c => c.Customers)
                //  .WithMany(o => o.Orders)
                //  .HasForeignKey(c => c.CustomerId)
                //  .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<OrderProducts>(entity =>
            {
                entity.HasKey(op => new { op.OrderId, op.ProductId });
                entity.Property(p => p.SubTotal).HasPrecision(18, 2);
                entity.HasOne(op => op.Orders)
                    .WithMany(o => o.OrderProducts)
                    .HasForeignKey(op => op.OrderId);

                entity.HasOne(op => op.Products)
                    .WithMany(p => p.OrderProducts)
                    .HasForeignKey(op => op.ProductId);
            });

            modelBuilder.Entity<CashDrawer>(entity =>
            {
                entity.HasKey(d => d.DrawerId);
                entity.Property(d => d.InitialCash).HasPrecision(18, 2);
                entity.Property(d => d.TotalSales).HasPrecision(18, 2);
                entity.Property(d => d.Withdrawals).HasPrecision(18, 2);
                entity.Property(d => d.Expense).HasPrecision(18, 2);
                entity.Property(d => d.DrawerCash).HasPrecision(18, 2);
                entity.Property(d => d.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
                entity.Property(d => d.TimeStart).HasDefaultValueSql("CURRENT_TIMESTAMP");


                entity.HasOne(u => u.Users)
                    .WithMany(cd => cd.CashDrawer)
                    .HasForeignKey(cd => cd.UserId);

                entity.HasOne(u => u.Locations)
                    .WithMany(cd => cd.CashDrawer)
                    .HasForeignKey(cd => cd.LocationId);
            });

            modelBuilder.Entity<Discounts>(entity =>
            {
                entity.HasKey(d => d.DiscountId);
                entity.Property(d => d.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<Suppliers>(entity =>
            {
                entity.HasKey(s => s.SupplierId);
                entity.Property(c => c.Name)
                     .HasMaxLength(50)
                     .IsRequired();
                entity.Property(c => c.Address)
                     .HasMaxLength(150)
                     .IsRequired();
                entity.Property(c => c.ContactPerson)
                     .HasMaxLength(50)
                     .IsRequired();
                entity.Property(c => c.ContactNo)
                    .HasMaxLength(50)
                    .IsRequired();
                entity.Property(s => s.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });


            modelBuilder.Entity<Customers>(entity =>
            {
                entity.HasKey(c => c.CustomerId);
                entity.Property(c => c.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");
            });

            modelBuilder.Entity<StockIn>(entity =>
            {
               entity.HasKey(s => s.StockId);
               entity.Property(s => s.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(u => u.Users)
                    .WithMany(s => s.StockIn)
                    .HasForeignKey(u => u.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(l => l.Locations)
                  .WithMany(s => s.StockIn)
                  .HasForeignKey(l => l.LocationId)
                  .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Products)
                  .WithMany(s => s.StockIn)
                  .HasForeignKey(p => p.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<StockAdjustments>(entity =>
            {
                entity.HasKey(s => s.AdjustmentId);
                entity.Property(s => s.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(u => u.Users)
                    .WithMany(s => s.StockAdjustments)
                    .HasForeignKey(u => u.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(l => l.Locations)
                  .WithMany(s => s.StockAdjustments)
                  .HasForeignKey(l => l.LocationId)
                  .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(p => p.Products)
                  .WithMany(s => s.StockAdjustments)
                  .HasForeignKey(p => p.ProductId)
                  .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Expense>(entity =>
            {
                entity.HasKey(s => s.ExpenseId);
                entity.Property(s => s.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(u => u.CashDrawer)
                    .WithMany(s => s.Expenses)
                    .HasForeignKey(u => u.DrawerId)
                  .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<Withdrawals>(entity =>
            {
                entity.HasKey(s => s.WithdrawalId);
                entity.Property(s => s.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(u => u.CashDrawer)
                    .WithMany(s => s.Withdrawal)
                    .HasForeignKey(u => u.DrawerId)
                  .OnDelete(DeleteBehavior.Cascade);

            });

            modelBuilder.Entity<InitialCash>(entity =>
            {
                entity.HasKey(s => s.InitialCashId);
                entity.Property(s => s.DateCreated).HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(u => u.CashDrawer)
                    .WithMany(s => s.AdditionalInitialCash)
                    .HasForeignKey(u => u.DrawerId)
                  .OnDelete(DeleteBehavior.Cascade);

            });
        }
    }
}
