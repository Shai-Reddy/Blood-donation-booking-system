# C01 — GitHub Copilot Security Review

## Objective

Evaluate GitHub Copilot's ability to identify known defects in the historical `DonorController.cs` implementation, and assess how different prompt styles affect the findings.

## Historical code reviewed

- **Commit:** `d26d2ce`
- **File:** `BloodDonation.Web/Controllers/DonorController.cs`
- **Copilot mode:** Ask
- **Model selection:** Auto

The historical controller was confirmed to exist at the specified commit.

## Prompt 1 — Neutral code review

Copilot identified **3 of 5 known defects**:

1. Missing cancellation ownership checks.
2. Missing rescheduling ownership checks.
3. Missing anti-forgery validation.

Copilot did not identify:

- Dashboard data exposure.
- Missing booking eligibility validation.

Copilot also raised four additional concerns requiring independent verification.

## Prompt 2 — Security-focused review

Copilot reported no security vulnerabilities despite reviewing the historical controller containing known defects.

**Result: 0 of 5 known defects identified.**

This demonstrated that explicitly requesting a security review did not guarantee accurate vulnerability detection.

## Prompt 3 — Targeted review

Copilot identified **3 of 5 known defects**.

The exact findings should be checked against the original chat export before listing them individually.

## Comparison

| Prompt style | Known defects identified |
|---|---|
| Neutral review | 3/5 |
| Security-focused review | 0/5 |
| Targeted review | 3/5 |

## Human evaluation

Copilot's findings were inconsistent and dependent on prompt framing.

The neutral and targeted prompts identified more known defects than the security-focused prompt. However, none of the three reviews identified all five.

The results reinforce the importance of independent code reviews, security testing and verification against project requirements. Copilot's responses should be treated as suggestions requiring validation rather than definitive security assessments.

## Evidence

- `image.png` — screenshot of Copilot review


## Conclusion

Copilot provided some useful security findings but failed to detect several known defects. Human verification remained necessary to establish the accuracy and completeness of the review.