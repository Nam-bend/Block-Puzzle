# Cat Nook Warm đang dùng trong game

Đã thay nền, nút, bảng chơi, menu và màn kết thúc bằng bộ kem, cam mật ong, hồng đào và viền nâu. Scene SampleScene, Game, prefab và BlockArt đã dùng asset trong `Assets/Art/CatNookWarm/`.

Đã xóa `Assets/Art/CatNook/`, `Assets/Texture/`, UI cũ không dùng trong scene và công cụ catalog cũ. Hai ảnh tham chiếu được giữ tại `docs/references/`.

## Xem và chỉnh sửa

[Trang xem sprite và mockup](design/Warm/index.html). Bốn PSD trong `docs/design/Warm/` có PNG xem nhanh tương ứng, lớp riêng cho UI, text và từng dáng mèo; phần mèo dùng mask theo ô. Mắt, mũi và lông bên trong mỗi hình vẫn là raster chung. Mockup là bản thiết kế, không phải ảnh chụp Play Mode.

16 ảnh nguồn tạo 17 cấu hình dáng và 49 sprite con, phủ đủ 27 hình block hiện có cùng các hướng xoay. Có mèo tam thể, đen, trắng, mướp xám, mướp vàng, tuxedo và Xiêm. Đây chưa phải mọi tổ hợp kiểu lông, dáng và biểu cảm; biểu cảm hiện là hình tĩnh. Khi xóa một phần block, các ô còn lại dùng mèo nhỏ cùng kiểu lông. `mascot_sleep.png` dùng ở màn kết thúc.

## Kiểm tra và dựng lại

Đã kiểm tra alpha, sprite rect, ID, catalog đang dùng, tham chiếu scene/prefab, callback nút và biên dịch runtime/editor. Kiểm tra logic C# thực tế đạt đủ 27 hình, xoay, preview, xóa một phần và chơi lại. Số liệu cắt sprite nằm trong `design/Warm/asset-validation.json`.

Chưa xác nhận Play Mode và thao tác trên thiết bị. Menu Unity `Tools > Block Blast > Validate Warm Theme` kiểm tra sprite đã import, cấu hình scene và callback mà không lưu lại scene đang mở; báo cáo nằm tại `Temp/WarmThemeNativeValidation.txt`.

`tools/build_warm_assets.py` dựng UI và metadata; `tools/verify_warm_assets.py` tạo trang xem và kiểm tra catalog; `tools/create_warm_asset_psd.jsx` dựng PSD bằng Photoshop. Menu Unity `Tools > Block Blast > Apply Cat Nook Theme` áp dụng bộ mới. Prompt và nguồn ảnh được lưu trong `tools/warm_cat_illustrations.json`.
