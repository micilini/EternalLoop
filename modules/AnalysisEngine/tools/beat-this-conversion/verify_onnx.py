from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

import numpy as np
import onnx
import onnxruntime as ort


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Verify a Beat This ONNX chunk model.")
    parser.add_argument("--model", required=True, help="Path to .onnx file.")
    parser.add_argument("--contract", default="sample_input_contract.json")
    parser.add_argument("--summary-output", default=None)
    parser.add_argument("--frames", type=int, default=None)
    parser.add_argument("--mel-bins", type=int, default=None)
    return parser.parse_args()


def load_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def main() -> int:
    args = parse_args()
    model_path = Path(args.model).resolve()
    contract_path = Path(args.contract).resolve()

    if not model_path.exists():
        raise FileNotFoundError(f"ONNX model not found: {model_path}")

    if not contract_path.exists():
        raise FileNotFoundError(f"Input contract not found: {contract_path}")

    contract = load_json(contract_path)
    input_name = contract["input"]["name"]
    output_names = [output["name"] for output in contract["outputs"]]
    frames = args.frames or int(contract["input"]["default_frames"])
    mel_bins = args.mel_bins or int(contract["input"]["default_mel_bins"])

    onnx_model = onnx.load(str(model_path))
    onnx.checker.check_model(onnx_model)

    session = ort.InferenceSession(str(model_path), providers=["CPUExecutionProvider"])
    session_inputs = [item.name for item in session.get_inputs()]
    session_outputs = [item.name for item in session.get_outputs()]

    if input_name not in session_inputs:
        raise AssertionError(f"Expected input '{input_name}' not found. Actual inputs: {session_inputs}")

    for output_name in output_names:
        if output_name not in session_outputs:
            raise AssertionError(f"Expected output '{output_name}' not found. Actual outputs: {session_outputs}")

    dummy = np.zeros((1, frames, mel_bins), dtype=np.float32)
    outputs = session.run(output_names, {input_name: dummy})

    summary = {
        "ok": True,
        "model": str(model_path),
        "contract": str(contract_path),
        "inputs": session_inputs,
        "outputs": session_outputs,
        "dummy_input_shape": list(dummy.shape),
        "output_shapes": [list(output.shape) for output in outputs],
        "output_finite": [bool(np.isfinite(output).all()) for output in outputs],
    }

    if not all(summary["output_finite"]):
        raise AssertionError(f"ONNX output contains non-finite values: {summary}")

    if args.summary_output:
        Path(args.summary_output).write_text(json.dumps(summary, indent=2) + "\n", encoding="utf-8")

    print(json.dumps(summary, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())