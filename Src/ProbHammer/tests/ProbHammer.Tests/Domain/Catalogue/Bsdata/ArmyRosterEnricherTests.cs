using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

public class ArmyRosterEnricherTests
{
    private static ResolvedBsdataCatalogue CrusaderSquadCatalogue() =>
        ResolvedBsdataCatalogue.Build(BsdataFixtures.Source(), "crusader-squad-enrichment.json");

    private static ParsedArmyList ArmyListWith(
        IReadOnlyList<ParsedAttachmentGroup>? attachmentGroups = null,
        IReadOnlyList<ParsedUnit>? standaloneUnits = null) =>
        new(
            Name: "Test Army",
            PointsSpent: 500,
            Faction: ["Space Marines", "Black Templars"],
            Detachments: ["Test Detachment"],
            ForceDisposition: "Test Disposition",
            BattleSize: "Incursion",
            PointsLimit: 1000,
            AttachmentGroups: attachmentGroups ?? [],
            StandaloneUnits: standaloneUnits ?? []);

    private static ParsedUnit CrusaderSquadUnit(IReadOnlyList<ParsedModelGroup>? modelGroups = null) =>
        new(
            Name: "Crusader Squad",
            ModelGroups: modelGroups ??
            [
                new ParsedModelGroup("Sword Brother", 1, ["Master-crafted power weapon", "Pyre pistol"])
            ],
            Enhancements: []);

    [Fact]
    public void ArmyMetadata_IsCarriedThroughOntoTheArmyRoster()
    {
        var parsed = ArmyListWith(standaloneUnits: [CrusaderSquadUnit()]);

        var roster = ArmyRosterEnricher.Enrich(parsed, CrusaderSquadCatalogue());

        roster.Name.Should().Be("Test Army");
        roster.PointsSpent.Should().Be(500);
        roster.Faction.Should().Equal("Space Marines", "Black Templars");
        roster.Detachments.Select(d => d.Name).Should().Equal("Test Detachment");
        roster.ForceDisposition.Should().Be("Test Disposition");
        roster.BattleSize.Should().Be("Incursion");
        roster.PointsLimit.Should().Be(1000);
    }

    [Fact]
    public void StandaloneParsedUnit_ResolvesToAPlainUnit()
    {
        var parsed = ArmyListWith(standaloneUnits: [CrusaderSquadUnit()]);

        var roster = ArmyRosterEnricher.Enrich(parsed, CrusaderSquadCatalogue());

        roster.Units.Should().ContainSingle();
        var unit = roster.Units[0].Should().BeOfType<Unit>().Subject;
        unit.Datasheet.Name.Should().Be("Crusader Squad");
        unit.ModelLines.Should().ContainSingle(ml => ml.StatlineName == "Sword Brother");
    }

    [Fact]
    public void AttachmentGroup_ResolvesToAnAttachedUnit_WithBodyguardAndAttachedInOrder()
    {
        var bodyguard = CrusaderSquadUnit();
        var leader = new ParsedUnit("Sword Brother", [new ParsedModelGroup("Sword Brother", 1, ["Pyre pistol"])], []);
        var parsed = ArmyListWith(attachmentGroups: [new ParsedAttachmentGroup(bodyguard, [leader])]);

        var roster = ArmyRosterEnricher.Enrich(parsed, CrusaderSquadCatalogue());

        roster.Units.Should().ContainSingle();
        var attachedUnit = roster.Units[0].Should().BeOfType<AttachedUnit>().Subject;
        attachedUnit.Bodyguard.Datasheet.Name.Should().Be("Crusader Squad");
        attachedUnit.Attached.Should().ContainSingle(u => u.Datasheet.Name == "Sword Brother");
    }

