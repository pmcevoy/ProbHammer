## REMOVED Requirements

### Requirement: On-Demand Characteristic-Modifier Candidate Exposure
**Reason**: The candidates were only ever read by the offline `RuleEffectClassificationReport` tool,
which this change deletes, along with the classification that produced them.
**Migration**: None. Characteristic effects come from the ability-classification catalogue
(`ability-classification-catalogue`).
