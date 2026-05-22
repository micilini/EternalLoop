using EternalLoop.Contracts.Options;
using EternalLoop.Core.AI;
using FluentAssertions;

namespace EternalLoop.Core.Tests.AI;

public sealed class AiMelFilterBankTests
{
    [Fact]
    public void Create_returns_expected_filter_count()
    {
        var filterBank = new AiMelFilterBank();

        var filters = CreateDefault(filterBank);

        filters.Should().HaveCount(AiPreprocessingDefaultValues.MelBands);
    }

    [Fact]
    public void Create_returns_expected_bin_count()
    {
        var filterBank = new AiMelFilterBank();
        var expectedBins = AiPreprocessingDefaultValues.FftSize / 2 + 1;

        var filters = CreateDefault(filterBank);

        filters.Should().OnlyContain(filter => filter.Length == expectedBins);
    }

    [Fact]
    public void Create_returns_non_negative_finite_weights()
    {
        var filterBank = new AiMelFilterBank();

        var filters = CreateDefault(filterBank);

        filters.SelectMany(filter => filter).Should().OnlyContain(weight => double.IsFinite(weight) && weight >= 0.0);
    }

    [Fact]
    public void Create_rejects_invalid_arguments()
    {
        var filterBank = new AiMelFilterBank();

        filterBank.Invoking(bank => bank.Create(0, AiPreprocessingDefaultValues.FftSize, AiPreprocessingDefaultValues.MelBands, AiPreprocessingDefaultValues.MinFrequencyHertz))
            .Should().Throw<ArgumentOutOfRangeException>();
        filterBank.Invoking(bank => bank.Create(AiPreprocessingDefaultValues.SampleRate, 0, AiPreprocessingDefaultValues.MelBands, AiPreprocessingDefaultValues.MinFrequencyHertz))
            .Should().Throw<ArgumentOutOfRangeException>();
        filterBank.Invoking(bank => bank.Create(AiPreprocessingDefaultValues.SampleRate, AiPreprocessingDefaultValues.FftSize, 0, AiPreprocessingDefaultValues.MinFrequencyHertz))
            .Should().Throw<ArgumentOutOfRangeException>();
        filterBank.Invoking(bank => bank.Create(AiPreprocessingDefaultValues.SampleRate, AiPreprocessingDefaultValues.FftSize, AiPreprocessingDefaultValues.MelBands, -1.0))
            .Should().Throw<ArgumentOutOfRangeException>();
    }

    private static double[][] CreateDefault(AiMelFilterBank filterBank)
    {
        return filterBank.Create(
            AiPreprocessingDefaultValues.SampleRate,
            AiPreprocessingDefaultValues.FftSize,
            AiPreprocessingDefaultValues.MelBands,
            AiPreprocessingDefaultValues.MinFrequencyHertz);
    }
}
