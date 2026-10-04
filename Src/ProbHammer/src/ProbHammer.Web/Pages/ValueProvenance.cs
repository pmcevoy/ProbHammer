using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Roster;

namespace ProbHammer.Web.Pages;

/// <summary>A highlighted value's popover content: the original value, one line per ability with
/// something to say about it, and the resulting value. <see cref="ResultLabel"/> is null when there is
/// no result row (a caveated value has no resolved result).</summary>
public sealed record ValueProvenance(
    string Title,
    string OriginalLabel,
    string OriginalText,
    IReadOnlyList<ProvenanceLine> Lines,
    string? ResultLabel,
    string? ResultText);

/// <summary><see cref="Source"/> is null for a fixed line with no ability behind it (Battle-shocked).
/// <see cref="Applied"/> is false for a conditional effect or a caveat, neither of which is in the
/// result.</summary>
public sealed record ProvenanceLine(
    Ability? Source,
    string Label,
    string ChangeText,
    IReadOnlyList<string> Notes,
    bool Applied);

/// <summary>Builds <see cref="ValueProvenance"/> for every highlighted Statline tile and weapon value.
/// <paramref name="classifications"/> supplies each ability's unmodelled residue; null means no residue
/// notes.</summary>
internal sealed class ValueProvenanceBuilder(AbilityClassificationCatalogue? classifications)
{
    // Page field key (LivePlayModel.ScalarStatlineFieldOrder), Core characteristic key, label, suffix.
    private static readonly (string Field, string Characteristic, string Label, string Suffix)[] StatlineFields =
    [
        ("M", "M", "M", "\""), ("T", "T", "T", ""), ("Sv", "Sv", "Sv", "+"),
        ("W", "W", "W", ""), ("LD", "Ld", "Ld", "+"), ("OC", "Oc", "OC", "")
    ];

    public IReadOnlyDictionary<string, ValueProvenance> ForStatlineRun(
        IReadOnlyList<AggregateStatlineEntry> run, bool isBattleShocked)
    {
        var statline = run[0].Statline;
        var subject = string.Join(", ", run.Select(e => e.StatlineName).Distinct());
        var notApplied = run.SelectMany(e => e.NotAppliedEffects)
            .DistinctBy(e => (e.SourceAbility.Name, e.SourceAbility.Text, e.Effect))
            .ToList();

        var result = new Dictionary<string, ValueProvenance>();
        foreach (var (field, characteristic, label, suffix) in StatlineFields)
        {
            var view = LivePlayModel.GetScalarField(statline, field);
            var pending = notApplied
                .Where(n => n.Effect is ScalarCharacteristicEffect s && s.Characteristic == characteristic)
                .Select(n => PendingLine(n.SourceAbility, n.Condition,
                    PendingScalarChange((ScalarCharacteristicEffect)n.Effect, view.OriginalValue, suffix)))
                .ToList();
            if (Scalar($"{label} · {subject}", view, characteristic, suffix, pending,
                    battleShocked: field == "OC" && isBattleShocked) is { } provenance)
                result[field] = provenance;
        }

        if (InvulnerableSave($"InSv · {subject}", statline.InSv, notApplied) is { } insv)
            result["InSv"] = insv;

        return result;
    }

    public IReadOnlyDictionary<string, ValueProvenance> ForWeapon(AggregateWeaponEntry entry)
    {
        var result = new Dictionary<string, ValueProvenance>();
        if (Attacks(entry) is { } attacks)
            result["A"] = attacks;

        foreach (var field in LivePlayModel.WeaponScalarFieldOrder)
        {
            var view = LivePlayModel.GetWeaponScalarField(entry.Profile, field);
            var pending = entry.NotAppliedEffects
                .Where(e => e.Characteristic == field)
                .Select(e => PendingLine(e.SourceAbility, e.Condition, PendingWeaponChange(e)))
                .ToList();
            if (Scalar($"{field} · {entry.Name}", view, field, "", pending) is { } provenance)
                result[field] = provenance;
        }

        return result;
    }

