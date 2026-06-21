# Third-party notices - Enhanced Analysis model

## Component

```text
Name: Beat This! / converted ONNX beat and downbeat model
Purpose: Local AI timing analysis provider for EternalLoop
Runtime: Microsoft.ML.OnnxRuntime
```

## Source

```text
Original project: CPJKU/beat_this
Original repository: https://github.com/CPJKU/beat_this
Original authors: Francesco Foscarin, Jan Schluter, Gerhard Widmer
Original institution: Institute of Computational Perception, JKU Linz, Austria
Original checkpoint: final0
Converted ONNX file: beat-this-large.onnx
Converted ONNX SHA256: eb2b205f4f49f8393daf7504d5822da34e23b3ec7da703a7700c2aa70a8c6c8b
```

## License

```text
Original code license: MIT
Published model weights license: MIT
Converted ONNX model license: MIT, derived from the published model weights
Redistribution: allowed under the MIT license terms
Commercial redistribution: allowed under the MIT license terms
```

The original project notes that the code and published model weights are released under the MIT license. It also notes that some training files may be copyrighted or under limited Creative Commons licenses. EternalLoop redistributes only the converted model used for inference, not the original training files.

See:

```text
assets/models/beat-this/BEAT_THIS_LICENSE.txt
```

## Citation

```bibtex
@inproceedings{foscarin2024beatthis,
  author = {Francesco Foscarin and Jan Schl{\"u}ter and Gerhard Widmer},
  title = {Beat this! Accurate beat tracking without {DBN} postprocessing},
  year = {2024},
  month = {nov},
  booktitle = {Proceedings of the 25th International Society for Music Information Retrieval Conference (ISMIR)},
  address = {San Francisco, CA, United States}
}
```

## Notes

```text
EternalLoop uses a converted ONNX runtime artifact for local inference.
EternalLoop does not redistribute the original training datasets.
EternalLoop is not affiliated with CPJKU, JKU Linz, or the Beat This! authors.
```
