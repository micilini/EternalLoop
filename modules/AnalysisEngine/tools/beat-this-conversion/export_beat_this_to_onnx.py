from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
from typing import Any

import torch


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Export Beat This PyTorch checkpoint to an ONNX chunk model."
    )
    parser.add_argument(
        "--checkpoint",
        required=True,
        help="Path to local Beat This .ckpt file. Short names such as final0 require --allow-shortname.",
    )
    parser.add_argument(
        "--output",
        required=True,
        help="Destination .onnx file path.",
    )
    parser.add_argument(
        "--metadata-output",
        default=None,
        help="Optional model.json path generated next to the ONNX file.",
    )
    parser.add_argument("--model-name", default="beat-this-large")
    parser.add_argument("--model-version", default="unknown")
    parser.add_argument("--license", default="MIT")
    parser.add_argument("--input-name", default="spectrogram")
    parser.add_argument("--beat-output-name", default="beat_logits")
    parser.add_argument("--downbeat-output-name", default="downbeat_logits")
    parser.add_argument("--sample-rate", type=int, default=22050)
    parser.add_argument("--frame-rate", type=float, default=100.0)
    parser.add_argument("--frames", type=int, default=1500)
    parser.add_argument("--mel-bins", type=int, default=128)
    parser.add_argument("--opset", type=int, default=17)
    parser.add_argument("--allow-shortname", action="store_true")
    parser.add_argument("--no-dynamic-frames", action="store_true")
    parser.add_argument(
        "--use-dynamo-exporter",
        action="store_true",
        help=(
            "Use the modern torch.export/dynamo ONNX exporter. "
            "Default is false because Beat This currently exports more reliably "
            "through the legacy TorchScript-based exporter."
        ),
    )
    return parser.parse_args()


class BeatThisOnnxWrapper(torch.nn.Module):
    def __init__(self, model: torch.nn.Module) -> None:
        super().__init__()
        self.model = model

    def forward(self, spectrogram: torch.Tensor) -> tuple[torch.Tensor, torch.Tensor]:
        prediction = self.model(spectrogram)

        if not isinstance(prediction, dict):
            raise TypeError("Beat This model output must be a dictionary.")

        if "beat" not in prediction or "downbeat" not in prediction:
            keys = ", ".join(str(key) for key in prediction.keys())
            raise KeyError(f"Expected output keys 'beat' and 'downbeat'. Got: {keys}")

        return prediction["beat"], prediction["downbeat"]


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def validate_checkpoint_argument(checkpoint: str, allow_shortname: bool) -> None:
    checkpoint_path = Path(checkpoint)
    if checkpoint_path.exists():
        if checkpoint_path.suffix.lower() != ".ckpt":
            raise ValueError(f"Checkpoint must be a .ckpt file: {checkpoint_path}")
        return

    if allow_shortname:
        return

    raise FileNotFoundError(
        "Checkpoint file was not found. Use a local .ckpt path, or pass "
        "--allow-shortname if you intentionally want Beat This to resolve/download a model name."
    )


def load_beat_this_model(checkpoint: str) -> torch.nn.Module:
    from beat_this.inference import load_model

    model = load_model(checkpoint_path=checkpoint, device="cpu")
    model.eval()
    return model


def export_onnx(args: argparse.Namespace) -> dict[str, Any]:
    validate_checkpoint_argument(args.checkpoint, args.allow_shortname)

    output_path = Path(args.output).resolve()
    output_path.parent.mkdir(parents=True, exist_ok=True)

    model = BeatThisOnnxWrapper(load_beat_this_model(args.checkpoint))
    model.eval()

    dummy = torch.zeros((1, args.frames, args.mel_bins), dtype=torch.float32)

    output_names = [args.beat_output_name, args.downbeat_output_name]
    dynamic_axes: dict[str, dict[int, str]] = {
        args.input_name: {0: "batch"},
        args.beat_output_name: {0: "batch"},
        args.downbeat_output_name: {0: "batch"},
    }

    if not args.no_dynamic_frames:
        dynamic_axes[args.input_name][1] = "frames"
        dynamic_axes[args.beat_output_name][1] = "frames"
        dynamic_axes[args.downbeat_output_name][1] = "frames"

    with torch.no_grad():
        torch.onnx.export(
            model,
            dummy,
            str(output_path),
            input_names=[args.input_name],
            output_names=output_names,
            dynamic_axes=dynamic_axes,
            opset_version=args.opset,
            do_constant_folding=True,
            dynamo=args.use_dynamo_exporter,
        )

    onnx_sha256 = sha256(output_path)

    metadata = {
        "name": args.model_name,
        "version": args.model_version,
        "license": args.license,
        "model_file": output_path.name,
        "model_sha256": onnx_sha256,
        "sample_rate": args.sample_rate,
        "frame_rate": args.frame_rate,
        "input_name": args.input_name,
        "output_names": output_names,
        "onnx_kind": "spectrogram-to-frame-logits",
        "chunk_frames": args.frames,
        "mel_bins": args.mel_bins,
        "opset": args.opset,
        "source_checkpoint": str(args.checkpoint),
        "exporter": "torch-onnx-dynamo" if args.use_dynamo_exporter else "torch-onnx-legacy",
    }

    metadata_output = (
        Path(args.metadata_output).resolve()
        if args.metadata_output
        else output_path.with_name("model.json")
    )
    metadata_output.write_text(json.dumps(metadata, indent=2) + "\n", encoding="utf-8")

    return metadata


def main() -> int:
    args = parse_args()
    metadata = export_onnx(args)
    print("Beat This ONNX export complete.")
    print(json.dumps(metadata, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())