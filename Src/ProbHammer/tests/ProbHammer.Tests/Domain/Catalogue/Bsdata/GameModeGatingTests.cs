using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Regression coverage for a real user-reported bug: /LivePlay showed abilities that
/// don't belong to matched play at all (Chaos Boons, Mark of Chaos options) for every unit in an
/// imported army. Root cause: BsdataDatasheetMapper's walk had no way to distinguish genuine
/// datasheet rules from content BSData itself marks as hidden outside "Army Roster" (matched-play)
/// mode - both use the identical "Abilities"-typeName profile shape. Fixed by recognizing the one
/// real modifier shape BSData uses for this (see BsdataDatasheetMapper.IsGameModeGated), resolved
/// by name against the game system's ForceEntries rather than a hardcoded id.</summary>
public class GameModeGatingTests
{
    private static ProbHammer.Core.Domain.Catalogue.Datasheet Build(IReadOnlyList<BsForceEntry>? forceEntries)
    {
        var closure = BsdataClosureResolver.Resolve(BsdataFixtures.Source(), "game-mode-gated-ability.json");
        var entry = BsdataNameResolver.Resolve(closure, "Test Unit")!;
        var idIndex = BsdataNameResolver.BuildIdIndex(closure);
        var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
        return BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, forceEntries: forceEntries);
    }

    [Fact]
    public void IntrinsicAbility_IsAlwaysIncluded()
    {
        var sheet = Build([new BsForceEntry { Id = "force-crusade", Name = "Crusade Force" }]);

        sheet.Abilities.Select(a => a.Name).Should().Contain("Intrinsic Ability");
    }

    [Fact]
    public void ContentHiddenUnlessCrusadeForcePresent_IsExcluded_WhenCrusadeForceIsResolved()
    {
        // Real shape: every datasheet's "Crusade" entryLink (Battle Honours, Chaos Boons, Crusade
        // Relic Upgrades) is hidden via a flat `conditions` list referencing "Crusade Force".
        var sheet = Build([new BsForceEntry { Id = "force-crusade", Name = "Crusade Force" }]);

        sheet.Abilities.Select(a => a.Name).Should().NotContain("Warp Stalker");
    }

    [Fact]
    public void ContentHiddenWhenArmyRosterPresent_IsExcluded_WhenArmyRosterIsResolved()
    {
        // Real shape: Chaos Space Marines' "Mark of Chaos" is hidden via a nested `conditionGroups`
        // referencing "Army Roster" - the opposite polarity from the Crusade case, same mechanism.
        var sheet = Build([new BsForceEntry { Id = "force-army-roster", Name = "Army Roster" }]);

        sheet.Abilities.Select(a => a.Name).Should().NotContain("Fake Mark Option");
    }

    [Fact]
    public void BothGatedAbilities_AreExcluded_WhenBothForceEntriesAreResolved()
    {
        var sheet = Build(
        [
            new BsForceEntry { Id = "force-crusade", Name = "Crusade Force" },
            new BsForceEntry { Id = "force-army-roster", Name = "Army Roster" }
        ]);

        sheet.Abilities.Select(a => a.Name).Should().BeEquivalentTo(["Intrinsic Ability"]);
    }

    [Fact]
    public void GatedContent_IsIncluded_WhenNoForceEntriesAreProvided()
    {
        // No game-system force entries reachable (e.g. BuildDatasheet's default forceEntries: null
        // - used by every pre-existing call site that doesn't pass one) means the gate can't be
        // resolved, so nothing is excluded - fails open rather than silently dropping content this
        // loader can't actually evaluate.
        var sheet = Build(forceEntries: null);

        sheet.Abilities.Select(a => a.Name).Should().Contain("Warp Stalker").And.Contain("Fake Mark Option");
    }
}
