using ProbHammer.Core.Domain.Catalogue.Bsdata;
using ProbHammer.Core.Domain.Import;
using ProbHammer.Core.Domain.Import.BattleScribe;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Web.Services;

/// <summary>Builds a live <see cref="ArmyRoster"/> from a <see cref="StoredArmyImport"/>, dispatching
/// internally on which pipeline produced it: a <see cref="TextArmyImport"/> resolves its faction's
/// starting catalogue file, fetches (or builds and caches) its
/// <see cref="ResolvedBsdataCatalogue"/> via the app-wide <see cref="BsdataCatalogueCache"/>, then
/// runs <see cref="ArmyRosterEnricher"/>; a <see cref="BattleScribeArmyImport"/> runs
/// <see cref="BattleScribeRosterMapper"/> directly, with no BSData catalogue involvement at all -
/// see import-battlescribe-json-rosters' design.md's "Format-discriminated session storage and
/// shared Build". Shared by the import page (to validate before committing to session) and
/// `/LivePlay`'s own rebuild-fresh-every-request path (see design.md's "Session stores the
/// intermediate, not the graph") so both call sites share one orchestration instead of duplicating
/// it, regardless of which format was originally submitted.</summary>
public interface IArmyRosterProvider
{
    ArmyRosterBuildResult Build(StoredArmyImport import);
}

/// <summary>Bundles the built <see cref="ArmyRoster"/> with a <see cref="RuleGlossary"/> - a
/// BSData-faction-closure-sourced one for a <see cref="TextArmyImport"/>, a roster-scoped one (see
/// <see cref="BattleScribeRuleGlossaryBuilder"/>) for a <see cref="BattleScribeArmyImport"/> -
/// either way giving callers that need to look up ability/weapon-keyword rule text (rules-glossary)
/// one, without needing to know which pipeline produced it.</summary>
public sealed record ArmyRosterBuildResult(ArmyRoster Roster, RuleGlossary Glossary);

public sealed class ArmyRosterProvider(BsdataCatalogueCache cache, IBsdataCatalogueSource source) : IArmyRosterProvider
{
    public ArmyRosterBuildResult Build(StoredArmyImport import) => import switch
    {
        TextArmyImport text => BuildFromText(text.ParsedArmyList),
        BattleScribeArmyImport battleScribe => BuildFromBattleScribe(battleScribe),
        _ => throw new ArgumentOutOfRangeException(nameof(import), import, "Unrecognized StoredArmyImport variant.")
    };

    private ArmyRosterBuildResult BuildFromText(ParsedArmyList parsedArmyList)
    {
        var startingFile = BsdataFactionResolver.ResolveStartingFileName(parsedArmyList.Faction, source.ListFileNames());
        var catalogue = cache.GetOrBuild(startingFile);
        var roster = ArmyRosterEnricher.Enrich(parsedArmyList, catalogue);
        return new ArmyRosterBuildResult(roster, catalogue.Glossary);
    }

    private static ArmyRosterBuildResult BuildFromBattleScribe(BattleScribeArmyImport import)
    {
        var roster = BattleScribeRosterMapper.Map(import.Roster);
        var glossary = BattleScribeRuleGlossaryBuilder.Build(import.Roster);
        return new ArmyRosterBuildResult(roster, glossary);
    }
}
