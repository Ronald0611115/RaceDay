
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Controllers;
using RaceDay.API.Data;
using RaceDay.API.Models;

namespace RaceDay.API.Tests;

public class EventsControllerTests
{
    private static RaceDayContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new RaceDayContext(options);
    }

    private static Event CreateEvent(
        string name,
        DateTime date,
        bool isCancelled = false,
        int organiserId = 1)
    {
        return new Event
        {
            EventName = name,
            EventDate = date,
            Location = "Johannesburg",
            Description = "Test event",
            IsCancelled = isCancelled,
            OrganiserId = organiserId
        };
    }

    [Fact]
    public async Task GetEvents_ReturnsUpcomingEvents()
    {
        using var context = CreateContext();

        context.Events.AddRange(
            CreateEvent("Future Marathon", DateTime.Today.AddDays(10)),
            CreateEvent("Past Marathon", DateTime.Today.AddDays(-10)),
            CreateEvent(
                "Cancelled Marathon",
                DateTime.Today.AddDays(5),
                isCancelled: true));

        await context.SaveChangesAsync();

        var controller = new EventsController(context);

        var result = await controller.GetEvents();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<IEnumerable<Event>>(
            okResult.Value);

        var returnedEvents = events.ToList();

        var singleEvent = Assert.Single(returnedEvents);
        Assert.Equal("Future Marathon", singleEvent.EventName);
    }

    [Fact]
    public async Task GetEvents_ReturnsEventsInDateOrder()
    {
        using var context = CreateContext();

        context.Events.AddRange(
            CreateEvent("Later Event", DateTime.Today.AddDays(20)),
            CreateEvent("Earlier Event", DateTime.Today.AddDays(5)),
            CreateEvent("Middle Event", DateTime.Today.AddDays(10)));

        await context.SaveChangesAsync();

        var controller = new EventsController(context);

        var result = await controller.GetEvents();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<IEnumerable<Event>>(
            okResult.Value);

        Assert.Equal(
            new[] { "Earlier Event", "Middle Event", "Later Event" },
            events.Select(e => e.EventName).ToArray());
    }

    [Fact]
    public async Task GetEvent_ExistingId_ReturnsCorrectEvent()
    {
        using var context = CreateContext();

        var expectedEvent = CreateEvent(
            "Cape Town Cycle Tour",
            DateTime.Today.AddDays(30));

        context.Events.Add(expectedEvent);
        await context.SaveChangesAsync();

        var controller = new EventsController(context);

        var result = await controller.GetEvent(expectedEvent.EventId);

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedEvent = Assert.IsType<Event>(okResult.Value);

        Assert.Equal(expectedEvent.EventId, returnedEvent.EventId);
        Assert.Equal("Cape Town Cycle Tour", returnedEvent.EventName);
    }

    [Fact]
    public async Task GetEvent_UnknownId_ReturnsNotFound()
    {
        using var context = CreateContext();

        var controller = new EventsController(context);

        var result = await controller.GetEvent(99999);

        Assert.IsType<NotFoundResult>(result.Result);
    }
}
