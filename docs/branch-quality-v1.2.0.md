# EternalLoop V1.2.0 - Branch Quality

## Goal

EternalLoop V1.2.0 reduces false-positive loop branches by adding local musical filters on top of the classic branch finder and the optional local AI similarity gate.

## What changed

- Beat duration similarity gate.
- Beat confidence penalty.
- Preset-aware metric position gate.
- Branch source density limiter.
- Local microsegment / sub-beat fingerprints.

## Important rule

The new filters never boost a branch score. They can preserve, penalize, or reject a candidate.

## Preset behavior

| Preset | Behavior |
|---|---|
| Conservative | Strictest filtering, fewer branches, safest jumps |
| Balanced | Default balance between branch quality and activity |
| Wild | More permissive, useful for difficult tracks |

## Local microsegments

Microsegments split each beat into smaller local fingerprints. They help catch cases where two beats look similar as a whole, but their internal structure does not match.

## No external APIs

This feature does not use Spotify, Echo Nest, cloud analysis, or any external service.

## Limitations

These filters reduce false positives, but they do not guarantee perfect loops. Some tracks may still need different presets.

## Manual listening checklist

Test with:

- pop song with repeated chorus
- electronic song with drops
- vaporwave/future funk repetitive track
- track with long intro and weak percussion
- track with bridge or transition
- short 2-3 minute track
- long 5+ minute track

Compare:

- Conservative
- Balanced
- Wild
- AI ON
- AI OFF
