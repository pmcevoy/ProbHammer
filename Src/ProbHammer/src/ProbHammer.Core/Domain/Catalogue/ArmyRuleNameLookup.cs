namespace ProbHammer.Core.Domain.Catalogue;

/// <summary>
/// Curated, per-faction table of known army-wide rule names (see
/// openspec/changes/classify-known-army-rules/design.md's "Initial table contents") - the sole
/// source of truth for classifying a Core Rule Ability Extraction reference's Origin as ArmyRule
/// (see <c>BsdataDatasheetMapper</c>'s "Core Rule Ability Extraction" and
/// <c>BattleScribeRosterMapper</c>'s "Core Rule Extraction"). Deliberately decoupled from
/// <c>Domain.Catalogue.Bsdata</c> - the BattleScribe/JSON pipeline has no BSData dependency at all
/// and must be able to use this table without introducing one.
///
/// Also carries a small exclusion list documenting confirmed non-army-rule names that share the
/// same BSData "primary-catalogue"-scoped gating shape as a genuine army rule (mustering/
/// composition rules, not gameplay rules) - consulted only by the corpus scan's own triage check
/// (bsdata-corpus-scan's Full-Corpus Primary-Catalogue-Gated Rule Triage Scan), never at runtime:
/// a name simply absent from <see cref="InclusionTable"/> already classifies Core Rule by default.
/// </summary>
public static class ArmyRuleNameLookup
{
    /// <summary>Keyed by the most-specific Faction entry (see <see cref="Resolve"/>). Every named
    /// Space Marine chapter gets its own row even though all but Black Templars/Grey Knights
    /// currently resolve to the same name - each chapter has its own dedicated catalogue file and
    /// is therefore its own most-specific Faction entry (see design.md's Faction-Scoped
    /// Resolution decision).</summary>
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> InclusionTable =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            // The rule's own declared BSData name carries a "(Aura)" suffix baked directly into
            // its Name (not a modifier-appended display name) - confirmed both in the bundled
            // BSData clone (Chaos - Death Guard.json's own local sharedRules) and in the real
            // captured data/gw-android-export-deathguard.json roster export (identical id
            // "ea42-7d15-fd81-73d6"). Caught by ArmyRuleNameResolutionScanTests' first real run.
            ["Death Guard"] = ["Nurgle's Gift (Aura)"],
            // Real BSData catalogue file is "Imperium - Adeptus Custodes.json" and a real captured
            // export's own catalogueName leaf is "Adeptus Custodes" (confirmed against
            // data/gw-android-export-custodes.json), not the shorter "Custodes" originally
            // guessed here - caught by ArmyRuleNameResolutionScanTests' first real run.
            ["Adeptus Custodes"] = ["Martial Ka'tah"],
            // Real BSData catalogue file is "Aeldari - Craftworlds.json" (the playable Aeldari
            // codex is titled "Craftworlds"; "Aeldari - Aeldari Library.json" is shared content,
            // not a playable faction identity itself) - "Aeldari" alone never matches any real
            // catalogue file, caught the same way as Adeptus Custodes above.
            ["Craftworlds"] = ["Battle Focus"],
            ["Drukhari"] = ["Power from Pain"],
            ["Chaos Daemons"] = ["The Shadow of Chaos"],
            ["Genestealer Cults"] = ["Cult Ambush"],
            ["Chaos Knights"] = ["Harbingers of Dread"],
            ["Imperial Knights"] = ["Code Chivalric"],
            ["Black Templars"] = ["Templar Vows"],
            ["Grey Knights"] = ["Gate of Infinity"],
            // Found by PrimaryCatalogueGatedRuleTriageScanTests' first real run: both rule
            // definitions live in the shared "Library - Tyranids.json" file, gated by
            // primary-catalogue (hidden unless the army's primary catalogue IS Tyranids), and
            // both open with the same "If your Army Faction is TYRANIDS" signature phrase every
            // other confirmed genuine army rule in this table uses - unlike a mustering/
            // composition rule (Assigned Agents et al.), each describes a standing army-wide
            // effect (Battle-shock/melee-Strength bonuses while in Synapse Range; a once-per-
            // battle army-wide Battle-shock effect on every enemy unit), not a model-granted
            // keyword ability. Tyranids is a real, confirmed case of two simultaneous ArmyRule-
            // origin abilities in one faction - see LivePlayArmyHeaderRenderingTests'
            // TwoSimultaneousArmyRuleAbilities test, which now cites this real pair instead of a
            // hand-picked hypothetical.
            ["Tyranids"] = ["Synapse", "Shadow in the Warp"],
            // Full-Faction Coverage Scan (ArmyRuleNameCoverageScanTests) found roughly half the
            // real corpus had no entry at all - every row from here down was added to close that
            // gap, each independently validated against Wahapedia (not just the frequency +
            // "If your Army Faction is..." text-shape signal that surfaced it as a candidate),
            // ordered alphabetically by faction.
            ["Adepta Sororitas"] = ["Acts of Faith"],
            ["Adeptus Mechanicus"] = ["Doctrina Imperatives"],
            // Real BSData catalogue file is "Imperium - Agents of the Imperium.json" and a real
            // export's catalogueName leaf would be "Agents of the Imperium" - the rule itself is
            // "Kill Team", not "Assigned Agents" (which remains on ExclusionList below - a
            // composition-eligibility rule for OTHER factions taking Agents of the Imperium allies,
            // not this faction's own army rule).
            ["Agents of the Imperium"] = ["Kill Team"],
            // Confirmed via Wahapedia validation to be a genuine ArmyRule, not the mustering/
            // composition rule its gating shape originally suggested - moved here from
            // ExclusionList (see that list's own comment below). Its own rule text opens with the
            // identical "If your Army Faction is Astra Militarum..." signature every other row in
            // this table shares.
            ["Astra Militarum"] = ["Voice Of Command"],
            ["Chaos Space Marines"] = ["Dark Pacts"],
            ["Emperor's Children"] = ["Thrill Seekers"],
            ["Leagues of Votann"] = ["Prioritised Efficiency"],
            ["Necrons"] = ["Reanimation Protocols"],
            ["Orks"] = ["Waaagh!"],
            ["T'au Empire"] = ["For The Greater Good"],
            ["Thousand Sons"] = ["Cabal of Sorcerers"],
            ["World Eaters"] = ["Blessings of Khorne"],
            // Confirmed via Wahapedia validation: no single unifying army-wide rule exists for this
            // catalogue (a rules-light bucket for unaligned/renegade allies, not a real playable
            // army in the same sense as the rows above) - an explicit empty entry, not an absent
            // key, so ArmyRuleNameCoverageScanTests treats this as "checked, found nothing" rather
            // than "nobody has looked yet".
            ["Unaligned Forces"] = [],
            // The rule names surfaced by earlier external research ("Towering Example" for
            // Adeptus Titanicus) do not appear anywhere in the bundled BSData corpus snapshot.
            // Adding an unverifiable name would violate this table's own "verified to resolve"
            // requirement; an explicit empty entry satisfies ArmyRuleNameCoverageScanTests without
            // guessing.
            ["Adeptus Titanicus"] = [],
            ["Titanicus Traitoris"] = [],
            ["Space Marines"] = ["Oath of Moment"],
            ["Blood Angels"] = ["Oath of Moment"],
            ["Dark Angels"] = ["Oath of Moment"],
            ["Deathwatch"] = ["Oath of Moment"],
            ["Imperial Fists"] = ["Oath of Moment"],
            ["Iron Hands"] = ["Oath of Moment"],
            ["Raven Guard"] = ["Oath of Moment"],
            ["Salamanders"] = ["Oath of Moment"],
            ["Space Wolves"] = ["Oath of Moment"],
            ["Ultramarines"] = ["Oath of Moment"],
            ["White Scars"] = ["Oath of Moment"]
        };

    /// <summary>Confirmed mustering/composition rules that carry BSData's chapter/sub-faction
    /// "primary-catalogue"-scoped gating shape but are not genuine army-wide gameplay rules -
    /// see design.md's "Exclusion list contents". Consulted only by
    /// bsdata-corpus-scan's Full-Corpus Primary-Catalogue-Gated Rule Triage Scan, never by
    /// <see cref="Resolve"/>.</summary>
    public static IReadOnlyList<(string Name, string Reason)> ExclusionList { get; } =
    [
        ("Assigned Agents", "Composition eligibility for allied Agents of the Imperium units, not a gameplay rule"),
        ("Disparate Paths", "Composition eligibility for allied Harlequins units (Aeldari), not a gameplay rule"),
        ("Corsairs and Travelling Players",
            "Composition eligibility for allied Harlequins/Anhrathe units (Drukhari), not a gameplay rule")
        // "Voice Of Command" was briefly listed here after PrimaryCatalogueGatedRuleTriageScanTests'
        // first real run reasoned it as a model-granted keyword ability, not a standing roster-wide
        // fact - reconsidered and moved to InclusionTable's Astra Militarum row instead once
        // Full-Faction Coverage validation (against Wahapedia) confirmed it IS that faction's own
        // genuine ArmyRule; its text opens with the same "If your Army Faction is X" signature every
        // other confirmed row shares, which the original exclusion reasoning didn't weigh.
    ];

    /// <summary>Every distinct rule name appearing anywhere in <see cref="InclusionTable"/>,
    /// across every faction - used only by bsdata-corpus-scan's Full-Corpus Primary-Catalogue-
    /// Gated Rule Triage Scan to filter out gated rules already accounted for by the inclusion
    /// table before checking what remains against the exclusion list.</summary>
    public static IReadOnlySet<string> AllKnownNames { get; } =
        InclusionTable.Values.SelectMany(names => names).ToHashSet(StringComparer.OrdinalIgnoreCase);

    /// <summary>Every (Faction, Names) entry in <see cref="InclusionTable"/>, exposed only so
    /// bsdata-corpus-scan's Full-Corpus Army-Rule Name Resolution Scan can resolve each entry's
    /// own faction against the live BSData clone and re-verify its names still resolve - ordinary
    /// runtime callers use <see cref="Resolve"/> instead.</summary>
    public static IReadOnlyList<(string Faction, IReadOnlyList<string> Names)> Entries { get; } =
        InclusionTable.Select(kvp => (kvp.Key, kvp.Value)).ToList();

    /// <summary>Resolves the known army-wide rule name set for an ordered Faction list, using the
    /// same "most specific (last) Faction entry" convention <c>BsdataFactionResolver
    /// .ResolveStartingFileName</c> already uses for catalogue file resolution - so a sub-faction
    /// (e.g. Black Templars under Space Marines) resolves against its own entry, never its parent
    /// codex's. A faction with no table entry resolves to an empty set, not an error.</summary>
    public static IReadOnlySet<string> Resolve(IReadOnlyList<string> faction)
    {
        if (faction.Count == 0)
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var mostSpecific = faction[^1];
        return InclusionTable.TryGetValue(mostSpecific, out var names)
            ? new HashSet<string>(names, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}