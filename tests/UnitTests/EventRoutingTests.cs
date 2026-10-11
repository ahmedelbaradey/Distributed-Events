using Shared.Contracts;
using Xunit;

namespace UnitTests;

public class EventRoutingTests
{
    [Fact]
    public void CriticalSeverity_ShouldRouteToCriticalQueue()
    {
        var route = EventRouting.Route(EventType.AuthenticationFailure, Severity.Critical);

        Assert.Equal(EventRoute.Critical, route);
        Assert.Equal(QueueNames.AlertCriticalRoutingKey, EventRouting.RouteKey(EventType.AuthenticationFailure, Severity.Critical));
    }

    [Fact]
    public void AnalyticsEvent_ShouldRouteToAnalyticsQueue()
    {
        var route = EventRouting.Route(EventType.AccessDenied, Severity.Medium);

        Assert.Equal(EventRoute.Analytics, route);
        Assert.Equal(QueueNames.EventAnalyticsRoutingKey, EventRouting.RouteKey(EventType.AccessDenied, Severity.Medium));
    }

    [Fact]
    public void SystemHealth_ShouldRouteToNotificationQueue()
    {
        var route = EventRouting.Route(EventType.SystemHealth, Severity.Low);

        Assert.Equal(EventRoute.Notification, route);
        Assert.Equal(QueueNames.NotificationRoutingKey, EventRouting.RouteKey(EventType.SystemHealth, Severity.Low));
    }
}
