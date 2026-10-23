import sys
import os
from PIL import Image
from rembg import remove, new_session

# Gunakan session model u2netp (sangat ringan ~4.5MB dan cepat)
session = new_session("u2netp")

def change_bg(input_path, output_path, r, g, b):
    try:
        # Buka gambar asli
        img = Image.open(input_path)
        
        # Ekstrak objek utama menggunakan AI model u2netp
        nobg = remove(img, session=session)
        
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

if __name__ == "__main__":
    if len(sys.argv) >= 6:
        in_p = sys.argv[1]
        out_p = sys.argv[2]
        red = int(sys.argv[3])
        green = int(sys.argv[4])
        blue = int(sys.argv[5])
        change_bg(in_p, out_p, red, green, blue)
    else:
        print("Usage: change_bg.py <input> <output> <r> <g> <b>")
