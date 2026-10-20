using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SupportDesk.Application.Contracts.Tickets;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Aggregates.Categories;
using SupportDesk.Domain.Aggregates.Customers;
using SupportDesk.Domain.Aggregates.Tickets;
using SupportDesk.Infrastructure.Data;
using SupportDesk.Infrastructure.Queries;
using SupportDesk.UnitTests.TestDoubles;
using Xunit;

namespace SupportDesk.UnitTests.Infrastructure.Queries;

public sealed class TicketQueriesTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly SupportDbContext _db;
    private readonly TicketQueries _queries;

    public TicketQueriesTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<SupportDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new SupportDbContext(options);
        _db.Database.EnsureCreated();

        _queries = new TicketQueries(_db, new FixedClock());

        SeedTestData();
    }

    private void SeedTestData()
    {
        var customer1 = new Customer("Acme Corp", "acme@example.com", "123", CustomerTier.Standard, FixedClock.DefaultNow);
        var customer2 = new Customer("Wide World Importers", "wwi@example.com", "456", CustomerTier.Premium, FixedClock.DefaultNow);
        _db.AddRange(customer1, customer2);

        var catGeneral = new Category("General", requiresSpecialist: false, forcesCriticalPriority: false);
        var catBilling = new Category("Billing", requiresSpecialist: true, forcesCriticalPriority: false);
        _db.AddRange(catGeneral, catBilling);

        var agentAlex = new Agent("Alex Turner", "alex@northwind.com", 10, FixedClock.DefaultNow);
        _db.Add(agentAlex);

        _db.SaveChanges();

        // Ticket 1: Acme, General, High, Open, Assigned to Alex
        var t1 = Ticket.Raise("TCK-0001", "Printer issue", "Description 1", customer1.Id, catGeneral.Id, TicketPriority.High, FixedClock.DefaultNow);
        t1.AssignTo(agentAlex.Id, FixedClock.DefaultNow);

        // Ticket 2: Wide World Importers, Billing, Low, Open, Unassigned
        var t2 = Ticket.Raise("TCK-0002", "Invoice query", "Description 2", customer2.Id, catBilling.Id, TicketPriority.Low, FixedClock.DefaultNow);

        // Ticket 3: Acme, General, High, Resolved, Unassigned
        var t3 = Ticket.Raise("TCK-0003", "Network downtime", "Description 3", customer1.Id, catGeneral.Id, TicketPriority.High, FixedClock.DefaultNow);
        t3.ChangeStatus(TicketStatus.Resolved, FixedClock.DefaultNow);

        _db.AddRange(t1, t2, t3);
        _db.SaveChanges();
    }

    [Fact]
    public async Task Filters_By_Exact_Priority()
    {
        var query = new TicketQuery { Priority = TicketPriority.High };
        var result = await _queries.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Equal(TicketPriority.High, item.Priority));
    }

    [Fact]
    public async Task Filters_By_Customer_Name_Search()
    {
        var query = new TicketQuery { Search = "Wide World" };
        var result = await _queries.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-0002", result.Items.Single().Reference);
        Assert.Equal("Wide World Importers", result.Items.Single().Customer.Name);
    }

    [Fact]
    public async Task Filters_By_Unassigned_Only()
    {
        var query = new TicketQuery { UnassignedOnly = true };
        var result = await _queries.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.All(result.Items, item => Assert.Null(item.AssignedAgent));
    }

    [Fact]
    public async Task Combines_Multiple_Filters_With_And()
    {
        // High priority AND Unassigned -> only Ticket 3
        var query = new TicketQuery
        {
            Priority = TicketPriority.High,
            UnassignedOnly = true
        };
        var result = await _queries.GetPagedAsync(query, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("TCK-0003", result.Items.Single().Reference);
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }
}
