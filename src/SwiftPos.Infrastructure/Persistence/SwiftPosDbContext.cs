using Microsoft.EntityFrameworkCore;
using SwiftPos.Domain.Catalog;
using SwiftPos.Domain.Pos;
using SwiftPos.Domain.Stores;
using SwiftPos.Domain.Tenants;
using SwiftPos.Domain.Users;

namespace SwiftPos.Infrastructure.Persistence;

public sealed class SwiftPosDbContext(DbContextOptions<SwiftPosDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<CashRegisterSession> CashRegisterSessions => Set<CashRegisterSession>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<Payment> Payments => Set<Payment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.ToTable("tenants");
            entity.HasKey(tenant => tenant.Id);
            entity.Property(tenant => tenant.Name).HasMaxLength(160).IsRequired();
            entity.Property(tenant => tenant.Slug).HasMaxLength(80).IsRequired();
            entity.Property(tenant => tenant.Status).HasMaxLength(32).IsRequired();
            entity.HasIndex(tenant => tenant.Slug).IsUnique();
        });

        modelBuilder.Entity<Store>(entity =>
        {
            entity.ToTable("stores");
            entity.HasKey(store => store.Id);
            entity.Property(store => store.Name).HasMaxLength(160).IsRequired();
            entity.Property(store => store.Code).HasMaxLength(40).IsRequired();
            entity.Property(store => store.Address).HasMaxLength(240);
            entity.HasIndex(store => new { store.TenantId, store.Code }).IsUnique();
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(store => store.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("roles");
            entity.HasKey(role => role.Id);
            entity.Property(role => role.Name).HasMaxLength(80).IsRequired();
            entity.Property(role => role.Code).HasMaxLength(40).IsRequired();
            entity.HasIndex(role => new { role.TenantId, role.Code }).IsUnique();
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(role => role.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.FullName).HasMaxLength(160).IsRequired();
            entity.Property(user => user.Email).HasMaxLength(180).IsRequired();
            entity.Property(user => user.PasswordHash).HasMaxLength(320).IsRequired();
            entity.HasIndex(user => new { user.TenantId, user.Email }).IsUnique();
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(user => user.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Store>()
                .WithMany()
                .HasForeignKey(user => user.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Role>()
                .WithMany()
                .HasForeignKey(user => user.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Category>(entity =>
        {
            entity.ToTable("categories");
            entity.HasKey(category => category.Id);
            entity.Property(category => category.Name).HasMaxLength(120).IsRequired();
            entity.Property(category => category.Description).HasMaxLength(320);
            entity.HasIndex(category => new { category.TenantId, category.Name }).IsUnique();
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(category => category.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("products");
            entity.HasKey(product => product.Id);
            entity.Property(product => product.Sku).HasMaxLength(80).IsRequired();
            entity.Property(product => product.Barcode).HasMaxLength(80);
            entity.Property(product => product.Name).HasMaxLength(180).IsRequired();
            entity.Property(product => product.Description).HasMaxLength(500);
            entity.Property(product => product.Price).HasPrecision(12, 2);
            entity.Property(product => product.TaxRate).HasPrecision(5, 4);
            entity.Property(product => product.Cost).HasPrecision(12, 2);
            entity.HasIndex(product => new { product.TenantId, product.Sku }).IsUnique();
            entity.HasIndex(product => new { product.TenantId, product.Name });
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(product => product.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Category>()
                .WithMany()
                .HasForeignKey(product => product.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<CashRegisterSession>(entity =>
        {
            entity.ToTable("cash_register_sessions");
            entity.HasKey(session => session.Id);
            entity.Property(session => session.Status).HasMaxLength(32).IsRequired();
            entity.Property(session => session.OpeningCashAmount).HasPrecision(12, 2);
            entity.Property(session => session.ClosingCashAmount).HasPrecision(12, 2);
            entity.HasIndex(session => new { session.TenantId, session.StoreId, session.Status });
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(session => session.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Store>()
                .WithMany()
                .HasForeignKey(session => session.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(session => session.OpenedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(session => session.ClosedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("orders");
            entity.HasKey(order => order.Id);
            entity.Property(order => order.Mode).HasMaxLength(32).IsRequired();
            entity.Property(order => order.Status).HasMaxLength(32).IsRequired();
            entity.Property(order => order.Notes).HasMaxLength(500);
            entity.Property(order => order.Subtotal).HasPrecision(12, 2);
            entity.Property(order => order.TaxTotal).HasPrecision(12, 2);
            entity.Property(order => order.Total).HasPrecision(12, 2);
            entity.HasIndex(order => new { order.TenantId, order.StoreId, order.Status });
            entity.HasIndex(order => new { order.TenantId, order.CreatedAt });
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(order => order.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Store>()
                .WithMany()
                .HasForeignKey(order => order.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(order => order.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CashRegisterSession>()
                .WithMany()
                .HasForeignKey(order => order.CashRegisterSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<OrderItem>(entity =>
        {
            entity.ToTable("order_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProductNameSnapshot).HasMaxLength(180).IsRequired();
            entity.Property(item => item.ProductSkuSnapshot).HasMaxLength(80).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(12, 2);
            entity.Property(item => item.TaxRate).HasPrecision(5, 4);
            entity.Property(item => item.Subtotal).HasPrecision(12, 2);
            entity.Property(item => item.TaxTotal).HasPrecision(12, 2);
            entity.Property(item => item.Total).HasPrecision(12, 2);
            entity.Property(item => item.Notes).HasMaxLength(500);
            entity.HasIndex(item => new { item.TenantId, item.OrderId });
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(item => item.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Order>()
                .WithMany()
                .HasForeignKey(item => item.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.ToTable("sales");
            entity.HasKey(sale => sale.Id);
            entity.Property(sale => sale.Status).HasMaxLength(32).IsRequired();
            entity.Property(sale => sale.Subtotal).HasPrecision(12, 2);
            entity.Property(sale => sale.TaxTotal).HasPrecision(12, 2);
            entity.Property(sale => sale.Total).HasPrecision(12, 2);
            entity.HasIndex(sale => new { sale.TenantId, sale.StoreId, sale.CompletedAt });
            entity.HasIndex(sale => new { sale.TenantId, sale.OrderId }).IsUnique();
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(sale => sale.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Store>()
                .WithMany()
                .HasForeignKey(sale => sale.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Order>()
                .WithMany()
                .HasForeignKey(sale => sale.OrderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(sale => sale.SoldByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CashRegisterSession>()
                .WithMany()
                .HasForeignKey(sale => sale.CashRegisterSessionId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SaleItem>(entity =>
        {
            entity.ToTable("sale_items");
            entity.HasKey(item => item.Id);
            entity.Property(item => item.ProductNameSnapshot).HasMaxLength(180).IsRequired();
            entity.Property(item => item.ProductSkuSnapshot).HasMaxLength(80).IsRequired();
            entity.Property(item => item.UnitPrice).HasPrecision(12, 2);
            entity.Property(item => item.TaxRate).HasPrecision(5, 4);
            entity.Property(item => item.Subtotal).HasPrecision(12, 2);
            entity.Property(item => item.TaxTotal).HasPrecision(12, 2);
            entity.Property(item => item.Total).HasPrecision(12, 2);
            entity.HasIndex(item => new { item.TenantId, item.SaleId });
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(item => item.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Sale>()
                .WithMany()
                .HasForeignKey(item => item.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Product>()
                .WithMany()
                .HasForeignKey(item => item.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.ToTable("payments");
            entity.HasKey(payment => payment.Id);
            entity.Property(payment => payment.Method).HasMaxLength(32).IsRequired();
            entity.Property(payment => payment.Status).HasMaxLength(32).IsRequired();
            entity.Property(payment => payment.Amount).HasPrecision(12, 2);
            entity.Property(payment => payment.Reference).HasMaxLength(160);
            entity.HasIndex(payment => new { payment.TenantId, payment.StoreId, payment.PaidAt });
            entity.HasIndex(payment => new { payment.TenantId, payment.SaleId });
            entity.HasOne<Tenant>()
                .WithMany()
                .HasForeignKey(payment => payment.TenantId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Store>()
                .WithMany()
                .HasForeignKey(payment => payment.StoreId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Sale>()
                .WithMany()
                .HasForeignKey(payment => payment.SaleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        SeedDemoData(modelBuilder);
    }

    private static void SeedDemoData(ModelBuilder modelBuilder)
    {
        var createdAt = new DateTimeOffset(2026, 05, 06, 0, 0, 0, TimeSpan.Zero);
        var tenantId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var storeId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var ownerRoleId = Guid.Parse("33333333-3333-3333-3333-333333333331");
        var adminRoleId = Guid.Parse("33333333-3333-3333-3333-333333333332");
        var cashierRoleId = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var adminUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        var coffeeCategoryId = Guid.Parse("55555555-5555-5555-5555-555555555551");
        var bakeryCategoryId = Guid.Parse("55555555-5555-5555-5555-555555555552");

        modelBuilder.Entity<Tenant>().HasData(new Tenant
        {
            Id = tenantId,
            Name = "SwiftPOS Demo",
            Slug = "swiftpos-demo",
            Status = TenantStatuses.Active,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        });

        modelBuilder.Entity<Store>().HasData(new Store
        {
            Id = storeId,
            TenantId = tenantId,
            Name = "Sucursal Demo",
            Code = "MAIN",
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        });

        modelBuilder.Entity<Role>().HasData(
            new Role { Id = ownerRoleId, TenantId = tenantId, Name = "Owner", Code = RoleCodes.Owner, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Role { Id = adminRoleId, TenantId = tenantId, Name = "Admin", Code = RoleCodes.Admin, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Role { Id = cashierRoleId, TenantId = tenantId, Name = "Cashier", Code = RoleCodes.Cashier, CreatedAt = createdAt, UpdatedAt = createdAt });

        modelBuilder.Entity<User>().HasData(new User
        {
            Id = adminUserId,
            TenantId = tenantId,
            StoreId = storeId,
            RoleId = adminRoleId,
            FullName = "Admin Demo",
            Email = "admin@swiftpos.local",
            PasswordHash = "pbkdf2-sha256:100000:U3dpZnRQT1MgZGVtbyBzYWx0IHYx:vY8JJtZHeQz+GKC0pyPN71oEoiAeyXk3MXmEi3VxptM=",
            IsActive = true,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        });

        modelBuilder.Entity<Category>().HasData(
            new Category { Id = coffeeCategoryId, TenantId = tenantId, Name = "Coffee", IsActive = true, CreatedAt = createdAt, UpdatedAt = createdAt },
            new Category { Id = bakeryCategoryId, TenantId = tenantId, Name = "Bakery", IsActive = true, CreatedAt = createdAt, UpdatedAt = createdAt });

        modelBuilder.Entity<Product>().HasData(
            new Product
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666661"),
                TenantId = tenantId,
                CategoryId = coffeeCategoryId,
                Sku = "COF-001",
                Name = "Espresso",
                Price = 3.50m,
                TaxRate = 0.1600m,
                IsActive = true,
                TrackStock = false,
                Stock = 0,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            },
            new Product
            {
                Id = Guid.Parse("66666666-6666-6666-6666-666666666662"),
                TenantId = tenantId,
                CategoryId = bakeryCategoryId,
                Sku = "BAK-001",
                Name = "Croissant",
                Price = 3.25m,
                TaxRate = 0.1600m,
                IsActive = true,
                TrackStock = true,
                Stock = 24,
                CreatedAt = createdAt,
                UpdatedAt = createdAt
            });
    }
}