    [Fact]
    public void SplitModelGroup_ProducesTwoModelLines_SharingOneStatlineName()
    {
        var modelGroups = new ParsedModelGroup[]
        {
            new("Initiate", 3, ["Bolt pistol", "Close combat weapon", "Astartes chainsword"]),
            new("Initiate", 2, ["Bolt pistol", "Close combat weapon", "Power fist"])
        };
        var parsed = ArmyListWith(standaloneUnits: [CrusaderSquadUnit(modelGroups)]);

        var roster = ArmyRosterEnricher.Enrich(parsed, CrusaderSquadCatalogue());

        var unit = (Unit)roster.Units[0];
        var initiateLines = unit.ModelLines.Where(ml => ml.StatlineName == "Initiate").ToList();
        initiateLines.Should().HaveCount(2);
        initiateLines.Should().ContainSingle(ml => ml.Count == 3 && ml.Weapons.Contains("Astartes chainsword"));
        initiateLines.Should().ContainSingle(ml => ml.Count == 2 && ml.Weapons.Contains("Power fist"));
    }

    [Fact]
    public void UnresolvableUnitName_ThrowsNamingTheUnresolvedText()
    {
        var parsed = ArmyListWith(standaloneUnits:
        [
            new ParsedUnit("No Such Unit", [new ParsedModelGroup("No Such Unit", 1, [])], [])
        ]);

        var act = () => ArmyRosterEnricher.Enrich(parsed, CrusaderSquadCatalogue());

        act.Should().Throw<BsdataNameResolutionException>().Which.Text.Should().Be("No Such Unit");
    }

    [Fact]
    public void UnresolvableWeaponName_ThrowsFailingLoud()
    {
        var parsed = ArmyListWith(standaloneUnits:
        [
            CrusaderSquadUnit([new ParsedModelGroup("Sword Brother", 1, ["No Such Weapon"])])
        ]);

        var act = () => ArmyRosterEnricher.Enrich(parsed, CrusaderSquadCatalogue());

        act.Should().Throw<BsdataNameResolutionException>().Which.Text.Should().Be("No Such Weapon");
    }

    [Fact]
    public void UnresolvableStatlineName_ThrowsFailingLoud()
    {
        // The Crusader Squad fixture has three statlines (Sword Brother/Initiate/Neophyte), so the
        // sole-statline fallback doesn't apply here either - a genuinely unresolvable name.
        var parsed = ArmyListWith(standaloneUnits:
        [
            CrusaderSquadUnit([new ParsedModelGroup("No Such Model", 1, [])])
        ]);

        var act = () => ArmyRosterEnricher.Enrich(parsed, CrusaderSquadCatalogue());

        act.Should().Throw<BsdataNameResolutionException>().Which.Text.Should().Be("No Such Model");
    }

    [Fact]
    public void ModelGroupNamedAfterARolePerSquad_ResolvesExactlyAsBefore()
    {
        // Crusader Squad's own statlines are named per-role ("Sword Brother"), so the exact-match
        // path is exercised, not the datasheet-name fallback.
        var parsed = ArmyListWith(standaloneUnits: [CrusaderSquadUnit()]);

        var roster = ArmyRosterEnricher.Enrich(parsed, CrusaderSquadCatalogue());

        var unit = (Unit)roster.Units[0];
        unit.ModelLines.Single().StatlineName.Should().Be("Sword Brother");
    }

    [Fact]
    public void ModelGroupWithNoExactMatch_FallsBackToTheDatasheetsSoleStatline()
    {
        // Mirrors real, confirmed shapes against the live BSData clone: a datasheet's one-and-only
        // Statline is often named nothing an export label or the Datasheet's own name would predict
        // (e.g. "Impulsor" -> "Black Templars Impulsor", "Emperor's Champion" -> "The Emperor's
        // Champion", "Sword Brethren Squad" -> "Sword Brethren") - but with only one Statline in
        // play, any model group name unambiguously refers to it regardless of what it's called.
        var parsed = ArmyListWith(standaloneUnits:
        [
            new ParsedUnit("Crusader Squad", [new ParsedModelGroup("Trooper", 1, [])], [])
        ]);

        var roster = ArmyRosterEnricher.Enrich(parsed, MultiProfileWeaponCatalogue());

        var unit = (Unit)roster.Units[0];
        unit.ModelLines.Single().StatlineName.Should().Be("Sword Brother");
    }

