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
        var route = EventRouting.Route(EventType.AuditTrail, Severity.Low);

        Assert.Equal(EventRoute.Analytics, route);
        Assert.Equal(QueueNames.EventAnalyticsRoutingKey, EventRouting.RouteKey(EventType.AuditTrail, Severity.Low));
    }
}
