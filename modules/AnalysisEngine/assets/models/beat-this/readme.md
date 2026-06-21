# Enhanced Analysis model assets

This folder stores the local model files used by the Enhanced Analysis provider.

## Local runtime files

These files are expected locally for development and publishing:

```text
beat-this-large.onnx
model.json
```

They are intentionally ignored by git because the model payload is large and should be managed as a release/runtime artifact.

## Tracked files

```text
readme.md
model.json.example
THIRD_PARTY_NOTICES.md
BEAT_THIS_LICENSE.txt
.gitignore
.gitkeep
```

## Release package files

The published application package should include only:

```text
beat-this-large.onnx
model.json
THIRD_PARTY_NOTICES.md
BEAT_THIS_LICENSE.txt
```

It should not include:

```text
.gitignore
.gitkeep
model.json.example
readme.md
*.verification.json
```

## Verification

Before release, run:

```powershell
.\tools\verify-third-party-notices.ps1
.\tools\publish-release-win-x64.ps1
.\tools\verify-publish-package.ps1
```
