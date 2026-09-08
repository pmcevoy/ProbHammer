## MODIFIED Requirements

### Requirement: Corpus-Wide Classification Reporting
The system SHALL provide a way to run classification against a supplied set of real rule/ability
Name+Text pairs (such as the live BSData corpus) and report, for each, its classified Target and
Effects — distinguishing an entry that produced a non-default result (a recognized Target broader
than `Self`, or one or more Effects) from one that classified to the all-default result — so
real-corpus classification coverage can be inspected directly.

For a text that already has a corresponding entry in the rule-effect-classification-baseline
capability's baseline, the report SHALL instead apply that capability's new/drift/unchanged
distinction in place of its normal non-default/default-only listing, so an already-verified,
unchanged result is never reprinted in full while a genuine change to it is always surfaced. A text
with no baseline entry is unaffected and continues to appear under the report's normal listing.

#### Scenario: Running against a real corpus reports classified and default-only entries separately
- **WHEN** classification is run against a supplied set of real rule/ability Name+Text pairs
- **THEN** the report distinguishes entries that produced a non-default Target or at least one
  Effect from entries that classified to the all-default result (`Self`, no Effects)

#### Scenario: A baselined text is reported via the baseline's diff, not the normal listing
- **WHEN** a corpus text being classified has a corresponding entry in the verified-classification
  baseline
- **THEN** the report applies the baseline's new/drift/unchanged distinction for that text instead
  of listing it under the normal Effect/Target-only/default-only sections

#### Scenario: A text with no baseline entry is unaffected
- **WHEN** a corpus text being classified has no corresponding baseline entry
- **THEN** the report lists it under its normal Effect/Target-only/default-only section exactly as
  it did before the baseline existed
