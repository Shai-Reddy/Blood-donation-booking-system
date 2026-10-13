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
| [ ] | **Now**, before Natalie approves | PR #24 or #26 merge box showing "Review required" | Task 5 | `evidence/ci/pr-review-required.png` |
| [ ] | **Now**, before any label changes | Issues list filtered by `type:defect`, showing current status labels | Task 6 (before clean-up) | `evidence/defects/register-before-cleanup.png` |
| [ ] | When #1's test-first commit is pushed | PR or commit page showing the failed check | Task 5 | `evidence/ci/red-check-test-first.png` |
| [ ] | Any time | Settings → Branches → protection rule for `main` | Task 5 | `evidence/ci/branch-protection.png` |
| [ ] | Any time | Actions tab, run history | Task 5 | `evidence/ci/actions-history.png` |
| [ ] | Any time | Run `38378148778` (red) and `38378373592` (green), test step expanded | Task 3, Task 5 | `evidence/ci/age-rule-red.png`, `age-rule-green.png` |
| [ ] | After clean-up | Issues list filtered by `type:defect` | Task 6 (after clean-up) | `evidence/defects/register-after-cleanup.png` |
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
| #24 | `docs/copilot-evidence` | Copilot evidence guide, AI usage log, attribution setting | – | pending | open |

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
- **Defect status labels don't match the lifecycle** (13 Oct):
  - #2, #3, #5 and #12 are closed but labelled `status:fixed`;
  - #7 and #11 are closed but labelled `status:in-progress`;
  - #9 is closed with no status label;
  - #1, #4, #6, #8, #10 and #17 are open with no status label.

  Only #13 and #22 carry `status:verified`.

---

## 7. Session log

### 13 Oct (code freeze)
- Opened #25 for the NZBS age rule. Test-first and fix pushed on `fix/age-rule-authorisation`, PR #26.
- PR #24: Copilot evidence guide and AI usage log, with mid-project rows M1–M5.
- Pushed `fix/navbar-booking-eligibility` (same as `main`) for Natalie's C02 test-first commit for #1.
- `.claude/settings.json`: AI attribution turned off (`822e57f`, in PR #24).
