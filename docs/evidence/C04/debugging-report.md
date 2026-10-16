# C04 — GitHub Copilot-Assisted Debugging Report

## 1. Objective

Investigate and resolve failing integration tests in the Blood Donation Appointment System using GitHub Copilot as a debugging assistant.

The objective was to identify the root causes of the failures, apply minimal corrective changes, and verify the results without weakening application validation, security or donor eligibility requirements.

## 2. Initial Test Results

The initial full test run produced:

- Total tests: 77
- Passed: 67
- Failed: 10

The failures occurred across four integration test classes:

| Test class | Failures |
|---|---:|
| AntiforgeryTests | 3 |
| BookingOwnershipTests | 4 |
| StaffOutcomeTests | 2 |
| BookingEligibilityTests | 1 |

The original output was preserved in `C04-failures.txt`.

## 3. Copilot-Assisted Investigation

### Issue 1 — Missing Booking Form Fields

GitHub Copilot was used to compare failing booking test helpers with the corrected antiforgery test.

The investigation identified missing `DateOfBirth` and `WeightKg` fields in prerequisite booking requests.

These incomplete requests returned HTTP 200 OK instead of the expected redirect, preventing subsequent test assertions from being reached.

**Corrective action:**

Added valid `DateOfBirth` and `WeightKg` values to the booking requests in:

- `AntiforgeryTests.cs`
- `BookingOwnershipTests.cs`
- `StaffOutcomeTests.cs`

No production-code changes were required.

**Verification:**

- Antiforgery tests: 3 passed.
- Booking ownership and staff outcome tests: 6 passed.

### Issue 2 — Incorrect Donation Interval Test Data

The remaining failing test was:

`DonorWithLastDonationBeforeMinimumInterval_CannotBook`

The test originally calculated the previous donation date relative to `DateTime.Today`.

Copilot traced the eligibility evaluation through the booking controller, eligibility check and domain service.

The application correctly evaluates the 84-day donation interval relative to the scheduled donation session date.

The seeded S3 session was scheduled nine days after the current date. Consequently, the test's supposedly ineligible donor had a 92-day interval at the scheduled appointment.

This was a test-data defect rather than an application eligibility defect.

**Corrective action:**

Updated both interval boundary tests to calculate `LastDonationDate` relative to the actual S3 session date retrieved from `InMemoryStore.Sessions`.

The tests now verify:

- 83-day interval: booking rejected.
- 84-day interval: booking accepted.

The 84-day eligibility rule remained unchanged.

## 4. Final Verification

After applying the corrections, the complete test suite was executed successfully.

| Metric | Initial run | Final run |
|---|---:|---:|
| Total tests | 77 | 77 |
| Passed | 67 | 77 |
| Failed | 10 | 0 |
| Pass rate | 87.0% | 100% |

The final successful test run completed in approximately 38.07 seconds.

The output was preserved in `C04-success.txt`.

## 5. GitHub Copilot Contribution

GitHub Copilot assisted with:

- Comparing failing and passing integration test helpers.
- Identifying incomplete booking form submissions.
- Tracing the eligibility calculation through the application.
- Distinguishing test-data defects from production defects.
- Proposing and applying minimal test corrections.
- Running targeted tests to verify the changes.

The developer reviewed the changes, independently reran the targeted tests and verified the complete test suite.

## 6. Conclusion

All ten original integration test failures were resolved through corrections to four test files. The investigation demonstrated the importance of checking test assumptions against application behaviour, particularly when validating date-sensitive eligibility rules. The final suite passed all 77 tests without modifying production code or weakening the existing 84-day minimum donation interval requirement.
