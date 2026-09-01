using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Core.Domain.Roster;

/// <summary>Whether a matched rule's mutation targets only its own bearer's row(s) (a specific
/// model-line, or a whole component when the matched ability is component-wide), or every row of
/// the whole <see cref="ICombatUnit"/> - see statline-flag-rules' design.md D4.</summary>
public enum StatlineFlagRuleScope
{
    Bearer,
    WholeUnit
}

/// <summary>One closed-vocabulary rule: matches a specific ability by exact Name and Text (no
/// partial/fuzzy comparison), and derives a mutated <see cref="Statline"/> for display wherever that
/// exact ability is currently present on a resolved unit. Never mutates the source
/// <see cref="Ability"/>, <see cref="Catalogue.Datasheet"/>, or <see cref="Unit"/> - see
/// statline-flag-rules' "Source Ability Always Remains Visible".</summary>
public abstract class StatlineFlagRule
{
    public abstract string AbilityName { get; }
    public abstract string AbilityText { get; }
    public abstract StatlineFlagRuleScope Scope { get; }
    public abstract StatlineFlagCharacteristic Characteristic { get; }

    public bool Matches(Ability ability) => ability.Name == AbilityName && ability.Text == AbilityText;

    public abstract Statline Apply(Statline baseStatline);
}

/// <summary>Impulsor's Shield Dome: grants the bearer a flat 5+ invulnerable save, replacing
/// whatever the Datasheet's own base InSv was (absent or otherwise).</summary>
public sealed class ShieldDomeStatlineFlagRule : StatlineFlagRule
{
    public override string AbilityName => "Shield Dome";
    public override string AbilityText => "The bearer has a 5+ invulnerable save.";
    public override StatlineFlagRuleScope Scope => StatlineFlagRuleScope.Bearer;
    public override StatlineFlagCharacteristic Characteristic => StatlineFlagCharacteristic.InvulnerableSave;

    public override Statline Apply(Statline baseStatline) =>
        baseStatline with { InSv = new InvulnerableSave(5, 5, caveated: false, caveatAbility: null) };
}

/// <summary>Custodian Guard's Vexilla: adds 1 to Objective Control for every model in the bearer's
/// unit - a whole-unit scope, not bearer-only, matching the real 11e rule.</summary>
public sealed class VexillaStatlineFlagRule : StatlineFlagRule
{
    public override string AbilityName => "Vexilla";

    public override string AbilityText =>
        "Add 1 to the Objective Control characteristic of models in the bearer's unit.";

    public override StatlineFlagRuleScope Scope => StatlineFlagRuleScope.WholeUnit;
    public override StatlineFlagCharacteristic Characteristic => StatlineFlagCharacteristic.ObjectiveControl;

    public override Statline Apply(Statline baseStatline) => baseStatline with { Oc = baseStatline.Oc + 1 };
}

/// <summary>The closed vocabulary's full rule set - a real third rule joins this list directly, per
/// statline-flag-rules' "no general ability-text parsing engine" non-goal.</summary>
public static class StatlineFlagRuleCatalogue
{
    public static readonly IReadOnlyList<StatlineFlagRule> All =
        [new ShieldDomeStatlineFlagRule(), new VexillaStatlineFlagRule()];
}
