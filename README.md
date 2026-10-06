
<div align="center">

# Block Blast · Block Puzzle

A 2D block puzzle game built with Unity and C#.
Drag blocks onto an 8 × 8 board, clear rows and columns, and build combos to beat your best score.

**Unity 6000.3.21f1** · **C#** · **Unity UI** · **TextMesh Pro** · **Input System**

[Gameplay](#gameplay) · [Technical highlights](#technical-highlights) · [Run locally](#run-locally)

</div>

## Gameplay demo

Gameplay video coming soon.

<!-- Replace the status above with your real video link when ready:
[▶ Watch the gameplay demo](YOUR_YOUTUBE_VIDEO_URL)

Optional: save a real gameplay screenshot to docs/images/gameplay.png,
then add a clickable preview:
[![Block Blast gameplay](docs/images/gameplay.png)](YOUR_YOUTUBE_VIDEO_URL)

For a GitHub-hosted video, upload the recording through GitHub's Markdown
editor and paste the resulting attachment URL here.
-->

## Gameplay

- **Drag and drop:** place blocks with mouse or touch input, with a placement preview and a raised drag position to keep the board visible.
- **Clear lines:** complete rows or columns to clear them simultaneously.
- **Three-piece tray:** use all three pieces to receive a new set.
- **Score and combos:** earn points for placed cells, cleared lines, and consecutive clears.
- **Saved best score:** keep your personal record between sessions using `PlayerPrefs`.
- **Complete game loop:** start from the main menu, restart after Game Over, or return to the menu.

Pieces cannot be rotated while dragging. Blocks stay in place after a clear; there is no gravity. The game ends when none of the remaining pieces fit on the board.

## Technical highlights

- **Board logic separated from presentation:** `BlockBoard` handles placement validation, available moves, and simultaneous line clearing; UI components render the result.
- **Reusable piece views:** three scene instances reuse their block Images as new shapes are dealt.
- **Scene and prefab workflow:** the board, cells, piece tray, and UI are configured through serialized Inspector references.
- **Configuration validation:** runtime checks report missing references, and Editor tooling validates scene references before a build.

| Component | Responsibility |
| --- | --- |
| [`BlockBoard`](Assets/Scripts/BlockBoard.cs) | Board state, shapes, placement, line clearing, and base scoring |
| [`BlockGame`](Assets/Scripts/BlockGame.cs) | Piece dealing, combos, game state, score persistence, and UI updates |
| [`BlockPiece`](Assets/Scripts/BlockPiece.cs) | Drag-and-drop input and reusable piece rendering |
| [`BlockGridView`](Assets/Scripts/BlockGridView.cs) / [`BlockCell`](Assets/Scripts/BlockCell.cs) | Board rendering, coordinates, and placement previews |
| [`BlockMenu`](Assets/Scripts/BlockMenu.cs) / [`BlockArt`](Assets/Scripts/BlockArt.cs) | Menu actions and sprite configuration |
| [`Editor tools`](Assets/Editor) | Scene/prefab setup, build validation, and smoke checks |

## Run locally

1. Open the project in **Unity 6000.3.21f1** and wait for importing and compilation to finish.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press **Play**, then select **PLAY** to start.

To start directly in gameplay, open `Assets/Scenes/Game.unity`. Both scenes are already included in Build Settings.

<details>
<summary><strong>Scoring rules</strong></summary>

- Each placed cell awards **10 points**.
- Clearing **N** rows/columns awards **100 × N² points**.
- Consecutive clearing moves award an additional **50 × (combo − 1) points**.
- A placement without a clear resets the combo.
- The best score is stored under the `PlayerPrefs` key `BlockBlastBest`.

</details>

<details>
<summary><strong>Scene and prefab customization</strong></summary>

The UI, grid, and three piece instances are already placed in the scene. Edit them in Scene View or the Inspector before entering Play Mode.

```text
GameManager (BlockGame)
Canvas
  BG / CatSafeContent
  Grid (BoardGrid prefab, BlockGridView)
    Cell 0,0 ... Cell 7,7 (64 BlockCell prefab instances)
  Score / BestScore / Message
  PieceSlots
    Slot 1 / Slot 2 / Slot 3
      BlockPiece (prefab instance, 9 child Images)
  DragLayer
  GameOverPanel (hidden at startup)
  MenuButton / RestartButton
EventSystem (InputSystemUIInputModule)
Main Camera
```

- **GameManager:** assign BlockArt, Grid View, Drag Layer, three Piece Slots, three Piece Views, labels, and the Game Over panel in the Inspector.
- **Grid:** move or scale the whole board. Each cell has its own coordinate; `(0,0)` is the bottom-left corner. Keep consistent spacing using `Cell Size`.
- **`Assets/Prefabs/BlockCell.prefab`:** edit cell appearance and size.
- **`Assets/Prefabs/BoardGrid.prefab`:** edit the grid layout.
- **`Assets/Prefabs/BlockPiece.prefab`:** edit draggable block appearance. Keep nine child Images assigned to `Blocks`; gameplay reuses the three instances by updating sprites, positions, and visibility.
- **`Assets/Resources/BlockArt.asset`:** configure sprites through Inspector references. Runtime code does not use `Resources.Load`.
- Button callbacks are already wired to `BlockMenu.PlayGame` / `ToggleSound` and `BlockGame.Restart` / `ReturnToMenu`.

**Tools → Block Blast → Set Up Scenes and Prefabs** creates scene and prefab content in Edit Mode when needed. The existing scenes are already configured. The tool does not run automatically during Play/import or overwrite customized sprites.

`BlockSceneSetup` handles Editor scene setup. `BlockGameSetup` creates art and validates scene references before builds. The game does not create its Canvas, UI, or GameManager at runtime.

</details>

## Validation

The warm cat theme passes offline runtime/editor compilation, scene and prefab reference checks, and cat rendering logic checks covering all 27 board shapes, rotations, partial clears, previews, and restart. Native Play Mode and device interaction have not been verified for this theme.

See [the warm asset documentation](docs/CAT-NOOK-WARM.vi.md) for Photoshop sources and asset validation. TextMesh Pro demo content has been removed; core fonts and resources remain.
