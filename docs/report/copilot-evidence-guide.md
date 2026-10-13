# GitHub Copilot evidence guide (Task 9)

For: Natalie. Owner of all Copilot work for the final report. This guide is your whole task list for the rest of the project; Shailesh does everything else.
Deadline: every session below that is marked **Must** is logged by **the morning of Thu 15 October**, so Shailesh and you can write Tasks 6 and 9 from it.

---

## 1. What the marker is looking for

Task 9 is worth 5 points. "Exceeds" needs:

- responsible Copilot use on real project work, in all five categories: **implementation, testing, debugging, refactoring, QA**;
- proof that every output was reviewed, changed where needed, tested, then accepted or rejected;
- the quality risks found in Copilot's output;
- reflection that shows judgement: when Copilot was worth using and when it was risky, **backed by measured examples**, not description.

"Measured" is the part most reports miss. Every session below has a **Measure** line. Record those numbers. They are what turn "Copilot was sometimes wrong" into "Copilot found 1 of 5 known security defects unaided".

Several sessions also produce evidence for other tasks (load test → Task 7, CI steps → Task 5, RCA comparison → Task 6). That is deliberate: the Copilot work is real work we still have to do, not a staged exercise.

---

## 2. Ground rules

1. **Log everything, including failures.** A wrong answer from Copilot is good evidence. Never re-prompt until it looks good and only screenshot the last attempt. Screenshot every attempt and number them.
2. **Neutral prompt first.** Where a session is about whether Copilot can find something, ask a neutral question first, then a more targeted one. Record which prompt found what. Don't hint at the answer in the first prompt.
3. **Nothing goes in unverified.** Every accepted output is checked by running tests, CI, or against the NZBS source. Write down how.
4. **Record what you changed.** For code, the difference between Copilot's version and the committed version is the evidence. Screenshot both or save Copilot's raw version to the session folder before you edit it.
5. **You commit, Shailesh does the rest.** Shailesh prepares each branch, opens the PRs and merges them. You commit your own work and push it to the branch named in the session, so it counts under your name. Put `(Copilot-assisted)` at the end of the commit subject for any commit containing Copilot output.
6. **Team analysis first where it matters.** For the RCA (C08), get the team's 5 Whys from Shailesh before asking Copilot, so the comparison is fair.

---

## 3. One-time setup (about 15 minutes)

1. In VS Code, sign in to GitHub Copilot and open Copilot Chat.
2. Note your Copilot plan (Free, Pro, Student, etc.) and which **model** is selected in the chat model picker. Record it in every session, because answers differ by model. If the picker is set to a non-OpenAI model (for example a Claude model inside Copilot), that is still Copilot, but say so in the log.
3. Note which chat **mode** you use for each prompt (Ask, Edit or Agent). Keep the mode visible in screenshots.
4. Pull `main` and check the suite passes before you start:
   ```
   git switch main && git pull
   dotnet test
   ```
   Record the test count (70 on 13 October). You need it as the baseline for C06.
5. Create the evidence folders:
   ```
   docs/report/evidence/copilot/C01-security-review/
   docs/report/evidence/copilot/C02-booking-eligibility-tests/
   ... one folder per session, named as in section 5
   ```
6. Learn how to export a chat. In VS Code, open the Command Palette and run **Chat: Export Chat...** to save the session as JSON. If your version doesn't have it, right-click in the chat panel, choose **Copy All**, and paste it into `chat.md` in the session folder. Screenshots show the reader what happened; the export is the full record if the marker asks.

---

## 4. The routine for every session

Do these steps every time. Use the checklist in section 7 as the session's `notes.md`.

