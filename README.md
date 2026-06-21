<p align="center">
  <img width="128" align="center" src="images/eternalloop-logo.png" alt="EternalLoop logo">
</p>

<h1 align="center">
  EternalLoop 1.3.0
</h1>

<p align="center">
  A local adaptive music player that analyzes your songs with classic deterministic timing or local AI-enhanced timing, then creates smooth loop branches automatically with branch-quality filters for reducing weak jump candidates.
</p>

<p align="center">
  <a href="https://micilini.com/apps/eternalloop" target="_blank">
    <img src="images/buttonDownload.png" width="300" alt="Download EternalLoop" />
  </a>
</p>

<p align="center">
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white">
  <img alt="WPF" src="https://img.shields.io/badge/WPF-Windows-0078D6?style=for-the-badge&logo=windows&logoColor=white">
  <img alt="Offline" src="https://img.shields.io/badge/Offline-Local%20Analysis-7C48FF?style=for-the-badge">
  <img alt="Audio" src="https://img.shields.io/badge/Audio-MP3%20%7C%20WAV%20%7C%20FLAC%20%7C%20M4A-FF77C8?style=for-the-badge">
  <img alt="License" src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge">
</p>

---

# EternalLoop

EternalLoop is a Windows-native adaptive music player that analyzes local audio files, detects beats, builds musical branch points, and keeps playback looping without relying on any kind of external API.

It can run with the classic EternalLoop analysis engine or with Enhanced Analysis, a local AI-assisted timing mode used to improve beat and rhythm understanding while keeping the app offline and user-controlled.

It analyzes your track, detects beats, extracts audio features, finds musically compatible jump points, and plays the song through a local adaptive engine designed to avoid harsh cuts, repeated jumps, dead-end branches, and obvious phrase restarts.

The application is built for offline use. Your audio stays on your machine, analysis results are cached locally, and tuning presets let you choose between safer or more active loop behavior. Enhanced Analysis also runs locally; it does not upload your audio to any cloud service.

<p align="center">
  <img src="images/eternalloop-splash.png" alt="EternalLoop splash screen" width="800">
</p>

<p align="center">
  <img src="images/eternalloop-player.png" alt="EternalLoop player screen" width="800">
</p>

<p align="center">
  <img src="images/eternalloop-settings.png" alt="EternalLoop settings screen" width="800">
</p>

---

## Highlights

- Windows-native desktop app built with WPF and .NET 8.
- local-first audio analysis and playback.
- Optional Enhanced Analysis mode using a local AI timing model.
- Classic Analysis mode for deterministic, compatibility-focused timing.
- Supports MP3, WAV, M4A, and AAC files.
- Automatic beat tracking and beat-aligned playback.
- MFCC, chroma, RMS/loudness, spectral flux, and bar-position features.
- Section, beat, tatum, and segment analysis for richer loop maps.
- Self-similarity branch detection with adaptive thresholds.
- Branch quality scoring based on acoustic similarity, timing, loudness, and beat confidence.
- Phrase continuation validation to reduce jumps that start well but continue poorly.
- Metric-position and bar-phase checks to reduce jumps that lose the musical pulse.
- Beat duration similarity checks to reduce timing-incompatible jumps.
- Branch density controls to avoid overly crowded loop maps.
- Local microsegment fingerprints for sub-beat structure comparison.
- Structural branch policy for phrase, section, and boundary-aware routing.
- Topology policy to reduce short local loops and repeated weak patterns.
- Resilient late-anchor routing to improve long-play continuity near the end of a track.
- Smart jump decision engine with tempo-normalized probability ramping.
- Weighted branch selection with anti-repeat destination shaping.
- Jump cooldown and first-pass controls for safer or more active playback.
- Bring It Home mode to stop branching and let the current track finish naturally.
- Conservative, Balanced, and Wild tuning presets.
- Recent tracks list and local analysis cache.
- Track artwork display when available.
- Single-instance app guard.
- Self-contained Windows release build support.

---

## Supported Audio Formats

| Format | Status | Notes |
|---|---|---|
| MP3 | Supported | Decoded locally through the Windows audio stack / NAudio |
| WAV | Supported | Read locally through NAudio |
| M4A | Supported | Requires compatible Windows Media Foundation support |
| AAC | Supported | Requires compatible Windows Media Foundation support |

