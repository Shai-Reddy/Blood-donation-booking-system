# C05 – Eligibility Performance Testing Report

## 1. Objective

The purpose of C05 was to test how well the Blood Donation Appointment System handles multiple booking requests at the same time, particularly when donors do not meet the eligibility requirements.

For this test, I focused on the 84-day minimum interval between whole-blood donations. I wanted to check that the system would correctly reject donors who had donated too recently, even when several people were trying to book at once.

I also wanted to measure how quickly the system responded to these requests and check whether the results were within the performance limit being used for testing.

## 2. Test Environment and Method

The performance test was written in C# using .NET 10, MSTest and ASP.NET Core's `WebApplicationFactory`. This allowed me to test the application's booking functionality without needing to run it on a live server.

The test used an in-memory environment, so the results reflect how the application performed during testing rather than how it would necessarily perform in production.

For the main performance test, I used:

- 20 registered donors
- 10 rounds of booking requests
- 20 concurrent requests per round
- 200 booking requests in total
- A last donation date set 60 days before the scheduled session

Since the system requires donors to wait at least 84 days between donations, all 200 requests should be rejected.

Each booking request included the required information, such as the donor's date of birth, weight, session details and anti-forgery token. This was important because I wanted the requests to reach the eligibility checks rather than fail because of missing form information.

To simulate concurrent requests, the test prepared 20 booking requests and used a shared start signal to release them together. Once the requests had completed, the test checked that each donor received the correct eligibility rejection message and that no bookings had been created.

I also measured the response time for each booking POST request. The measurements did not include donor registration, preparing the requests or checking the rejection messages afterwards.

The test calculated three main performance measurements:

- **Average response time:** How long the booking requests took on average.
- **P95 response time:** The response time at or below which 95% of the measured requests completed.
- **Maximum response time:** The longest response time recorded.

I also included a smaller smoke test using one donor. This helped verify that the eligibility rejection worked correctly before running the full performance test.

## 3. How I Used GitHub Copilot

I used GitHub Copilot to help investigate problems with the original performance test, identify why it was not producing a successful result and make the necessary corrections.

The first version of the test had several issues.

**Missing booking information**

The original booking requests did not include `DateOfBirth` and `WeightKg`, even though these fields were required.

This meant the system could reject the requests because the form was incomplete, rather than because the donors had not waited 84 days.

I used Copilot to help identify this issue and update the test so that all the required fields were included.

**Incorrect donation date calculation**

The original test calculated the last donation date using the current date.

However, eligibility is checked against the date of the scheduled donation session. This meant the test was not checking the exact 60-day interval that I intended.

The calculation was changed to:

`session.SessionDate.AddDays(-60)`

This ensured that each donor's previous donation was exactly 60 days before their scheduled appointment.

**Inconclusive test result**

The original test also contained `Assert.Inconclusive()`, which prevented it from producing a successful result.

With Copilot's assistance, I replaced this with proper assertions that checked whether the requests were rejected for the correct reason.

The updated test now checks that each request redirects back to the booking page, displays the 84-day interval error message and does not create a booking.

**Incorrect assertion order**

After making these changes, I ran the performance test again. Although the response times were below the configured limit, the test still failed.

The problem was with the order of the arguments in MSTest's `Assert.IsLessThanOrEqualTo()` method.

The performance limit and measured P95 had been passed in the wrong order, causing the assertion to fail even though the measured response time was acceptable.

I corrected the argument order and reran the test.

Throughout this process, I reviewed the changes suggested by Copilot and checked them against the existing application code and test results. I also made sure that the changes were limited to the performance test rather than modifying the application's production functionality.

## 4. Initial Test Results and Debugging

When I first ran the performance test, it was reported as skipped because the test ended with an inconclusive assertion.

This meant that although the test had executed, it had not actually confirmed whether the eligibility checks were working as intended.

After correcting the missing fields, donation date calculation and rejection checks, I ran the test again.

The test completed all 200 requests and recorded a P95 response time of **58.81 ms**. However, it failed because the performance assertion had its arguments in the wrong order.

This was an issue with the test code itself, not evidence that the application had failed its performance requirement.

Once I corrected the assertion, the test passed successfully.

## 5. Final Performance Results

After fixing the issues, I ran the full performance test again using 20 concurrent donors across 10 rounds.

The results were:

| Measurement | Result |
|---|---:|
| Concurrent donors | 20 |
| Number of rounds | 10 |
| Total booking requests | 200 |
| Average response time | 10.96 ms |
| P95 response time | 27.56 ms |
| Maximum response time | 32.14 ms |
| Overall test execution | 16.19 seconds |
| Test result | **PASSED** |

The P95 response time was **27.56 ms**, which was well below the 2,000 ms limit currently configured in the test.

However, the 2,000 ms threshold is still provisional because the official NFR2 performance requirement has not yet been confirmed.

The results show that the system was able to process the tested eligibility rejection requests quickly in the in-memory test environment.

The test also confirmed that the requests were rejected for the correct eligibility reason and that no bookings were created for the ineligible donors.

## 6. Regression Testing

After the performance test passed, I ran the full test suite to make sure that the changes had not affected any existing functionality.

The results were:

| Result | Total |
|---|---:|
| Tests passed | 79 |
| Tests failed | 0 |
| Overall result | **PASSED** |

All 79 tests passed successfully.

This gave me confidence that the updated performance test worked alongside the existing tests without introducing any detected regressions.

## 7. Limitations

Although the performance results were positive, there are some limitations to consider.

Firstly, the tests were run using an in-memory application environment rather than a deployed system. Performance could be different when using a production database, server or network.

Secondly, the test focused on donors who were not eligible to book because they had donated too recently. It therefore measured how quickly the system handled eligibility rejections, rather than how quickly it could process successful bookings under the same level of concurrency.

The test also used a fixed number of donors and requests, so it does not demonstrate how the system would perform under much larger workloads.

Finally, the 2,000 ms P95 limit still needs to be checked against the official NFR2 requirement.

These limitations mean that the results are useful for evaluating the current implementation, but they should not be treated as proof that the system is ready for production-scale usage.

## 8. Conclusion

Overall, C05 helped me test both the performance and correctness of the booking eligibility functionality under concurrent requests.

Using GitHub Copilot, I was able to identify and fix problems in the original test, improve the assertions and add more useful performance measurements.

The final test successfully completed 200 booking requests, with an average response time of **10.96 ms** and a P95 response time of **27.56 ms**.

The system correctly rejected donors who had not met the 84-day minimum donation interval, and no bookings were created for those requests.

I also ran the full regression suite, where all **79 tests passed**.

Based on these results, the eligibility rejection functionality performed well under the workload tested. However, further testing would be needed to evaluate successful concurrent bookings and performance in a production environment.

## 9. Evidence References

- [Original performance failure and debugging results](./performance-original-failure.txt)
- [Successful performance test results](./performance-success.txt)
- [Full regression test results](./full-suite-success.txt)
- [Eligibility performance test implementation](../../../BloodDonation.Tests/Performance/EligibilityLoadTests.cs)