using FluentAssertions;
using ProbHammer.Web.Pages;

namespace ProbHammer.Tests.Web;

public class LivePlayModelTests
{
    [Fact]
    public void OnGet_OrdersAttachedUnitsBeforePlainUnits_LargestFirstWithinEachGroup_TiesByName()
    {
        var model = new LivePlayModel();

        model.OnGet();

        // Attached-sourced units (Crusader Squad x2 @ 12 models, Sword Bretheren @ 5) precede
        // plain-Unit-sourced ones (Assault Intercessor / Scout @ 5 models, Impulsor @ 1). Within
        // the tied 12-model attached pair, "High Marshal Helbrecht..." sorts before "Marshal..."
        // ascending; within the tied 5-model plain pair, "Assault Intercessor" sorts before "Scout".
        model.Units.Select(u => u.Name).Should().Equal(
            "Crusader Squad with High Marshal Helbrecht and Crusade Ancient",
            "Crusader Squad with Marshal and Lieutenant",
            "Sword Bretheren Squad with Marshal",
            "Assault Intercessor Squad",
            "Scout Squad",
            "Impulsor");
    }
}
