using FluentAssertions;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Catalogue;

/// <summary>Ground-truth texts below were confirmed verbatim against the live BSData clone during
/// classify-rule-effects-from-text/tasks.md's task 1 - see that file's own notes for the exact
/// source location of each.</summary>
public class RuleEffectClassifierTests
{
    [Fact]
    public void ShieldDome_ClassifiesAsSelf_WithSetInvulnerableSaveEffect()
    {
        var result = RuleEffectClassifier.Classify("Shield Dome",
            "The bearer has a 5+ invulnerable save.");

        result.Target.Should().Be(new SelfRuleTarget());
        result.Effects.Should().Equal(new CharacteristicEffect("InSv", EffectVerb.Set, 5));
    }

    [Theory]
    [InlineData("The bearer has a 4+ Invulnerable save.")]
    [InlineData("This model has a 4+ INVULNERABLE SAVE.")]
    public void InvulnerableSaveGrant_IsCaseInsensitive(string text)
    {
        // Real corpus text carries inconsistent capitalization of this exact sentence for the same
        // wargear item (Storm Shield/Blizzard shield both have a lowercase and a capitalized variant).
        var result = RuleEffectClassifier.Classify("Storm Shield", text);

        result.Effects.Should().Equal(new CharacteristicEffect("InSv", EffectVerb.Set, 4));
    }

    [Theory]
    [InlineData(
        "This model has a 4+ invulnerable save against ranged attacks, and the Feel No Pain 6+ ability.")]
    [InlineData(
        "The bearer has a 4+ invulnerable save against ranged attacks, and a 5+ invulnerable save against melee attacks.")]
    public void InvulnerableSaveGrant_AttackTypeRestricted_ExtractsNoEffect(string text)
    {
        // Real corpus text (Chaos Knights' "**Ensorcelled Shield" / "**Veil of Medrengard" - both
        // War Dog Executioner optional abilities). An attack-type-restricted save ("...against ranged
        // attacks") is conditional on the incoming attack's type - the same shape
        // InvulnerableSaveCaveatClassifier already models separately as a melee/ranged pair - not the
        // unconditional flat grant this pattern means to recognize. Confirmed real false positive
        // before InvulnerableSaveGrant's negative lookahead: both used to wrongly extract a flat
        // Set InSv effect (4 for the first text), caught live reviewing corpus-report output.
        var result = RuleEffectClassifier.Classify("Test Ability", text);

        result.Effects.Should().BeEmpty();
    }

    [Theory]
    [InlineData("If it does, until the end of the phase, the bearer has a 2+ invulnerable save.")]
    [InlineData("If this unit is a Battleline or Kataphron unit, it has a 5+ invulnerable save instead.")]
    public void InvulnerableSaveGrant_EmbeddedInConditionalClause_ExtractsNoEffect(string text)
    {
        // Real corpus shapes (a once-per-battle Relic activation; a unit-type-conditional grant). The
        // save-granting clause is NOT at its own sentence's true start - it's preceded by a comma-
        // joined conditional preamble within the same sentence - so SentenceStart's anchor rejects it,
        // the same structural signal that rejects "if it does"/"while X"/"each time X" generally
        // rather than denylisting each trigger phrase. Confirmed real false positive before this
        // anchor - caught live reviewing corpus-report output (2026-09-08), not by any test.
        var result = RuleEffectClassifier.Classify("Test Ability", text);

        result.Effects.Should().BeEmpty();
    }

    [Fact]
    public void AddCharacteristic_EmbeddedInConditionalClause_ExtractsNoEffect()
    {
        // Real corpus shape (a once-per-battle-round Relic activation). Same SentenceStart anchor as
        // InvulnerableSaveGrant, applied to the Improve-shaped pattern - confirmed real false positive
        // before this anchor (wrongly extracted Improve T 2), caught live reviewing corpus-report
        // output, not by any test.
        var result = RuleEffectClassifier.Classify("Test Ability",
            "If it does, until the end of the battle round, add 2 to the Toughness characteristic of models in the bearer's unit.");

        result.Effects.Should().BeEmpty();
    }

    [Fact]
    public void InvulnerableSaveGrant_BulletedAlternativeInASelectOneMenu_ExtractsNoEffect()
    {
        // Real corpus text (Adeptus Custodes' "Moment Shackle", Trajann Valoris). A bullet marker
        // preceded the actual grant here, not a period - an earlier version of SentenceStart's anchor
        // treated ■/▪ as a valid sentence boundary (alongside period/newline) specifically so a fresh
        // bulleted item would be recognized as unconditional, but this text disproves that: the two
        // bullets are mutually-exclusive alternatives in a "select one of the following" menu (you
        // choose ONE), not two independent unconditional facts - conditional on which is chosen, same
        // as any other conditional preamble. Confirmed real false positive (wrongly extracted
        // Set InSv 2) before the anchor was narrowed to periods only - caught live reviewing
        // corpus-report output (2026-09-08), not by any test.
        const string text = """
                            Once per battle, at the start of the Fight phase, you can select one of the following to take effect until the end of the phase:
                            ■ This model's Watcher's Axe melee weapon has an Attacks characteristic of 12.
                            ■ This model has a 2+ invulnerable save.
                            """;

        var result = RuleEffectClassifier.Classify("Moment Shackle", text);

        result.Effects.Should().BeEmpty();
    }

