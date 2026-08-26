using System.Text.Json;
using System.Text.RegularExpressions;
using ProbHammer.Core.Domain.Catalogue.Bsdata.Json;

namespace ProbHammer.Core.Domain.Catalogue.Bsdata;

/// <summary>
/// Maps a resolved <see cref="BsSelectionEntry"/> (see <see cref="BsdataNameResolver"/>) into a
/// <see cref="Datasheet"/>. Walks the entry's full descendant tree once, collecting every
/// "Unit"-typeName profile as a named <see cref="Statline"/> (deduped by name, first occurrence
/// wins, in declared order - this single walk handles both the single-model case, where the
/// profile sits on the entry itself, and the squad case, where it sits on distinct nested child
/// entries, per design.md's squad-detection decision), every "Ranged Weapons"/"Melee
/// Weapons"-typeName profile as a resolvable <see cref="WeaponProfile"/>, and every
/// "Abilities"-typeName profile as an <see cref="Ability"/>. EntryLinks are followed through an
/// id index built over the closure so that wargear defined via a shared entry (in this file or an
/// imported one) is still reachable - see design.md's "entryLink target location" risk. InfoLinks
/// of type "profile" are followed through a separate id index into BsCatalogue.SharedProfiles -
/// some model statlines (observed in real data: Chaos - Chaos Space Marines.json's "Legionaries"
/// squad reaches its troop model's Unit profile this way, not nested in any selectionEntry's own
/// Profiles list) exist only as a standalone shared profile referenced this way.
/// </summary>
public static partial class BsdataDatasheetMapper
{
    /// <summary>Force-type names whose id, if referenced by a "hidden=true" modifier's condition
    /// tree (see IsGameModeGated), marks an entry/group as belonging to a game mode other than the
    /// matched-play "Army Roster" this loader exclusively represents - resolved by name (never a
    /// hardcoded id) from the game system's own ForceEntries, matching this codebase's existing
    /// "resolve by name" convention. Both names are needed: real BSData content gates itself either
    /// as "hidden if force IS Army Roster" (confirmed: Chaos Space Marines' "Mark of Chaos", a
    /// genuine matched-play mechanic that's still Crusade-Force-only per NewRecruit's own UI) or as
    /// "hidden unless a Crusade Force is present" (confirmed: the "Crusade" content block every
    /// datasheet carries - Battle Honours, Chaos Boons, Crusade Relic Upgrades) - opposite polarity,
    /// same underlying game-mode concept, so both id references are treated as an exclusion
    /// trigger regardless of which specific comparison the modifier uses.</summary>
    private static readonly string[] GameModeGateNames = ["Army Roster", "Crusade Force"];