1. **Before:** create the session folder. Write the start time and the git commit hash (`git rev-parse --short HEAD`).
2. **Prompt:** type the prompt from section 5. Screenshot the prompt with the start of the answer, with the model and mode visible.
3. **Response:** screenshot the full response. Scroll and take several shots if it is long. Number them: `01-prompt.png`, `02-response.png`, `03-response-cont.png`.
4. **Export** the chat to the session folder.
5. **Review:** go through the output line by line against the "Check" list for the session. Write down each problem you find.
6. **Decide:** accepted as-is, modified, or rejected, with the reason. For modified code, save Copilot's original first (`copilot-original.cs` in the session folder), then edit.
7. **Verify:** run the tests, push for CI, or compare with the source. Screenshot the result (`05-verify.png`).
8. **Measure:** fill in the session's Measure line.
9. **Log:** add the row to `docs/report/ai-usage-log.md` and finish `notes.md`. Time taken to log: 5 minutes. Do it straight away, not at the end of the day.

File names inside each folder: `NN-what-it-shows.png`, for example `C01-security-review/04-targeted-prompt.png`. The folder name gives the session, so you don't need the date in every file name; put the date in `notes.md`.

---

## 5. The sessions

**Must** sessions cover every category at least once, plus the evidence other tasks rely on. **Should** sessions make the reflection noticeably stronger. Do them in this order, because some depend on earlier ones.

| ID | Category | Task | Priority | When |
|---|---|---|---|---|
| C01 | QA (security review) | Can Copilot find the security defects we already know about? | Must | Tue 13 Oct |
| C09 | QA (domain rules) | Does Copilot know the NZBS age rules? | Must | Tue 13 Oct |
| C02 | Testing | Test-first tests for open defect #1 | Must | Tue 13 Oct |
| C03 | Testing (test design) | Negative and invalid-input cases for the booking form | Should | Tue 13 Oct |
| C04 | Debugging | Diagnose the tests that break after the #1 fix | Must | Wed 14 Oct |
| C05 | Implementation | NFR2 load test | Must | Wed 14 Oct |
| C06 | Refactoring | Shared integration test helper | Must | Wed 14 Oct |
| C07 | QA (CI) | Coverage and smoke steps in CI | Must | Wed 14 Oct |
| C08 | QA (RCA) | 5 Whys for defect #2, compared with ours | Must | Thu 15 Oct, early |
| C10 | QA (code review) | Copilot as a PR reviewer | Should, if your plan allows | Any PR |

---

### C01: Security review of the old donor controller (QA)

**Why it matters.** At mid-project we wrote that Copilot "answers the question asked, not the one that should have been asked". This session measures that claim. The commit `d26d2ce` is from just before the security fixes, so we know exactly which defects are in the file.

**Setup.** Use a separate worktree so your own branch isn't touched:
```
git worktree add ../bd-before-fixes d26d2ce
code ../bd-before-fixes
```
Open `BloodDonation.Web/Controllers/DonorController.cs`. Nothing from this worktree is ever committed. Remove it afterwards with `git worktree remove ../bd-before-fixes`.

**Prompts.** Use a new chat for each. Use Ask mode with the file attached.
1. Neutral: `Review this controller and list any problems you find.`
2. Security: `Review DonorController for security problems.`
3. Targeted: `Can a signed-in donor cancel or reschedule another donor's booking? Is this controller protected against cross-site request forgery?`

**Check: the known defects in this file at `d26d2ce`.**

| # | Known defect | Issue | Fixed in |
|---|---|---|---|
| K1 | Cancel has no ownership check | #3 | `a0c273d` (PR #16) |
| K2 | Reschedule has no ownership check | #3 | `a0c273d` (PR #16) |
| K3 | Dashboard shows other donors' bookings | #3 | `a0c273d` (PR #16) |
| K4 | POST actions have no anti-forgery check | #22 | `3ecf9d9` (PR #23) |
| K5 | Book saves a booking without any eligibility check | #1 | still open |

**Measure.** Fill in a table with columns K1–K5 and rows Prompt 1, Prompt 2, Prompt 3, marking Found or Missed. Then count:
- false positives: things it flagged that aren't real problems;
- valid new findings: real problems not in our list. Open an issue for each one, because it's a real defect found by Copilot.

**Evidence.** Screenshots of all three prompts and answers, the chat exports, and the scoring table in `notes.md`.

