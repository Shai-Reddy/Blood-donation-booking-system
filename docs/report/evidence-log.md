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
| [x] | 16 Oct | Pull request list row for #32: "Review required", checks 0/2 failed (after the #1 fix, 10 tests) | Task 5 | `evidence/ci/red-check-pr32.png` |
| [ ] | Any time | Settings → Branches → protection rule for `main` | Task 5 | `evidence/ci/branch-protection.png` |
| [x] | 16 Oct | Actions tab, run history (public page, captured with headless Chrome) | Task 5 | `evidence/ci/actions-history.png` |
| [x] | 16 Oct | Run `38378148778` (red) and `38378373592` (green) for #25; run `38881677765` (red, broken merge on #39). Run summaries; logs need sign-in | Task 3, Task 5 | `evidence/ci/age-rule-red-38378148778.png`, `age-rule-green-38378373592.png`, `broken-merge-red-38881677765.png` |
| [x] | 16 Oct | Defect register after clean-up: open (7, `status:deferred` or `status:in-progress`) and closed (9, all `status:verified`). Every issue is a defect, so the two tabs cover the same 16 as the before shot | Task 6 (after clean-up) | `evidence/defects/register-after-cleanup-open.png`, `register-after-cleanup-closed.png` |
| [x] | 16 Oct | NZBS Eligibility Criteria page, Age section. Matches PR #26 (comment on #25). The address bar and date are not in the shot; retake with them if time allows | Task 2, Task 6, #25 | `evidence/sources/nzbs-age-criteria.png` |
| [ ] | After clean-up | One issue's timeline showing every lifecycle stage (#3 or #25) | Task 6 | `evidence/defects/lifecycle-example.png` |
| [x] | 16 Oct, freeze `4e93f8d` | TRX results (95/95) and coverage report | Task 3, Task 4, Task 8 | `evidence/tests/freeze-4e93f8d-test-results.trx.xml`, `cobertura-freeze-4e93f8d.xml` |
| [x] | 16 Oct, freeze build | App: each role's main screens, rejection and authorisation messages, validation, staff and coordinator dashboards (scripted browser) | Task 1, Task 3, slides | `evidence/app/01–18*.png` |

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
| #1 Navbar booking skips eligibility | `414770c` (Natalie, C02) | `38495890975` | `c89f6e8` | red: `38831085782` (see below) | #32 (draft) | pending | open |
| #4 Stand-downs not remembered | `45de589` | `38830933862` | `c89f6e8` | red: `38831085782` (see below) | #32 (draft) | pending | open |
| #35 Staff bookings page unreachable | `da656d0` | `38845936932` | `4d76ef3` | `38846031875` | #38 | pending | open |

### #25 details (13 Oct)
- Found by checking `EligibilityService.IsAgeEligible` against the NZBS criteria (CRITERIA.md section 3). The #2 fix (`deb8737`) had set the limit to the 81st birthday with no authorisation step, and its tests passed because they used the same wrong limit.
- Test-first commit `82c13f9`: 9 integration tests in `AgeRuleBookingTests`, posting to `/Donor/Booking/Book`. Before the fix, 4 failed and 5 passed (CI run `38378148778`):
  - failed: first-time donor on 71st birthday; existing donor on 75th birthday; existing donor the day before their 81st birthday; lapsed donor on 71st birthday;
  - passed: the day-before-limit cases, existing donor on 81st birthday, and the exactly-2-years boundary.
