# Block Blast

Mo project bang Unity 6000.3.21f1, doi import/compile xong, mo
`Assets/Scenes/SampleScene.unity` va bam Play > CHOI NGAY.
Co the mo `Assets/Scenes/Game.unity` de vao thang game.

UI, luoi va ba khoi deu duoc dat san trong scene, co the chinh ngay trong
Scene View/Inspector truoc khi Play. Game khong tu tao Canvas, UI hay GameManager.
Hai scene da co trong Build Settings.

## Chinh scene va prefab

Trong `Game.unity`:

```text
GameManager (BlockGame)
Canvas
  BG / TopBackgroundPanel
  Grid (BoardGrid prefab, BlockGridView)
    Cell 0,0 ... Cell 7,7 (64 BlockCell prefab instances)
  Score / BestScore / Message
  PieceSlots
    Slot 1 / Slot 2 / Slot 3
      BlockPiece (prefab instance, 9 Image con)
  DragLayer
  GameOverPanel (an luc bat dau)
  MenuButton / RestartButton
EventSystem (InputSystemUIInputModule)
Main Camera
```

- Chon **GameManager** de keo tha BlockArt, Grid View, Drag Layer, ba Piece Slots,
  ba Piece Views, cac label va Game Over panel vao Inspector.
- Chon **Grid** de di chuyen/scale toan bang. Moi o co Coordinate rieng;
  (0,0) la goc duoi trai. Giu bo cuc 8x8 deu nhau voi khoang cach `Cell Size`.
- Chinh **Assets/Prefabs/BlockCell.prefab** de doi hinh dang/kich thuoc o.
- Chinh **Assets/Prefabs/BoardGrid.prefab** de doi bo cuc luoi.
- Chinh **Assets/Prefabs/BlockPiece.prefab** de doi hinh dang block keo tha.
  Giu 9 Image con va gan chung vao truong Blocks. Game tai su dung ba instance
  trong scene; chi doi sprite, vi tri block va bat/tat block theo hinh.
- Chinh sprite trong **Assets/Resources/BlockArt.asset**. Sprite da gan truc tiep
  vao Inspector; runtime khong dung Resources.Load.
- Button On Click da gan san: BlockMenu.PlayGame/ToggleSound va
  BlockGame.Restart/ReturnToMenu.

**Tools > Block Blast > Set Up Scenes and Prefabs** la cong cu tao scene/prefab
trong Edit Mode khi can. Scene hien tai da duoc tao va gan xong, khong can chay lai.
Cong cu khong chay tu dong khi Play/import va khong tu ghi de sprite da chinh.
Neu thieu tham chieu, Console se bao ro truong can gan; build cung kiem tra scene.

- Keo tha bang chuot hoac cam ung. Khoi duoc nang len de ngon tay khong che bang.
- Bang 8x8; hang/cot day duoc xoa cung luc, khong co trong luc hay xoay khi keo.
- Moi luot co 3 khoi; dung het se cap bo moi.
- Moi o dat: 10 diem. Xoa N hang/cot: 100 x N x N diem.
- Xoa lien tiep nhan them 50 diem moi bac combo.
- Het cho dat tat ca khoi con lai: Game Over. CHOI LAI tao van moi.
- Ky luc luu bang PlayerPrefs (`BlockBlastBest`).

## Code

`BlockBoard` xu ly trang thai o, dat khoi, xoa hang/cot va mau khoi.
`BlockGridView` + `BlockCell` hien thi luoi da dat trong scene.
`BlockGame` quan ly luot choi/diem va cap nhat UI qua tham chieu Inspector.
`BlockPiece` xu ly keo tha va tai su dung cac Image cua prefab.
`BlockMenu` xu ly nut menu; `BlockArt` luu sprite.
`BlockSceneSetup` chi tao/gia tri scene trong Editor; `BlockGameSetup` tao art
va kiem tra tham chieu truoc build.

Da kiem tra bang Unity Play Mode batch tren ban sao tam: khoi tao scene,
64 o/3 khoi, raycast UI, keo tha sai/dung, scale luoi, diem, restart,
Game Over, nut Try Again va chuyen menu/game. Kiem tra batch khong danh gia
chat luong hinh anh bang mat.