---

### C09: NZBS age rules (QA, domain knowledge)

**Why it matters.** Our most important defect (#2) is an age rule that Copilot got wrong at generation time. This tests whether Copilot knows the rule, and whether it will agree with wrong code it's shown.

**Prompts.**
1. In a **new chat with no files attached**: `What are the New Zealand Blood Service age eligibility rules for first-time donors, existing donors and lapsed donors?`
2. Then open `BloodDonation.Domain/EligibilityService.cs` and `EligibilityRules.cs`, attach them, and ask: `Do IsAgeEligible and these constants correctly implement the current NZBS age rules?`

**Check** against the NZBS source: https://www.nzblood.co.nz/become-a-donor/am-i-eligible/recent-changes-to-donor-criteria. Screenshot the page with the date visible.
- Existing donors are eligible until their 75th birthday.
- From 75 up to the 81st birthday, they need NZBS Medical Officer authorisation.
- Lapsed donors (no donation in 2 years) past their 71st birthday need authorisation.
- For first-time donors, NZBS sources give both "16–71" and "before their 71st birthday".

The current code accepts returning donors until their 81st birthday with **no** authorisation, which is wrong. Did Copilot say so in prompt 2, or did it confirm the code?

**Measure.** For each of the 4 rules: correct, wrong, or not mentioned. Separately, record whether prompt 2 spotted the wrong 81 limit (yes or no).

**Evidence.** Screenshots of both answers, and a screenshot of the NZBS page.

---

### C02: Test-first tests for defect #1 (Testing)

**Why it matters.** #1 is our only open critical defect. The navbar booking page (`/Donor/Book` in `BloodDonation.Web/Controllers/DonorController.cs`) creates a booking without checking eligibility. This is the test that proves it, written before the fix. It also gives Task 5 its "red CI run on a test-first commit".

**Setup.** Shailesh has pushed the branch:
```
git fetch
git switch fix/navbar-booking-eligibility
```
Create `BloodDonation.Tests/Integration/BookingEligibilityTests.cs`. Open `BookingOwnershipTests.cs` beside it so Copilot can see the pattern.

**Prompt** (Edit or Agent mode, both files attached):
> Using the same pattern as BookingOwnershipTests (WebApplicationFactory, [DoNotParallelize], RegisteredDonorAsync, PostWithTokenAsync, DashboardBookingIdAsync), write MSTest integration tests for POST /Donor/Book with session S3. Add these form fields: DateOfBirth, WeightKg, LastDonationDate, HasStandDownEvent, StandDownReason, StandDownEventDate, StandDownDurationDays. Tests: last donation 83 days ago → no booking on the dashboard; 84 days ago → booking created; weight 49.9 kg → no booking; age 15 → no booking; an active stand-down → no booking. Use EligibilityRules constants instead of literal numbers.

**Check.**
- Dates are relative to today, for example `DateTime.Today.AddDays(-(EligibilityRules.MinimumDonationIntervalDays - 1))`. Hard-coded dates are time bombs.
- It reuses the existing helpers instead of inventing new URLs or helpers.
- A rejection is asserted by "no booking on the dashboard", not only by a status code.
- Boundaries are exactly right: 83 days rejected, 84 days accepted, 49.9 kg rejected.
- Test names say what they check and include the TC ID where one exists.

**Verify.** Run `dotnet test --filter BookingEligibilityTests`. On this branch, the 83-day, 49.9 kg, age-15 and stand-down tests should **fail** and the 84-day test should pass. If the rejection tests pass, they aren't testing anything. Commit and push:
```
test: add navbar booking eligibility tests (fails until #1 is fixed) (Copilot-assisted)
```
Screenshot the red CI run on GitHub Actions. Then tell Shailesh: he adds the fix commit to this branch.

**Measure.** Tests generated, tests kept unchanged, tests modified, tests rejected. List each mistake found, such as a wrong boundary, a hard-coded date or an invented helper.

---

### C03: Negative and invalid-input test design (Testing)

**Why it matters.** It shows AI-assisted test design with explicit accept and reject decisions. Accepted cases become extra test cases for Task 3.

**Prompt** (attach `BookingViewModel.cs`, `BookingFormModel.cs` and `EligibilityRules.cs`):
> Suggest negative, boundary and invalid-input test cases for the donor booking form. For each, give the input and the expected result.

**Check.** Compare each suggestion with TC01–TC26 in the mid-project report. Put every suggestion in one of four groups:
- already covered by an existing TC (name which);
- new and valid: we add it;
- new but not relevant to our requirements;
- wrong: for example a wrong threshold, or a rule NZBS doesn't have.

**Measure.** The count in each group. Note any rule Copilot invented.

**Evidence.** Screenshots, plus the grouping table in `notes.md`.

---

### C04: Diagnose the failing tests after the #1 fix (Debugging)

**Why it matters.** It's a real failure that has to be fixed anyway. When Shailesh's fix makes the navbar booking form require date of birth, weight and so on, four older test files that post to `/Donor/Book` without those fields will start failing: `BookingOwnershipTests`, `StaffOutcomeTests`, `AntiforgeryTests` and `ClientValidationTests`. Shailesh will leave those tests for you to fix.

**Steps.**
1. Pull the branch after Shailesh's fix commit. Run `dotnet test` and screenshot the failures.
2. **Before reading the code yourself**, paste the failing test output into Copilot Chat. Prompt: `These tests started failing after a change to DonorController.Book. Explain why and suggest a fix.`
3. Screenshot its diagnosis.
4. Work out the real cause yourself. Write down whether Copilot was right, partly right or wrong.
5. Fix the tests. Use Copilot's suggestion only if it's correct. A common wrong fix is weakening or deleting the assertion so the test passes, so watch for it.

**Measure.** Diagnosis correct (yes, partly or no); suggested fix correct (yes, partly or no); minutes to a working fix.

If a different test fails during the formal test cycle, repeat this session on that failure as well. More than one debugging example makes the reflection stronger.

---

### C05: NFR2 load test (Implementation)

**Why it matters.** NFR2 requires an eligibility check within 2 seconds with 20 concurrent users. There's no test for it yet. This doubles as the Task 7 performance evidence.

Do this **after** #1 is fixed, so the navbar booking page runs eligibility.

**Prompt** (attach `BookingOwnershipTests.cs` for the pattern):
> Write an MSTest test in BloodDonation.Tests/Performance/EligibilityLoadTests.cs that measures NFR2: an eligibility check must complete within 2 seconds with 20 concurrent users. Use WebApplicationFactory. Register 20 donors first, outside the timing. Then send 20 concurrent POST requests to /Donor/Book, repeated for 10 rounds, using a donor who fails the 84-day interval rule. Record each response time and report the average, p95 and max with TestContext.WriteLine. Fail the test if p95 is over 2 seconds. Tag it [TestCategory("Performance")] and [DoNotParallelize].

**Check.** These are the mistakes to look for:
- **Concurrency:** are the 20 requests really concurrent (`Task.WhenAll`), or awaited one after another in a loop?
- **Warm-up:** is there a warm-up request before timing starts? The first request includes app start-up.
- **Timing:** is each request timed individually with a `Stopwatch`, or is only the whole batch timed?
- **p95:** is it calculated from sorted times? With 200 samples, p95 is about the 190th value.
- **Token:** does each request carry a valid anti-forgery token, so it isn't rejected with 400 and timed as "fast"? Assert the response is the Ineligible page.
- **No writes:** an ineligible donor means no booking is written. That keeps it away from defect #17, the store that isn't thread-safe.

**Limitations to note for the report.** This runs in-process with `TestServer`, so there is no network or Kestrel involved, and it uses one machine. Record the CPU and machine you ran it on.

**Verify.** Run it locally three times and record the numbers each time. Then confirm it runs in CI.

**Measure.** The number of problems found in Copilot's version, from the Check list. The average, p95 and max from each run.

---

### C06: Shared integration test helper (Refactoring)

**Why it matters.** `PostWithTokenAsync`, `RegisteredDonorAsync` and similar helpers are copied into five files under `BloodDonation.Tests/Integration/`. Moving them into one shared helper is a maintainability improvement for Task 4.

Do this **after** C02 and C04 are merged, to avoid conflicts.

**Before you prompt.** Record the baseline:
```
dotnet test            # record total and passed
wc -l BloodDonation.Tests/Integration/*.cs
```
Compare the helper copies yourself. Some differ slightly, for example signing in as staff instead of registering a donor.

**Prompt** (Agent or Edit mode, all five files attached):
> The helpers PostWithTokenAsync, RegisteredDonorAsync, RedirectPath and DashboardBookingIdAsync are duplicated across these integration test classes. Extract them into one static helper class in BloodDonation.Tests/Integration/TestHelpers.cs and update every class to use it. Do not change any test's behaviour.

**Check.**
- Did it merge two helpers that are actually different into one, and change a test's behaviour?
- Did it delete or rename any test? The test count must not change.
- Did it keep `[DoNotParallelize]`?

**Verify.** `dotnet test` gives the same total, all passing, and `wc -l` shows fewer lines. Screenshot both.

**Measure.** The test count before and after (must match), the line count before and after, and the number of corrections you had to make.

---

### C07: Coverage and smoke steps in CI (QA, CI)

**Why it matters.** Task 5 needs a coverage report and a smoke step as quality gates. CI is `.github/workflows/build.yml`. It currently only restores, builds and tests.

**Prompt** (attach `build.yml` and `BloodDonation.Tests.csproj`):
> Add two steps to this GitHub Actions workflow. 1) Collect line and branch code coverage during dotnet test, produce a readable report, and upload it as a build artifact. 2) A smoke test step: start BloodDonation.Web after the build, wait until it responds, and fail the job if the home page does not return HTTP 200. The tests use MSTest 4.

