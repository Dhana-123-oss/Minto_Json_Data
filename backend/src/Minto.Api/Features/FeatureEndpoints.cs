using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;
using Minto.Api.Domain;

namespace Minto.Api.Features;

/// <summary>Persisted operational features shared by customer, picker, rider and admin clients.</summary>
public static class FeatureEndpoints
{
    public static IEndpointRouteBuilder MapFeatureEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/v1/features").WithTags("Features").RequireAuthorization();
        g.MapGet("/orders/{id:guid}/picking-checklist", Checklist);
        g.MapPost("/orders/{id:guid}/pick-items/{lineId:guid}", PickItem);
        g.MapPost("/orders/{id:guid}/replacements", ProposeReplacement);
        g.MapGet("/orders/{id:guid}/replacements", ListReplacements);
        g.MapPost("/replacements/{id:guid}/review", ReviewReplacement);
        g.MapPost("/orders/{id:guid}/delivery-proof", DeliveryProof);
        g.MapPost("/orders/{id:guid}/location", RecordLocation);
        g.MapGet("/orders/{id:guid}/tracking", Tracking);
        g.MapPost("/riders/{id}/kyc", SubmitKyc);
        g.MapPost("/riders/{id}/kyc/review", ReviewKyc);
        g.MapPost("/riders/{id}/availability", Availability);
        g.MapPost("/riders/{id}/status", UpdateRiderStatus);
        g.MapGet("/riders", ListRiders);
        g.MapPost("/pickers/me/availability", PickerAvailability);
        g.MapGet("/inventory/alerts", InventoryAlerts);
        g.MapPost("/complaints", CreateComplaint);
        g.MapGet("/complaints", ListComplaints);
        g.MapPost("/complaints/{id:guid}/resolve", ResolveComplaint);
        g.MapPost("/refunds", CreateRefund);
        g.MapPost("/refunds/{id:guid}/review", ReviewRefund);
        g.MapGet("/shifts", ListShifts); g.MapPost("/shifts", CreateShift); g.MapPost("/shifts/{id:guid}/attendance", Attendance);
        g.MapPost("/settlements", CreateSettlement); g.MapPost("/settlements/{id:guid}/settle", Settle);
        g.MapGet("/reports", Reports);
        g.MapGet("/coupons", Coupons); g.MapPost("/coupons", CreateCoupon);
        g.MapGet("/loyalty", Loyalty); g.MapPost("/loyalty/redeem", RedeemPoints);
        g.MapPost("/push-subscriptions", SubscribePush);
        g.MapGet("/permissions", Permissions);
        g.MapPost("/orders/{id:guid}/quality-check", Quality);
        g.MapPost("/orders/{id:guid}/handover", HandoverOrder);
        g.MapGet("/rider-issues", ListRiderIssues); g.MapPost("/rider-issues", RiderIssue);
        g.MapPost("/rider-ratings", RiderRating);
        g.MapGet("/riders/{id}/reminders", Reminders);
        g.MapPost("/addresses", AddAddress); g.MapGet("/addresses", Addresses); g.MapGet("/wishlist", Wishlist); g.MapPost("/wishlist", AddWishlist); g.MapDelete("/wishlist/{productId:guid}", RemoveWishlist);
        g.MapPost("/reviews", ProductReview);
        g.MapPost("/subscriptions", Subscription); g.MapGet("/subscriptions", Subscriptions);
        g.MapPost("/support", Support); g.MapGet("/support", SupportList);
        g.MapPost("/payments", Payment);
        g.MapPost("/orders/{id:guid}/cancel", CancelOrder);
        g.MapPost("/preferences", Preferences); g.MapGet("/preferences", GetPreferences);
        g.MapGet("/stores", Stores); g.MapPost("/stores", CreateStore); g.MapPut("/stores/{id:guid}", UpdateStore);
        g.MapGet("/catalog/products", ManagedProducts); g.MapPost("/catalog/products", CreateManagedProduct); g.MapPut("/catalog/products/{id:guid}", UpdateManagedProduct); g.MapDelete("/catalog/products/{id:guid}", DeleteManagedProduct); g.MapPut("/catalog/products/{id:guid}/stock", UpdateManagedStock);
        g.MapGet("/suppliers", ListSuppliers); g.MapPost("/suppliers", CreateSupplier); g.MapGet("/purchase-orders", ListPurchaseOrders); g.MapPost("/purchase-orders", CreatePurchaseOrder); g.MapGet("/stock-transfers", ListTransfers); g.MapPost("/stock-transfers", CreateTransfer);
        g.MapGet("/audit", Audit); g.MapGet("/login-history", LoginHistory);
        g.MapGet("/orders/{id:guid}/timeline", Timeline);
        g.MapPost("/orders/{id:guid}/assignment", AssignOrder);
        g.MapGet("/dashboard", Dashboard);
        g.MapGet("/notifications", Notifications); g.MapPost("/notifications/broadcast", Broadcast); g.MapPost("/notifications/{id:guid}/read", MarkRead);
        g.MapPost("/staff/assignments", AssignStaff); g.MapGet("/staff/assignments", StaffAssignments);
        g.MapPost("/staff/leave", RequestLeave); g.MapPost("/staff/leave/{id:guid}/review", ReviewLeave);
        g.MapGet("/performance/{staffId}", Performance);
        g.MapGet("/export/{kind}", Export);
        return app;
    }

    private static string Actor(ClaimsPrincipal user) => user.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
    private static bool IsAdmin(ClaimsPrincipal user) => user.IsInRole("Admin");
    private static IResult AdminOnly(ClaimsPrincipal user) => Results.Forbid();

    private static async Task<IResult> Checklist(Guid id, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!user.IsInRole("Picker") && !IsAdmin(user)) return Results.Forbid();
        var order = await db.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, ct);
        if (order is not null && user.IsInRole("Picker") && order.PickerId is not null && order.PickerId != Actor(user)) return Results.Forbid();
        return order is null ? Results.NotFound() : Results.Ok(order.Lines.Select(x => new { x.Id, x.ProductId, x.ProductName, x.Quantity, Picked = 0, Barcode = x.ProductId.ToString("N") }));
    }
    private static async Task<IResult> PickItem(Guid id, Guid lineId, PickRequest request, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!user.IsInRole("Picker") && !IsAdmin(user)) return Results.Forbid();
        var line = await db.OrderLines.SingleOrDefaultAsync(x => x.Id == lineId && x.OrderId == id, ct);
        if (line is null) return Results.NotFound();
        if (!string.Equals(request.Barcode, line.ProductId.ToString("N"), StringComparison.OrdinalIgnoreCase)) return Results.BadRequest(new { error = "Barcode does not match the ordered product." });
        if (request.Quantity < 1 || request.Quantity > line.Quantity) return Results.BadRequest(new { error = "Invalid picked quantity." });
        line.PickedQuantity = request.Quantity; await db.SaveChangesAsync(ct); return Results.Ok(new { line.Id, line.ProductName, line.Quantity, Picked = line.PickedQuantity, Verified = true });
    }
    private static async Task<IResult> ProposeReplacement(Guid id, ReplacementRequestBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!user.IsInRole("Picker") && !IsAdmin(user)) return Results.Forbid();
        if (!await db.Orders.AnyAsync(x => x.Id == id, ct) || !await db.Products.AnyAsync(x => x.Id == body.ReplacementProductId && x.IsActive, ct)) return Results.NotFound();
        var entity = new ReplacementRequest { OrderId = id, OrderLineId = body.OrderLineId, ReplacementProductId = body.ReplacementProductId, Reason = body.Reason };
        db.Replacements.Add(entity); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/replacements/{entity.Id}", entity);
    }
    private static async Task<IResult> ListReplacements(Guid id, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var order = await db.Orders.FindAsync([id], ct); if (order is null) return Results.NotFound();
        if (user.IsInRole("Customer") && order.CustomerId != Actor(user)) return Results.Forbid();
        if (user.IsInRole("Rider") && order.RiderId != Actor(user)) return Results.Forbid();
        if (user.IsInRole("Picker") && order.PickerId is not null && order.PickerId != Actor(user)) return Results.Forbid();
        return Results.Ok(await db.Replacements.Where(x => x.OrderId == id).ToListAsync(ct));
    }
    private static async Task<IResult> ReviewReplacement(Guid id, ReviewBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        var item = await db.Replacements.FindAsync([id], ct); if (item is null) return Results.NotFound();
        if (!user.IsInRole("Customer") && !IsAdmin(user)) return Results.Forbid();
        if (user.IsInRole("Customer") && !await db.Orders.AnyAsync(x => x.Id == item.OrderId && x.CustomerId == Actor(user), ct)) return Results.Forbid();
        item.Status = body.Approved ? ReplacementStatus.Approved : ReplacementStatus.Rejected; item.ReviewedBy = Actor(user); await db.SaveChangesAsync(ct); return Results.Ok(item);
    }
    private static async Task<IResult> DeliveryProof(Guid id, ProofBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!user.IsInRole("Rider") && !IsAdmin(user)) return Results.Forbid();
        var order = await db.Orders.FindAsync([id], ct); if (order is null) return Results.NotFound();
        if (user.IsInRole("Rider") && order.RiderId != Actor(user)) return Results.Forbid();
        if (string.IsNullOrWhiteSpace(body.Otp) && string.IsNullOrWhiteSpace(body.PhotoData)) return Results.BadRequest(new { error = "Photo or customer OTP is required." });
        if (!string.IsNullOrWhiteSpace(body.Otp) && !string.Equals(order.DeliveryOtp, body.Otp, StringComparison.Ordinal)) return Results.BadRequest(new { error = "Delivery OTP verification failed." });
        var proof = new Domain.DeliveryProof { OrderId = id, SubmittedBy = Actor(user), PhotoData = body.PhotoData, CustomerOtp = body.Otp };
        db.DeliveryProofs.Add(proof); await db.SaveChangesAsync(ct); return Results.Ok(proof);
    }
    private static async Task<IResult> RecordLocation(Guid id, LocationBody body, MintoDbContext db, ClaimsPrincipal user, IHubContext<TrackingHub> hub, CancellationToken ct)
    {
        if (!user.IsInRole("Rider") && !IsAdmin(user)) return Results.Forbid();
        var order = await db.Orders.FindAsync([id], ct); if (order is null) return Results.NotFound(); if (user.IsInRole("Rider") && order.RiderId != Actor(user)) return Results.Forbid();
        var location = new OrderLocation { OrderId = id, RiderId = Actor(user), Latitude = body.Latitude, Longitude = body.Longitude };
        db.OrderLocations.Add(location); await db.SaveChangesAsync(ct); await hub.Clients.Group(id.ToString()).SendAsync("order.location", location, ct); return Results.Ok(location);
    }
    private static async Task<IResult> Tracking(Guid id, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var order = await db.Orders.FindAsync([id], ct); if (order is null) return Results.NotFound(); if (user.IsInRole("Customer") && order.CustomerId != Actor(user)) return Results.Forbid(); if (user.IsInRole("Rider") && order.RiderId != Actor(user)) return Results.Forbid(); return Results.Ok(new { Order = new { order.Id, order.Status, order.RiderId }, Location = await db.OrderLocations.Where(x => x.OrderId == id).OrderByDescending(x => x.RecordedAt).FirstOrDefaultAsync(ct), Proof = await db.DeliveryProofs.Where(x => x.OrderId == id).OrderByDescending(x => x.SubmittedAt).FirstOrDefaultAsync(ct) }); }
    private static async Task<IResult> SubmitKyc(string id, KycBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (Actor(user) != id && !IsAdmin(user)) return Results.Forbid();
        var rider = await db.Riders.FindAsync([id], ct) ?? new RiderProfile { Id = id, Name = id };
        db.Riders.Update(rider); var document = new KycDocument { RiderId = id, Type = body.Type, DocumentReference = body.DocumentReference }; db.KycDocuments.Add(document); await db.SaveChangesAsync(ct); return Results.Ok(document);
    }
    private static async Task<IResult> Availability(string id, AvailabilityBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (Actor(user) != id && !IsAdmin(user)) return Results.Forbid(); var rider = await db.Riders.FindAsync([id], ct) ?? new RiderProfile { Id = id, Name = id }; if (rider.IsSuspended && body.Online) return Results.BadRequest(new { error = "Rider access is suspended." }); rider.IsOnline = body.Online; db.Riders.Update(rider); await db.SaveChangesAsync(ct); return Results.Ok(rider); }
    private static async Task<IResult> ListRiders(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid();
        return Results.Ok(await db.Riders.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Phone, x.Vehicle, Kyc = x.KycStatus.ToString(), Status = x.IsSuspended ? "Blocked" : x.IsOnline ? "Online" : "Offline" }).ToListAsync(ct));
    }
    private static async Task<IResult> UpdateRiderStatus(string id, StatusBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!IsAdmin(user)) return Results.Forbid();
        var rider = await db.Riders.FindAsync([id], ct) ?? new RiderProfile { Id = id, Name = id };
        rider.IsSuspended = body.Suspended; if (body.Suspended) rider.IsOnline = false; db.Riders.Update(rider); await db.SaveChangesAsync(ct); return Results.Ok(rider);
    }
    private static async Task<IResult> PickerAvailability(PickerShiftBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!user.IsInRole("Picker") && !IsAdmin(user)) return Results.Forbid();
        var staffId = Actor(user); var now = DateTimeOffset.UtcNow;
        var active = await db.StaffShifts.Where(x => x.StaffId == staffId && x.Role == "Picker" && x.Status == AttendanceStatus.CheckedIn).OrderByDescending(x => x.StartsAt).FirstOrDefaultAsync(ct);
        if (body.Online)
        {
            if (active is not null) return Results.Ok(active);
            var shift = new StaffShift { StaffId = staffId, Role = "Picker", StoreId = body.StoreId, StartsAt = now, EndsAt = now.AddHours(12), Status = AttendanceStatus.CheckedIn };
            db.StaffShifts.Add(shift); await db.SaveChangesAsync(ct); return Results.Ok(shift);
        }
        if (active is null) return Results.Ok(new { online = false });
        active.EndsAt = now; active.Status = AttendanceStatus.CheckedOut; await db.SaveChangesAsync(ct); return Results.Ok(active);
    }
    private static async Task<IResult> ReviewKyc(string id, ReviewBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!IsAdmin(user)) return Results.Forbid(); var rider = await db.Riders.FindAsync([id], ct) ?? new RiderProfile { Id = id, Name = id }; rider.KycStatus = body.Approved ? ReviewStatus.Approved : ReviewStatus.Rejected; rider.KycReviewedBy = Actor(user); rider.KycReviewedAt = DateTimeOffset.UtcNow; db.Riders.Update(rider); await db.SaveChangesAsync(ct); return Results.Ok(rider);
    }
    private static async Task<IResult> InventoryAlerts(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct)
    {
        if (!IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid();
        var low = await db.Inventory.Join(db.Products, i => i.ProductId, p => p.Id, (i, p) => new { p.Id, p.Name, i.Available }).Where(x => x.Available <= 5).ToListAsync(ct);
        var expiring = await db.InventoryBatches.Where(x => x.ExpiryDate != null && x.ExpiryDate <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7))).ToListAsync(ct); return Results.Ok(new { lowStock = low, expiring });
    }
    private static async Task<IResult> CreateComplaint(ComplaintBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var order = await db.Orders.FindAsync([body.OrderId], ct); if (order is null) return Results.NotFound(); if (user.IsInRole("Customer") && order.CustomerId != Actor(user)) return Results.Forbid(); if (!user.IsInRole("Customer") && !IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid(); var c = new Complaint { OrderId = body.OrderId, CustomerId = user.IsInRole("Customer") ? Actor(user) : order.CustomerId, Type = body.Type, Description = body.Description }; db.Complaints.Add(c); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/complaints/{c.Id}", c); }
    private static async Task<IResult> ListComplaints(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => IsAdmin(user) || user.IsInRole("Manager") ? Results.Ok(await db.Complaints.OrderByDescending(x => x.CreatedAt).ToListAsync(ct)) : Results.Ok(await db.Complaints.Where(x => x.CustomerId == Actor(user)).ToListAsync(ct));
    private static async Task<IResult> ResolveComplaint(Guid id, ReviewBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid(); var c = await db.Complaints.FindAsync([id], ct); if (c is null) return Results.NotFound(); c.Status = body.Approved ? ComplaintStatus.Resolved : ComplaintStatus.Rejected; c.ResolvedBy = Actor(user); await db.SaveChangesAsync(ct); return Results.Ok(c); }
    private static async Task<IResult> CreateRefund(RefundBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (body.Amount <= 0) return Results.BadRequest(new { error = "Refund amount must be positive." }); var order = await db.Orders.FindAsync([body.OrderId], ct); if (order is null) return Results.NotFound(); if (user.IsInRole("Customer") && order.CustomerId != Actor(user)) return Results.Forbid(); if (!user.IsInRole("Customer") && !IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid(); var r = new Refund { OrderId = body.OrderId, ComplaintId = body.ComplaintId, CustomerId = user.IsInRole("Customer") ? Actor(user) : order.CustomerId, Amount = body.Amount }; db.Refunds.Add(r); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/refunds/{r.Id}", r); }
    private static async Task<IResult> ReviewRefund(Guid id, ReviewBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid(); var r = await db.Refunds.FindAsync([id], ct); if (r is null) return Results.NotFound(); r.Status = body.Approved ? ReviewStatus.Approved : ReviewStatus.Rejected; r.ReviewedBy = Actor(user); await db.SaveChangesAsync(ct); return Results.Ok(r); }
    private static async Task<IResult> CreateShift(ShiftBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var selfBooking = user.IsInRole("Picker") && body.Role.Equals("Picker", StringComparison.OrdinalIgnoreCase) && body.StaffId == Actor(user); if (!IsAdmin(user) && !user.IsInRole("Manager") && !selfBooking) return Results.Forbid(); var s = new StaffShift { StaffId = body.StaffId, Role = body.Role, StoreId = body.StoreId, StartsAt = body.StartsAt, EndsAt = body.EndsAt }; db.StaffShifts.Add(s); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/shifts/{s.Id}", s); }
    private static async Task<IResult> ListShifts(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => IsAdmin(user) || user.IsInRole("Manager") ? Results.Ok(await db.StaffShifts.OrderByDescending(x => x.StartsAt).ToListAsync(ct)) : Results.Ok(await db.StaffShifts.Where(x => x.StaffId == Actor(user)).OrderByDescending(x => x.StartsAt).ToListAsync(ct));
    private static async Task<IResult> Attendance(Guid id, AttendanceBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var s = await db.StaffShifts.FindAsync([id], ct); if (s is null) return Results.NotFound(); if (s.StaffId != Actor(user) && !IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid(); s.Status = body.Status; if (body.Status == AttendanceStatus.CheckedIn && s.StartsAt > DateTimeOffset.UtcNow) s.StartsAt = DateTimeOffset.UtcNow; if (body.Status == AttendanceStatus.CheckedOut) s.EndsAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(ct); return Results.Ok(s); }
    private static async Task<IResult> CreateSettlement(SettlementBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user) && !user.IsInRole("Rider")) return Results.Forbid(); var s = new CashSettlement { RiderId = body.RiderId, ExpectedAmount = body.ExpectedAmount, CollectedAmount = body.CollectedAmount }; db.CashSettlements.Add(s); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/settlements/{s.Id}", s); }
    private static async Task<IResult> Settle(Guid id, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user)) return Results.Forbid(); var s = await db.CashSettlements.FindAsync([id], ct); if (s is null) return Results.NotFound(); s.SettledAmount = s.CollectedAmount; s.SettledAt = DateTimeOffset.UtcNow; s.SettledBy = Actor(user); await db.SaveChangesAsync(ct); return Results.Ok(s); }
    private static async Task<IResult> Reports(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid(); return Results.Ok(new { totalOrders = await db.Orders.CountAsync(ct), deliveredOrders = await db.Orders.CountAsync(x => x.Status == OrderStatus.Delivered, ct), sales = await db.Orders.Where(x => x.PaymentCompleted || x.PaymentMethod == "COD").SumAsync(x => (decimal?)x.Total, ct) ?? 0, complaints = await db.Complaints.CountAsync(ct), refunds = await db.Refunds.SumAsync(x => (decimal?)x.Amount, ct) ?? 0, riders = await db.Riders.CountAsync(ct), shifts = await db.StaffShifts.CountAsync(ct) }); }
    private static async Task<IResult> Coupons(MintoDbContext db, CancellationToken ct) => Results.Ok(await db.Coupons.Where(x => x.IsActive && x.ExpiresAt > DateTimeOffset.UtcNow).ToListAsync(ct));
    private static async Task<IResult> CreateCoupon(CouponBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user)) return Results.Forbid(); var c = new Coupon { Code = body.Code.Trim().ToUpperInvariant(), Amount = body.Amount, MinimumOrderValue = body.MinimumOrderValue, ExpiresAt = body.ExpiresAt }; db.Coupons.Add(c); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/coupons/{c.Id}", c); }
    private static async Task<IResult> Loyalty(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(await db.LoyaltyAccounts.FindAsync([Actor(user)], ct) ?? new LoyaltyAccount { CustomerId = Actor(user), Points = 0 });
    private static async Task<IResult> RedeemPoints(RedeemBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var a = await db.LoyaltyAccounts.FindAsync([Actor(user)], ct) ?? new LoyaltyAccount { CustomerId = Actor(user) }; if (body.Points < 1 || a.Points < body.Points) return Results.BadRequest(new { error = "Insufficient loyalty points." }); a.Points -= body.Points; db.LoyaltyAccounts.Update(a); await db.SaveChangesAsync(ct); return Results.Ok(a); }
    private static async Task<IResult> SubscribePush(PushBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var s = new PushSubscription { UserId = Actor(user), Endpoint = body.Endpoint, Platform = body.Platform }; db.PushSubscriptions.Add(s); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/push-subscriptions/{s.Id}", s); }
    private static IResult Permissions(ClaimsPrincipal user) => Results.Ok(new { userId = Actor(user), role = user.FindFirstValue(ClaimTypes.Role), permissions = user.FindAll(ClaimTypes.Role).Select(x => x.Value).Distinct().ToArray() });
    private static async Task<IResult> Quality(Guid id, QualityBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!user.IsInRole("Picker") && !IsAdmin(user)) return Results.Forbid(); var order = await db.Orders.FindAsync([id], ct); if (order is null) return Results.NotFound(); if (user.IsInRole("Picker") && order.PickerId is not null && order.PickerId != Actor(user)) return Results.Forbid(); var q = new QualityCheck { OrderId = id, PickerId = Actor(user), FreshnessPassed = body.FreshnessPassed, DamageFree = body.DamageFree, ConfirmedWeight = body.ConfirmedWeight, PhotoData = body.PhotoData, PackingComplete = body.PackingComplete }; db.QualityChecks.Add(q); await db.SaveChangesAsync(ct); return Results.Ok(q); }
    private static async Task<IResult> HandoverOrder(Guid id, HandoverBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!user.IsInRole("Picker") && !IsAdmin(user)) return Results.Forbid(); var o = await db.Orders.FindAsync([id], ct); if (o is null) return Results.NotFound(); if (user.IsInRole("Picker") && o.PickerId is not null && o.PickerId != Actor(user)) return Results.Forbid(); if (string.IsNullOrWhiteSpace(body.PickupOtp) || body.PickupOtp != o.PickupCode) return Results.BadRequest(new { error = "Pickup OTP verification failed." }); var h = new Domain.Handover { OrderId = id, PickerId = Actor(user), RiderId = body.RiderId, SignatureData = body.SignatureData, PickupOtp = body.PickupOtp }; db.Handovers.Add(h); await db.SaveChangesAsync(ct); return Results.Ok(h); }
    private static async Task<IResult> RiderIssue(RiderIssueBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var x = new Domain.RiderIssue { RiderId = Actor(user), OrderId = body.OrderId, Type = body.Type, Description = body.Description }; db.RiderIssues.Add(x); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/rider-issues/{x.Id}", x); }
    private static async Task<IResult> ListRiderIssues(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => IsAdmin(user) || user.IsInRole("Manager") ? Results.Ok(await db.RiderIssues.OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct)) : Results.Forbid();
    private static async Task<IResult> RiderRating(RatingBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!user.IsInRole("Customer")) return Results.Forbid(); if (body.Score is < 1 or > 5) return Results.BadRequest(new { error = "Score must be between 1 and 5." }); var order = await db.Orders.FindAsync([body.OrderId], ct); if (order is null) return Results.NotFound(); if (order.CustomerId != Actor(user)) return Results.Forbid(); if (order.RiderId is not null && order.RiderId != body.RiderId) return Results.BadRequest(new { error = "The rider is not assigned to this order." }); var x = new Domain.RiderRating { OrderId = body.OrderId, CustomerId = Actor(user), RiderId = body.RiderId, Score = body.Score, Feedback = body.Feedback }; db.RiderRatings.Add(x); await db.SaveChangesAsync(ct); return Results.Ok(x); }
    private static async Task<IResult> Reminders(string id, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => Actor(user) == id || IsAdmin(user) ? Results.Ok(await db.VehicleReminders.Where(x => x.RiderId == id && !x.Completed).OrderBy(x => x.DueDate).ToListAsync(ct)) : Results.Forbid();
    private static async Task<IResult> AddAddress(AddressBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var x = new CustomerAddress { CustomerId = Actor(user), Label = body.Label, Address = body.Address, IsDefault = body.IsDefault }; db.CustomerAddresses.Add(x); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/addresses/{x.Id}", x); }
    private static async Task<IResult> Addresses(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(await db.CustomerAddresses.Where(x => x.CustomerId == Actor(user)).ToListAsync(ct));
    private static async Task<IResult> Wishlist(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(await db.WishlistItems.Where(x => x.CustomerId == Actor(user)).OrderByDescending(x => x.CreatedAt).ToListAsync(ct));
    private static async Task<IResult> AddWishlist(GuidProductBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var customerId = Actor(user); if (!await db.WishlistItems.AnyAsync(x => x.CustomerId == customerId && x.ProductId == body.ProductId, ct)) { db.WishlistItems.Add(new WishlistItem { CustomerId = customerId, ProductId = body.ProductId }); await db.SaveChangesAsync(ct); } return Results.Ok(await db.WishlistItems.SingleAsync(x => x.CustomerId == customerId && x.ProductId == body.ProductId, ct)); }
    private static async Task<IResult> RemoveWishlist(Guid productId, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var item = await db.WishlistItems.SingleOrDefaultAsync(x => x.CustomerId == Actor(user) && x.ProductId == productId, ct); if (item is null) return Results.NotFound(); db.WishlistItems.Remove(item); await db.SaveChangesAsync(ct); return Results.NoContent(); }
    private static async Task<IResult> ProductReview(ReviewProductBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!user.IsInRole("Customer")) return Results.Forbid(); if (body.Score is < 1 or > 5) return Results.BadRequest(new { error = "Score must be between 1 and 5." }); var order = await db.Orders.FindAsync([body.OrderId], ct); if (order is null) return Results.NotFound(); if (order.CustomerId != Actor(user)) return Results.Forbid(); var x = new Domain.ProductReview { ProductId = body.ProductId, OrderId = body.OrderId, CustomerId = Actor(user), Score = body.Score, Comment = body.Comment }; db.ProductReviews.Add(x); await db.SaveChangesAsync(ct); return Results.Ok(x); }
    private static async Task<IResult> Subscription(SubscriptionBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var x = new CustomerSubscription { CustomerId = Actor(user), ProductName = body.ProductName, Quantity = body.Quantity, Frequency = body.Frequency, NextDelivery = body.NextDelivery }; db.Subscriptions.Add(x); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/subscriptions/{x.Id}", x); }
    private static async Task<IResult> Subscriptions(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(await db.Subscriptions.Where(x => x.CustomerId == Actor(user)).ToListAsync(ct));
    private static async Task<IResult> Support(SupportBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var x = new SupportMessage { CustomerId = Actor(user), OrderId = body.OrderId, Message = body.Message }; db.SupportMessages.Add(x); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/support/{x.Id}", x); }
    private static async Task<IResult> SupportList(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => IsAdmin(user) || user.IsInRole("Manager") ? Results.Ok(await db.SupportMessages.OrderBy(x => x.SentAt).ToListAsync(ct)) : Results.Ok(await db.SupportMessages.Where(x => x.CustomerId == Actor(user)).OrderBy(x => x.SentAt).ToListAsync(ct));
    private static async Task<IResult> Payment(PaymentBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!user.IsInRole("Customer")) return Results.Forbid(); if (body.Amount <= 0) return Results.BadRequest(new { error = "Payment amount must be positive." }); var order = await db.Orders.FindAsync([body.OrderId], ct); if (order is null) return Results.NotFound(); if (order.CustomerId != Actor(user)) return Results.Forbid(); var x = new PaymentRecord { OrderId = body.OrderId, CustomerId = Actor(user), Method = body.Method, Amount = body.Amount, Status = body.Method == "COD" ? "PendingAtDelivery" : "Pending" }; db.Payments.Add(x); await db.SaveChangesAsync(ct); return Results.Ok(x); }
    private static async Task<IResult> CancelOrder(Guid id, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var o = await db.Orders.Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id && x.CustomerId == Actor(user), ct); if (o is null) return Results.NotFound(); if (o.Status is not (OrderStatus.Placed or OrderStatus.Picking)) return Results.BadRequest(new { error = "Order can no longer be cancelled." }); o.Status = OrderStatus.Cancelled; foreach (var l in o.Lines) { var stock = await db.Inventory.FindAsync([l.ProductId], ct); if (stock is not null) { stock.Available += l.Quantity; stock.Reserved = Math.Max(0, stock.Reserved - l.Quantity); } } await db.SaveChangesAsync(ct); return Results.Ok(o); }
    private static async Task<IResult> Preferences(PreferenceBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var x = await db.CustomerPreferences.FindAsync([Actor(user)], ct) ?? new CustomerPreference { CustomerId = Actor(user) }; x.Language = body.Language; x.DarkMode = body.DarkMode; x.PushEnabled = body.PushEnabled; db.CustomerPreferences.Update(x); await db.SaveChangesAsync(ct); return Results.Ok(x); }
    private static async Task<IResult> GetPreferences(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(await db.CustomerPreferences.FindAsync([Actor(user)], ct) ?? new CustomerPreference { CustomerId = Actor(user) });
    private static async Task<IResult> Stores(MintoDbContext db, CancellationToken ct) => Results.Ok(await db.Stores.ToListAsync(ct));
    private static bool CanManage(ClaimsPrincipal user) => IsAdmin(user) || user.IsInRole("Manager");
    private static async Task<IResult> ManagedProducts(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => CanManage(user) ? Results.Ok(await db.Products.Join(db.Inventory, p => p.Id, i => i.ProductId, (p, i) => new { p.Id, p.Name, p.Category, p.Price, p.IsActive, i.Available, i.Reserved }).OrderBy(x => x.Category).ToListAsync(ct)) : Results.Forbid();
    private static async Task<IResult> CreateManagedProduct(ManagedProductBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!CanManage(user)) return Results.Forbid(); var p = new Product { Name = body.Name, Category = body.Category, Price = body.Price, IsActive = body.IsActive }; db.Products.Add(p); db.Inventory.Add(new InventoryItem { ProductId = p.Id, Available = body.Stock }); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/catalog/products/{p.Id}", p); }
    private static async Task<IResult> UpdateManagedProduct(Guid id, ManagedProductBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!CanManage(user)) return Results.Forbid(); var p = await db.Products.FindAsync([id], ct); if (p is null) return Results.NotFound(); p.Name = body.Name; p.Category = body.Category; p.Price = body.Price; p.IsActive = body.IsActive; var i = await db.Inventory.FindAsync([id], ct); if (i is not null) i.Available = body.Stock; await db.SaveChangesAsync(ct); return Results.Ok(p); }
    private static async Task<IResult> DeleteManagedProduct(Guid id, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!CanManage(user)) return Results.Forbid(); var p = await db.Products.FindAsync([id], ct); if (p is null) return Results.NotFound(); p.IsActive = false; await db.SaveChangesAsync(ct); return Results.NoContent(); }
    private static async Task<IResult> UpdateManagedStock(Guid id, StockBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!CanManage(user)) return Results.Forbid(); var i = await db.Inventory.FindAsync([id], ct); if (i is null) return Results.NotFound(); if (body.Available < 0) return Results.BadRequest(); i.Available = body.Available; i.Version++; await db.SaveChangesAsync(ct); return Results.Ok(i); }
    private static async Task<IResult> CreateStore(StoreBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user)) return Results.Forbid(); var x = new StoreBranch { Name = body.Name, Address = body.Address, Latitude = body.Latitude, Longitude = body.Longitude, OpensAt = body.OpensAt, ClosesAt = body.ClosesAt, IsOpen = body.IsOpen ?? true, DeliveryRadiusKm = body.DeliveryRadiusKm }; db.Stores.Add(x); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/stores/{x.Id}", x); }
    private static async Task<IResult> UpdateStore(Guid id, StoreBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid(); var x = await db.Stores.FindAsync([id], ct); if (x is null) return Results.NotFound(); x.Name = body.Name; x.Address = body.Address; x.Latitude = body.Latitude; x.Longitude = body.Longitude; x.OpensAt = body.OpensAt; x.ClosesAt = body.ClosesAt; if (body.IsOpen.HasValue) x.IsOpen = body.IsOpen.Value; x.DeliveryRadiusKm = body.DeliveryRadiusKm; await db.SaveChangesAsync(ct); return Results.Ok(x); }
    private static async Task<IResult> CreateSupplier(SupplierBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid(); var x = new Supplier { Name = body.Name, Contact = body.Contact }; db.Suppliers.Add(x); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/suppliers/{x.Id}", x); }
    private static async Task<IResult> ListSuppliers(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => IsAdmin(user) || user.IsInRole("Manager") ? Results.Ok(await db.Suppliers.OrderBy(x => x.Name).ToListAsync(ct)) : Results.Forbid();
    private static async Task<IResult> CreatePurchaseOrder(PurchaseOrderBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid(); var x = new Domain.PurchaseOrder { SupplierId = body.SupplierId, StoreId = body.StoreId, Lines = body.Lines.Select(l => new PurchaseOrderLine { ProductId = l.ProductId, Quantity = l.Quantity }).ToList() }; db.PurchaseOrders.Add(x); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/purchase-orders/{x.Id}", x); }
    private static async Task<IResult> ListPurchaseOrders(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => IsAdmin(user) || user.IsInRole("Manager") ? Results.Ok(await db.PurchaseOrders.Include(x => x.Lines).OrderByDescending(x => x.CreatedAt).ToListAsync(ct)) : Results.Forbid();
    private static async Task<IResult> ListTransfers(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => IsAdmin(user) || user.IsInRole("Manager") ? Results.Ok(await db.StockTransfers.ToListAsync(ct)) : Results.Forbid();
    private static async Task<IResult> CreateTransfer(TransferBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid(); var x = new StockTransfer { FromStoreId = body.FromStoreId, ToStoreId = body.ToStoreId, ProductId = body.ProductId, Quantity = body.Quantity }; db.StockTransfers.Add(x); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/stock-transfers/{x.Id}", x); }
    private static async Task<IResult> Audit(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => IsAdmin(user) ? Results.Ok(await db.AuditEntries.OrderByDescending(x => x.CreatedAt).Take(500).ToListAsync(ct)) : Results.Forbid();
    private static async Task<IResult> LoginHistory(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => IsAdmin(user) ? Results.Ok(await db.LoginHistory.OrderByDescending(x => x.LoggedInAt).Take(500).ToListAsync(ct)) : Results.Forbid();
    private static async Task<IResult> Timeline(Guid id, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var order = await db.Orders.FindAsync([id], ct); if (order is null) return Results.NotFound(); if (user.IsInRole("Customer") && order.CustomerId != Actor(user)) return Results.Forbid(); if (user.IsInRole("Rider") && order.RiderId != Actor(user)) return Results.Forbid(); if (user.IsInRole("Picker") && order.PickerId is not null && order.PickerId != Actor(user)) return Results.Forbid(); return Results.Ok(await db.OrderEvents.Where(x => x.OrderId == id).OrderBy(x => x.OccurredAt).ToListAsync(ct)); }
    private static async Task<IResult> AssignOrder(Guid id, AssignmentBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!IsAdmin(user) && !user.IsInRole("Manager")) return Results.Forbid(); var o = await db.Orders.FindAsync([id], ct); if (o is null) return Results.NotFound(); o.PickerId = body.PickerId ?? o.PickerId; o.RiderId = body.RiderId ?? o.RiderId; o.StoreId = body.StoreId ?? o.StoreId; o.IsPriority = body.IsPriority ?? o.IsPriority; o.DeliveryDeadline = body.DeliveryDeadline ?? o.DeliveryDeadline; db.OrderEvents.Add(new OrderEvent { OrderId = id, Status = o.Status.ToString(), ActorId = Actor(user), Note = "Assignment updated" }); await db.SaveChangesAsync(ct); return Results.Ok(o); }
    private static async Task<IResult> Dashboard(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!CanManage(user)) return Results.Forbid(); var pending = await db.Orders.CountAsync(x => x.Status != OrderStatus.Delivered && x.Status != OrderStatus.Cancelled, ct); var delayed = await db.Orders.CountAsync(x => x.DeliveryDeadline != null && x.DeliveryDeadline < DateTimeOffset.UtcNow && x.Status != OrderStatus.Delivered && x.Status != OrderStatus.Cancelled, ct); return Results.Ok(new { totalOrders = await db.Orders.CountAsync(ct), activePickers = await db.StaffAssignments.CountAsync(x => x.Role == "Picker" && x.Active, ct), activeRiders = await db.Riders.CountAsync(x => x.IsOnline, ct), pendingOrders = pending, delayedOrders = delayed, revenue = await db.Orders.Where(x => x.PaymentCompleted || x.PaymentMethod == "COD").SumAsync(x => (decimal?)x.Total, ct) ?? 0, cancellations = await db.Orders.CountAsync(x => x.Status == OrderStatus.Cancelled, ct) }); }
    private static async Task<IResult> Notifications(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => Results.Ok(await db.Notifications.Where(x => x.UserId == null || x.UserId == Actor(user)).OrderByDescending(x => x.CreatedAt).Take(100).ToListAsync(ct));
    private static async Task<IResult> Broadcast(BroadcastBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!CanManage(user)) return Results.Forbid(); var n = new Notification { Title = body.Title, Message = body.Message, Type = "Broadcast" }; db.Notifications.Add(n); await db.SaveChangesAsync(ct); return Results.Ok(n); }
    private static async Task<IResult> MarkRead(Guid id, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var n = await db.Notifications.FindAsync([id], ct); if (n is null || (n.UserId != null && n.UserId != Actor(user))) return Results.NotFound(); n.Read = true; await db.SaveChangesAsync(ct); return Results.Ok(n); }
    private static async Task<IResult> AssignStaff(StaffAssignmentBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!CanManage(user)) return Results.Forbid(); var a = new StaffAssignment { StaffId = body.StaffId, Role = body.Role, StoreId = body.StoreId }; db.StaffAssignments.Add(a); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/staff/assignments/{a.Id}", a); }
    private static async Task<IResult> StaffAssignments(MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => CanManage(user) ? Results.Ok(await db.StaffAssignments.Where(x => x.Active).ToListAsync(ct)) : Results.Forbid();
    private static async Task<IResult> RequestLeave(LeaveBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { var x = new LeaveRequest { StaffId = Actor(user), From = body.From, To = body.To, Reason = body.Reason }; db.LeaveRequests.Add(x); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/features/staff/leave/{x.Id}", x); }
    private static async Task<IResult> ReviewLeave(Guid id, ReviewBody body, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!CanManage(user)) return Results.Forbid(); var x = await db.LeaveRequests.FindAsync([id], ct); if (x is null) return Results.NotFound(); x.Status = body.Approved ? ReviewStatus.Approved : ReviewStatus.Rejected; x.ReviewedBy = Actor(user); await db.SaveChangesAsync(ct); return Results.Ok(x); }
    private static async Task<IResult> Performance(string staffId, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) => Actor(user) == staffId || CanManage(user) ? Results.Ok(await db.PerformanceSnapshots.Where(x => x.StaffId == staffId).OrderByDescending(x => x.Period).Take(100).ToListAsync(ct)) : Results.Forbid();
    private static async Task<IResult> Export(string kind, MintoDbContext db, ClaimsPrincipal user, CancellationToken ct) { if (!CanManage(user)) return Results.Forbid(); object data = kind.ToLowerInvariant() switch { "orders" => await db.Orders.AsNoTracking().ToListAsync(ct), "audit" => await db.AuditEntries.AsNoTracking().ToListAsync(ct), "refunds" => await db.Refunds.AsNoTracking().ToListAsync(ct), "reports" => new { orders = await db.Orders.CountAsync(ct), sales = await db.Orders.SumAsync(x => (decimal?)x.Total, ct) ?? 0 }, _ => new { error = "Unknown export type" } }; return Results.Ok(data); }
}

public sealed record PickRequest(string Barcode, int Quantity);
public sealed record ReplacementRequestBody(Guid OrderLineId, Guid ReplacementProductId, string Reason);
public sealed record ReviewBody(bool Approved);
public sealed record ProofBody(string? PhotoData, string? Otp);
public sealed record LocationBody(decimal Latitude, decimal Longitude);
public sealed record KycBody(string Type, string DocumentReference);
public sealed record AvailabilityBody(bool Online);
public sealed record StatusBody(bool Suspended);
public sealed record PickerShiftBody(bool Online, string StoreId);
public sealed record ComplaintBody(Guid OrderId, string Type, string Description);
public sealed record RefundBody(Guid OrderId, Guid? ComplaintId, decimal Amount);
public sealed record ShiftBody(string StaffId, string Role, string StoreId, DateTimeOffset StartsAt, DateTimeOffset EndsAt);
public sealed record AttendanceBody(AttendanceStatus Status);
public sealed record SettlementBody(string RiderId, decimal ExpectedAmount, decimal CollectedAmount);
public sealed record CouponBody(string Code, decimal Amount, decimal MinimumOrderValue, DateTimeOffset ExpiresAt);
public sealed record RedeemBody(int Points);
public sealed record PushBody(string Endpoint, string? Platform);
public sealed record QualityBody(bool FreshnessPassed, bool DamageFree, decimal? ConfirmedWeight, string? PhotoData, bool PackingComplete);
public sealed record HandoverBody(string RiderId, string? SignatureData, string? PickupOtp);
public sealed record RiderIssueBody(Guid? OrderId, string Type, string Description);
public sealed record RatingBody(Guid OrderId, string RiderId, int Score, string? Feedback);
public sealed record AddressBody(string Label, string Address, bool IsDefault);
public sealed record GuidProductBody(Guid ProductId);
public sealed record ReviewProductBody(Guid ProductId, Guid OrderId, int Score, string? Comment);
public sealed record SubscriptionBody(string ProductName, int Quantity, string Frequency, DateTimeOffset NextDelivery);
public sealed record SupportBody(Guid? OrderId, string Message);
public sealed record PaymentBody(Guid OrderId, string Method, decimal Amount);
public sealed record PreferenceBody(string Language, bool DarkMode, bool PushEnabled);
public sealed record StoreBody(string Name, string Address, decimal Latitude, decimal Longitude, TimeOnly OpensAt, TimeOnly ClosesAt, decimal DeliveryRadiusKm, bool? IsOpen = null);
public sealed record SupplierBody(string Name, string Contact);
public sealed record PurchaseOrderBody(Guid SupplierId, string StoreId, List<PurchaseOrderLineBody> Lines);
public sealed record PurchaseOrderLineBody(Guid ProductId, int Quantity);
public sealed record TransferBody(string FromStoreId, string ToStoreId, Guid ProductId, int Quantity);
public sealed record ManagedProductBody(string Name, string Category, decimal Price, int Stock, bool IsActive);
public sealed record StockBody(int Available);
public sealed record AssignmentBody(string? PickerId, string? RiderId, string? StoreId, bool? IsPriority, DateTimeOffset? DeliveryDeadline);
public sealed record BroadcastBody(string Title, string Message);
public sealed record StaffAssignmentBody(string StaffId, string Role, string StoreId);
public sealed record LeaveBody(DateOnly From, DateOnly To, string Reason);
