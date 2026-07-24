# Sound Files

This directory should contain the following MP3 files for the chess sound system:

| File | Description |
|------|-------------|
| `move.mp3` | Normal piece move sound |
| `capture.mp3` | Piece capture sound |
| `check.mp3` | Check notification sound |
| `gameover.mp3` | Game over / result sound |

## Requirements

- Format: MP3 (for broadest browser compatibility)
- Recommended duration: 0.2–0.5 seconds for move/capture, 0.5–1.0 seconds for check/gameover
- Volume: Normalize all files to similar perceived loudness
- Style: Short, crisp, non-intrusive UI sounds

## Source Suggestions

- [Freesound](https://freesound.org) — search for "chess move", "chess capture"
- [OpenGameArt](https://opengameart.org) — chess sound packs
- [Mixkit](https://mixkit.co/free-sound-effects/game/) — free game sound effects
- Create your own using [Audacity](https://www.audacityteam.org)

## Adding Files

1. Place your `.mp3` files in this directory
2. Ensure filenames match exactly: `move.mp3`, `capture.mp3`, `check.mp3`, `gameover.mp3`
3. The `SoundService` references these paths via JS interop (`audio.playSound`)
