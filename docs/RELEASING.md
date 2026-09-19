# Maintaining and releasing FluentSharp

FluentSharp is maintained by Matías Iacono. Changes are committed directly to `master`; external pull requests are not accepted.

## Continuous integration

Every push to `master` runs `.github/workflows/ci.yml` on Windows and Linux with the .NET 10 SDK. It restores dependencies, builds the solution in Release mode, runs the tests with coverage, and creates NuGet and symbol packages.

Each run uploads `test-results-<os>` and `packages-<os>` artifacts, retained for 14 days. Test results are uploaded even when tests fail. Open the repository's **Actions → CI** page to inspect or download them. CI can also be started manually with **Run workflow**.

The library continues to target .NET Standard 2.0. The test suite runs on .NET 10; these jobs do not verify every runtime that can consume .NET Standard 2.0.

## One-time NuGet setup

The release workflow uses [NuGet trusted publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing). Sign in to NuGet.org as **MatiasIacono**, the existing FluentSharp package owner, and add a trusted publishing policy with:

| Setting | Value |
| --- | --- |
| Repository owner | `MatiasIac` |
| Repository | `fluentsharp` |
| Workflow file | `release.yml` |
| Environment | Leave empty |
| Package scope, if requested | `FluentSharp` |

The workflow requests a short-lived publishing credential through `NuGet/login`. No persistent NuGet API key or GitHub repository secret is required. If package ownership changes, update the `user` value in the workflow and the NuGet policy together.

Ensure GitHub Actions is enabled for the repository and can run the official `actions/*` and `NuGet/login` actions. There are no branch protection or pull request requirements.

## Publish a version

1. Choose an unused package version. Version `1.0.6` is already published; for example, the next patch release could be `1.0.7`.
2. Update `<Version>` in `FunctionalSharp/FunctionalSharp.csproj`. Assembly and file versions are derived by the SDK. Keep `.NET Standard 2.0` as the library target.
3. Move the relevant changelog entries from **Unreleased** into a section for the version and release date.
4. Run the build, test, and package commands in the [README](../readme.md#build-and-test).
5. Commit and push the changes to `master`. Wait for both CI jobs to pass.
6. In GitHub, create a release with a new tag that exactly matches `v<Version>` and targets the verified commit on `master`. For example, version `1.0.7` uses tag `v1.0.7`. Copy the version's changelog entries into the release notes.
7. Publish the GitHub Release. This starts **Publish NuGet release**.

The release workflow verifies that the tag matches the project version and that the commit belongs to `master`. It then runs the same Windows and Linux CI jobs against the release commit. After both succeed, it publishes the Linux job's verified `.nupkg` and companion `.snupkg` to NuGet and attaches them to the GitHub Release.

Draft releases do not publish packages. A prerelease version such as `1.0.7-rc.1` must be present in the project version and tag; mark its GitHub Release as a prerelease too. NuGet determines prerelease status from the package version suffix.

## If publishing fails

Inspect the failed step in GitHub Actions. Fix a missing trusted-publishing policy in NuGet, then rerun the failed jobs. For source or workflow changes, commit the fix to `master` and prepare a new release version.

NuGet package versions are immutable. Never reuse a version to distribute different contents. The workflow uses `--skip-duplicate` so a retry can finish after a partially successful publication; it cannot replace an existing package. Downloadable CI artifacts are intended for inspection and do not publish anything by themselves.

## Package contents

`dotnet pack` creates a `.nupkg` with the assembly, XML IntelliSense documentation, README, MIT license, and repository metadata. It also creates a `.snupkg` with portable PDBs. Source Link is supplied by the .NET 10 SDK and maps the symbols to the exact Git commit. Publish from committed source so those links resolve on GitHub.