    private ValueProvenance? Scalar(string title, ScalarCharacteristicView view, string characteristic,
        string suffix, IReadOnlyList<ProvenanceLine> pending, bool battleShocked = false)
    {
        if (view.ContributingAbilities.Count == 0 && pending.Count == 0 && !battleShocked)
            return null;

        var original = Format(view.OriginalValue, suffix);
        var lines = new List<ProvenanceLine>();
        if (view.IsCaveated)
            lines.AddRange(view.ContributingAbilities.Select(CaveatLine));
        else
            lines.AddRange(view.ContributingAbilities.Select(a =>
                Line(a, AppliedChange(characteristic, view.OriginalValue, view.Value, suffix), [], applied: true)));
        lines.AddRange(pending);

        if (battleShocked)
        {
            lines.Add(new ProvenanceLine(null, "Battle-shocked", "→ 0", [], Applied: true));
            return new ValueProvenance(title, view.IsCaveated ? "Datasheet" : "Original", original, lines, "Total", "0");
        }

        if (view.IsCaveated)
            return new ValueProvenance(title, "Datasheet", original, lines, null, null);

        return new ValueProvenance(title, "Original", original, lines,
            view.ContributingAbilities.Count > 0 ? "Total" : "Shown", Format(view.Value, suffix));
    }

    private ValueProvenance? InvulnerableSave(string title, InvulnerableSaveCharacteristicView view,
        IReadOnlyList<NotAppliedStatlineEffect> notApplied)
    {
        var pending = notApplied
            .Where(n => n.Effect is InvulnerableSaveCharacteristicEffect)
            .Select(n => PendingLine(n.SourceAbility, n.Condition,
                FormatInvulnerableSave(((InvulnerableSaveCharacteristicEffect)n.Effect).Value)))
            .ToList();
        if (view.ContributingAbilities.Count == 0 && pending.Count == 0)
            return null;

        var original = FormatInvulnerableSave(view.OriginalValue);
        if (view.IsCaveated)
            return new ValueProvenance(title, "Datasheet", original,
                [.. view.ContributingAbilities.Select(CaveatLine), .. pending], null, null);

        var resolved = FormatInvulnerableSave(view.Value);
        return new ValueProvenance(title, "Original", original,
            [.. view.ContributingAbilities.Select(a => Line(a, resolved, [], applied: true)), .. pending],
            view.ContributingAbilities.Count > 0 ? "Total" : "Shown", resolved);
    }

    // Original is the total without ability contributions; one line per distinct Attacks source.
    private ValueProvenance? Attacks(AggregateWeaponEntry entry)
    {
        var pending = entry.NotAppliedEffects
            .Where(e => e.Characteristic == "A")
            .Select(e => PendingLine(e.SourceAbility, e.Condition, PendingWeaponChange(e)))
            .ToList();
        var sources = entry.Contributions
            .SelectMany(c => c.AttacksContributions.Select(a => (c.Count, Contribution: a)))
            .GroupBy(x => (x.Contribution.SourceAbility.Name, x.Contribution.SourceAbility.Text))
            .ToList();
        if (sources.Count == 0 && pending.Count == 0)
            return null;

        var applied = sources.Select(g =>
        {
            var models = g.Sum(x => x.Count);
            var amounts = g.Select(x => x.Contribution.Amount).Distinct().ToList();
            IReadOnlyList<string> notes = amounts.Count == 1
                ? [$"{Signed(amounts[0])} per model × {models} {(models == 1 ? "model" : "models")}"]
                : [];
            return Line(g.First().Contribution.SourceAbility, Signed(g.Sum(x => x.Count * x.Contribution.Amount)),
                notes, applied: true);
        });

        return new ValueProvenance($"A · {entry.Name}", "Original",
            SumText(entry.Contributions.Select(c => c.PerModelAttacks.Scale(c.Count)).ToList()),
            [.. applied, .. pending], sources.Count > 0 ? "Total" : "Shown", entry.TotalAttacks.ToString());
    }

    private ProvenanceLine Line(Ability source, string changeText, IReadOnlyList<string> notes, bool applied)
    {
        var residue = classifications is not null && classifications.TryGet(source.Text, out var c)
            ? c.UnclassifiedResidue
            : null;
        var label = source.Origin == AbilityOrigin.Enhancement ? $"✦ {source.Name}" : source.Name;
        return new ProvenanceLine(source, label, changeText,
            string.IsNullOrWhiteSpace(residue) ? notes : [.. notes, residue], applied);
    }