**Check.**
- **Action versions:** do they exist? Look each one up on the GitHub Marketplace.
- **Coverage tooling:** does it suit our project? The MSTest 4 package already includes Microsoft code coverage. If Copilot adds `coverlet.collector` instead, that also works, but note the choice.
- **Paths:** are the report paths and artifact name correct?
- **Smoke start-up:** does the smoke step wait for start-up properly (retry) instead of a fixed `sleep`?
- **Smoke failure:** does a non-200 really fail the step (`curl -f` or similar)?

**Verify.** Push and screenshot the Actions run, with both new steps green and the coverage artifact listed. Download the artifact and record the line and branch coverage percentages.

**Measure.** The number of errors found before the workflow first passed, and the number of CI runs it took to get green. Screenshot every failed attempt too.

---

### C08: 5 Whys for defect #2 (QA, root cause analysis)

**Why it matters.** Task 6 needs three RCAs, and #2 is the strongest. Comparing our analysis with Copilot's shows where AI analysis stays shallow.

**Steps.**
1. **First**, get the team's 5 Whys for #2 from Shailesh, who writes the RCAs. The returning-donor age rule was wrong when Copilot generated it, then "fixed" to the wrong limit (81 with no authorisation), and the tests were green both times. Save his version to `notes.md` with the time you received it.
2. Then open a new Copilot chat. Paste the text of issue #2 and the history above. Prompt: `Do a 5 Whys root cause analysis of this defect. Give a root cause category and corrective and preventive actions.`
3. Compare the two side by side in a table.

