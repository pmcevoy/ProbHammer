using System.Text.Json.Serialization;
using ProbHammer.Core.Domain.Import.BattleScribe.Json;

namespace ProbHammer.Core.Domain.Import;

/// <summary>Format-discriminated wrapper around whichever source-level intermediate a successful
/// import produced - a <see cref="ParsedArmyList"/> (GW-app text pipeline) or a <see cref="BsRoster"/>
/// (battlescribe-roster-import). This is what <c>ISessionArmyListStore</c>/<c>IArmyRosterProvider</c>
/// accept and return instead of being hard-typed to <see cref="ParsedArmyList"/> - see
/// import-battlescribe-json-rosters' design.md's "Format-discriminated session storage and shared
/// Build". Session storage still holds only this source-level intermediate, never a built
/// <c>ArmyRoster</c>, matching the existing "session stores the intermediate, not the graph"
/// principle for either format.</summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(TextArmyImport), "text")]
[JsonDerivedType(typeof(BattleScribeArmyImport), "battlescribe")]
public abstract record StoredArmyImport;

public sealed record TextArmyImport(ParsedArmyList ParsedArmyList) : StoredArmyImport;

public sealed record BattleScribeArmyImport(BsRoster Roster) : StoredArmyImport;
