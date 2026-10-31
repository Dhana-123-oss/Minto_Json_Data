using Microsoft.EntityFrameworkCore;
using Minto.Api.Domain;

namespace Minto.Api.Data;
public sealed class MintoDbContext(DbContextOptions<MintoDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<InventoryItem> Inventory => Set<InventoryItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartLine> CartLines => Set<CartLine>();
    public DbSet<OutboxMessage> Outbox => Set<OutboxMessage>();
    public DbSet<RiderProfile> Riders => Set<RiderProfile>();
    public DbSet<KycDocument> KycDocuments => Set<KycDocument>();
    public DbSet<ReplacementRequest> Replacements => Set<ReplacementRequest>();
    public DbSet<DeliveryProof> DeliveryProofs => Set<DeliveryProof>();
    public DbSet<OrderLocation> OrderLocations => Set<OrderLocation>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Refund> Refunds => Set<Refund>();
    public DbSet<InventoryBatch> InventoryBatches => Set<InventoryBatch>();
    public DbSet<StaffShift> StaffShifts => Set<StaffShift>();
    public DbSet<CashSettlement> CashSettlements => Set<CashSettlement>();
    public DbSet<Coupon> Coupons => Set<Coupon>();
    public DbSet<LoyaltyAccount> LoyaltyAccounts => Set<LoyaltyAccount>();
    public DbSet<PushSubscription> PushSubscriptions => Set<PushSubscription>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<StoreBranch> Stores => Set<StoreBranch>(); public DbSet<QualityCheck> QualityChecks => Set<QualityCheck>(); public DbSet<Handover> Handovers => Set<Handover>();
    public DbSet<RiderIssue> RiderIssues => Set<RiderIssue>(); public DbSet<RiderRating> RiderRatings => Set<RiderRating>(); public DbSet<VehicleReminder> VehicleReminders => Set<VehicleReminder>();
    public DbSet<CustomerAddress> CustomerAddresses => Set<CustomerAddress>(); public DbSet<ProductReview> ProductReviews => Set<ProductReview>(); public DbSet<CustomerSubscription> Subscriptions => Set<CustomerSubscription>();
    public DbSet<SupportMessage> SupportMessages => Set<SupportMessage>(); public DbSet<PaymentRecord> Payments => Set<PaymentRecord>(); public DbSet<Referral> Referrals => Set<Referral>(); public DbSet<CustomerPreference> CustomerPreferences => Set<CustomerPreference>(); public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
    public DbSet<Supplier> Suppliers => Set<Supplier>(); public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>(); public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>(); public DbSet<StockTransfer> StockTransfers => Set<StockTransfer>(); public DbSet<LoginHistory> LoginHistory => Set<LoginHistory>();
    public DbSet<OrderEvent> OrderEvents => Set<OrderEvent>(); public DbSet<StaffAssignment> StaffAssignments => Set<StaffAssignment>(); public DbSet<LeaveRequest> LeaveRequests => Set<LeaveRequest>(); public DbSet<PerformanceSnapshot> PerformanceSnapshots => Set<PerformanceSnapshot>(); public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<OtpChallenge> OtpChallenges => Set<OtpChallenge>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Product>().HasKey(x => x.Id); b.Entity<Product>().Property(x => x.Price).HasPrecision(12, 2);
        b.Entity<InventoryItem>().HasKey(x => x.ProductId); b.Entity<InventoryItem>().Property(x => x.Version).IsConcurrencyToken();
        b.Entity<Order>().HasKey(x => x.Id); b.Entity<Order>().HasIndex(x => x.IdempotencyKey).IsUnique(); b.Entity<Order>().Property(x => x.Total).HasPrecision(12, 2); b.Entity<Order>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.OrderId);
        b.Entity<OrderLine>().HasKey(x => x.Id); b.Entity<OrderLine>().Property(x => x.UnitPrice).HasPrecision(12, 2);
        b.Entity<Cart>().HasKey(x => x.Id); b.Entity<Cart>().HasIndex(x => x.CustomerId).IsUnique(); b.Entity<Cart>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.CartId);
        b.Entity<CartLine>().HasKey(x => x.Id); b.Entity<CartLine>().HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();
        b.Entity<OutboxMessage>().HasKey(x => x.Id);
        b.Entity<RiderProfile>().HasKey(x => x.Id); b.Entity<KycDocument>().HasKey(x => x.Id);
        b.Entity<ReplacementRequest>().HasKey(x => x.Id); b.Entity<DeliveryProof>().HasKey(x => x.Id);
        b.Entity<OrderLocation>().HasKey(x => x.Id); b.Entity<Complaint>().HasKey(x => x.Id); b.Entity<Refund>().HasKey(x => x.Id);
        b.Entity<InventoryBatch>().HasKey(x => x.Id); b.Entity<StaffShift>().HasKey(x => x.Id); b.Entity<CashSettlement>().HasKey(x => x.Id);
        b.Entity<Coupon>().HasKey(x => x.Id); b.Entity<Coupon>().HasIndex(x => x.Code).IsUnique();
        b.Entity<LoyaltyAccount>().HasKey(x => x.CustomerId); b.Entity<PushSubscription>().HasKey(x => x.Id); b.Entity<AuditEntry>().HasKey(x => x.Id);
        b.Entity<StoreBranch>().HasKey(x => x.Id); b.Entity<QualityCheck>().HasKey(x => x.Id); b.Entity<Handover>().HasKey(x => x.Id); b.Entity<RiderIssue>().HasKey(x => x.Id); b.Entity<RiderRating>().HasKey(x => x.Id); b.Entity<VehicleReminder>().HasKey(x => x.Id);
        b.Entity<CustomerAddress>().HasKey(x => x.Id); b.Entity<ProductReview>().HasKey(x => x.Id); b.Entity<CustomerSubscription>().HasKey(x => x.Id); b.Entity<SupportMessage>().HasKey(x => x.Id); b.Entity<PaymentRecord>().HasKey(x => x.Id); b.Entity<Referral>().HasKey(x => x.Id); b.Entity<CustomerPreference>().HasKey(x => x.CustomerId); b.Entity<WishlistItem>().HasKey(x => x.Id);
        b.Entity<Supplier>().HasKey(x => x.Id); b.Entity<PurchaseOrder>().HasKey(x => x.Id); b.Entity<PurchaseOrder>().HasMany(x => x.Lines).WithOne().HasForeignKey(x => x.PurchaseOrderId); b.Entity<PurchaseOrderLine>().HasKey(x => x.Id); b.Entity<StockTransfer>().HasKey(x => x.Id); b.Entity<LoginHistory>().HasKey(x => x.Id);
        b.Entity<OrderEvent>().HasKey(x => x.Id); b.Entity<StaffAssignment>().HasKey(x => x.Id); b.Entity<LeaveRequest>().HasKey(x => x.Id); b.Entity<PerformanceSnapshot>().HasKey(x => x.Id); b.Entity<Notification>().HasKey(x => x.Id);
        b.Entity<OtpChallenge>().HasKey(x => x.Id);
    }
}
public static class Seed
{
    public static async Task SeedAsync(MintoDbContext db)
    {
        if (await db.Products.AnyAsync()) return;
        var items = new[] { ("Tomato", "Fresh produce", 20m, 40), ("Onion", "Fresh produce", 28m, 40), ("Aavin Milk", "Dairy", 28m, 25), ("Potato", "Fresh produce", 24m, 35), ("Carrot", "Fresh produce", 32m, 30), ("Banana", "Fruits", 40m, 35), ("Bread", "Bakery", 30m, 20) };
        foreach (var x in items) { var p = new Product { Name = x.Item1, Category = x.Item2, Price = x.Item3, IsActive = true }; db.Products.Add(p); await db.SaveChangesAsync(); db.Inventory.Add(new InventoryItem { ProductId = p.Id, Available = x.Item4, Reserved = 0 }); }
        db.Stores.Add(new StoreBranch { Name = "Minto Fresh Hub", Address = "Aruppukkottai Main Road", Latitude = 9.512m, Longitude = 78.096m, OpensAt = new TimeOnly(7, 0), ClosesAt = new TimeOnly(22, 0), DeliveryRadiusKm = 5 });
        db.Riders.Add(new RiderProfile { Id = "rider-1", Name = "Minto Rider", Phone = "9876543210", Vehicle = "Bike", KycStatus = ReviewStatus.Pending });
        await db.SaveChangesAsync();
    }
}