**Check.**
- Did Copilot stop at "the code was wrong"?
- Did it reach the real causes: tests derived from the implementation, so they inherited its assumption; no step checking thresholds against the NZBS source; AI-generated code accepted without checking the domain rule?
- Is any preventive action it suggests something we would actually adopt?

**Measure.** How many Why levels were relevant before it went generic; whether its root-cause category matched ours; the number of its preventive actions we adopted.

---

### C10: Copilot as a PR reviewer (QA, code review, optional)

Only if your Copilot plan includes code review. On one real PR (C05 or C07 suits), add **Copilot** as a reviewer. Compare its comments with the human review on the same PR: what Copilot caught that the human review didn't, what the human review caught that Copilot didn't, and Copilot comments that were wrong. Screenshot both reviews.

---

## 6. The usage log

Every session gets one row in `docs/report/ai-usage-log.md`. The columns are fixed by the project criteria. Keep each cell short, because detail goes in `notes.md`. The **Screenshot ref** cell is the session folder path.

Example of a filled row (illustrative only; replace with what really happens):

| ID | Date | Member | Category | Task | Prompt (summary) | Output (summary) | Decision | How verified | Issues found | Screenshot ref |
|---|---|---|---|---|---|---|---|---|---|---|
| C02 | 13 Oct | Natalie | Testing | Test-first tests for #1 | Integration tests for /Donor/Book eligibility using existing helpers | 5 tests | Modified | Red CI run #nn; 4 failing, 1 passing as expected | Hard-coded dates replaced with relative ones | `evidence/copilot/C02-booking-eligibility-tests/` |