Unsupported, missing, or corrupted audio files are rejected before analysis.

---

## Loop Intelligence

EternalLoop does not simply jump to the closest-looking beat. It builds a branch graph and then applies multiple safety and quality layers before playback uses that graph.

| System | Purpose |
|---|---|
| Acoustic similarity scoring | Compares timbre, pitch-class, loudness, duration, and confidence signals |
| Adaptive thresholding | Keeps branch graphs useful without making every beat a branch source |
| Position-in-bar checks | Reduces jumps that land on the wrong musical pulse |
| Beat duration similarity | Penalizes jumps between incompatible beat lengths |
| Beat confidence penalty | Reduces branches from unstable beat regions |
| Phrase continuation validation | Verifies that the destination keeps matching after the landing point |
| Microsegment comparison | Compares smaller sub-beat structures to reduce false-positive matches |
| Structural branch policy | Rewards phrase-safe and section-aware jumps while penalizing weak local loops |
| Topology filtering | Reduces clusters of overly local branches that can trap playback |
| Late-anchor routing | Improves the final part of the graph by preferring return paths with better long-play continuity |
| Tempo-normalized ramping | Keeps jump probability behavior more consistent across slow and fast tracks |
| Weighted branch selection | Favors stronger branches while still allowing variation |
| Anti-repeat shaping | Reduces immediate repetition of the same destination from the same source |
| Bring It Home | Lets the user disable branching in place and allow the track to finish naturally |

---

## Tuning Presets

EternalLoop includes three built-in loop behavior presets:

| Preset | Description |
|---|---|
| Conservative | Fewer jumps, stricter matching, and safer playback |
| Balanced | Default behavior focused on musical, controlled looping |
| Wild | More active jumping while keeping safety checks enabled |

The settings view lets you switch presets and adjust loop behavior without editing configuration files manually.

---

## Enhanced Analysis

EternalLoop 1.3.0 adds an Enhanced Analysis mode for local AI-assisted timing analysis.

| Mode | Description |
|---|---|
| Enhanced | Uses local AI timing analysis for rhythm, beat, and jump timing. This is the default mode in EternalLoop 1.3.0. |
| Classic | Uses EternalLoop's original deterministic timing analysis for compatibility and fallback testing. |

Enhanced Analysis is still local-first. It runs from model files shipped with the release package and does not send audio outside your computer.

---

## Local Cache

EternalLoop saves analysis results locally so previously opened songs can load faster.

Cached analysis is stored under the user's local application data folder:

```text
%LocalAppData%\EternalLoop
```

The cache stores analysis data and user preferences. It does not store copies of your music files.

If the analysis schema changes between versions, old cache entries can be ignored and rebuilt the next time a track is opened.

---

## Built With

- C# / .NET 8
- Windows Presentation Foundation (WPF)
- NAudio
- NWaves
- MaterialDesignThemes
- MaterialDesignColors
- Microsoft.Extensions.Hosting
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging
- Microsoft.ML.OnnxRuntime
- xUnit
- FluentAssertions

---

## Third-party Notices

Enhanced Analysis uses a locally packaged AI timing model derived from the CPJKU Beat This project.

Third-party model and license notices are kept in the model asset folder:

- [Third-party notices](modules/AnalysisEngine/assets/models/beat-this/THIRD_PARTY_NOTICES.md)
- [Third-party license file](modules/AnalysisEngine/assets/models/beat-this/BEAT_THIS_LICENSE.txt)

EternalLoop redistributes the converted inference model used by the app. It does not redistribute the original training datasets.

---

## How to Run Locally

Requirements:

- Windows 10 or 11 x64
- Visual Studio Community 2022 or newer
- .NET 8 SDK
- .NET desktop development workload
- Windows Media Foundation support for compressed audio formats

Steps:

1. Clone the repository.
2. Open `EternalLoop.slnx` in Visual Studio.
3. Restore NuGet packages.
4. Build the solution.
5. Run `EternalLoop.App`.

You can also run from PowerShell:

```powershell
dotnet restore .\EternalLoop.slnx
dotnet build .\EternalLoop.slnx --configuration Debug
dotnet run --project .\src\EternalLoop.App\EternalLoop.App.csproj
```

For local development with Enhanced Analysis, make sure the local model files exist under:

```text
modules\AnalysisEngine\assets\models\beat-this
```

Expected local files:

