# Beat This ONNX Conversion Tooling

This folder contains external Python tooling for converting a Beat This PyTorch checkpoint into an ONNX model usable by EternalLoop AnalysisEngine.

This is not runtime code.

The C# AnalysisEngine must never require Python, PyTorch, torchaudio, soxr, ffmpeg, or the Beat This Python package at runtime.

## What is exported

The export script exports the Beat This neural frame model:

```text
input:  spectrogram float32 [batch, frames, mel_bins]
output: beat_logits      [batch, frames]
output: downbeat_logits  [batch, frames]
```

It does not export:

```text
File2Beats
Audio2Beats
audio decoding
resampling
log-mel preprocessing
minimal/DBN postprocessing
```

Those steps are intentionally handled by later C# phases.

## Setup

Recommended Python:

```powershell
py -3.11 -m venv .venv
.\.venv\Scripts\Activate.ps1
python -m pip install --upgrade pip
python -m pip install -r requirements.txt
```

If PyTorch installation fails or picks the wrong CPU/GPU build, install PyTorch manually first for your platform, then run the requirements file again.

## Export from local checkpoint

```powershell
python .\export_beat_this_to_onnx.py `
  --checkpoint "C:\Models\BeatThis\final0.ckpt" `
  --output ".\output\beat-this-large.onnx" `
  --metadata-output ".\output\model.json" `
  --model-name "beat-this-large" `
  --model-version "final0" `
  --license "MIT"
```

## Verify ONNX

```powershell
python .\verify_onnx.py `
  --model ".\output\beat-this-large.onnx" `
  --contract ".\sample_input_contract.json" `
  --summary-output ".\output\beat-this-large.verification.json"
```

## Import into AnalysisEngine assets

After export and verification, go back to `modules/AnalysisEngine` and run:

```powershell
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
.\tools\import-beat-this-model.ps1 `
  -SourceModelPath ".\tools\beat-this-conversion\output\beat-this-large.onnx" `
  -Name "beat-this-large" `
  -Version "final0" `
  -License "MIT" `
  -SampleRate 22050 `
  -FrameRate 100.0 `
  -InputName "spectrogram" `
  -OutputNames beat_logits,downbeat_logits `
  -Force
```

Do not commit the generated `.onnx` unless licensing/package policy explicitly approves it.

## Notes

- The large Beat This models are roughly 78 MB.
- `sample_input_contract.json` is the bridge between this Python tooling and the future C# preprocessor/inference code.
- Generated output belongs in local `output/`, not in git.