    [Fact]
    public void MultiProfileWeapon_ExpandsToEveryModeProfile()
    {
        // Mirrors a real, confirmed shape (High Marshal Helbrecht's "Sword of the High Marshals"
        // against the live BSData clone): one exported wargear line resolves to more than one BSData
        // weapon profile, each suffixed with its own attack mode.
        var parsed = ArmyListWith(standaloneUnits:
        [
            CrusaderSquadUnit([new ParsedModelGroup("Sword Brother", 1, ["Ancestor sword"])])
        ]);

        var roster = ArmyRosterEnricher.Enrich(parsed, MultiProfileWeaponCatalogue());

        var unit = (Unit)roster.Units[0];
        unit.ModelLines.Single().Weapons.Should().BeEquivalentTo(
            ["➤ Ancestor sword - Sweep", "➤ Ancestor sword - Strike"]);
    }

    [Fact]
    public void NonWeaponWargear_ResolvesAsAnAbilityOnTheModelLine_NotAWeapon()
    {
        // Mirrors a real, confirmed shape (Impulsor's "Shield Dome" against the live BSData clone):
        // some exported wargear lines are BSData Abilities, not weapons at all.
        var parsed = ArmyListWith(standaloneUnits:
        [
            CrusaderSquadUnit([new ParsedModelGroup("Sword Brother", 1, ["Shield Dome"])])
        ]);

        var roster = ArmyRosterEnricher.Enrich(parsed, MultiProfileWeaponCatalogue());

        var unit = (Unit)roster.Units[0];
        var modelLine = unit.ModelLines.Single();
        modelLine.Weapons.Should().BeEmpty();
        modelLine.Abilities.Should().ContainSingle(a => a.Name == "Shield Dome");
    }

    private static ResolvedBsdataCatalogue MultiProfileWeaponCatalogue() =>
        ResolvedBsdataCatalogue.Build(BsdataFixtures.Source(), "crusader-squad-multiprofile-weapon.json");

    private static ResolvedBsdataCatalogue AbilityClassificationCatalogue() =>
        ResolvedBsdataCatalogue.Build(BsdataFixtures.Source(), "ability-classification.json");

    private static ParsedUnit TestUnit(IReadOnlyList<string>? enhancements = null) =>
        new(
            Name: "Test Unit",
            ModelGroups: [new ParsedModelGroup("Test Unit", 1, [])],
            Enhancements: enhancements ?? []);

    [Fact]
    public void ASelectedEnhancement_ResolvesToItsAbilityText()
    {
        var parsed = ArmyListWith(standaloneUnits: [TestUnit(["Some Enhancement"])]);

        var roster = ArmyRosterEnricher.Enrich(parsed, AbilityClassificationCatalogue());

        var unit = (Unit)roster.Units[0];
        unit.Enhancements.Should().ContainSingle(a => a.Name == "Some Enhancement" && a.Text == "An Enhancement.");
    }

    [Fact]
    public void AnUnresolvableEnhancementName_ThrowsNamingTheUnresolvedText()
    {
        var parsed = ArmyListWith(standaloneUnits: [TestUnit(["Does not exist"])]);

        var act = () => ArmyRosterEnricher.Enrich(parsed, AbilityClassificationCatalogue());

        act.Should().Throw<BsdataNameResolutionException>().Where(e => e.Text == "Does not exist");
    }

    [Fact]
    public void AUnitWithNoEnhancementsSelected_ResolvesWithAnEmptyEnhancementsList()
    {
        // The Datasheet defines "Some Enhancement" as available - it must not be attached just
        // because it exists, only when the export actually selected it (resolve-enhancement-
        // abilities' core fix).
        var parsed = ArmyListWith(standaloneUnits: [TestUnit()]);

        var roster = ArmyRosterEnricher.Enrich(parsed, AbilityClassificationCatalogue());

        var unit = (Unit)roster.Units[0];
        unit.Enhancements.Should().BeEmpty();
    }
}