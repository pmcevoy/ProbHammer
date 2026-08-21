using ProbHammer.Core.Domain.Catalogue;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata.CorpusScan;

/// <summary>
/// Permanent, manually-triggered regression scan over the full local BSData clone's ability and
/// rule text - mirrors <see cref="WeaponKeywordScanTests"/>/<see cref="CharacteristicResolutionScanTests"/>'s
/// pattern (see design.md and rules-glossary-popovers' tasks.md 3.4). Every catalogue file gets a
/// turn as its own closure's starting file; every `[BRACKET]` token extracted from that closure's
/// own local `rules` text and every locally-defined entry's resolved `Ability.Text` is resolved
/// against that same closure's own <see cref="RuleGlossary"/> - validates the "exact-match
/// resolution" assumption at real-corpus scale, beyond the hand-picked examples the unit tests
/// cover. The game system's `sharedRules` are scanned once (not once per starting file) since
/// every closure shares the identical game-system catalogue.
/// </summary>
public class BracketTokenResolutionScanTests
{
    [Fact(Explicit = true)]
    public void Full_corpus_bracket_token_resolution_scan()
    {
        var source = LiveClone.RequireSource();
        var unresolved = new List<(string Token, string Location)>();
        var scannedSharedRules = false;

        foreach (var fileName in LiveClone.CatalogueFileNames(source))
        {
            var closure = BsdataClosureResolver.Resolve(source, fileName);
            var glossary = RuleGlossary.Build(closure);
            var idIndex = BsdataNameResolver.BuildIdIndex(closure);
            var groupIndex = BsdataNameResolver.BuildGroupIdIndex(closure);
            var profileIndex = BsdataNameResolver.BuildProfileIdIndex(closure);

            void Scan(string text, string location)
            {
                foreach (var token in RuleTextTokenizer.ExtractBracketTokens(text))
                {
                    if (glossary.TryResolve(token) is null)
                        unresolved.Add((token, location));
                }
            }

            foreach (var rule in closure.Files[0].Catalogue.Rules)
                Scan(rule.Description, $"{fileName} :: rule '{rule.Name}'");

            if (!scannedSharedRules && closure.GameSystem is not null)
            {
                scannedSharedRules = true;
                foreach (var rule in closure.GameSystem.SharedRules)
                    Scan(rule.Description, $"(game system) :: shared rule '{rule.Name}'");
            }

            foreach (var entry in closure.Files[0].Catalogue.SharedSelectionEntries)
            {
                Datasheet datasheet;
                try
                {
                    datasheet = BsdataDatasheetMapper.BuildDatasheet(entry, idIndex, groupIndex, profileIndex);
                }
                catch
                {
                    // Characteristic-resolution failures are covered by CharacteristicResolutionScanTests;
                    // an entry that fails to build a Datasheet at all has no Ability.Text to scan here.
                    continue;
                }

                foreach (var ability in datasheet.Abilities)
                    Scan(ability.Text, $"{fileName} :: '{entry.Name}' ability '{ability.Name}'");
            }
        }

        AllowlistCheck.AssertClean(
            unresolved,
            BracketTokenResolutionAllowlist.Entries,
            u => $"'{u.Token}' (seen on: {u.Location})");
    }
}