    public static Datasheet BuildDatasheet(
        BsSelectionEntry entry,
        IReadOnlyDictionary<string, BsSelectionEntry> idIndex,
        IReadOnlyDictionary<string, BsSelectionEntryGroup> groupIdIndex,
        IReadOnlyDictionary<string, BsProfile>? profileIdIndex = null,
        IReadOnlyList<BsForceEntry>? forceEntries = null,
        RuleGlossary? glossary = null,
        string? primaryCatalogueId = null,
        IReadOnlySet<string>? knownArmyRuleNames = null)
    {
        var gateIds = (forceEntries ?? [])
            .Where(f => GameModeGateNames.Contains(f.Name, StringComparer.OrdinalIgnoreCase))
            .Select(f => f.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var context = new WalkContext(idIndex, groupIdIndex, profileIdIndex ?? new Dictionary<string, BsProfile>(),
            gateIds, glossary, primaryCatalogueId, knownArmyRuleNames);
        WalkEntry(entry, context, [], enclosingGroupName: null);

        return new Datasheet(
            entry.Name,
            factionKeywords: [],
            keywords: [],
            abilities: context.Abilities,
            statlines: context.Statlines,
            weaponProfiles: context.Weapons.Values,
            optionalAbilities: context.OptionalAbilities.Values);
    }

    private sealed class WalkContext(
        IReadOnlyDictionary<string, BsSelectionEntry> idIndex,
        IReadOnlyDictionary<string, BsSelectionEntryGroup> groupIdIndex,
        IReadOnlyDictionary<string, BsProfile> profileIdIndex,
        IReadOnlySet<string> gameModeGateIds,
        RuleGlossary? glossary = null,
        string? primaryCatalogueId = null,
        IReadOnlySet<string>? knownArmyRuleNames = null)
    {
        public IReadOnlyDictionary<string, BsSelectionEntry> IdIndex { get; } = idIndex;
        public IReadOnlyDictionary<string, BsSelectionEntryGroup> GroupIdIndex { get; } = groupIdIndex;
        public IReadOnlyDictionary<string, BsProfile> ProfileIdIndex { get; } = profileIdIndex;
        public IReadOnlySet<string> GameModeGateIds { get; } = gameModeGateIds;
        public RuleGlossary? Glossary { get; } = glossary;

        /// <summary>The resolved army's own starting/primary catalogue id (e.g. Black Templars'
        /// own catalogue.id) - null when not supplied (every pre-existing BuildDatasheet call
        /// site). Used only to evaluate a "scope: primary-catalogue" hidden condition on a Core
        /// Rule's own target rule (chapter/sub-faction exclusivity) - see
        /// IsProvablyAlwaysTrueInMatchedPlay.</summary>
        public string? PrimaryCatalogueId { get; } = primaryCatalogueId;

        /// <summary>The roster's own known army-wide rule names (see
        /// ArmyRuleNameLookup.Resolve), resolved once per roster by the caller that owns Faction
        /// and threaded in here - the sole signal Core Rule Ability Extraction uses to decide
        /// ArmyRule vs. CoreRule Origin (classify-known-army-rules). Empty when not supplied
        /// (every pre-existing BuildDatasheet call site) - every reference then classifies
        /// CoreRule, same fail-open-default shape as PrimaryCatalogueId/GameModeGateIds.</summary>
        public IReadOnlySet<string> KnownArmyRuleNames { get; } =
            knownArmyRuleNames ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public List<(string Name, Statline Statline)> Statlines { get; } = [];
        public HashSet<string> SeenStatlineNames { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, WeaponProfile> Weapons { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<Ability> Abilities { get; } = [];
        public HashSet<string> SeenAbilityNames { get; } = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>Every ability found nested inside a "type: upgrade" selection entry (an
        /// Enhancement, or any other optional ability grant, e.g. Impulsor's "Shield Dome") -
        /// resolvable only via Datasheet.TryResolveAbility, never enumerated in the public
        /// Abilities list. Deduped by name via TryAdd (first occurrence wins), mirroring Weapons'
        /// own dedup convention exactly - no separate "Seen" set needed since Dictionary.TryAdd
        /// already provides it.</summary>
        public Dictionary<string, Ability> OptionalAbilities { get; } = new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> VisitedEntryIds { get; } = new(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> VisitedGroupIds { get; } = new(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>True when any of the given modifiers sets "hidden" to true in a way that is
    /// PROVABLY always excluded for the current context - either the matched-play "Army Roster"
    /// mode this loader exclusively represents, or (since gate-and-dedupe-core-rule-abilities) the
    /// resolved army's own chapter/sub-faction not matching a rule's own primary-catalogue
    /// exclusivity - see <see cref="IsProvablyAlwaysTrueInMatchedPlay"/>/
    /// <see cref="IsConditionProvablyTrue"/> for exactly what "provably" means for each. Despite
    /// the name (kept for minimal diff against its original game-mode-only purpose), this is now
    /// called on both entry/group modifiers (game-mode gating, its original use - found via a real
    /// user-reported bug: /LivePlay showed abilities like Chaos Boons/Mark of Chaos options that
    /// don't belong to matched play at all) and a resolved Core Rule's own target-rule modifiers
    /// (chapter gating - found via a real user-reported bug: a Black Templars roster showed both
    /// "Oath of Moment" and its own chapter-exclusive replacement "Templar Vows" together, and the
    /// same pipeline run against a Salamanders closure produced the identical, equally-wrong
    /// list).</summary>
    private static bool IsGameModeGated(IReadOnlyList<BsModifier> modifiers, WalkContext ctx)
    {
        if (ctx.GameModeGateIds.Count == 0 && ctx.PrimaryCatalogueId is null)
            return false;

        foreach (var modifier in modifiers)
        {
            if (modifier.Field != "hidden" || modifier.Value.ValueKind != JsonValueKind.True)
                continue;

            if (IsProvablyAlwaysTrueInMatchedPlay(modifier.Conditions, modifier.ConditionGroups, "and", ctx))
                return true;
        }

        return false;
    }

    /// <summary>Evaluates a condition tree (a flat conditions list plus a conditionGroups list,
    /// combined via <paramref name="combinator"/> - BattleScribe's own convention, matching how a
    /// single BsModifier's own top-level conditions/conditionGroups are implicitly ANDed together)
    /// against everything this loader can provably know about the current context - see
    /// <see cref="IsConditionProvablyTrue"/> for what each individual condition recognizes.
    /// An "and" combinator is provably true only when EVERY item is provably true - a gate
    /// condition combined via AND with an unrelated, unevaluated condition is NOT provably true,
    /// since that other condition could make the whole AND false. An "or" combinator is provably
    /// true when ANY item is - a gate condition combined via OR with anything else still forces
    /// the whole OR true regardless of that other condition. This distinction is a real,
    /// confirmed-necessary generalization, not a hypothetical: Black Templars' "Companions of
    /// Vehemence Enhancements" is hidden by `AND(forces(Crusade Force) < 1, selections(some other
    /// id) < 1)` - "hidden unless you have a Crusade Force OR this Detachment selected" - a plain
    /// existence-check treating the mere presence of the Crusade Force reference as sufficient
    /// would incorrectly exclude a real, always-available matched-play Enhancement for players
    /// using that exact Detachment.</summary>
    private static bool IsProvablyAlwaysTrueInMatchedPlay(
        IReadOnlyList<BsCondition> conditions, IReadOnlyList<BsConditionGroup> groups, string combinator,
        WalkContext ctx)
    {
        var items = conditions
            .Select(c => IsConditionProvablyTrue(c, ctx))
            .Concat(groups.Select(g => IsProvablyAlwaysTrueInMatchedPlay(g.Conditions, g.ConditionGroups, g.Type, ctx)))
            .ToList();

        if (items.Count == 0)
            return false;

        return combinator.Equals("or", StringComparison.OrdinalIgnoreCase)
            ? items.Any(x => x)
            : items.All(x => x);
    }

    /// <summary>Evaluates one condition against everything this loader provably knows. A
    /// "scope: primary-catalogue" condition (chapter/sub-faction exclusivity - confirmed real
    /// shape: Oath of Moment's own rule definition is hidden unless the army's primary catalogue
    /// is one of 11 named chapters; Templar Vows' is hidden unless it's specifically Black
    /// Templars, by catalogue id, not name) is evaluated WITH its own comparison Type against
    /// <see cref="WalkContext.PrimaryCatalogueId"/> - unlike the game-mode gate check below, this
    /// can't ignore polarity: "notInstanceOf" is true when the ids differ, "instanceOf" when they
    /// match, since a real corpus condition uses each direction for a genuinely different meaning.
    /// Every other scope (the existing "force"/"roster" game-mode gate) keeps the prior,
    /// type-agnostic "is this id one of the known gate ids" check unchanged - confirmed safe for
    /// both real shapes seen there regardless of comparison type. A condition matching neither is
    /// unknowable (false), same conservative default as before.</summary>
    private static bool IsConditionProvablyTrue(BsCondition c, WalkContext ctx)
    {
        if (c.Scope == "primary-catalogue")
        {
            if (ctx.PrimaryCatalogueId is null)
                return false;

            return c.Type switch
            {
                "notInstanceOf" => !string.Equals(c.ChildId, ctx.PrimaryCatalogueId,
                    StringComparison.OrdinalIgnoreCase),
                "instanceOf" => string.Equals(c.ChildId, ctx.PrimaryCatalogueId, StringComparison.OrdinalIgnoreCase),
                _ => false
            };
        }

        return ctx.GameModeGateIds.Contains(c.ChildId);
    }

    private static void WalkEntry(BsSelectionEntry entry, WalkContext ctx, IReadOnlyList<BsSelectionEntry> ancestry,
        string? enclosingGroupName)
    {
        if (!string.IsNullOrEmpty(entry.Id) && !ctx.VisitedEntryIds.Add(entry.Id))
            return;

        if (IsGameModeGated(entry.Modifiers, ctx))
            return;

        var chain = new List<BsSelectionEntry>(ancestry) { entry };

        foreach (var profile in entry.Profiles)
            ProcessProfile(profile, ctx, chain, enclosingGroupName);

        foreach (var child in entry.SelectionEntries)
            WalkEntry(child, ctx, chain, enclosingGroupName);

        foreach (var group in entry.SelectionEntryGroups)
            WalkGroup(group, ctx, chain, enclosingGroupName);

        foreach (var link in entry.EntryLinks)
            WalkLink(link, ctx);

        foreach (var link in entry.InfoLinks)
            WalkInfoLink(link, ctx, chain, enclosingGroupName);
    }

    private static void WalkGroup(BsSelectionEntryGroup group, WalkContext ctx,
        IReadOnlyList<BsSelectionEntry> ancestry, string? enclosingGroupName)
    {
        if (!string.IsNullOrEmpty(group.Id) && !ctx.VisitedGroupIds.Add(group.Id))
            return;

        if (IsGameModeGated(group.Modifiers, ctx))
            return;

        // The nearest enclosing group's own Name wins over whatever was passed in - classification
        // (see ProcessProfile's Enhancement-vs-plain-grant split) always reads the *directly*
        // enclosing group, not some ancestor further up (confirmed necessary: Crusade Ancient
        // reaches "Thirst for Glory" through a group named "Legends of Saga and Song Enhancements",
        // not through the outer "Enhancements" group one hop up - see design.md).
        var nearestGroupName = group.Name;

        foreach (var child in group.SelectionEntries)
            WalkEntry(child, ctx, ancestry, nearestGroupName);

        foreach (var nested in group.SelectionEntryGroups)
            WalkGroup(nested, ctx, ancestry, nearestGroupName);

        foreach (var link in group.EntryLinks)
            WalkLink(link, ctx);

        foreach (var link in group.InfoLinks)
            WalkInfoLink(link, ctx, ancestry, nearestGroupName);
    }

    private static void WalkLink(BsEntryLink link, WalkContext ctx)
    {
        // Crusade-mode-only content (every datasheet's "Crusade" entryLink - Battle Honours, Chaos
        // Boons, Crusade Relic Upgrades) and other force-type-gated matched-play mechanics (Chaos
        // Space Marines' "Mark of Chaos") are excluded once resolved, via IsGameModeGated on the
        // target entry/group itself - not by name here, since the gating shape (a modifier
        // referencing a force-type id) generalizes across differently-named content, unlike a
        // literal "Crusade" name match would.
        if (ctx.IdIndex.TryGetValue(link.TargetId, out var targetEntry))
        {
            // A shared/reusable target (e.g. a wargear option linked from many unrelated places)
            // starts its own fresh ancestry rather than inheriting the linking entry's - it isn't
            // structurally "part of" whatever entry happened to link to it. The enclosing-group
            // name resets the same way, for the same reason.
            WalkEntry(targetEntry, ctx, [], enclosingGroupName: null);
        }
        else if (ctx.GroupIdIndex.TryGetValue(link.TargetId, out var targetGroup))
        {
            WalkGroup(targetGroup, ctx, [], enclosingGroupName: null);
        }
        // else: target not found in this closure (e.g. links into apparatus this loader doesn't
        // model) - skipped rather than failing resolution.
    }

    private static void WalkInfoLink(BsEntryLink link, WalkContext ctx, IReadOnlyList<BsSelectionEntry> ancestry,
        string? enclosingGroupName)
    {
        if (link.Type == "profile" && ctx.ProfileIdIndex.TryGetValue(link.TargetId, out var profile))
        {
            ProcessProfile(profile, ctx, ancestry, enclosingGroupName);
            return;
        }

        if (link.Type == "rule")
            ProcessRuleInfoLink(link, ctx, ancestry);

        // else: an unresolvable target - not something this loader maps.
    }

    /// <summary>Resolves a "type: rule" infoLink (BSData's shape for a datasheet-wide Core or
    /// faction rule reference - Oath of Moment, Templar Vows, Deadly Demise D3, Firing Deck 6,
    /// Infiltrators, Scouts 6" all take this shape) into an Ability, added to the same always-
    /// exposed Abilities list an Intrinsic ability populates - a Core rule is never an optional,
    /// player-selectable grant, so it doesn't go through the on-demand OptionalAbilities index.
    /// Text is resolved via RuleGlossary (the same glossary rules-glossary-popovers already
    /// resolves a [BRACKET] cross-reference against), keyed by the infoLink's own un-appended
    /// Name - never the modifier-appended display name, which isn't itself a rule name. A missing
    /// glossary (ctx.Glossary is null - every pre-existing BuildDatasheet call site that doesn't
    /// pass one) or an unresolvable rule name both silently produce no Ability, mirroring
    /// WalkLink's existing "target not found in this closure... skipped rather than failing
    /// resolution" convention - this is a data-completeness gap the full-corpus scan catches in
    /// aggregate, not something a single import should fail over.
    ///
    /// Nested inside a "type: upgrade" wargear-option entry (the same isOptional signal
    /// ProcessAbilityProfile already uses), a "type: rule" infoLink is a DIFFERENT, unrelated
    /// shape - not a datasheet-wide Core rule at all, but that specific weapon profile's own
    /// keyword cross-reference (confirmed real shape: the Impulsor's "Ironhail Skytalon Array"
    /// weapon-option entry carries "Sustained Hits"/"Anti" infoLinks describing its own Keywords
    /// characteristic, redundant with what WeaponAbilityTags/RuleGlossary already independently
    /// resolves for that weapon's own keyword chip). A real user-reported near-miss: without this
    /// guard, every weapon's own keyword rule-references leaked into Datasheet.Abilities as bogus
    /// datasheet-wide entries ("Sustained Hits", "Anti", "Melta", "Rapid Fire", "Blast" appearing
    /// unit-wide on the Impulsor) - caught by running the fix against a real export before
    /// shipping, not by any existing test. Skipped entirely (not routed to OptionalAbilities
    /// either) since it isn't an ability grant of any kind.</summary>
    private static void ProcessRuleInfoLink(BsEntryLink link, WalkContext ctx, IReadOnlyList<BsSelectionEntry> ancestry)
    {
        if (ancestry.Count > 0 && ancestry[^1].Type == "upgrade")
            return;

        if (ctx.Glossary is null)
            return;

        var rule = ctx.Glossary.TryResolve(link.Name);
        if (rule is null)
            return;

        if (IsGameModeGated(rule.Modifiers, ctx))
            return;

        var name = BuildAppendedName(link);
        if (ctx.SeenAbilityNames.Add(name))
        {
            ctx.Abilities.Add(new Ability
            {
                Name = name,
                Text = rule.Text,
                Scope = AbilityScope.Unit,
                Origin = ctx.KnownArmyRuleNames.Contains(rule.Name) ? AbilityOrigin.ArmyRule : AbilityOrigin.CoreRule
            });
        }
    }

    /// <summary>Builds a "type: rule" infoLink's display name: its own base Name, plus every
    /// "append"/field:"name" modifier's value found directly on the infoLink itself (in array
    /// order), space-joined - e.g. "Deadly Demise" + "D3" -> "Deadly Demise D3". A modifier's
    /// Value is mixed-typed in real data (a JSON string for Deadly Demise, a JSON number for
    /// Firing Deck), so both JsonValueKind.String and JsonValueKind.Number are handled.</summary>
    private static string BuildAppendedName(BsEntryLink link)
    {
        var name = link.Name;

        foreach (var modifier in link.Modifiers)
        {
            if (modifier.Type != "append" || modifier.Field != "name")
                continue;

            var suffix = modifier.Value.ValueKind == JsonValueKind.String
                ? modifier.Value.GetString()
                : modifier.Value.GetRawText();

            if (!string.IsNullOrEmpty(suffix))
                name = $"{name} {suffix}";
        }

        return name;
    }

    private static void ProcessProfile(BsProfile profile, WalkContext ctx, IReadOnlyList<BsSelectionEntry> ancestry,
        string? enclosingGroupName)
    {
        switch (profile.TypeName)
        {
            case "Unit":
                if (ctx.SeenStatlineNames.Add(profile.Name))
                    ctx.Statlines.Add((profile.Name, MapStatline(profile, ancestry, ctx)));
                break;
            case "Ranged Weapons":
                ctx.Weapons.TryAdd(profile.Name, MapRangedWeapon(profile));
                break;
            case "Melee Weapons":
                ctx.Weapons.TryAdd(profile.Name, MapMeleeWeapon(profile));
                break;
            case "Abilities":
                ProcessAbilityProfile(profile, ctx, ancestry, enclosingGroupName);
                break;
        }
    }

    /// <summary>Classifies an "Abilities"-typeName profile as intrinsic (a fact about the
    /// datasheet, always exposed via Datasheet.Abilities) or optional (an Enhancement, or any
    /// other ability grant nested inside its own selection entry, e.g. Impulsor's "Shield Dome" -
    /// resolvable only via Datasheet.TryResolveAbility, never enumerated). The signal is the
    /// *nearest* entry in <paramref name="ancestry"/> - the one that actually makes this profile
    /// reachable, whether directly (entry.Profiles) or through a "profile"-type infoLink it
    /// declared: BSData's own "type": "upgrade" marks a player-selectable choice, the same shape a
    /// weapon option uses, distinct from a "model"/"unit"-typed entry's own unconditional facts.
    /// An optional ability is further classified as an Enhancement when the nearest enclosing
    /// selection group's own Name contains "Enhancements" (case-insensitive substring, not exact
    /// match - confirmed necessary: a nested Enhancement sub-pool like "Legends of Saga and Song
    /// Enhancements" is the *directly* enclosing group for its own entries, not the outer
    /// "Enhancements" group one hop up), else a plain optional grant.</summary>
    private static void ProcessAbilityProfile(BsProfile profile, WalkContext ctx,
        IReadOnlyList<BsSelectionEntry> ancestry, string? enclosingGroupName)
    {
        var isOptional = ancestry.Count > 0 && ancestry[^1].Type == "upgrade";
        if (!isOptional)
        {
            if (ctx.SeenAbilityNames.Add(profile.Name))
                ctx.Abilities.Add(MapAbility(profile, AbilityOrigin.Intrinsic));
            return;
        }

        var origin = enclosingGroupName is not null &&
                     enclosingGroupName.Contains("Enhancements", StringComparison.OrdinalIgnoreCase)
            ? AbilityOrigin.Enhancement
            : AbilityOrigin.OptionalGrant;
        ctx.OptionalAbilities.TryAdd(profile.Name, MapAbility(profile, origin));
    }

    private static Statline MapStatline(BsProfile profile, IReadOnlyList<BsSelectionEntry> ancestry, WalkContext ctx) =>
        new(
            M: ParseMeasurement(profile.CharacteristicText("M")),
            T: ParsePlainInt(profile.CharacteristicText("T")),
            Sv: ParseThreshold(profile.CharacteristicText("Sv"), CharacteristicPattern.Sv),
            W: ParsePlainInt(profile.CharacteristicText("W")),
            Ld: ParseThreshold(profile.CharacteristicText("LD"), CharacteristicPattern.Ld),
            Oc: ParsePlainInt(profile.CharacteristicText("OC")))
        {
            InSv = ResolveInvulnerableSave(profile.CharacteristicText("InSv"), ancestry, ctx)
        };

    /// <summary>Resolves a Unit profile's raw InSv characteristic text into an
    /// <see cref="InvulnerableSave"/>. Three shapes are structurally recognized: a plain value
    /// (no marker); a parenthetical attack-type restriction (e.g. "5+ (Ranged)", confirmed on the
    /// four Titans and the Sokar-pattern Stormbird) which is fully self-contained - resolved
    /// straight from the characteristic text, no ability involved; and a footnoted value - a bare
    /// trailing "*" (e.g. "5+*", by far the most common real shape, confirmed on Aeldari Rangers,
    /// most Imperial/Chaos Knights, Judiciar, Astraeus) or a "/"-split where exactly one side
    /// carries the marker (e.g. "4+* / 5+", confirmed on Howling Banshees/Wyches). A footnoted
    /// value always resolves as caveated: this method deliberately never reads what the linked
    /// ability's Description text says (see ResolveCaveatAbility) - only which specific ability is
    /// linked, by id. Throws <see cref="AmbiguousCharacteristicException"/> for any text matching
    /// none of these shapes.</summary>
    private static InvulnerableSave ResolveInvulnerableSave(string? text, IReadOnlyList<BsSelectionEntry> ancestry,
        WalkContext ctx)
    {
        if (string.IsNullOrWhiteSpace(text)) return new InvulnerableSave();
        text = text.Trim();
        if (IsNotApplicable(text) || IsNA(text)) return new InvulnerableSave();

        var parenMatch = InSvParentheticalRegex().Match(text);
        if (parenMatch.Success)
        {
            var value = int.Parse(parenMatch.Groups[1].Value);
            var isRanged = parenMatch.Groups[2].Value.Equals("Ranged", StringComparison.OrdinalIgnoreCase);
            return isRanged
                ? new InvulnerableSave(meleeInSv: 0, rangedInSv: value, caveated: false, caveatAbility: null)
                : new InvulnerableSave(meleeInSv: value, rangedInSv: 0, caveated: false, caveatAbility: null);
        }

        if (text.Contains('/'))
        {
            var parts = text.Split('/').Select(p => p.Trim()).ToArray();
            if (parts.Length != 2)
                throw new AmbiguousCharacteristicException("InSv", text);

            var left = InSvBareValueRegex().Match(parts[0]);
            var right = InSvBareValueRegex().Match(parts[1]);
            if (!left.Success || !right.Success)
                throw new AmbiguousCharacteristicException("InSv", text);

            var leftFootnoted = left.Groups[2].Success;
            var rightFootnoted = right.Groups[2].Success;
            if (leftFootnoted == rightFootnoted)
                throw new AmbiguousCharacteristicException("InSv", text);

            var footnotedDigit = int.Parse((leftFootnoted ? left : right).Groups[1].Value);
            var plainDigit = int.Parse((leftFootnoted ? right : left).Groups[1].Value);
            var splitAbility = ResolveCaveatAbility(footnotedDigit, ancestry, ctx, text);
            return new InvulnerableSave(plainDigit, plainDigit, caveated: true, caveatAbility: splitAbility);
        }

        var bareMatch = InSvBareValueRegex().Match(text);
        if (!bareMatch.Success)
            throw new AmbiguousCharacteristicException("InSv", text);

        var bareDigit = int.Parse(bareMatch.Groups[1].Value);
        if (!bareMatch.Groups[2].Success)
            return new InvulnerableSave(bareDigit, bareDigit, caveated: false, caveatAbility: null);

        var bareAbility = ResolveCaveatAbility(bareDigit, ancestry, ctx, text);
        return new InvulnerableSave(bareDigit, bareDigit, caveated: true, caveatAbility: bareAbility);
    }

    /// <summary>Resolves the specific Ability a footnoted InSv value of the given digit is linked
    /// to, by id - never by matching a name alone against the whole datasheet's flattened,
    /// name-deduped ability list, since the real corpus's base catalogue contains two different
    /// abilities both named "Invulnerable Save (4+*)" with opposite meanings (one "against ranged
    /// attacks", one "against melee attacks"). Two real naming conventions are recognized: the
    /// digit-parameterized "Invulnerable Save ({digit}+*)" (confirmed: Imperial Knights' Canis
    /// Rex, linked via infoLink into the base rules catalogue's shared profile; Howling Banshees,
    /// same shape) and the generic, unparameterized "*Invulnerable Save" (confirmed: Space
    /// Marines' Judiciar and Astraeus, defined locally; Chaos Knights' War Dog Executioner, linked
    /// via infoLink). Searches every entry in <paramref name="ancestry"/> from nearest (the entry
    /// that actually owns the Unit profile) to furthest, since the real corpus doesn't always put
    /// the link/local-ability on the same entry as the footnoted characteristic itself - confirmed
    /// on Howling Banshees, where the squad's own top-level entry carries the "Invulnerable Save
    /// (4+*)" infoLink but the footnoted InSv characteristic sits on a nested child model entry
    /// one level down. Each candidate entry's own locally-nested Abilities profiles are checked
    /// before its own "profile"-type InfoLinks (confirmed shape: Orks' Makari, whose "Invulnerable
    /// Save (2+*)" sits directly in his own entry.Profiles, not behind any link). Throws when no
    /// entry in the ancestry has a matching candidate under either naming convention - a footnote
    /// with no resolvable ability is a new, unprecedented shape, not something to guess at.</summary>
    private static Ability ResolveCaveatAbility(int digit, IReadOnlyList<BsSelectionEntry> ancestry, WalkContext ctx,
        string rawText)
    {
        var expectedNames = new[] { $"Invulnerable Save ({digit}+*)", "*Invulnerable Save" };

        for (var i = ancestry.Count - 1; i >= 0; i--)
        {
            var candidate = ancestry[i];

            var localProfile = candidate.Profiles.FirstOrDefault(p =>
                p.TypeName == "Abilities" &&
                expectedNames.Any(n => string.Equals(p.Name, n, StringComparison.OrdinalIgnoreCase)));
            if (localProfile is not null)
                return MapAbility(localProfile, AbilityOrigin.Intrinsic);

            var link = candidate.InfoLinks.FirstOrDefault(l =>
                l.Type == "profile" &&
                expectedNames.Any(n => string.Equals(l.Name, n, StringComparison.OrdinalIgnoreCase)));
            if (link is not null && ctx.ProfileIdIndex.TryGetValue(link.TargetId, out var linkedProfile))
                return MapAbility(linkedProfile, AbilityOrigin.Intrinsic);
        }

        throw new AmbiguousCharacteristicException("InSv", rawText);
    }

    [GeneratedRegex(@"^(\d+)\+\s*\((Ranged|Melee)\)$")]
    private static partial Regex InSvParentheticalRegex();

    [GeneratedRegex(@"^(\d+)\+(\*)?$")]
    private static partial Regex InSvBareValueRegex();

    private static WeaponProfile MapRangedWeapon(BsProfile profile)
    {
        var weapon = new RangedWeapon(
            profile.Name,
            ParseMeasurement(profile.CharacteristicText("Range")),
            DiceExpression.Parse(profile.CharacteristicText("A") ?? "0"),
            ParseThreshold(profile.CharacteristicText("BS"), CharacteristicPattern.Bs),
            ParsePlainInt(profile.CharacteristicText("S")),
            ParsePlainInt(profile.CharacteristicText("AP")),
            DiceExpression.Parse(profile.CharacteristicText("D") ?? "0"));

        return WeaponKeywordParser.Apply(weapon, profile.CharacteristicText("Keywords") ?? "-");
    }

    private static WeaponProfile MapMeleeWeapon(BsProfile profile)
    {
        var weapon = new MeleeWeapon(
            profile.Name,
            DiceExpression.Parse(profile.CharacteristicText("A") ?? "0"),
            ParseThreshold(profile.CharacteristicText("WS"), CharacteristicPattern.Ws),
            ParsePlainInt(profile.CharacteristicText("S")),
            ParsePlainInt(profile.CharacteristicText("AP")),
            DiceExpression.Parse(profile.CharacteristicText("D") ?? "0"));

        return WeaponKeywordParser.Apply(weapon, profile.CharacteristicText("Keywords") ?? "-");
    }

    private static Ability MapAbility(BsProfile profile, AbilityOrigin origin) =>
        new()
        {
            Name = profile.Name,
            Text = profile.CharacteristicText("Description") ?? "",
            Scope = AbilityScope.Unit,
            Origin = origin
        };

    /// <summary>True for BSData's "not applicable" marker - confirmed on M (a Fortification/
    /// terrain piece that cannot move, e.g. Feculent Gnarlmaw), OC (a piece that can never
    /// contribute to objective control, e.g. Heldrake), and Sv (an unarmoured terrain piece).
    /// Parses as 0, the same "0 means absent" convention already used for
    /// Statline.InSv.</summary>
    private static bool IsNotApplicable(string text) => text == "-";

    private static bool IsNA(string text) => text.Equals("N/A", StringComparison.OrdinalIgnoreCase);

    /// <summary>Strips a trailing measurement mark and parses the number. Usually a trailing `"`
    /// (5", 18"), but some flyers print a minimum-move constraint as e.g. `12+` instead of a fixed
    /// distance (confirmed on Heldrake, Hell Talon, Raven Strike Fighter) - `+` is stripped too,
    /// losing the "minimum, not exact" nuance rather than failing to parse at all. "N/A" (confirmed
    /// on the Deathstrike missile's own Range - it targets by a different rule entirely) parses as
    /// 0, same as "-". A literal "Melee" (confirmed on one "Ranged Weapons"-typeName profile,
    /// Greater Blight Drone [Legends] - a data-entry classification quirk, not a real ranged
    /// weapon) also parses as 0, mirroring MeleeWeapon's own fixed Range=0.</summary>
    private static int ParseMeasurement(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Trim();
        if (IsNotApplicable(text) || IsNA(text) || text.Equals("Melee", StringComparison.OrdinalIgnoreCase)) return 0;
        return int.Parse(text.TrimEnd('"', '+'));
    }


    /// <summary>Parses a single-value threshold characteristic (Sv, BS, WS, LD) against the shape
    /// permitted for that specific characteristic (see <see cref="CharacteristicPattern"/>):
    /// digits with an optional trailing "+". An empty string, "N/A" (BSData's no-hit-roll-required
    /// marker on e.g. a Torrent weapon's BS/WS), or "-" (not applicable) parses as 0 regardless of
    /// characteristic. Anything else throws <see cref="AmbiguousCharacteristicException"/> instead
    /// of silently keeping one value. InSv is deliberately not one of this method's callers - its
    /// caveated/multi-value forms need entry-scoped ability resolution, not a single regex; see
    /// <see cref="ResolveInvulnerableSave"/>.</summary>
    private static int ParseThreshold(string? text, CharacteristicPattern expectedPattern)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Trim();
        if (IsNotApplicable(text) || IsNA(text)) return 0;

        var match = expectedPattern.Pattern.Match(text);
        if (!match.Success)
            throw new AmbiguousCharacteristicException(expectedPattern.Characteristic, text);

        return int.Parse(match.Groups[1].Value);
    }

    /// <summary>Parses a plain integer characteristic (W, OC, S, AP - AP already carries its own
    /// negative sign in source data, matching WeaponProfile.Ap's convention). "-" (not applicable,
    /// confirmed on OC) parses as 0. A trailing "+" (confirmed on a Deathwatch Terminator Squad
    /// weapon's Strength, "8+") is stripped, same as the measurement/threshold parsers.</summary>
    private static int ParsePlainInt(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return 0;
        text = text.Trim();
        return IsNotApplicable(text) ? 0 : int.Parse(text.TrimEnd('+'));
    }
}

/// <summary>The exact single-value shape <see cref="BsdataDatasheetMapper"/>'s ParseThreshold
/// accepts for one named characteristic, plus that name for use in a thrown
/// <see cref="AmbiguousCharacteristicException"/>'s message and properties. Sv/BS/WS/LD each get
/// their own instance (identical patterns today) so a future divergence for one characteristic
/// doesn't silently affect the others. InSv is not one of these - it has its own, richer
/// resolution in <see cref="BsdataDatasheetMapper.ResolveInvulnerableSave"/>, not a single
/// regex.</summary>
public partial class CharacteristicPattern
{
    private CharacteristicPattern(string characteristic, Regex pattern)
    {
        Characteristic = characteristic;
        Pattern = pattern;
    }

    internal static readonly CharacteristicPattern Ws = new("Ws", WsRegex());
    internal static readonly CharacteristicPattern Bs = new("Bs", BsRegex());
    internal static readonly CharacteristicPattern Sv = new("Sv", SvRegex());
    internal static readonly CharacteristicPattern Ld = new("Ld", LdRegex());

    public string Characteristic { get; init; }
    public Regex Pattern { get; init; }

    [GeneratedRegex(@"^(\d+)[+]?$")]
    private static partial Regex WsRegex();

    [GeneratedRegex(@"^(\d+)[+]?$")]
    private static partial Regex BsRegex();

    [GeneratedRegex(@"^(\d+)[+]?$")]
    private static partial Regex SvRegex();

    [GeneratedRegex(@"^(\d+)[+]?$")]
    private static partial Regex LdRegex();
}

/// <summary>Thrown by ParseThreshold when a threshold characteristic's text doesn't match its
/// allowed single-value shape (see <see cref="CharacteristicPattern"/>), and by
/// <see cref="BsdataDatasheetMapper.ResolveInvulnerableSave"/> when InSv text matches none of its
/// recognized shapes, or a footnoted value has no resolvable candidate ability (see
/// <see cref="BsdataDatasheetMapper.ResolveCaveatAbility"/>). Carries both the characteristic name
/// and the raw offending text as properties, not just baked into the message, so a catching
/// corpus-wide test can check a failure against a maintained "known limitation" allowlist without
/// needing to parse <see cref="Exception.Message"/>.</summary>
public class AmbiguousCharacteristicException(string characteristic, string text)
    : Exception($"Unexpected text for characteristic: {characteristic}={text}")
{
    public string Text { get; } = text;
    public string Characteristic { get; } = characteristic;
}