```text
beat-this-large.onnx
model.json
THIRD_PARTY_NOTICES.md
BEAT_THIS_LICENSE.txt
```

The `.onnx` model and real `model.json` are intentionally ignored by git, but they are required before running or publishing builds that use Enhanced Analysis.

---

## Testing

Run the full test suite:

```powershell
dotnet test .\EternalLoop.slnx --configuration Debug
```

Run module-specific tests:

```powershell
dotnet test .\modules\AnalysisEngine\EternalLoop.AnalysisEngine.slnx --configuration Debug
dotnet test .\modules\BranchAnalysis\EternalLoop.BranchAnalysis.slnx --configuration Debug
```

The test suite covers audio format detection, audio loading, feature extraction, beat tracking, branch analysis, graph topology, runtime package building, playback scheduling, branch decision behavior, cache persistence, settings persistence, UI view models, release packaging, and repository hygiene.

Additional release verifiers are available in `tools`:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\verify-audio-format-support.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-audio-load-limits.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-audio-loader-memory.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-playback-branch-index.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-playback-read-events.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-playback-graph-isolation.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-loop-map-render-cache.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-mini-spectrum-render-cache.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-settings-save-concurrency.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-command-error-handling.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-app-regression-coverage.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-release-memory.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-release-clean-code.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-release-clean-source.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-third-party-notices.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-publish-package.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-repository-hygiene.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-ui-exception-policy.ps1
```

For the complete release gate, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\verify-release-ready.ps1
```

---

## Enhanced Analysis Model Assets for Developers

The Windows release package includes the Enhanced Analysis model files. The source repository does not commit the large `.onnx` model or the local runtime `model.json`.

The Python conversion tooling lives in:

```text
modules\AnalysisEngine\tools\beat-this-conversion
```

Recommended Python setup:

```powershell
cd .\modules\AnalysisEngine\tools\beat-this-conversion

py -3.11 -m venv .venv
.\.venv\Scripts\Activate.ps1

python -m pip install --upgrade pip
python -m pip install -r requirements.txt
```

Export the ONNX model from a local checkpoint:

```powershell
python .\export_beat_this_to_onnx.py `
  --checkpoint "C:\Models\BeatThis\final0.ckpt" `
  --output ".\output\beat-this-large.onnx" `
  --metadata-output ".\output\model.json" `
  --model-name "beat-this-large" `
  --model-version "final0" `
  --license "MIT"
```

Verify the exported model:

```powershell
python .\verify_onnx.py `
  --model ".\output\beat-this-large.onnx" `
  --contract ".\sample_input_contract.json" `
  --summary-output ".\output\beat-this-large.verification.json"
```

Then go back to the AnalysisEngine module and sync the generated model assets:

```powershell
cd ..\..

powershell -ExecutionPolicy Bypass -File .\tools\sync-beatthis-model-assets.ps1 `
  -SourceRoot ".\tools\beat-this-conversion\output" `
  -DestinationRoot ".\assets\models\beat-this"
```

Before publishing, validate the local model notices and hash:

```powershell
cd ..\..

powershell -ExecutionPolicy Bypass -File .\tools\verify-third-party-notices.ps1
```

The release package expects these files to be present locally:

```text
modules\AnalysisEngine\assets\models\beat-this\beat-this-large.onnx
modules\AnalysisEngine\assets\models\beat-this\model.json
modules\AnalysisEngine\assets\models\beat-this\THIRD_PARTY_NOTICES.md
modules\AnalysisEngine\assets\models\beat-this\BEAT_THIS_LICENSE.txt
```

Do not commit the generated `.onnx`, the real `model.json`, or local verification files. They are runtime/release artifacts, not source files.

---

## Release Build

Before publishing a public build with Enhanced Analysis, confirm the local model assets exist:

```powershell
Get-ChildItem .\modules\AnalysisEngine\assets\models\beat-this
powershell -ExecutionPolicy Bypass -File .\tools\verify-third-party-notices.ps1
```

The publish package should include only these model runtime files:

```text
assets\models\beat-this\beat-this-large.onnx
assets\models\beat-this\model.json
assets\models\beat-this\THIRD_PARTY_NOTICES.md
assets\models\beat-this\BEAT_THIS_LICENSE.txt
```

Recommended release automation:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\publish-release-win-x64.ps1
```

Manual publish command:

```powershell
dotnet publish .\src\EternalLoop.App\EternalLoop.App.csproj `
  -c Release `
  -p:PublishProfile=win-x64-self-contained
```

The publish output is written to:

```text
artifacts\publish\EternalLoop-1.3.0-win-x64
```

After publishing, validate the package:

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\verify-publish-package.ps1
```

Recommended release settings:

| Option | Value |
|---|---|
| Target framework | `net8.0-windows` |
| Runtime | `win-x64` |
| Self-contained | Yes |
| Single file | Yes |
| ReadyToRun | Yes |
| Trimming | No |
| Native AOT | No |

Before packaging a public build, run:

```powershell
dotnet build .\EternalLoop.slnx -c Release
dotnet test .\EternalLoop.slnx -c Release
dotnet test .\modules\AnalysisEngine\EternalLoop.AnalysisEngine.slnx -c Release
dotnet test .\modules\BranchAnalysis\EternalLoop.BranchAnalysis.slnx -c Release
powershell -ExecutionPolicy Bypass -File .\tools\verify-third-party-notices.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-release-clean-source.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-release-ready.ps1
```

After publishing, test the generated executable before distribution:

1. Open the app.
2. Confirm only one instance can run.
3. Open Settings.
4. Open an MP3 or WAV file.
5. Wait for analysis to complete.
6. Play for several minutes.
7. Switch between Conservative, Balanced, and Wild presets.
8. Test Bring It Home during playback.
9. Close and reopen the app.
10. Confirm cached tracks load correctly.

---

## Version History

### Version 1.3.0

Enhanced Analysis release.

Highlights:

- Added Enhanced Analysis mode using a local AI timing model for rhythm and beat understanding.
- Added Classic Analysis mode for deterministic fallback and compatibility testing.
- Added Settings support for switching between Enhanced and Classic analysis.
- Improved beat timing precision in the WPF app while keeping the app offline-first.
- Added ONNX runtime model packaging for the Windows release build.
- Added third-party notices and model license packaging for the Enhanced Analysis model.
- Added release checks for model notices, model hash validation, publish contents, and clean source packaging.
- Refined release hygiene by removing roadmap artifacts, internal cleanup terms, and development-only files from the public package.
- Preserved local cache, tuning presets, branch analysis, adaptive playback, and Bring It Home behavior.

### Version 1.2.0

Branch Quality and Smart Playback release.

Highlights:

- Improved beat tracking, bar-phase selection, and timing refinement.
- Added richer branch analysis with structural, phrase, and topology-aware filtering.
- Added resilient late-anchor routing for better long-play continuity.
- Added tempo-normalized jump probability ramping.
- Added weighted branch selection and anti-repeat destination shaping.
- Added active jump shaping controls for probability, cooldown, and first-pass behavior.
- Added Bring It Home mode for natural track completion.
- Updated Conservative, Balanced, and Wild tuning presets.
- Improved settings migration and local cache behavior.
- Expanded automated test coverage across playback, branch analysis, app behavior, settings, and release checks.
- Preserved offline-first local analysis and playback.

### Version 1.0.0

Initial public release of EternalLoop.

Highlights:

- Local continuous loop playback for supported audio files.
- Beat tracking and feature extraction.
- Self-similarity branch detection.
- Circular branch graph visualization.
- Local analysis caching.
- Recent tracks list.
- Tuning presets.
- Anti-repeat jump protection.
- End-guard loop survival.
- WPF desktop interface for Windows.

---

## Privacy

EternalLoop analyzes audio locally.

The application does not require a cloud account, does not upload your songs, and does not depend on streaming-service APIs for analysis.

Enhanced Analysis also runs locally from packaged model files. Your audio is not sent to an external AI service.

---

## Contributing

Contributions are welcome.

You can open issues for bugs, improvements, audio-analysis ideas, UI refinements, release checks, or feature suggestions. Pull Requests are also welcome.

Before opening a Pull Request, please run:

```powershell
dotnet build .\EternalLoop.slnx -c Release
dotnet test .\EternalLoop.slnx -c Release
powershell -ExecutionPolicy Bypass -File .\tools\verify-third-party-notices.ps1
powershell -ExecutionPolicy Bypass -File .\tools\verify-release-ready.ps1
```

---

## License

EternalLoop is open-source software released under the MIT License.

See [LICENSE](LICENSE) for details.
