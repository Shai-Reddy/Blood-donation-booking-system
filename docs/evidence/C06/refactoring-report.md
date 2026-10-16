# C06 Integration-Test Helper Refactoring

## Objective

I completed a small refactoring of duplicated integration-test setup in the Blood Donation Appointment System. The aim was to make common HTTP test operations easier to maintain while preserving each test's existing behaviour, assertions, authentication flow and anti-forgery protection.

## Original duplication

In [BookingEligibilityTests.cs](../../../BloodDonation.Tests/Integration/BookingEligibilityTests.cs) and [BookingOwnershipTests.cs](../../../BloodDonation.Tests/Integration/BookingOwnershipTests.cs), I found the same helper patterns:

- Both classes created `HttpClient` instances with `AllowAutoRedirect = false`.
- Both implemented a `PostWithTokenAsync` helper that fetched a form page, extracted its anti-forgery token, added it to the supplied fields and posted the form.
- Both registered a unique donor through the real registration page, using that token-bearing POST flow.

These duplicated helpers made small changes to HTTP form handling or anti-forgery extraction easy to apply inconsistently. The donor registration methods were not merged: each class keeps its own password, test data and success assertion.

## Shared helpers

I added [IntegrationTestHelpers.cs](../../../BloodDonation.Tests/Integration/IntegrationTestHelpers.cs), a test-only helper with three methods:

- `CreateClient` creates a client using `WebApplicationFactoryClientOptions` with automatic redirects disabled.
- `ExtractAntiForgeryToken` extracts the token from the form HTML.
- `PostWithTokenAsync` loads the specified form page, extracts its token, adds it to the supplied fields and posts to the requested URL.

The eligibility and ownership tests now call these shared methods. They retain their own donor-registration and booking setup, including their original routes, credentials, form values, and assertions.

## Files changed

The C06 implementation changed exactly these three files:

1. [IntegrationTestHelpers.cs](../../../BloodDonation.Tests/Integration/IntegrationTestHelpers.cs) — added the shared test helpers.
2. [BookingEligibilityTests.cs](../../../BloodDonation.Tests/Integration/BookingEligibilityTests.cs) — replaced the class-local client and token-post implementation with calls to the shared helpers.
3. [BookingOwnershipTests.cs](../../../BloodDonation.Tests/Integration/BookingOwnershipTests.cs) — made the equivalent replacements.

No production code or eligibility rules changed.

## Preserving behaviour and isolation

I kept the existing assertions in both classes, including registration redirects, booking redirects, donor ownership checks and dashboard booking checks. Registration still uses each class's original data and the real registration form, so the returned clients retain the same authentication cookies. Routes and form fields remain unchanged.

Both classes still obtain a token from the relevant form and include it in ordinary POSTs. The intentional tokenless requests in `AntiforgeryTests` were not routed through the helper or changed. The helper refactoring did not consolidate `WebApplicationFactory` lifetimes: each test class retains its own factory and lifecycle. Existing MSTest parallelisation attributes are also unchanged, preserving the tests' existing isolation around shared in-memory state.

## GitHub Copilot assistance and review

I used GitHub Copilot to identify duplicated client creation and anti-forgery form submission in the integration tests, propose a narrowly scoped extraction, and apply the shared-helper refactoring. I reviewed the changes against the original methods to check that client options, token-bearing form submission, registration values, routes and assertions remained intact. I also checked that the intentionally tokenless antiforgery tests and production files were untouched.

## Build and test results

The test project build completed successfully with **zero warnings and zero errors**:

```text
dotnet build .\BloodDonation.Tests\BloodDonation.Tests.csproj --no-restore
Build succeeded.
0 Warning(s)
0 Error(s)
```

The affected integration tests passed **9 of 9**, with no failures. The full regression suite passed **79 of 79**, with no failures. These results are recorded in the evidence logs:

- [Affected integration-test results](./affected-tests.txt)
- [Full regression-suite results](./full-suite.txt)

## Limitations

This change only removes duplication in two integration-test classes. It does not standardise every test class or unify registration, login, booking, or booking-ID helpers that have different routes, data or expected outcomes. The tests still use an in-memory application host and shared in-memory application state; the refactoring does not make that state thread-safe or establish production behaviour.

## Conclusion

The C06 refactoring centralises three identical HTTP test operations while leaving test-specific setup and outcomes in their original classes. The build and recorded targeted and full-suite test results provide evidence that the limited change compiled and preserved the tested behaviour. No production code was changed.
