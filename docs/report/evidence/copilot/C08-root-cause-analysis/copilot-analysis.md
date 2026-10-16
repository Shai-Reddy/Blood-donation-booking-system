# Root cause analysis: defect #2

## Incident context

- **Defect:** Returning-donor age eligibility omitted the NZBS Medical Officer authorisation requirement for donors aged 75 and over.
- **Observed faulty rule:** Returning donors were treated as age-eligible until their 81st birthday without medical authorisation.
- **Analysis date in original evidence:** 14 October 2024.
- **Method:** Five Whys, using the recorded Copilot interaction and the repository implementation and tests.

## Problem statement

The returning-donor age rule allowed a donor to book up to (but not including) their 81st birthday based on age alone. It did not require Medical Officer authorisation for donors aged 75 and over, contrary to the stated eligibility requirement. Automated tests passed both before and after the change; therefore, a green test result did not demonstrate that the rule matched the requirement.

## Five Whys

1. **Why was the returning-donor eligibility decision wrong?**  
   The decision checked the donor's age against the under-81 limit but did not apply the separate Medical Officer authorisation condition for ages 75 and over.

2. **Why was the authorisation condition not applied?**  
   The rule was represented as a simple age cutoff. The implementation exposed only age, booking date, and a new-versus-returning flag, so it had no authorisation input or decision path. The recorded Copilot evidence says its response repeated the repository's existing assumption that returning and lapsed donors could donate until age 81 without authorisation.

3. **Why did the automated tests pass?**  
   The tests checked the behavior encoded by that assumption rather than independently testing the complete eligibility requirement. The current age tests cover age boundaries (including ages 80 and 81), but the age method cannot represent authorisation and there is no test for the age-75 authorisation condition. Tests can pass while consistently asserting an incorrect rule.

4. **Why was the assumption not caught during implementation or review?**  
   The age thresholds and authorisation condition were not checked against an authoritative NZBS source. The evidence records that Copilot could not access the official criteria page and did not identify the missing condition; its answer and the existing code/tests were consequently weak substitutes for domain verification.

5. **Why was independent policy verification absent?**  
   The development/review process did not require AI-generated, safety-relevant eligibility rules to be traced to an authoritative requirement and separately reviewed before acceptance. Nor did it require tests to be derived from that requirement rather than from the implementation.

## Likely root cause and classification

**Likely root cause:** A process/control failure in requirements validation and test design allowed an incorrect, AI-repeated domain assumption to become both the implementation rule and the test oracle. The specific coding omission was the missing authorisation decision, but the tests passing before and after the change indicate that test execution was not the primary failure: the tests did not independently encode the full business rule.

**Classification:** Requirements/acceptance-criteria validation and test-oracle design (with inadequate human/domain review of AI-generated logic). This is not evidence of an automated test-runner failure.

This is the most likely explanation supported by the available evidence, not a confirmed causal finding. The exact source policy and change history should be checked before treating the root cause as final. The repository's NZBS URL comment is a pointer, not proof that the relevant policy was consulted or interpreted correctly.

## Corrective actions

1. Confirm the complete returning-donor rule, including the age-75 authorisation threshold, age-81 boundary, and which donor-history categories it applies to, against current authoritative NZBS guidance; record the source and policy version/date.
2. Implement the complete decision using explicit donor status and Medical Officer authorisation data. Do not make an under-81 age check alone sufficient for donors who require authorisation.
3. Add requirement-derived boundary tests that independently assert:
   - just below, on, and just above the age-75 threshold, both with and without authorisation;
   - the day before, on, and after the 81st birthday;
   - relevant new, returning, and lapsed donor cases, where the authoritative policy distinguishes them.
4. Check the booking/eligibility call path to ensure the authorisation status is available and enforced, not merely added to a unit-test-only or unused code path.
5. Re-run the automated suite and record the new tests' linkage to the approved policy requirement. A passing suite is necessary but is not, by itself, proof of policy correctness.

## Preventive actions

1. Require a named authoritative source and a reviewed, versioned acceptance criterion for domain-sensitive eligibility rules before implementation.
2. Derive test cases and expected outcomes from those approved criteria, independently of the implementation; explicitly include conditions as well as numeric boundaries.
3. Require human domain-owner review of AI-generated or AI-modified safety-relevant business rules and their tests. Do not treat repository code, generated comments, or green tests as independent validation.
4. Make source, rule interpretation, and requirement-to-test traceability part of the change review record. If the authoritative source cannot be accessed, block policy-sensitive changes pending domain-owner confirmation.
5. Add review coverage for the end-to-end decision path whenever an eligibility rule depends on data beyond age (for example, authorisation or donor history).

## Evidence and limitations

- `EligibilityService.IsAgeEligible` only receives date of birth, booking date, and `isNewDonor`; its returning-donor branch accepts ages below 81 and has no authorisation input.
- `EligibilityRules` names 81 as the returning-donor age limit.
- Existing returning-donor tests assert eligibility at ages 71, 72, and 80, eligibility the day before the 81st birthday, and ineligibility at age 81. They do not test medical authorisation or the age-75 threshold.
- The supplied Copilot evidence says the model repeated the existing age-only assumption, missed the authorisation requirement, and could not access the official criteria page.
- The claim that tests passed before and after the change is supplied in the defect description. The artifacts reviewed here do not establish which test versions ran or prove the exact chronology of the rule change.
- Confirm the applicable NZBS policy version and whether lapsed donors share the returning-donor rule before expanding the fix or stating those details as verified.
