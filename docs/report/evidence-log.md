# Evidence log

Running record of checkable evidence for the final report: issues, commits, CI runs, PRs, test counts and screenshots. Updated as work happens. Every entry here comes from git or GitHub; nothing is estimated.

- Repo: https://github.com/Shai-Reddy/Blood-donation-booking-system (moved from `Natalie25-4/Blood-donation-booking-system`; the old URL redirects)
- CI run links: `https://github.com/Shai-Reddy/Blood-donation-booking-system/actions/runs/<run id>`
- Copilot evidence is logged separately in `ai-usage-log.md`.

---

## 1. Screenshots still needed

Items marked **Now** show a state that will change. Capture them before the next step happens.

| Status | When | Screenshot | For | Save as |
|---|---|---|---|---|
| [x] | 13 Oct | Pull request list: #24 and #26 showing "Review required" with checks 2/2 passed | Task 5 | `evidence/ci/pr-review-required.png` |
| [x] | 13 Oct | Issues list filtered by `type:defect` (all 16), status labels before clean-up | Task 6 (before clean-up) | `evidence/defects/register-before-cleanup.png` |
| [ ] | When #1's test-first commit is pushed | PR or commit page showing the failed check | Task 5 | `evidence/ci/red-check-test-first.png` |
| [ ] | Any time | Settings → Branches → protection rule for `main` | Task 5 | `evidence/ci/branch-protection.png` |
| [ ] | Any time | Actions tab, run history | Task 5 | `evidence/ci/actions-history.png` |
| [ ] | Any time | Run `38378148778` (red) and `38378373592` (green), test step expanded | Task 3, Task 5 | `evidence/ci/age-rule-red.png`, `age-rule-green.png` |
| [ ] | **Now**, clean-up done 13 Oct | Issues list filtered by `type:defect`, same view as the before shot | Task 6 (after clean-up) | `evidence/defects/register-after-cleanup.png` |
| [ ] | After clean-up | One issue's timeline showing every lifecycle stage (#3 or #25) | Task 6 | `evidence/defects/lifecycle-example.png` |
| [ ] | On the freeze commit | `dotnet test` output, TRX file, coverage report | Task 3, Task 4 | `evidence/tests/` |
| [ ] | On the freeze commit | App: each role's main screens, a rejection message, the coordinator dashboard | Task 1 | `evidence/app/` |

---

## 2. Defect fixes: test first, then fix

Every fix follows the same chain: a test-first commit that fails CI, a fix commit that passes, then a PR with review. The CI runs listed are the push runs; each commit also has a `pull_request` run with the same result.

| Issue | Test-first commit | Red run | Fix commit | Green run | PR | Approved by | Merged |
|---|---|---|---|---|---|---|---|
| #2, #5 Returning donor age limit | `907ade7` | `36945510631` | `deb8737` | `36945666864` | #15 | no review of its own (see section 6) | 2 Oct, via #18 |
| #3 Booking ownership | `8d5563a` | `36946646272` | `a0c273d` | `36947046146` | #16 | Natalie25-4 | 2 Oct |
| #7 Deferral reason required | `3aae71f` | `36992134268` | `96d3568` | `37083805915` | #19 | Shai-Reddy | 3 Oct |
| #11 Today's sessions count | `a9d059f` | `37087531109` | `223789f` | `37089283133` | #20 | Shai-Reddy | 3 Oct |
| #9 Client-side validation | `e3f3510` | `37096971901` | `59c282f` | `37103971876` | #21 | Shai-Reddy | 3 Oct |
| #22 Anti-forgery | `61b6592` | `37108388480` | `3ecf9d9` | `37109501824` | #23 | Shai-Reddy | 3 Oct |
| #25 NZBS age rule (corrects #2) | `82c13f9` | `38378148778` | `0a394f4` | `38378373592` | #26 | pending | open |

### #25 details (13 Oct)
- Found by checking `EligibilityService.IsAgeEligible` against the NZBS criteria (CRITERIA.md section 3). The #2 fix (`deb8737`) had set the limit to the 81st birthday with no authorisation step, and its tests passed because they used the same wrong limit.
- Test-first commit `82c13f9`: 9 integration tests in `AgeRuleBookingTests`, posting to `/Donor/Booking/Book`. Before the fix, 4 failed and 5 passed (CI run `38378148778`):
  - failed: first-time donor on 71st birthday; existing donor on 75th birthday; existing donor the day before their 81st birthday; lapsed donor on 71st birthday;
  - passed: the day-before-limit cases, existing donor on 81st birthday, and the exactly-2-years boundary.
- The test ages are literals from the NZBS criteria, not `EligibilityRules` constants, so a wrong constant fails them.
- Fix commit `0a394f4`: `EligibilityService.CheckAge` returning `Eligible`, `TooYoung`, `TooOld` or `NeedsAuthorisation`; new constants in `EligibilityRules`; per-outcome messages in the donor booking controller; age unit tests rewritten for every boundary.
- Decision recorded in PR #26: first-time donors are accepted until their 71st birthday, following the Transfusion Medicine Handbook 2.2. The registration form already used this limit.
- Source note: the NZBS pages block non-browser requests, so the rules were taken from nzblood.co.nz search results and CRITERIA.md. **Open the page in a browser and screenshot it before citing it.**

