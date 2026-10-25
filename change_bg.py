import sys
import os
from PIL import Image
from rembg import remove, new_session

# Model session: gunakan u2net (akurasi tinggi, utuh memotong hewan, manusia & benda tanpa hilang)
# dan fallback otomatis jika memori terbatas
try:
    session = new_session("u2net")
except Exception:
    session = new_session("u2netp")

def change_bg(input_path, output_path, r, g, b):
    try:
        # Buka gambar asli
        img = Image.open(input_path)
        
        # Ekstrak objek utama menggunakan AI model u2net dengan post_process_mask untuk menjaga keutuhan telinga, rambut, dan sudut objek
        nobg = remove(img, session=session, post_process_mask=True)
        
        # Buat canvas warna baru (RGBA)
        bg = Image.new("RGBA", nobg.size, (r, g, b, 255))
        
        # Gabungkan objek di atas background warna baru
        bg.paste(nobg, (0, 0), nobg)
        
        # Simpan hasil dalam format PNG kualitas tinggi
        result = bg.convert("RGB")
        result.save(output_path, "PNG")
        print("SUCCESS")
    except Exception as e:
        print(f"ERROR: {str(e)}")

PRESET_DIMS = {
    "2x3": (21.6, 27.9),
    "3x4": (27.9, 38.1),
    "4x6": (38.1, 55.9),
    "2r": (60.0, 90.0),
    "3r": (89.0, 127.0),
    "4r": (102.0, 152.0),
    "5r": (127.0, 178.0),
    "6r": (152.0, 203.0),
    "8r": (203.0, 254.0),
    "10r": (254.0, 305.0),
    "ktp": (85.6, 53.98)
}

def crop_preset(input_path, output_path, preset_name, dpi=300):
    try:
        preset_key = preset_name.lower().strip()
        if preset_key not in PRESET_DIMS:
            print(f"ERROR: Unknown preset '{preset_name}'")
            return

        w_mm, h_mm = PRESET_DIMS[preset_key]
        target_w = int(round((w_mm / 25.4) * dpi))
        target_h = int(round((h_mm / 25.4) * dpi))

        img = Image.open(input_path)
        is_src_landscape = img.width > img.height
        is_preset_landscape = target_w > target_h

        if is_src_landscape != is_preset_landscape:
            target_w, target_h = target_h, target_w

        target_aspect = target_w / target_h
        src_aspect = img.width / img.height

        if src_aspect > target_aspect:
            crop_w = int(round(img.height * target_aspect))
            crop_x = (img.width - crop_w) // 2
            crop_y = 0
            crop_h = img.height
        else:
            crop_h = int(round(img.width / target_aspect))
            crop_x = 0
            crop_w = img.width
            crop_y = max(0, int((img.height - crop_h) * 0.25))
            if crop_y + crop_h > img.height:
                crop_y = img.height - crop_h

        cropped = img.crop((crop_x, crop_y, crop_x + crop_w, crop_y + crop_h))
        resized = cropped.resize((target_w, target_h), Image.Resampling.LANCZOS)
        resized.save(output_path, "PNG", dpi=(dpi, dpi))
        print("SUCCESS")
    except Exception as e:
        print(f"ERROR: {str(e)}")

if __name__ == "__main__":
    if len(sys.argv) >= 4 and sys.argv[1] == "--preset":
        # python change_bg.py --preset <preset_code> <input> <output>
        p_code = sys.argv[2]
        in_p = sys.argv[3]
        out_p = sys.argv[4]
        crop_preset(in_p, out_p, p_code)
    elif len(sys.argv) >= 6:
        in_p = sys.argv[1]
        out_p = sys.argv[2]
        red = int(sys.argv[3])
        green = int(sys.argv[4])
        blue = int(sys.argv[5])
        change_bg(in_p, out_p, red, green, blue)
    else:
        print("Usage: change_bg.py <input> <output> <r> <g> <b>")
        print("   or: change_bg.py --preset <2x3|3x4|4x6|2r|3r|4r|ktp> <input> <output>")
