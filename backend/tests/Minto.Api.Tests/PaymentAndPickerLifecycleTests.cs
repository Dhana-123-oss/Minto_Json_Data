using Microsoft.EntityFrameworkCore;
using Minto.Api.Data;
using Minto.Api.Domain;
using Xunit;

namespace Minto.Api.Tests;

public sealed class PaymentAndPickerLifecycleTests
{
    [Fact]
    public async Task Payment_record_and_picker_shift_are_persisted_with_terminal_state()
    {
        await using var db = new MintoDbContext(new DbContextOptionsBuilder<MintoDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

        var order = new Order { CustomerId = "customer-1", Address = "Minto Street", Lines = [], Total = 248m, PaymentMethod = "COD" };
        var shift = new StaffShift { StaffId = "picker-1", Role = "Picker", StoreId = "store-1", StartsAt = DateTimeOffset.UtcNow.AddHours(-1), EndsAt = DateTimeOffset.UtcNow, Status = AttendanceStatus.CheckedOut };
        db.Orders.Add(order);
        db.StaffShifts.Add(shift);
        db.Payments.Add(new PaymentRecord { OrderId = order.Id, CustomerId = order.CustomerId, Method = order.PaymentMethod, Amount = order.Total, Status = "Recorded" });
        await db.SaveChangesAsync();

        var savedPayment = await db.Payments.SingleAsync();
        var savedShift = await db.StaffShifts.SingleAsync();
        Assert.Equal(order.Id, savedPayment.OrderId);
        Assert.Equal(248m, savedPayment.Amount);
        Assert.Equal(AttendanceStatus.CheckedOut, savedShift.Status);
    }
}
