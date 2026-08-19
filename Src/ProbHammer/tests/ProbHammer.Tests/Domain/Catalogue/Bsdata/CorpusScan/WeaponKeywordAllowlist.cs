namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Seed "known limitation" allowlist for <see cref="WeaponKeywordScanTests"/>, populated from the
/// first real run against the live clone (task 4.4) - 45 distinct unrecognized tokens, none of
/// them fixed here: recognizing a new token as an existing <c>WeaponProfile</c> flag, or adding a
/// new flag for a genuinely unmodeled mechanic, is explicitly out of scope for this change (see
/// design.md's Non-Goals) - this only makes each one an expected, tracked finding instead of an
/// unexplained failure. See PROGRESS.md's Known Issues for the follow-up triage this seeds.
/// Grouped by kind, matched by exact (case-sensitive) token text - a same-named token appearing
/// with different casing or punctuation in the future is a genuinely new finding, not covered by
/// an existing entry here, by design.
/// </summary>
public static class WeaponKeywordAllowlist
{
    public static IReadOnlyList<AllowlistEntry<string>> Entries { get; } =
    [
        // Already-documented "no corresponding flag" tokens (datasheet-catalogue's own
        // "Token with no corresponding WeaponProfile flag" scenario names Hazardous/Precision/
        // Heavy explicitly) - expected, not a new finding.
        new("Hazardous - no corresponding flag; already an explicit datasheet-catalogue scenario.", t => t == "Hazardous"),
        new("Precision - no corresponding flag; already an explicit datasheet-catalogue scenario.", t => t == "Precision"),
        new("Heavy - no corresponding flag; already an explicit datasheet-catalogue scenario.", t => t == "Heavy"),

        // Case-only variants of an already-recognized exact-match token - WeaponKeywordParser's
        // switch/regex vocabulary is case-sensitive; these are candidates for a future
        // case-insensitivity fix (follow-up, not this change), not a new mechanic.
        new("'Devastating wounds' - lowercase variant of the recognized 'Devastating Wounds'.", t => t == "Devastating wounds"),
        new("'Ignores cover' - lowercase variant of the recognized 'Ignores Cover'.", t => t == "Ignores cover"),
        new("'Sustained hits 1' - lowercase variant of the recognized 'Sustained Hits N' pattern.", t => t == "Sustained hits 1"),
        new("'Sustained hits 2' - lowercase variant of the recognized 'Sustained Hits N' pattern.", t => t == "Sustained hits 2"),
        new("'Rapid fire 2' - lowercase variant of the recognized 'Rapid Fire N' pattern.", t => t == "Rapid fire 2"),
        new("'Twin Linked' - space instead of the recognized 'Twin-linked' hyphen/casing.", t => t == "Twin Linked"),
        new("'Twin-Linked' - capital 'L' variant of the recognized 'Twin-linked'.", t => t == "Twin-Linked"),

        // Punctuation/shape variants of the recognized "Anti-X N+" pattern (AntiPattern requires
        // a literal hyphen right after "Anti" and a trailing "+").
        new("'Anti Vehicle 3+' - space instead of the recognized 'Anti-X N+' hyphen.", t => t == "Anti Vehicle 3+"),
        new("'Anti-FLY 2' - matches 'Anti-X' but has no trailing '+' AntiPattern requires.", t => t == "Anti-FLY 2"),
        new("'Anti-fly 2' - same missing-'+' shape as 'Anti-FLY 2', lowercase target.", t => t == "Anti-fly 2"),
        new(
            "'ANTI-non‑MONSTER/VEHICLE 2+' - all-caps 'ANTI', a U+2011 non-breaking hyphen " +
            "(not ASCII '-') between 'non' and 'MONSTER', and a dual slash-separated exclusion " +
            "target ('non-MONSTER/VEHICLE') - a genuinely different targeting expression from the " +
            "modeled single-category Anti-X mechanic, not just a spelling variant.",
            t => t == "ANTI-non‑MONSTER/VEHICLE 2+"),

        // Dice-valued variants of mechanics currently modeled with a plain int (RapidFire/
        // SustainedHits regexes require \d+, not dice notation) - same class of gap as the
        // parked WeaponProfile.S dice-Strength representation question, not a simple parser fix.
        new("'Rapid Fire D3' - dice-valued Rapid Fire; RapidFire is a plain int today.", t => t == "Rapid Fire D3"),
        new("'Rapid Fire D6' - dice-valued Rapid Fire; RapidFire is a plain int today.", t => t == "Rapid Fire D6"),
        new("'Rapid Fire D6+3' - dice-valued Rapid Fire; RapidFire is a plain int today.", t => t == "Rapid Fire D6+3"),
        new("'Sustained Hits D3' - dice-valued Sustained Hits; SustainedHits is a plain int today.", t => t == "Sustained Hits D3"),

        // Genuinely new/unmodeled mechanics, no existing flag corresponds to any of these.
        new("'Blast 1' - value-carrying variant of the flag-only 'Blast'; no existing value slot.", t => t == "Blast 1"),
        new("'Bubblechukka' - weapon-specific named ability (Ork Bubblechukka), no generic flag.", t => t == "Bubblechukka"),
        new("'Cleave 1' - value-carrying variant of the already-deferred 'Cleave' alias.", t => t == "Cleave 1"),
        new("'Cleave 2' - value-carrying variant of the already-deferred 'Cleave' alias.", t => t == "Cleave 2"),
        new("'Close-Quarters' - distinct from the already-deferred 'Close Combat' alias; no flag.", t => t == "Close-Quarters"),
        new("'Close-quarters' - lowercase variant of 'Close-Quarters', same gap.", t => t == "Close-quarters"),
        new("'Conversion' - weapon-specific mechanic (conversion beam cannons), no generic flag.", t => t == "Conversion"),
        new("'Dead Choppy' - weapon-specific named ability (Ork Dread klaw), no generic flag.", t => t == "Dead Choppy"),
        new("'Defensive Array' - weapon-specific named ability, no generic flag.", t => t == "Defensive Array"),
        new("'Devastating Wounds: Monster/Vehicle' - target-conditional variant, distinct from the unconditional flag - must never be silently mapped to it.", t => t == "Devastating Wounds: Monster/Vehicle"),
        new("'Devastating Wounds: non-Monster/Vehicle' - target-conditional variant, distinct from the unconditional flag - must never be silently mapped to it.", t => t == "Devastating Wounds: non-Monster/Vehicle"),
        new("'Extra Attacks' - real, widespread (150+ occurrences) core mechanic with no flag yet - worth a dedicated future addition, not this change.", t => t == "Extra Attacks"),
        new("'Harpooned' - weapon-specific named ability (Tyranid Toxinjecter Harpoon), no generic flag.", t => t == "Harpooned"),
        new("'Hive Defences' - weapon-specific named ability (Tyranid Sporocyst), no generic flag.", t => t == "Hive Defences"),
        new("'Hooked' - weapon-specific named ability (T'au Kroot bolt thrower), no generic flag.", t => t == "Hooked"),
        new("'Impaled' - weapon-specific named ability (Impaler harpoon, Legends), no generic flag.", t => t == "Impaled"),
        new("'Lance' - real, fairly widespread (~70 occurrences) core mechanic with no flag yet - worth a dedicated future addition, not this change.", t => t == "Lance"),
        new("'Linked Fire' - weapon-specific named ability, no generic flag.", t => t == "Linked Fire"),
        new("'One Shot' - real core mechanic (~30+ occurrences) with no flag yet - worth a dedicated future addition, not this change.", t => t == "One Shot"),
        new("'Overcharge' - weapon-specific named ability, no generic flag.", t => t == "Overcharge"),
        new("'Plasma Warhead' - weapon-specific named ability (Deathstrike Missile), no generic flag.", t => t == "Plasma Warhead"),
        new("'Psychic' - real, widespread core mechanic with no flag yet - worth a dedicated future addition, not this change.", t => t == "Psychic"),
        new("'PSYCHIC' - all-caps source-data spelling variant of 'Psychic', same underlying gap.", t => t == "PSYCHIC"),
        new("'Psychic Assassin' - named ability distinct from the generic 'Psychic' keyword, no generic flag.", t => t == "Psychic Assassin"),
        new("'Reverberating Summons' - weapon-specific named ability (Doomsday bell), no generic flag.", t => t == "Reverberating Summons"),
        new("'Snagged' - weapon-specific named ability (Ork Stikka kannon), no generic flag.", t => t == "Snagged"),
        new("'Sonic Devastation' - weapon-specific named ability, no generic flag.", t => t == "Sonic Devastation"),
    ];
}
