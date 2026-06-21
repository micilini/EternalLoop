using System.IO.Compression;
using System.Text.Json;

namespace EternalLoop.AnalysisEngine.Tests.BeatTracking.Ai.Advisor;

public sealed class AdvisorGoldenMasterZipReader
{
    private readonly string _zipPath;
    private readonly JsonDocument _manifest;

    public AdvisorGoldenMasterZipReader(string zipPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(zipPath);

        _zipPath = Path.GetFullPath(zipPath);

        if (!File.Exists(_zipPath))
        {
            throw new FileNotFoundException("Advisor Golden Master ZIP was not found.", _zipPath);
        }

        _manifest = ReadJson("manifest.json");
        TrackIds = _manifest.RootElement
            .GetProperty("tracks")
            .EnumerateArray()
            .Select(track => track.GetProperty("track_id").GetString()!)
            .ToArray();
    }

    public IReadOnlyList<string> TrackIds { get; }

    public AdvisorGoldenMasterFixture LoadTrack(string trackId)
    {
        var track = ReadJson($"{trackId}/golden-track.json");
        var spectrogram = ReadNpzData($"{trackId}/input-spectrogram.float32.npz");
        var beatLogits = ReadNpzData($"{trackId}/expected-beat-logits.float32.npz");
        var downbeatLogits = ReadNpzData($"{trackId}/expected-downbeat-logits.float32.npz");
        var shape = spectrogram.Shape;

        return new AdvisorGoldenMasterFixture
        {
            TrackId = trackId,
            Spectrogram = spectrogram.Data,
            SpectrogramFrames = shape[0],
            MelBins = shape[1],
            ExpectedBeatLogits = beatLogits.Data,
            ExpectedDownbeatLogits = downbeatLogits.Data,
            DurationSeconds = track.RootElement.GetProperty("duration_seconds").GetDouble(),
            FrameRate = track.RootElement.GetProperty("estimated_frame_rate").GetDouble(),
            AggregateContract = ReadJson($"{trackId}/expected-aggregate-contract.json"),
            PostprocessContract = ReadJson($"{trackId}/expected-postprocess-contract.json"),
            ExpectedPostprocessBeats = ReadJson($"{trackId}/expected-postprocess-beats.json"),
            ExpectedPostprocessDownbeats = ReadJson($"{trackId}/expected-postprocess-downbeats.json")
        };
    }

    public JsonDocument ReadManifest()
    {
        return ReadJson("manifest.json");
    }

    private (float[] Data, int[] Shape) ReadNpzData(string entryName)
    {
        using var archive = ZipFile.OpenRead(_zipPath);
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidDataException($"Golden Master ZIP entry not found: {entryName}");
        using var stream = entry.Open();

        return NpzFloat32Reader.ReadDataArray(stream);
    }

    private JsonDocument ReadJson(string entryName)
    {
        using var archive = ZipFile.OpenRead(_zipPath);
        var entry = archive.GetEntry(entryName)
            ?? throw new InvalidDataException($"Golden Master ZIP entry not found: {entryName}");
        using var stream = entry.Open();

        return JsonDocument.Parse(stream);
    }
}
