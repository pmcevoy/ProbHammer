using System.Runtime.CompilerServices;
using ProbHammer.Core.Domain.Catalogue.Bsdata;

namespace ProbHammer.Tests.Domain.Catalogue.Bsdata;

/// <summary>Points at tests/ProbHammer.Tests/Domain/Fixtures/Bsdata/, trimmed real BSData
/// excerpts and small hand-built closure fixtures (see design.md's "Automated tests use trimmed
/// real excerpts" decision) - never the external local clone. Resolved from this file's own
/// source path (via CallerFilePath) rather than a bin-relative "CopyToOutputDirectory" path -
/// the project's existing `<None Include="Fixtures\**">` copy item was never previously
/// exercised (every prior file under Fixtures\ was a .cs fixture, auto-claimed as a Compile item
/// ahead of None) and doesn't reliably copy these new data files to the output directory.</summary>
public static class BsdataFixtures
{
    public static LocalDiskBsdataCatalogueSource Source([CallerFilePath] string here = "") =>
        new(Path.Combine(Path.GetDirectoryName(here)!, "..", "..", "Fixtures", "Bsdata"));
}
