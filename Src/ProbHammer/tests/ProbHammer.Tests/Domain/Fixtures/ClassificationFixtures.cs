using System.Runtime.CompilerServices;
using ProbHammer.Core.Domain.Catalogue;

namespace ProbHammer.Tests.Domain.Fixtures;

public static class ClassificationFixtures
{
    /// <summary>A catalogue entry keyed by <paramref name="text"/>; <paramref name="conditional"/> gives
    /// every effect a never-evaluable condition, so none applies on its own.</summary>
    public static (string Text, AbilityClassification Classification) Entry(
        string text, RuleTarget target, IReadOnlyList<RuleEffect> effects, bool conditional = false) =>
        (text, new AbilityClassification
        {
            Target = target,
            Effects =
            [
                .. effects.Select(effect => new ClassifiedEffect
                {
                    Effect = effect,
                    ResidualConditionBucket =
                        conditional ? ResidualConditionBucket.Never : ResidualConditionBucket.None,
                    ConditionText = conditional ? "Test condition" : null
                })
            ],
            CoverageStatus = CoverageStatus.Complete
        });

    public static AbilityClassificationCatalogue Catalogue(
        IEnumerable<(string Text, AbilityClassification Classification)> entries) =>
        AbilityClassificationCatalogue.FromTexts(entries);

    /// <summary>Shield Dome and Vexilla, the two texts the statline-flag tests are built around.</summary>
    public static readonly AbilityClassificationCatalogue ShieldDomeAndVexilla = Catalogue(
    [
        Entry(
            text: "The bearer has a 5+ invulnerable save.",
            target: new SelfRuleTarget(),
            effects: [new InvulnerableSaveCharacteristicEffect(new InvulnerableSave(5, 5))]),
        Entry(
            text: "Add 1 to the Objective Control characteristic of models in the bearer's unit.",
            target: new AttachedUnitRuleTarget(),
            effects: [new ScalarCharacteristicEffect("Oc", EffectVerb.Improve, 1)])
    ]);

    /// <summary>The exported catalogue the app ships, for tests driving real BSData end to end.</summary>
    public static AbilityClassificationCatalogue CheckedIn => CheckedInCatalogue.Value;

    private static readonly Lazy<AbilityClassificationCatalogue> CheckedInCatalogue =
        new(() => AbilityClassificationCatalogue.Load(CheckedInCataloguePath()));

    public static string CheckedInCataloguePath([CallerFilePath] string here = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "..", "..", "src", "ProbHammer.Web",
            "Data", "ability-classifications.json"));
}