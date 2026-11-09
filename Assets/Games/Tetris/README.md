# Tetris (Bringup) — kraineArcade

Initial Tetris bringup for [issue #4](https://github.com/stkraine/kraineArcade/issues/4), aligned with the [Tetris Guideline](https://tetris.wiki/Tetris_Guideline) where practical.

## Implemented (core)
- **Playfield**: 10×20 visible + 20-row buffer (`TetrisBoard`)
- **Tetrominoes**: I, O, T, S, Z, J, L with standard spawn orientations and guideline color mapping
- **Super Rotation System (SRS)**: rotation states and kick tables for JLSTZ and I (`SRSKickData`)
- **7-bag randomizer**: shuffled bag of all 7 pieces (`SevenBagRandomizer`)
- **Spawn**: centered/left-rounded spawn in buffer rows
- **Hold**: single hold slot, one swap per lock (`TetrisGame.Hold`)
- **Preview**: next queue (6 pieces)
- **Ghost piece**: computed drop target (`TetrisBoard.GetGhostY`)
- **Lock delay**: 0.5s with move-reset behavior (max 15 resets)
- **Line clears** and collapse
- **Scoring**: basic guideline-style scoring (singles–tetris, soft/hard drop, combos, back-to-back, level from lines)
- **T-spin detection**: 3-corner rule (bringup); T-spin mini is a simplified approximation to refine
- **Game over**: spawn overlap / lock completely above visible playfield
- **Unity integration**: `TetrisGameController` MonoBehaviour with input/gravity hooks

## Not yet implemented / follow-ups
- Full rendering (tilemap/sprites/UI), audio, and scene wiring
- Complete T-spin mini corner/pointing-side rules and all scoring edge cases
- IRS/IHS, configurable DAS/ARR/ARE, multiple lock-down modes
- Marathon/40 Lines/Ultra modes, level speed tables matching a specific guideline version exactly
- Multiplayer/garbage, polished mobile controls, accessibility, and full test coverage in Unity Test Runner

## Tests
See `Tests/TetrisCoreTests.cs` for NUnit tests covering bag behavior, board basics, spawn, hold, and core game operations.
