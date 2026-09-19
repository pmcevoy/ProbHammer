namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Seed "known limitation" allowlist for <see cref="WeaponKeywordScanTests"/> - 19 distinct
/// tokens from a real run against the live clone that don't resolve against a closure's own
/// <see cref="ProbHammer.Core.Domain.Catalogue.Bsdata.RuleGlossary"/>, none of them fixed here:
/// adding a glossary entry (or a `sharedRule` alias) for a genuinely unmodeled mechanic is out of
/// scope here - this only makes each one an expected, tracked finding instead of an unexplained
/// failure. Since `retire-weapon-keyword-flags` repointed the scan from flag-recognition to
/// glossary-resolution, every prior entry here described a gap in the retired flag vocabulary, not
/// a glossary-resolution gap, and was removed; this is the fresh replacement. Matched by exact
/// (case-sensitive) token text - a same-named token appearing with different casing or punctuation
/// in the future is a genuinely new finding, not covered by an existing entry here, by design.
/// </summary>
public static class WeaponKeywordAllowlist
{
    public static IReadOnlyList<AllowlistEntry<string>> Entries { get; } =
    [
        // Weapon-specific named abilities with no BSData rule/sharedRule definition at all -
        // there is nothing for RuleGlossary to index, let alone resolve against.
        new("'Conversion' - weapon-specific mechanic (conversion beam cannons), no glossary entry.",
            t => t == "Conversion"),
        new("'Defensive Array' - weapon-specific named ability (Hammerfall arrays), no glossary entry.",
            t => t == "Defensive Array"),
        new("'Harpooned' - weapon-specific named ability (Tyranid Toxinjecter Harpoon), no glossary entry.",
            t => t == "Harpooned"),
        new("'Hive Defences' - weapon-specific named ability (Tyranid Sporocyst), no glossary entry.",
            t => t == "Hive Defences"),
        new("'Hooked' - weapon-specific named ability (T'au Kroot bolt thrower), no glossary entry.",
            t => t == "Hooked"),
        new("'Impaled' - weapon-specific named ability (Impaler harpoon, Legends), no glossary entry.",
            t => t == "Impaled"),
        new("'Linked Fire' - weapon-specific named ability (Aeldari Prism Cannon), no glossary entry.",
            t => t == "Linked Fire"),
        new("'Overcharge' - weapon-specific named ability (Kin transmatter inverter), no glossary entry.",
            t => t == "Overcharge"),
        new("'Plasma Warhead' - weapon-specific named ability (Deathstrike Missile), no glossary entry.",
            t => t == "Plasma Warhead"),
        new("'Psychic Assassin' - named ability distinct from the generic 'Psychic' keyword, no glossary entry.",
            t => t == "Psychic Assassin"),
        new("'Reverberating Summons' - weapon-specific named ability (Doomsday bell), no glossary entry.",
            t => t == "Reverberating Summons"),
        new(
            "'Sonic Devastation' - weapon-specific named ability (Adeptus Mechanicus Syntaxik Charger), no glossary entry.",
            t => t == "Sonic Devastation"),

        // Target-conditional variants of a generic mechanic that DOES have a glossary entry under
        // its bare name - RuleGlossary.Normalize's "anti"-prefix collapse is Anti-specific (see its
        // own doc comment); it has no equivalent rule for a trailing "target category" suffix on
        // Devastating Wounds/Lethal Hits/Sustained Hits, so these remain a genuinely different key
        // from the bare mechanic they specialize, by design - must never be silently mapped to it.
        new(
            "'Devastating Wounds: Monster/Vehicle' - target-conditional variant, distinct key from the bare 'Devastating Wounds' glossary entry.",
            t => t == "Devastating Wounds: Monster/Vehicle"),
        new("'DEVASTATING WOUNDS: MONSTER/VEHICLE' - all-caps spelling of the same target-conditional variant.",
            t => t == "DEVASTATING WOUNDS: MONSTER/VEHICLE"),
        new("'Devastating Wounds: non-Monster/Vehicle' - negated-target-conditional variant, distinct key.",
            t => t == "Devastating Wounds: non-Monster/Vehicle"),
        new("'DEVASTATING WOUNDS: INFANTRY' - all-caps target-conditional variant naming a different category.",
            t => t == "DEVASTATING WOUNDS: INFANTRY"),
        new("'LETHAL HITS: non-MONSTER/VEHICLE' - negated-target-conditional variant of 'Lethal Hits', distinct key.",
            t => t == "LETHAL HITS: non-MONSTER/VEHICLE"),
        new(
            "'LETHAL HITS: non-' - truncated source-data variant of the same negated-target-conditional shape (Orks.json's Dual Big Shoota).",
            t => t == "LETHAL HITS: non-"),
        new(
            "'SUSTAINED HITS 2: MONSTER/VEHICLE' - value- and target-conditional variant of 'Sustained Hits', distinct key.",
            t => t == "SUSTAINED HITS 2: MONSTER/VEHICLE"),
    ];
}