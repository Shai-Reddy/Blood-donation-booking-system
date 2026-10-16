C08 Roots cause analysis: defect #2

Session information:

- Date: 14 October 2026
- Category: QA root cause analysis
- Defect: #2 - incorrect returning donor age eligibility
- Git branch: docs/booking-eligibility-rca
- Starting commit: 3ff66d8
- Analysis completed before consulting Copilot: Yes:
- Time completed: 

Independent 5 WHY's analysis 

Problem statement:

The returning-donor-age eligibility implemenation allowed donors up to their 81st birthday without requiring NZBS Medical officer authorisation. this was inconsistent with the documented eligibility requirement 

Why 1: Why was the eligibility rule incorrect?
- The application did not enforce the mdeical authorisation requirement for returning donors aged 75 and above

Why 2: Why did the implementation omit the requirement?

- The AI-generated logic incorrectly interpreted the age limits. A correction still retained an incorrect assumption about eligibility up to age 81

Why 3: Why did automated testing fail to detect the defact?

- The test were derived from the implementations assumptions instead of independently checking the requirements. therefore the tests passed even though the business rule was incorrect

Why 4: Why were the incorrect assumption not identified?

- The age thresholds and medical authorisation conditions were not correctly validated against the official NZBS eligibility guidance during implementation and review. 

Why 5 - Why was independent verification missing>?

- The development process lacked an explicit requirement to verify AI-generated eligibility rules against authoritative domain documentation before accepting the implementation and its tests. 

Provisional root cause

Category: requirement validation and test design 

- the defect resulted from inadequate verification of AI-generated domain logic, combined with tests that repeated the implementations assumptions 

Corrective actions

1. correct the returning-donor age eligibility logic to enforce the appropriate authorisation requirements. 
2. Add independent boundary tests for donors approaching ages 75 and 81
3. Add tests covering medical authorisation and lapsed-donor conditions

Preventive actions

1. Verify eligibility rules against official NZBS guidance before implementation
2. Derive test expectations from documented requirements rather than implementation behaviour
3. Require human of AI-generated safety critical business rules
4. Record authoritative sources alongside eligibility requirements and tests. 

Verification status

- this is a preliminary analysis based on the project evidence guide. the issue and implementation should be reviewed before confirming the root cause 

Copilot comparision

- to be completed after the independent analysis has been saved and Copilot has responsed 

## Comparison with GitHub Copilot

| Criterion | Our analysis | Copilot's analysis |
|---|---|---|
| Incorrect age rule | Identified | Identified |
| Missing medical authorisation | Identified | Identified |
| Tests repeated incorrect assumptions | Identified | Identified |
| Missing NZBS verification | Identified | Identified |
| Missing human review process | Identified | Identified |

### Measured results

- Relevant Why levels: 5/5
- Root-cause classification matched ours: Yes
- Preventive actions worth adopting: 5 proposed; 3 selected for consideration
- Selected actions: authoritative-source verification, independent test design, and mandatory human review.
- Decision: Accept the analysis as supporting evidence, subject to verification.

### Limitations

Copilot had access to our original notes.md during its analysis.
Therefore, this was not a fully independent comparison.

Copilot also modified notes.md without being asked and
introduced an incorrect date (14 October 2024 instead of 2026).

The original analysis was restored, and Copilot's output
was preserved separately in copilot-analysis.md.

The root cause remains provisional until verified against
the relevant NZBS policy and project history.