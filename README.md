# NZBS Blood Donation Booking System

A web application for booking blood donation appointments. It checks each booking against the New Zealand Blood Service eligibility rules (donation interval, age, weight and stand-down periods), lets clinic staff record donation outcomes, and gives coordinators booking, deferral and stock figures.

ENSE707 final project by Shailesh Mamidi and Natalie Kanyuchi.

## Requirements

- .NET 10 SDK

## Run the app

```
dotnet run --project BloodDonation.Web
```

Then open http://localhost:5157.

The app runs in the Development environment and creates one test account per role at start-up:

| Role | Email | Password | Sign in from |
|---|---|---|---|
| Donor | donor@test.local | Donor#Test1 | Donor Login |
| Staff | staff@test.local | Staff#Test1 | Staff Login |
| Coordinator | coordinator@test.local | Coord#Test1 | Staff Login |

New donors can also register from the home page. All data is held in memory, so it resets when the app restarts.

## Run the tests

```
dotnet test
```

This runs 95 tests: unit tests for the eligibility rules and analytics, integration tests that send real HTTP requests through the web app, and a performance test of the eligibility check under 20 concurrent users.

## Project structure

| Project | Contents |
|---|---|
| `BloodDonation.Domain` | Eligibility rules (`EligibilityService`, `EligibilityRules`) and analytics. It has no dependencies on the web layer |
| `BloodDonation.Web` | ASP.NET Core MVC app with ASP.NET Core Identity and three roles: donor, staff and coordinator |
| `BloodDonation.Tests` | MSTest unit, integration and performance tests |

GitHub Actions (`.github/workflows/build.yml`) builds the solution with warnings treated as errors, runs the tests with coverage, and checks that the app starts, on every push and pull request.
