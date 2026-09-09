## REMOVED Requirements

### Requirement: Presence-Gated Application, No Unconditional Bake-In
**Reason**: This capability's own always-caveat, never-resolve Build-time application is retired
outright, not preserved or migrated verbatim — a present characteristic-modifier candidate now
resolves (or stays caveated) through `statline-flag-rules`' single, generalized resolution pass, the
same mechanism every other characteristic-affecting ability already goes through. Presence-gating
itself (a candidate only ever applies when its own granting selection is confirmed present) is not
lost — it is exactly what `statline-flag-rules`' "Mutation Liveness Follows Ability Presence"
requirement already guarantees for every ability it resolves.

**Migration**: No external contract changes — a present candidate whose granting ability's text is in
the checked-in baseline (including a structurally-derived entry `widen-baseline-generation-coverage`
adds) now resolves to a real, non-caveated value instead of always staying caveated; a present
candidate with no matching baseline entry produces no flagged value at all rather than an
always-caveated one. Any code depending on `AttachedUnitAggregator.ApplyCharacteristicModifierCandidates`
existing as a separate step must be updated — see this change's own Impact section.

### Requirement: Applies Uniformly Alongside Hand-Authored Rules
**Reason**: There is no longer a separate mechanism to apply "uniformly alongside" — this capability's
own liveness/re-evaluation guarantee is superseded by `statline-flag-rules`' identical guarantee,
which now covers every characteristic-affecting ability in one pass rather than two coordinated ones.

**Migration**: No external contract changes — see `statline-flag-rules`' "Mutation Liveness Follows
Ability Presence" requirement, unchanged by this capability's retirement.

### Requirement: No Cross-Unit Application
**Reason**: This capability's own scoping guarantee is superseded by
`statline-flag-rules`' "Deferred Characteristic Associations Resolve At Their Recorded Scope" and
existing "Target-Scoped Application" requirements, which govern scope for every characteristic
association this system resolves, not only structurally-derived ones.

**Migration**: No external contract changes — a classified candidate's own granting selection still
determines which single unit it can ever affect, now expressed through the same attachment-scope
mechanism every other association uses rather than a mechanism of its own.