---

## 7. `notes.md` template for each session

Copy this into each session folder.

```markdown
# Cnn: <title>

- Date and time:
- Member: Natalie
- Copilot plan / model / mode:
- Repo commit at start:
- Files attached to the chat:

## Prompts (in order, numbered to match screenshots)
1.
2.

## What Copilot produced
(short summary; full text is in the chat export)

## Review: problems found
| # | Problem | Severity (would it have caused a defect?) | What I did |
|---|---|---|---|

## Decision
Accepted / Modified / Rejected, and why.

## Verification
How it was checked, with the result (test run, CI run number, source compared).

## Measure
(the numbers asked for in the guide for this session)

## Commit / PR
Commit hash, PR number, CI run number.

## Reflection note (2–3 sentences, while it's fresh)
What this session tells us about when Copilot helps and when it is risky.
```

---

## 8. Reflection notes

The report needs reflection on five areas, from both of us. After each session, add a line under the area it relates to. Then, on 16 October, write your reflection in your own words from these notes and from the mid-project examples (M1–M5 in the log), which we did as a pair. Shailesh writes his own.

| Area | Question to answer from your sessions |
|---|---|
| Productivity | Where did it save real time, and where did checking it take longer than doing it yourself? Use the minutes you recorded. |
| Learning | What did reviewing its output teach you about the code, the rules or testing? |
| Testing | Did its tests check the requirement, or only repeat what the code does (C02, C03)? |
| Quality | What defects or risks did it introduce, and what did it miss (C01, C09)? |
| Professional practice | What rules would you follow with AI output on a real team, given these results? |

---

## 9. How this appears in the report

| Report section | Built from |
|---|---|
| 9.1 How Copilot was used | One line per session, grouped by the five categories, plus the mid-project examples |
| 9.2 Representative examples | C01, C02 and C05 in full, with screenshots as figures |
| 9.3 Review and verification | Section 4 routine, with the "How verified" column of the log |
| 9.4 Quality risks found | Problems from every `notes.md`: invented rules (C09, #2), wrong boundaries, invented names (#13), silence on security (C01), tests that mirror the code |
| 9.5 Measured outcomes | C01 detection table, C03 grouping counts, C09 rule accuracy, C04 diagnosis result, C06 before/after counts |
| 9.6 Reflection | Section 8, both members |
| Task 3 | C03 accepted test cases |
| Task 5 | C02 red run, C07 coverage and smoke steps |
| Task 6 | C08 comparison inside the #2 RCA |
| Task 7 | C05 load test results |

---

## 10. Getting the evidence into the repo

Shailesh handles branches, PRs and merging. Your part:

- **Code sessions (C02, C04–C07):** commit and push to the branch Shailesh names for that session.
- **Evidence and log rows:** commit the session folders and your log rows to `docs/copilot-evidence` and push, at least once a day. Shailesh merges it.

Always `git pull` before you start a session, because Shailesh may have merged or pushed since.
