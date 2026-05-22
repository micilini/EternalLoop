# EternalLoop V1.1.0 Release Checklist

## Automated validation

- [ ] `dotnet restore .\EternalLoop.slnx`
- [ ] `dotnet build .\EternalLoop.slnx --configuration Debug`
- [ ] `dotnet test .\EternalLoop.slnx --configuration Debug`
- [ ] `.\tools\download-ai-models.ps1`
- [ ] `dotnet test .\EternalLoop.slnx --configuration Debug --filter "Category=AI"`
- [ ] `.\tools\validate-v1.1.0-release.ps1`

## Manual smoke test - AI ON

- [ ] Open app
- [ ] Confirm version v1.1.0
- [ ] Confirm Use local AI is enabled
- [ ] Open a 3-5 minute MP3
- [ ] Confirm analysis reaches Running local AI similarity
- [ ] Confirm player opens
- [ ] Confirm AI status says AI used or AI cache
- [ ] Confirm branch graph is not empty
- [ ] Listen for at least 2 minutes
- [ ] Confirm playback does not crash
- [ ] Reopen same track
- [ ] Confirm cache is reused

## Manual smoke test - AI OFF

- [ ] Disable Use local AI
- [ ] Reanalyze the same track
- [ ] Confirm AI stage is skipped
- [ ] Confirm player opens
- [ ] Confirm status says AI off or classic analysis
- [ ] Confirm branch graph is not empty

## Manual smoke test - fallback

- [ ] Temporarily create a controlled AI failure in Debug only
- [ ] Confirm app falls back to classic analysis
- [ ] Confirm player opens
- [ ] Confirm Classic fallback is visible
- [ ] Confirm Copy AI diagnostics works
- [ ] Restore normal AI path

## Publish validation

- [ ] Publish output contains EternalLoop.exe
- [ ] Publish output contains Assets/Models/DiscogsEffNet/discogs_track_embeddings-effnet-bs64-1.onnx
- [ ] Publish output contains Assets/Models/DiscogsEffNet/discogs_track_embeddings-effnet-bs64-1.json
- [ ] Publish output contains Assets/Models/DiscogsEffNet/model-manifest.json
- [ ] Publish output contains Assets/Models/DiscogsEffNet/MODEL-LICENSE-NOTICE.txt
- [ ] App opens from publish folder with internet disconnected

## License validation

- [ ] EternalLoop source remains MIT
- [ ] Third-party model notice is included
- [ ] README does not claim the model is MIT
- [ ] Public/commercial/store distribution license status is reviewed before release