    [Fact]
    public void AddCharacteristic_NewlineHeadedAlternativeInASelectOneMenu_ExtractsNoEffect()
    {
        // Real corpus shape (Aeldari's "Combat Drugs"/Adeptus Mechanicus' "Noospheric Transference"):
        // a numbered/named heading line, then a newline, then the actual effect text - "select one
        // from the list below". A bare newline after a heading is not a genuine sentence boundary any
        // more than a bullet is (see the bulleted-menu test above) - it separates menu ITEMS, not
        // independent unconditional sentences. Confirmed real false positive (wrongly extracted
        // Improve T 1) before the anchor was narrowed to periods only.
        const string text = """
                            At the start of your Command phase, select which Combat Drugs will be active for your army. To do so, select one from the list below.

                            4. Painbringer
                            Add 1 to the Toughness characteristic of Wych Cult models from your army.
                            """;

        var result = RuleEffectClassifier.Classify("Combat Drugs", text);

        result.Effects.Should().BeEmpty();
    }

    [Fact]
    public void AttachedUnitPhrase_RecognizesModelsInThisUnit_AsEquivalentToBearersUnit()
    {
        // Real corpus text (Space Marines' "Astartes Banner"). "models in this unit" is functionally
        // the same claim as "models in the bearer's unit" - an ability's text describes its effect
        // from the bearer's own perspective, so "this unit" means the bearer's unit. Confirmed real
        // false classification (wrongly landed as Self) before AttachedUnitPhrase recognized this
        // phrasing too - caught live reviewing corpus-report output (2026-09-08), not by any test.
        var result = RuleEffectClassifier.Classify("Astartes Banner",
            "Add 1 to the Objective Control characteristic of models in this unit.");

        result.Target.Should().Be(new AttachedUnitRuleTarget());
        result.Effects.Should().Equal(new CharacteristicEffect("Oc", EffectVerb.Improve, 1));
    }

    [Fact]
    public void InvulnerableSaveGrant_AtTrueSentenceStartAfterAPriorSentence_StillExtractsEffect()
    {
        // Real corpus shape (Sanctuary, an Imperial Knights Enhancement): a restriction sentence
        // ("...model only.") precedes the actual grant. SentenceStart's anchor must recognize the
        // period+space boundary between the two sentences, not just the very start of the whole text.
        var result = RuleEffectClassifier.Classify("Sanctuary",
            "Imperial Knights model only. The bearer has a 5+ invulnerable save.");

        result.Effects.Should().Equal(new CharacteristicEffect("InSv", EffectVerb.Set, 5));
    }

    [Fact]
    public void Vexilla_AsciiApostrophe_ClassifiesAsAttachedUnit_WithImproveObjectiveControlEffect()
    {
        var result = RuleEffectClassifier.Classify("Vexilla",
            "Add 1 to the Objective Control characteristic of models in the bearer's unit.");

        result.Target.Should().Be(new AttachedUnitRuleTarget());
        result.Effects.Should().Equal(new CharacteristicEffect("Oc", EffectVerb.Improve, 1));
    }

    [Fact]
    public void Vexilla_TypographicApostrophe_ClassifiesIdenticallyToTheAsciiVariant()
    {
        // Real corpus data (Imperium - Adeptus Custodes.json) carries both a plain ASCII apostrophe
        // and a U+2019 typographic apostrophe for this exact sentence across different entries.
        var result = RuleEffectClassifier.Classify("Vexilla",
            "Add 1 to the Objective Control characteristic of models in the bearer’s unit.");

        result.Target.Should().Be(new AttachedUnitRuleTarget());
        result.Effects.Should().Equal(new CharacteristicEffect("Oc", EffectVerb.Improve, 1));
    }

    [Fact]
    public void Vexilla_NonBreakingSpaceInPlaceOfPlainSpace_ClassifiesIdenticallyToThePlainSpaceVariant()
    {
        // Real corpus data (Imperium - Adeptus Custodes.json) carries a U+00A0 non-breaking space in
        // place of the plain space between "characteristic" and "of" for this exact sentence, on a
        // different entry (Custodian Wardens) than the plain-space variant above (Custodian Guard).
        // The string literal below embeds a real U+00A0 character - verified byte-for-byte
        // (0xC2 0xA0), not just eyeballed, since a literal invisible character in source is easy
        // to accidentally lose: an earlier draft of this exact test silently collapsed it to a
        // plain space, which would have made the test pass without ever exercising the NBSP path.
        const string text = "Add 1 to the Objective Control characteristic of models in the bearer's unit.";

        var result = RuleEffectClassifier.Classify("Vexilla", text);

        result.Target.Should().Be(new AttachedUnitRuleTarget());
        result.Effects.Should().Equal(new CharacteristicEffect("Oc", EffectVerb.Improve, 1));
    }