---

## 3. Other PRs

| PR | Branch | Purpose | Closes | Approved by | Merged |
|---|---|---|---|---|---|
| #14 | `chore/quality-gates` | CI build and test workflow, defect process, warnings as errors | #12 | Natalie25-4 (with comment) | 30 Sep |
| #18 | `refactor/eligibility-domain` | Thresholds and date logic moved into the domain | – | Natalie25-4 | 2 Oct |
| #24 | `docs/copilot-evidence` | Copilot evidence guide, AI usage log, attribution setting, evidence log | – | pending | open |
| #27 | `chore/mailmap` | `.mailmap` so each member counts once in commit statistics | – | pending | open |

---

## 4. CI figures

Snapshot on 13 Oct, after the #25 fix was pushed:

| Measure | Value |
|---|---|
| Total runs | 44 |
| Passed | 31 |
| Failed | 13 |
| Failed runs that were test-first commits | 13 of 13 |
| Runs on `main` | 8, all passed |

Every red run was intended: a test written to expose a known defect before its fix. No run on `main` has failed. Recount on the freeze commit with:
```
gh run list --limit 200 --json conclusion,headBranch -q '.[] | "\(.conclusion) \(.headBranch)"' | sort | uniq -c
```

---

## 5. Test counts

| Date | Commit | Total | Passed | Failed | Source | Note |
|---|---|---|---|---|---|---|
| 13 Oct | `3ff66d8` (`main`) | 70 | 70 | 0 | local `dotnet test` | Baseline at code freeze |
| 13 Oct | `82c13f9` | 79 | 75 | 4 | CI run `38378148778` | Test-first for #25; the 4 failures are new tests |
| 13 Oct | `0a394f4` | 85 | 85 | 0 | CI run `38378373592` | #25 fixed |

---

## 6. Observations for the report

Facts found while collecting evidence that the report should state plainly.

- **PR #15 was never reviewed on its own.** Its commits reached `main` through PR #18 (merge commit `fe889fc`), because the refactor branch was built on top of it. #18 was approved, so the code was reviewed, but only indirectly. It's a gap in the gate worth mentioning in Task 5.
- **Most approvals have no comment.** Only #14's approval has text. #16 and #18 were approved in the same minute (2 Oct, 00:49 UTC), and #23 was approved twice within a minute. The lab-work criterion asks for "real comments, not same-minute approvals", so reviews from now on should include comments.
- **Defect status labels didn't match the lifecycle** before 13 Oct: only #13 and #22 carried `status:verified`; #2, #3, #5 and #12 were closed as `status:fixed`, #7 and #11 closed as `status:in-progress`, and #9 closed with no status. Corrected on 13 Oct (section 7); the before screenshot is `evidence/defects/register-before-cleanup.png`.

---

## 7. Defect register clean-up (13 Oct)

Each change has a comment on the issue giving the evidence. Verification evidence: CI run `37110123735` on `main` (`3ff66d8`), 70 passed, 0 failed.

| Issue | Was | Now | Evidence on the issue |
|---|---|---|---|
| #2 | `status:fixed` | `status:verified` | `TC07b_IsAgeEligible_ExistingDonor_Age81_ReturnsFalse`; notes the wrong limit is #25 |
| #3 | `status:fixed` | `status:verified` | 4 `BookingOwnershipTests` |
| #5 | `status:fixed` | `status:verified` | `TC07b_IsAgeEligible_ExistingDonor_Age81_ReturnsFalse` |
| #7 | `status:in-progress` | `status:verified` | `TC11_DeferredWithoutReason_IsRejected`, `DeferredWithReason_IsSaved` |
| #9 | none | `status:verified` | 4 `TC18_*` client validation tests |
| #11 | `status:in-progress` | `status:verified` | 3 `SessionStatsTests` |
| #12 | `status:fixed` | `status:verified` | Build with `TreatWarningsAsErrors` passing |
| #6 | none | `status:deferred` | P2 under code freeze; `/Coordinator` (tested service) is the reported dashboard |
| #8 | none | `status:deferred` | P2 under code freeze; 71 mismatch removed by #26; registration still blocks existing donors aged 71–74 |
| #10 | none | `status:deferred` | Partly fixed by #18; JavaScript copy in `Register.cshtml` remains, tied to #8 |
| #17 | none | `status:deferred` | Prototype in-memory store; production condition in Task 8 |

Still open without a status: #1 and #4 (P1; to be fixed together once Natalie's C02 test-first commit for #1 is in).

---

## 8. Session log

### 13 Oct (code freeze)
- Opened #25 for the NZBS age rule. Test-first and fix pushed on `fix/age-rule-authorisation`, PR #26.
- PR #24: Copilot evidence guide and AI usage log, with mid-project rows M1–M5.
- Pushed `fix/navbar-booking-eligibility` (same as `main`) for Natalie's C02 test-first commit for #1.
- `.claude/settings.json`: AI attribution turned off (`822e57f`, in PR #24).
- Screenshots: pull request list showing "Review required"; defect register before clean-up.
- Defect register clean-up: 7 verified, 4 deferred (section 7).
- PR #27: `.mailmap`. `git shortlog -sne` now shows two people instead of four identities.