- The test ages are literals from the NZBS criteria, not `EligibilityRules` constants, so a wrong constant fails them.
- Fix commit `0a394f4`: `EligibilityService.CheckAge` returning `Eligible`, `TooYoung`, `TooOld` or `NeedsAuthorisation`; new constants in `EligibilityRules`; per-outcome messages in the donor booking controller; age unit tests rewritten for every boundary.
- Decision recorded in PR #26: first-time donors are accepted until their 71st birthday, following the Transfusion Medicine Handbook 2.2. The registration form already used this limit.
- Source: first implemented from nzblood.co.nz search results and CRITERIA.md, because the NZBS pages block non-browser requests. Checked against the NZBS Eligibility Criteria page in a browser on 16 Oct: it matches (comment on #25, `evidence/sources/nzbs-age-criteria.png`).

### #1 and #4 details (14–16 Oct)
- Test-first for #1: Natalie's C02 commit `414770c`, 5 integration tests in `BookingEligibilityTests` posting to `/Donor/Book`. CI run `38495890975`: 75 total, 67 passed, 8 failed. 4 failures were the new tests, as intended. The other 4 were existing tests (`DonorCancelsOwnBooking_IsAllowed`, `DonorDashboard_DoesNotShowAnotherDonorsBooking`, `TC11_DeferredWithoutReason_IsRejected`, `DeferredWithReason_IsSaved`). Their booking requests were accepted but no booking appeared on the dashboard, which is what a full session produces. Likely cause, not yet confirmed: the new tests booked into session S3 in the shared static store and used up its places. A test-isolation problem linked to #17.
- Test-first for #4: `45de589`, 2 tests in `StandDownRecordTests` posting to `/Donor/Booking/Book`. `TC08_StandDownEnteredEarlier_StillBlocksBookingWithoutIt` failed on the second booking, as intended (run `38830933862`).
- Fix `c89f6e8`: one `BookingEligibilityCheck` used by both booking pages; the navbar form asks for date of birth, weight, last donation and stand-down; eligibility is judged on the session date; stand-downs recorded in `StandDownRegister` (singleton, locked).
- After the fix, CI run `38831085782`: 77 total, 67 passed, 10 failed. 9 older tests post the old form without the new required fields ("Booking failed"). 1 of Natalie's tests (`DonorWithLastDonationBeforeMinimumInterval_CannotBook`) sets the last donation relative to today; by the session date 92 days have passed, so the donor is correctly allowed to book. Both are handed to Natalie as C04 (debugging). PR #32 stays a draft until they pass.

---

## 3. Other PRs

| PR | Branch | Purpose | Closes | Approved by | Merged |
|---|---|---|---|---|---|
| #14 | `chore/quality-gates` | CI build and test workflow, defect process, warnings as errors | #12 | Natalie25-4 (with comment) | 30 Sep |
| #18 | `refactor/eligibility-domain` | Thresholds and date logic moved into the domain | – | Natalie25-4 | 2 Oct |
| #24 | `docs/copilot-evidence` | Copilot evidence guide, AI usage log, attribution setting, evidence log | – | pending | open |
| #27 | `chore/mailmap` | `.mailmap` so each member counts once in commit statistics | – | pending | open |
| #28 | `ci/coverage-smoke-tests` | C07: TRX and Cobertura coverage uploaded as CI artefacts (`coverlet.collector`) | – | none of its own (see section 6) | 16 Oct, via #31 |
| #29 | `docs/booking-eligibility-rca` | C08: #2 RCA and Copilot comparison | – | Shai-Reddy | 16 Oct |
| #30 | `docs/c01-security-review-evidence` | C01 evidence | – | none of its own (see section 6) | 16 Oct, via #31 |
| #31 | `docs/c09-age-rules-evidence` | C09 evidence | – | Shai-Reddy | 16 Oct |
| #32 | `fix/navbar-booking-eligibility` | #1 and #4 fix (draft until C04) | #1, #4 | pending | open |
| #33 | `ci/smoke-step` | CI smoke test: start the Release build, require HTTP 200 from the home page within 60 s | – | pending | open |
| #38 | `fix/staff-bookings-route` | #35: default route registered before the area route; TC18 staff test pointed at the real outcome form | #35 | pending | open |

---

## 4. CI figures

Snapshot on 16 Oct, after the #1 and #4 fix was pushed:

| Measure | Value |
|---|---|
| Total runs | 74 |
| Passed | 56 |
| Failed | 18 |
| Runs on `main` | 10, all passed |

The 18 failed runs come from 11 commits:
- 9 test-first commits, red on purpose: `907ade7`, `8d5563a`, `3aae71f`, `a9d059f`, `e3f3510`, `61b6592`, `82c13f9`, `414770c`, `45de589`.
- `c89f6e8`: the #1 fix broke 9 older tests that build bookings with the old form, and exposed a wrong assumption in one new test. CI caught both before merge.
- `1b37f37`: the Copilot-written NFR2 load test passed its arguments to `Assert.IsLessThanOrEqualTo` the wrong way round, so it asserted 2000 ≤ 8.36 ms and failed. CI caught a test bug, not a product bug.

No run on `main` has failed.

Code coverage (C07, 14 Oct, 70 tests): line 71.02% (527/742), branch 75.37% (499/662). Source: `evidence/copilot/C07-ci-coverage/notes.md`. Re-measure on the freeze commit.

Recount on the freeze commit with:
```
gh run list --limit 200 --json conclusion,headBranch -q '.[] | "\(.conclusion) \(.headBranch)"' | sort | uniq -c
```

---

## 5. Test counts

| Date | Commit | Total | Passed | Failed | Source | Note |
|---|---|---|---|---|---|---|
| 13 Oct | `3ff66d8` (`main`) | 70 | 70 | 0 | local `dotnet test` | Baseline at code freeze |
| 13 Oct | `82c13f9` | 79 | 75 | 4 | CI run `38378148778` | Test-first for #25; the 4 failures are new tests |
| 13 Oct | `0a394f4` | 85 | 85 | 0 | CI run `38378373592` | #25 fixed (branch, not yet on `main`) |
| 14 Oct | `414770c` | 75 | 67 | 8 | CI run `38495890975` | Test-first for #1 (Natalie, C02) |
| 16 Oct | `d03f051` (`main`) | 70 | 70 | 0 | CI run `38830168657` | `main` after #28–#31 (docs and CI only) |
| 16 Oct | `c89f6e8` | 77 | 67 | 10 | CI run `38831085782` | #1 and #4 fixed; 10 test failures left for C04 |

---

## 6. Observations for the report

Facts found while collecting evidence that the report should state plainly.

- **PR #15 was never reviewed on its own.** Its commits reached `main` through PR #18 (merge commit `fe889fc`), because the refactor branch was built on top of it. #18 was approved, so the code was reviewed, but only indirectly. It's a gap in the gate worth mentioning in Task 5.
- **Stacked branches let two more PRs skip their own review.** Natalie built each branch on the previous one, so merging #31 also carried in #28 (a CI workflow change) and #30. GitHub marked both merged with no review of their own. With #15 that is three PRs. Rule from 16 Oct: one branch per PR, always created from `main`.
- **Most approvals have no comment.** Only #14's approval has text. #16 and #18 were approved in the same minute (2 Oct, 00:49 UTC), and #23 was approved twice within a minute. The lab-work criterion asks for "real comments, not same-minute approvals", so reviews from now on should include comments. #29 and #31 (16 Oct) were also approved with no comment, in the same minute as the merge.
- **Defect status labels didn't match the lifecycle** before 13 Oct: only #13 and #22 carried `status:verified`; #2, #3, #5 and #12 were closed as `status:fixed`, #7 and #11 closed as `status:in-progress`, and #9 closed with no status. Corrected on 13 Oct (section 7); the before screenshot is `evidence/defects/register-before-cleanup.png`.

---

- **C07 added coverage but no smoke step.** Natalie's notes say so explicitly. Added separately in PR #33 (not Copilot-assisted). CI run `38834335237`: 70/70 tests, then "Smoke test passed: home page returned HTTP 200 after 2 attempt(s)." Checked locally that the step fails when the app does not respond (HTTP 000).
- **The usage log diverged.** `main` gained an older copy of `ai-usage-log.md` (from #29–#31), with a C08 row in the mid-project section. Resolved in the `docs/copilot-evidence` branch on 16 Oct (`d53d631`), keeping M1–M5 and Natalie's C rows.

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

### 14–15 Oct (Natalie)
- Copilot sessions C01, C02, C07, C08 and C09; C05 started (scaffold `1b37f37`, no PR).
- PRs #28–#31 opened; usage log consolidated on `docs/copilot-evidence` (`9bdab3e`).

### 16 Oct
- #28–#31 merged (#28 and #30 through #31, see section 6). `main` green, 70 tests.
- #4 test-first `45de589`; #1 and #4 fix `c89f6e8`; draft PR #32. Issues #1 and #4 labelled `status:in-progress`.
- Usage-log conflict on #24 resolved (`d53d631`).
- Screenshots filed: NZBS source (matches #26; comment on #25), PR #32 red check, defect register after clean-up (open and closed).
- CI smoke step: PR #33, green in run `38834335237`.

---

## 9. Copilot sessions (Task 9)

Status on 16 Oct. Details in `ai-usage-log.md` and `evidence/copilot/`.

| Session | Category | Status |
|---|---|---|
| C01 Security review | QA | Done. Neutral prompt 3/5 known defects, security prompt 0/5, targeted 3/5 |
| C02 Test-first tests for #1 | Testing | Done (`414770c`). No evidence folder yet; the log row says "location to confirm" |
| C03 Test design | Testing | Not started (optional) |
| C04 Debugging | Debugging | Done (#39): both causes found and fixed; 77/77 |
| C05 Load test | Implementation | Done (#40): average 10.96 ms, p95 27.56 ms, max 32.14 ms |
| C06 Test helper refactor | Refactoring | Done (#41): helpers shared by 2 classes; 8 files still duplicate |
| C07 CI coverage | QA (CI) | Done (#28). No smoke step |
| C08 RCA for #2 | QA (RCA) | Done (#29). Notes say the comparison was not independent |
| C09 NZBS age rules | QA | Done (#31) |
| C10 Copilot as reviewer | QA | Not started (optional) |

All five categories now have at least one logged example. C04–C06 evidence is in `docs/evidence/C04–C06/`; their rows still need adding to `ai-usage-log.md`.

### 16 Oct (later): quality testing and new defects
- **Scope reasons** for Partial and Out-of-scope requirements reconstructed from the mid-project sprint plan and the git and issue timeline (Task 2.1). FR7 was never assigned a sprint: a planning gap.
- **#34 logged** (FR5 24-hour cutoff not implemented; high, P2, deferred). Sessions store their start time only as display text.
- **Coverage measured on `main` (`d03f051`)**, Release build, `coverlet.collector`: overall line 71.03% (466/656), branch 75.0% (483/644); Domain line 84.4%, branch 100%; Web line 69.6%, branch 74.3%. File: `evidence/tests/cobertura-main-d03f051.xml`. Domain's uncovered lines are unused model classes (`Session`, `BloodStock`, part of `StandDownEvent`).
- **Defect close times** (9 closed): mean 43.2 h, median 48.6 h, all within 80 h. #13 is 0 h (logged after being fixed).
- **Release preview build**, local only (branch `tmp/release-preview`, not pushed): `main` + #26 + #33 + #32. One conflict in `Areas/Donor/BookingController.cs`, resolved by keeping #32's version and porting #26's `CheckAge` into `BookingEligibilityCheck`. 92 tests: 82 passed, 10 failed (the known C04 failures only). Reuse this resolution when #26 merges and #32 is updated.
- **Security probes** against the preview app on Kestrel (`evidence/quality/security-probes-2026-10-16.txt`): 44/44 direct-URL role checks as expected; open redirect blocked; no anti-forgery token gives 400; cross-donor cancel refused; cookie HttpOnly, SameSite=Lax. **Lockout failed**: signed in after 10 wrong passwords → **#36** (medium, P2, deferred).
- **#35 found** by exploratory testing of the running app: the staff dashboard's "View All Bookings" link (`/Staff/Bookings`) opened the area prototype page, because the area route was registered first. A donor's booking was invisible to staff. Logged as **P1**, fixed test-first in **PR #38** (red `38845936932`, green `38846031875`). `TC18_StaffRecordOutcome_LoadsClientValidation` had been written against the prototype page and was updated.
- **NFR2 load test**, out of process (`evidence/quality/load-test-nfr2-2026-10-16.txt`): 20 signed-in donors × 10 rounds, 200 requests, all refused on the 84-day rule as designed. Average 3.3 ms, median 3.1, p95 5.8, max 7.1 ms. Apple M4, same machine.
- **Lighthouse 12.6.1 accessibility** (`evidence/quality/lighthouse/`): home 92 (step-number contrast 1.8:1 → **#37**, low, P3, deferred), sign in 100, staff sign in 100, register 100.
- **Report drafts** for Tasks 1–11 written in `docs/report/` (not yet committed).

### 16 Oct (evening): merges and the freeze commit
- Natalie merged #24, #26, #27, #33 and #38. #25 and #35 closed; marked `status:verified` with test names.
- Natalie's C04 (#39), C05 (#40) and C06 (#41), each with CI green. All three were built on #32's branch and on each other.
- `847539b`: `main` merged into #39 through GitHub's web conflict editor. The donor booking controller was resolved by mixing two versions; CI failed at Build with 15 compile errors (runs `38881677765`, `38881685074`). Redone in `945f249` (controller from #32, `BookingEligibilityCheck` using `CheckAge` from #26): 93/93 locally, CI green. #40 and #41 updated from the corrected #39 (95/95 each).
- Merged one at a time, each with a written approval by Shailesh: #39 (`721225a`), #40 (`058a70d`), #41 (`4e93f8d`). #32 shows merged through #39. #1 and #4 closed; marked `status:verified`.
- **Freeze commit `4e93f8d`.** CI run `38883522127` green. Local Release run: **95 passed, 0 failed**, 0 warnings. 45 unit, 48 integration, 2 performance.
- **Coverage at freeze:** overall line 84.54% (607/718), branch 80.8% (564/698); Domain 87.3% / 100%; Web 84.2% / 80.1%.
- **Security probes on the freeze build** (`evidence/quality/security-probes-freeze-4e93f8d.txt`): 44/44 role checks; open redirect blocked; anti-forgery 400; cross-donor cancel refused; **P6 new: staff see a donor's booking with its outcome form (#35 fixed)**; lockout still fails (#36, deferred); cookie HttpOnly, SameSite=Lax.
- **NFR2 on the freeze build** (`evidence/quality/load-test-nfr2-freeze-4e93f8d.txt`): 200 requests, all refused on the 84-day rule; average 3.6 ms, median 3.3, p95 7.1, max 8.6 ms. Natalie's in-process C05: average 10.96, p95 27.56, max 32.14 ms.
- **Final figures:**
  - CI: 133 runs, 112 passed, 21 failed (19 at Test, 2 at Build); 18/18 on `main`; median 42 s.
  - Failed-run commits: 10 test-first; 3 caught before merge (`c89f6e8`, `1b37f37`, `847539b`).
  - Defects: 20; 13 closed and verified, 7 deferred; 0 open P1.
  - Time to close: median 48.7 h, mean 96.3 h, longest 393.5 h (#1).
  - Size: 1,387 non-blank C# lines in Domain and Web, so 14.4 defects per KLOC.
  - Commits on `main` (no merges, `.mailmap`): Shailesh 40, Natalie 24.
  - PRs: 21 merged; 3 without their own review (#15, #28, #30); 4 of 18 approvals with a written comment (#14, #39, #40, #41).
- Report drafts for Tasks 1–11 updated with the freeze figures.
- **Natalie's remaining Copilot paperwork, finished by Shailesh where possible:**
  - C04–C06 rows added to `ai-usage-log.md` from her reports, with Member = Natalie.
  - Her C05 report corrected: NFR2 sets the 2,000 ms limit, so it is not provisional. The correction is marked in the report.
  - The load test's "provisional" comment and messages corrected.
  - C07 CI-run screenshot captured from the public run page (`evidence/copilot/C07-ci-coverage/ci-run-with-coverage-artefact.png`, run `38504692279`, artefact `test-results-and-coverage`, 39.8 KB).
- **Still Natalie's:** C01 and C09 chat screenshots (only her chat history has them); her Task 9 and Task 11 reflections.
- **Scripted browser run** (Puppeteer driving Chrome) against the freeze build (`evidence/app/`, `results.txt`). Session S1 was 21 Oct.
  - MT1: 83 days refused with "You can book a session from 22 Oct 2026"; 84 days confirmed.
  - MT3: S4 "Fully booked", option disabled.
  - MT4: per-field messages on the client, and on the server with JavaScript off.
  - MT6: O+ stock 14 → 15 after staff recorded Donated.
  - MT7: aged 75 told to contact NZBS for Medical Officer authorisation.
  - Access: donor on a staff page sent to Access Denied.
  - MT5: register → dashboard → booking form → dashboard with confirmation, which is 3 screens before the confirmation. No newcomer took part.
  - TC11 visual: deferral without a reason refused, but the message appeared on every row → **#43** (low, P3, deferred). The register now has 21 defects.
