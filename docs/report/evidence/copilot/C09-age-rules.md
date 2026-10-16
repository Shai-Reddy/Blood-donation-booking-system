# C09 — GitHub Copilot Age Eligibility Rules QA

## Objective

Evaluate whether GitHub Copilot could identify incorrect or inconsistent age eligibility rules in the Blood Donation Booking System.

## Prompt 1 — Initial age eligibility review

Copilot's first response repeated the project's existing age eligibility rules, including the incorrect assumption that returning and lapsed donors could donate until their 81st birthday without medical officer authorisation.

Copilot accessed the repository despite no files being explicitly attached to the prompt.

## Prompt 2 — Follow-up review

Copilot identified an inconsistency between `EligibilityService.cs` and `Register.cshtml` concerning 71-year-old first-time donors.

However, it did not identify the missing medical officer authorisation requirements for older returning and lapsed donors.

## Limitations

Copilot could not access the official New Zealand Blood Service (NZBS) eligibility criteria page and acknowledged that its assessment was incomplete.

## Human evaluation and conclusion

The review demonstrated the risk of relying on existing implementation code and automated tests as evidence that domain-specific eligibility rules are correct.

Copilot identified one implementation inconsistency but did not independently establish the correct NZBS requirements.

Human verification against authoritative eligibility criteria remains essential, particularly for safety-critical rules.