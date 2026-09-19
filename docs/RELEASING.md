# Maintaining and releasing FluentSharp

FluentSharp is maintained by Matías Iacono. Changes are committed directly to `master`; external pull requests are not accepted.

## Continuous integration

Every push to `master` runs `.github/workflows/ci.yml` on Windows and Linux with the .NET 10 SDK. It restores dependencies, builds the solution in Release mode, runs tests with coverage, creates NuGet and symbol packages, verifies the CLR API baseline, and builds/runs the consumer examples against the packed package with nullable warnings treated as errors.

Each run uploads `test-results-<os>` and `packages-<os>` artifacts, retained for 14 days. Test results are uploaded even when tests fail. Open the repository's **Actions → CI** page to inspect or download them. CI can also be started manually with **Run workflow**.

Version 2 targets .NET 10 and C# 14. Version 1.0.7 is the final .NET Standard 2.0 legacy release. The current checkout is prepared for the stable `2.0.0` release; a local package build does not publish it.

## One-time NuGet setup

The release workflow uses [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing) to obtain short-lived publishing credentials. Follow the official setup instructions and configure a policy matching `.github/workflows/release.yml`. No persistent NuGet API key is required.

## Publish a version

1. Use `2.0.0` for the approved stable release. For subsequent releases, choose an unused version appropriate to the changes. Version 1.0.7 remains the final legacy release.
2. Update `<Version>` in `FunctionalSharp/FunctionalSharp.csproj`. Assembly and file versions are derived by the SDK. Keep `net10.0` as the library target. Update the README's install example and the consumer project's default `FluentSharpVersion`; CI passes the library version explicitly when testing the consumer.
3. Move the relevant changelog entries from **Unreleased** into a section for the version and release date.
4. Run all build/test/package/API/consumer commands in the [README](../readme.md#build-and-test). If public APIs intentionally changed, inspect the difference and regenerate the baseline with `dotnet run --project tools/ApiSnapshot --configuration Release -- --write docs/PUBLIC-API.txt`. Review nullable/source behavior through the consumer examples as well; the baseline records CLR signatures. Rerun [benchmarks](PERFORMANCE.md) when changing relevant hot paths.
5. Commit and push the changes to `master`. Wait for both CI jobs to pass.
6. In GitHub, create a release with a new tag that exactly matches `v<Version>` and targets the verified commit on `master`. Version `2.0.0` uses tag `v2.0.0`; leave the prerelease option unchecked. Copy the version's changelog entries into the release notes and link the [migration guide](MIGRATING-TO-V2.md).
7. Publish the GitHub Release. This starts **Publish NuGet release**.

The release workflow verifies that the tag matches the project version and that the commit belongs to `master`. It then runs the same Windows and Linux CI jobs against the release commit. After both succeed, it publishes the Linux job's verified `.nupkg` and companion `.snupkg` to NuGet and attaches them to the GitHub Release.

Draft releases do not publish packages. For a future prerelease, use a suffix such as `2.1.0-beta.1` in both the project version and tag, and mark its GitHub Release as a prerelease. NuGet determines prerelease status from the package version suffix; `2.0.0` is a stable version.

## If publishing fails

Inspect the failed step in GitHub Actions. Fix a missing trusted-publishing policy in NuGet, then rerun the failed jobs. For source or workflow changes, commit the fix to `master` and prepare a new release version.

NuGet package versions are immutable. Never reuse a version to distribute different contents. The workflow uses `--skip-duplicate` so a retry can finish after a partially successful publication; it cannot replace an existing package. Downloadable CI artifacts are intended for inspection and do not publish anything by themselves.

## Package contents

`dotnet pack` creates a `.nupkg` with the assembly, XML IntelliSense documentation, README, MIT license, and repository metadata. It also creates a `.snupkg` with portable PDBs. Source Link is supplied by the .NET 10 SDK and maps the symbols to the exact Git commit. Publish from committed source so those links resolve on GitHub.