    private ProvenanceLine PendingLine(Ability source, EffectCondition condition, string changeText) =>
        Line(source, changeText, [ConditionSummary(condition)], applied: false);

    private ProvenanceLine CaveatLine(Ability source) =>
        Line(source, "", ["May modify this value; check its text."], applied: false);

    internal static string ConditionSummary(EffectCondition condition)
    {
        var parts = new List<string>();
        if (condition.UsageLimit is { } limit)
            parts.Add(limit switch
            {
                UsageLimit.OncePerBattle => "Once per battle",
                UsageLimit.TwicePerBattle => "Twice per battle",
                UsageLimit.OncePerBattleRound => "Once per battle round",
                UsageLimit.OncePerTurn => "Once per turn",
                UsageLimit.OncePerPhase => "Once per phase",
                _ => limit.ToString()
            });
        if (condition.TurnOwnership is { } turn)
            parts.Add(turn == GameTurn.Mine ? "Your turn only" : "Opponent's turn only");
        if (!string.IsNullOrWhiteSpace(condition.ConditionText))
            parts.Add(condition.ConditionText);
        if (condition.IsChoiceBranch)
            parts.Add("One option of a choice");
        parts.Add("not added");
        return string.Join("; ", parts);
    }

    // Roll thresholds (Sv, Ld) show the resulting value; everything else a signed delta.
    private static string AppliedChange(string characteristic, CharacteristicValue original,
        CharacteristicValue derived, string suffix)
    {
        if (CharacteristicModificationKinds.Of(characteristic) == CharacteristicModificationKind.RollThreshold)
            return Format(derived, suffix);

        return (original, derived) switch
        {
            (NumericCharacteristicValue o, NumericCharacteristicValue d) => Signed(d.Value - o.Value),
            (DiceCharacteristicValue o, DiceCharacteristicValue d)
                when o.Value.Count == d.Value.Count && o.Value.Sides == d.Value.Sides =>
                Signed(d.Value.Modifier - o.Value.Modifier),
            _ => $"= {Format(derived, suffix)}"
        };
    }

    private static string PendingScalarChange(ScalarCharacteristicEffect effect, CharacteristicValue original,
        string suffix)
    {
        if (effect.Verb == EffectVerb.Set)
            return $"= {effect.Amount}{suffix}";

        var kind = CharacteristicModificationKinds.Of(effect.Characteristic);
        return kind == CharacteristicModificationKind.RollThreshold
            ? Format(CharacteristicModificationResolver.Resolve(effect.Characteristic, original, effect.Verb,
                effect.Amount), suffix)
            : Signed(CharacteristicModificationResolver.ResolveDelta(kind, effect.Verb, effect.Amount));
    }

    private static string PendingWeaponChange(NotAppliedWeaponEffect effect) =>
        effect.Verb == EffectVerb.Set ? $"= {effect.Amount}" : Signed(effect.Amount);

    private static string Signed(int delta) => delta >= 0 ? $"+{delta}" : delta.ToString();

    private static string Format(CharacteristicValue value, string suffix) =>
        value is SymbolicCharacteristicValue ? value.ToString()! : $"{value}{suffix}";

    internal static string FormatInvulnerableSave(InvulnerableSave save) =>
        (save.MeleeInSv, save.RangedInSv) switch
        {
            (0, 0) => "–",
            var (m, r) when m == r => $"{m}+",
            (var m, 0) => $"Melee {m}+",
            (0, var r) => $"Ranged {r}+",
            var (m, r) => $"Ranged {r}+, melee {m}+"
        };

    private static string SumText(IReadOnlyList<DiceExpression> parts)
    {
        var sides = parts.Where(p => p.Count > 0).Select(p => p.Sides).Distinct().ToList();
        if (sides.Count > 1)
            return string.Join(" + ", parts);

        return new DiceExpression
        {
            Count = parts.Sum(p => p.Count),
            Sides = sides.FirstOrDefault(),
            Modifier = parts.Sum(p => p.Modifier)
        }.ToString();
    }
}
