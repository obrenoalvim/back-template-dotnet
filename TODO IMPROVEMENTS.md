### Bump Testcontainers.PostgreSql to drop vulnerable transitive SSH.NET
- **Category:** Dependency
- **What:** `Testcontainers.PostgreSql` 4.1.0 pulls in `SSH.NET` 2024.1.0, which has a known high-severity advisory (GHSA-q939-rpr3-3284). Test-only, never in the published image, so no production risk — but a newer `Testcontainers.PostgreSql` may resolve a patched `SSH.NET` and clear the `NU1903` build warning.
- **Where:** `tests/BackTemplate.Tests/BackTemplate.Tests.csproj`
- **Why:** Removes noise from `dotnet build`/`dotnet test` output and closes the advisory, even though current exposure is zero.
- **Risk:** Low-medium — a Testcontainers major/minor bump can change container-startup behavior; needs a test run to confirm before merging.
- **Effort:** Low
