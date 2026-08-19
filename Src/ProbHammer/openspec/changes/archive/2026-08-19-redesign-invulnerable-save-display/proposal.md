## Why

`/LivePlay`'s invulnerable save today renders as a small subvalue nested inside the Sv stat-tile
(`4++`, or `5++*` with a tooltip for a caveated value) — it doesn't distinguish which attack type a
split save applies to at a glance, and a caveated save's actual condition is invisible unless the
player hunts through the unit's ability list for it. Real GW and Wahapedia card layouts solve both
problems with a distinct, labeled InSv box below the main stat row, and Wahapedia goes further by
printing the caveat's actual condition text directly underneath it. Bring `/LivePlay` in line with
that pattern.

## What Changes

- Replace the current `.stat-subvalue` nested inside the Sv tile with a separate InSv box rendered
  below the full M/T/Sv/W/Ld/Oc stat-tile row, with a side label instead of a stacked one.
- Render logic changes per `InvulnerableSave` shape:
  - **Uniform** (`MeleeInSv == RangedInSv`, non-caveated): single value in the box, label
    `InSv vs ⚔ / ⌖`.
  - **Differing, both known** (`MeleeInSv != RangedInSv`, both non-zero, non-caveated): dual value
    (`4+ / 5+`) in the box, same `InSv vs ⚔ / ⌖` label. No real BSData text produces this today (see
    Design), but the value shape supports it and the render logic should too.
  - **Melee-only** (`RangedInSv == 0`): single value, label `InSv vs ⚔`.
  - **Ranged-only** (`MeleeInSv == 0`): single value, label `InSv vs ⌖`.
  - **Caveated**: bare `InSv*` label (no attack-type icons — the caveat means the exact split isn't
    known), off-color (light amber) box background distinct from the normal box's background,
    black text kept in both. The linked `CaveatAbility.Text` renders in italics beneath the box,
    full card width.
- **First place on the page rendering `Ability.Text` rather than just `Ability.Name`.** Scoped
  narrowly to the InSv caveat, not a step toward general ability full-text rendering (that stays the
  deferred `vnext-ideas.md` popup idea).
- Two new example fixtures (`Examples/Datasheets.cs` / `Examples/Units.cs`):
  - A Howling Banshee-based unit, using real verified BSData values, to exercise the caveated case
    against genuine two-value source text (`"4+* / 5+"`) collapsed to one kept digit per the
    existing, unchanged `ResolveInvulnerableSave` behavior.
  - An explicitly synthetic unit (clearly documented as hand-constructed, not sourced from real
    BSData) to exercise the "differing, both known, non-caveated" case, since no real corpus text
    can produce that shape today.
- Exact melee/ranged glyphs and the InSv box's shape (plain rounded rect vs. a shield silhouette)
  are left open, to be settled during `/opsx:apply` by trying candidates live in a browser rather
  than guessed at up front — see Design.

## Capabilities

### New Capabilities

None.

### Modified Capabilities

- `live-play-view`: the "Statline Section Rendering" requirement's four existing invulnerable-save
  scenarios (`Invulnerable save renders when present`, `Invulnerable save is omitted when absent`,
  `Differing melee and ranged values render distinctly`, `Caveated invulnerable save renders with an
  indicator`) change to describe the new box+label rendering, and a new scenario covers the
  caveated ability-text line.

## Impact

- `src/ProbHammer.Web/Pages/Shared/_UnitBlock.cshtml` — InSv rendering block (currently
  `_UnitBlock.cshtml:82-107`).
- `src/ProbHammer.Web/wwwroot/css/site.css` — new InSv box styles, replacing/extending
  `.stat-subvalue`/`.stat-subvalue-caveated`.
- `src/ProbHammer.Core/Domain/Examples/Datasheets.cs` and `Examples/Units.cs` — two new fixtures.
- No `Domain/Catalogue`, `Domain/Roster`, or `Bsdata/*` changes — `InvulnerableSave` and
  `BsdataDatasheetMapper.ResolveInvulnerableSave` are unchanged; this is a rendering-only change
  reading the same value shape that already exists.