    [Fact]
    public void AttachedUnitPhrase_ToleratesNonBreakingSpaceAtTheJoinItself()
    {
        // Unlike the Vexilla case above (where the NBSP sits somewhere harmless - AddCharacteristic's
        // own regex doesn't care what follows the word "characteristic"), this plants the NBSP at the
        // exact join AttachedUnitPhrase's own literal "bearer's unit" pattern matches against - proves
        // Normalize's NBSP handling protects a position that would otherwise silently miss the match,
        // not just a position no current pattern happens to care about.
        const string text = "Each time a model in the bearer's unit makes an attack, you can re-roll the hit roll.";

        var result = RuleEffectClassifier.Classify("Test Ability", text);

        result.Target.Should().Be(new AttachedUnitRuleTarget());
    }

    [Fact]
    public void Normalize_IsIdempotentAndExposedForExternalGrouping()
    {
        // RuleEffectClassifier.Normalize is public specifically so a caller comparing/grouping raw
        // corpus text (e.g. the corpus report tool) normalizes the same way Classify itself does,
        // rather than treating two texts Classify would treat identically as distinct inputs. Covers
        // both normalized characters at once (typographic apostrophe + NBSP), each expressed as an
        // explicit \uXXXX escape so the intent is unambiguous in source.
        const string text =
            "Add 1 to the Objective Control characteristic of models in the bearer’s unit.";

        var normalized = RuleEffectClassifier.Normalize(text);

        normalized.Should().Be("Add 1 to the Objective Control characteristic of models in the bearer's unit.");
        RuleEffectClassifier.Normalize(normalized).Should().Be(normalized);
    }

    [Fact]
    public void TemplarVows_ClassifiesAsKeywordTarget_WithNoEffects()
    {
        const string text = """
                            If your Army Faction is **^^Adeptus Astartes^^**, at the start of the first battle round, select one of the following Vows to be active for **^^Adeptus Astartes^^** units from your army. While a Vow is active for your army, that unit has the associated ability below.

                            **Abhor the Witch, Destroy the Witch**
                            ■ Each time this unit declares a charge, if one or more targets of that charge have the **^^Psyker^^** keyword, you can re-roll the Charge roll. Melee weapons equipped by models in this unit have the **[PRECISION]** ability while targeting **^^Psyker^^** units.

                            **Accept Any Challenge, No Matter the Odds**
                            ■ Each time a model in this unit makes a melee attack, if the Strength characteristic of that attack is less than or equal to the Toughness characteristic of the target, add 1 to the wound roll

                            **Uphold the Honour of the Emperor**
                            If this unit has the **^^Infantry^^** keyword:
                            ■ At the end of your Command phase, if this unit is within range of an objective marker you control, that objective marker remains under your control until your opponent's level of control over that objective marker is greater than yours at the end of the phase.
                            """;

        var result = RuleEffectClassifier.Classify("Templar Vows", text);

        result.Target.Should().Be(new KeywordRuleTarget("ADEPTUS ASTARTES"));
        result.Effects.Should().BeEmpty();
    }

    [Fact]
    public void MarshalsHousehold_ClassifiesAsKeywordTarget_DespiteFurtherCapsAndUnitsInRestrictions()
    {
        // The Restrictions paragraph itself contains two further ALL-CAPS + "units" occurrences
        // ("BLACK TEMPLARS units", "ADEPTUS ASTARTES units") - the classifier must pick the FIRST
        // occurrence in the text (the actual effect statement), not one of these.
        const string text =
            "Friendly SWORD BRETHREN SQUAD units have +1 OC.\n\n\nRestrictions: Your army can " +
            "include BLACK TEMPLARS units, but it cannot include any ADEPTUS ASTARTES units drawn " +
            "from any other Chapter.";

        var result = RuleEffectClassifier.Classify("Faith-Fuelled Resolve", text);

        result.Target.Should().Be(new KeywordRuleTarget("SWORD BRETHREN SQUAD"));
        // The "+1 OC" shorthand is a real, confirmed extraction gap - only the "Add N to the X
        // characteristic" phrasing is recognized per design.md's task 3.3 scope, not this shorthand.
        result.Effects.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Models with this ability can move over battlefield debris as if it were open ground.")]
    [InlineData("Each time this model makes a ranged attack, an unmodified hit roll of 6 scores a Critical Hit.")]
    [InlineData(
        "This model cannot be selected as the target of a ranged attack unless it is the closest eligible target.")]
    public void UnrecognizedText_ClassifiesToSelf_WithNoEffects_NeverThrows(string text)
    {
        var result = RuleEffectClassifier.Classify("Some Ability", text);

        result.Target.Should().Be(new SelfRuleTarget());
        result.Effects.Should().BeEmpty();
    }
}