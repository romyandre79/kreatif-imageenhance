using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Fotocopy
{
    public static class ImageEnhanceService
    {
        /// <summary>
        /// Menghilangkan background warna gelap / abu-abu kusam dari scanner menjadi putih bersih (clean white background),
        /// dan menajamkan tulisan/teks agar kontras dan terbaca jelas seperti fotokopi berkualitas.
        /// </summary>
        /// <param name="source">Gambar sumber</param>
        /// <param name="whiteThreshold">Ambang batas putih (0-255). Pixel di atas nilai ini akan diputihkan (default: 195)</param>
        /// <param name="blackThreshold">Ambang batas tulisan (0-255). Pixel di bawah nilai ini akan dihitamkan (default: 120)</param>
        public static Bitmap CleanDarkBackground(Image source, byte whiteThreshold = 195, byte blackThreshold = 120)
        {
            if (source == null) return null;

            Bitmap bmp = new Bitmap(source);
            BitmapData data = null;

            try
            {
                data = bmp.LockBits(
                    new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);

                int bytes = Math.Abs(data.Stride) * bmp.Height;
                byte[] rgbValues = new byte[bytes];

                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, rgbValues, 0, bytes);

                // Proses tiap pixel 32bpp (B, G, R, A)
                for (int i = 0; i < bytes; i += 4)
                {
                    byte b = rgbValues[i];
                    byte g = rgbValues[i + 1];
                    byte r = rgbValues[i + 2];

                    // Luminance / tingkat kecerahan pixel
                    int lum = (r * 299 + g * 587 + b * 114) / 1000;

                    // Evaluasi apakah pixel ini background scanner yang kusam / abu-abu / putih kotor
                    // ataukah konten warna nyata (saturasi warna)
                    int maxChannel = Math.Max(r, Math.Max(g, b));
                    int minChannel = Math.Min(r, Math.Min(g, b));
                    int colorDiff = maxChannel - minChannel; // rendah = abu-abu/netral, tinggi = berwarna (foto, logo, stempel)

                    if (lum >= whiteThreshold && colorDiff < 30)
                    {
                        // Background abu-abu / kotor scanner diputihkan bersih (255, 255, 255)
                        rgbValues[i] = 255;
                        rgbValues[i + 1] = 255;
                        rgbValues[i + 2] = 255;
                    }
                    else if (lum <= blackThreshold && colorDiff < 25)
                    {
                        // Teks atau garis netral dipertegas/dipertajam (dihitamkan)
                        rgbValues[i] = (byte)(b * 0.75);
                        rgbValues[i + 1] = (byte)(g * 0.75);
                        rgbValues[i + 2] = (byte)(r * 0.75);
                    }
                    else
                    {
                        // Bagian berwarna (foto KTP, logo, teks berwarna) atau kontras bertingkat:
                        // Naikkan kontras dan kecerahan sedikit namun TETAP PERTAHANKAN WARNA ASLI (RGB)!
                        double boost = 1.0 + (0.15 * (lum - blackThreshold) / Math.Max(1, (whiteThreshold - blackThreshold)));
                        boost = Math.Max(0.9, Math.Min(1.25, boost));

                        rgbValues[i] = (byte)Math.Min(255, (int)(b * boost));
                        rgbValues[i + 1] = (byte)Math.Min(255, (int)(g * boost));
                        rgbValues[i + 2] = (byte)Math.Min(255, (int)(r * boost));
                    }
                }

                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, data.Scan0, bytes);
            }
            finally
            {
                if (data != null)
                {
                    bmp.UnlockBits(data);
                }
            }

            return bmp;
        }

        /// <summary>
        /// Otomatis memotong (auto-crop) area kosong di tepi gambar jika tidak ada lagi tulisan atau objek.
        /// </summary>
        /// <param name="source">Gambar sumber</param>
        /// <param name="blankThreshold">Toleransi kecerahan background kosong (0-255, default: 242)</param>
        /// <param name="padding">Jarak margin ekstra di sekeliling teks (default: 30 pixel)</param>
        public static Bitmap AutoCropContent(Image source, byte blankThreshold = 242, int padding = 30)
        {
            if (source == null) return null;

            using (Bitmap temp = new Bitmap(source))
            {
                BitmapData data = temp.LockBits(
                    new Rectangle(0, 0, temp.Width, temp.Height),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);

                int width = temp.Width;
                int height = temp.Height;
                int stride = Math.Abs(data.Stride);
                int bytes = stride * height;
                byte[] rgbValues = new byte[bytes];

                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, rgbValues, 0, bytes);
                temp.UnlockBits(data);

                int minX = width;
                int minY = height;
                int maxX = 0;
                int maxY = 0;
                bool foundContent = false;

                // Step scan (sampling) untuk kecepatan tinggi
                int step = 2;

                for (int y = 0; y < height; y += step)
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < width; x += step)
                    {
                        int idx = rowOffset + (x * 4);
                        byte b = rgbValues[idx];
                        byte g = rgbValues[idx + 1];
                        byte r = rgbValues[idx + 2];

                        int gray = (r * 299 + g * 587 + b * 114) / 1000;

                        // Jika pixel tidak putih kosong (artinya ada tulisan/garis/objek)
                        if (gray < blankThreshold)
                        {
                            foundContent = true;
                            if (x < minX) minX = x;
                            if (x > maxX) maxX = x;
                            if (y < minY) minY = y;
                            if (y > maxY) maxY = y;
                        }
                    }
                }

                // Jika seluruh halaman kosong atau konten terdeteksi seluruhnya
                if (!foundContent || minX >= maxX || minY >= maxY)
                {
                    return new Bitmap(source);
                }

                // Tambahkan padding
                minX = Math.Max(0, minX - padding);
                minY = Math.Max(0, minY - padding);
                maxX = Math.Min(width - 1, maxX + padding);
                maxY = Math.Min(height - 1, maxY + padding);

                int cropW = maxX - minX + 1;
                int cropH = maxY - minY + 1;

                if (cropW <= 10 || cropH <= 10)
                {
                    return new Bitmap(source);
                }

                Bitmap cropped = new Bitmap(cropW, cropH);
                using (Graphics g = Graphics.FromImage(cropped))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(source, new Rectangle(0, 0, cropW, cropH), new Rectangle(minX, minY, cropW, cropH), GraphicsUnit.Pixel);
                }

                return cropped;
            }
        }

        /// <summary>
        /// Mengganti warna background dokumen / pas foto menggunakan Model AI Deep Learning (U2Net / remove.bg).
        /// Jika Python / AI tidak tersedia, otomatis fallback ke algoritma Border Flood Fill.
        /// </summary>
        public static Bitmap ReplaceBackgroundColor(Image source, Color newBgColor, int tolerance = 48)
        {
            if (source == null) return null;

            // 1. Coba proses dengan Mesin AI Deep Learning (rembg)
            try
            {
                Bitmap aiResult = ReplaceBackgroundColorWithAI(source, newBgColor);
                if (aiResult != null)
                {
                    return aiResult;
                }
            }
            catch { }

            // 2. Fallback jika Python / AI tidak tersedia
            int width = source.Width;
            int height = source.Height;
            Bitmap bmp = new Bitmap(source);
            BitmapData data = null;

            try
            {
                data = bmp.LockBits(
                    new Rectangle(0, 0, width, height),
                    ImageLockMode.ReadWrite,
                    PixelFormat.Format32bppArgb);

                int stride = Math.Abs(data.Stride);
                int bytes = stride * height;
                byte[] rgbValues = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, rgbValues, 0, bytes);

                // 1. Sampling warna background dari 4 sudut gambar dan sepanjang tepi luar (border)
                long sumR = 0, sumG = 0, sumB = 0;
                int sampleCount = 0;

                // Fungsi lokal untuk ambil pixel
                Func<int, int, Color> getPixel = (x, y) =>
                {
                    int idx = (y * stride) + (x * 4);
                    return Color.FromArgb(rgbValues[idx + 2], rgbValues[idx + 1], rgbValues[idx]);
                };

                // Sample tepian luar (Top, Bottom, Left, Right)
                for (int x = 0; x < width; x += Math.Max(1, width / 20))
                {
                    Color cTop = getPixel(x, 0);
                    Color cBottom = getPixel(x, height - 1);
                    sumR += cTop.R + cBottom.R;
                    sumG += cTop.G + cBottom.G;
                    sumB += cTop.B + cBottom.B;
                    sampleCount += 2;
                }
                for (int y = 0; y < height; y += Math.Max(1, height / 20))
                {
                    Color cLeft = getPixel(0, y);
                    Color cRight = getPixel(width - 1, y);
                    sumR += cLeft.R + cRight.R;
                    sumG += cLeft.G + cRight.G;
                    sumB += cLeft.B + cRight.B;
                    sampleCount += 2;
                }

                int baseR = (int)(sumR / Math.Max(1, sampleCount));
                int baseG = (int)(sumG / Math.Max(1, sampleCount));
                int baseB = (int)(sumB / Math.Max(1, sampleCount));

                // 2. Tandai area background menggunakan BFS Flood Fill dari seluruh tepian luar
                bool[,] isBackground = new bool[width, height];
                var queue = new System.Collections.Generic.Queue<Point>();

                // Fungsi mengukur selisih warna Euclidean distance
                Func<int, int, int, bool> matchesBgColor = (pxR, pxG, pxB) =>
                {
                    int dr = pxR - baseR;
                    int dg = pxG - baseG;
                    int db = pxB - baseB;
                    double dist = Math.Sqrt(dr * dr + dg * dg + db * db);
                    
                    // Juga cocok jika pixel sangat terang / putih (dokumen scanner putih)
                    int lum = (pxR * 299 + pxG * 587 + pxB * 114) / 1000;
                    return (dist <= tolerance) || (lum >= 225 && Math.Abs(pxR - pxG) < 20);
                };

                // Masukkan seluruh batas tepi luar ke queue jika cocok dengan warna latar belakang
                for (int x = 0; x < width; x++)
                {
                    Color cTop = getPixel(x, 0);
                    if (matchesBgColor(cTop.R, cTop.G, cTop.B))
                    {
                        isBackground[x, 0] = true;
                        queue.Enqueue(new Point(x, 0));
                    }
                    Color cBottom = getPixel(x, height - 1);
                    if (matchesBgColor(cBottom.R, cBottom.G, cBottom.B))
                    {
                        isBackground[x, height - 1] = true;
                        queue.Enqueue(new Point(x, height - 1));
                    }
                }

                for (int y = 0; y < height; y++)
                {
                    Color cLeft = getPixel(0, y);
                    if (matchesBgColor(cLeft.R, cLeft.G, cLeft.B) && !isBackground[0, y])
                    {
                        isBackground[0, y] = true;
                        queue.Enqueue(new Point(0, y));
                    }
                    Color cRight = getPixel(width - 1, y);
                    if (matchesBgColor(cRight.R, cRight.G, cRight.B) && !isBackground[width - 1, y])
                    {
                        isBackground[width - 1, y] = true;
                        queue.Enqueue(new Point(width - 1, y));
                    }
                }

                // 3. BFS traversal: menelusuri ke dalam hanya pixel yang tersambung ke latar belakang
                int[] dx = { 0, 0, 1, -1 };
                int[] dy = { 1, -1, 0, 0 };

                while (queue.Count > 0)
                {
                    Point pt = queue.Dequeue();

                    for (int d = 0; d < 4; d++)
                    {
                        int nx = pt.X + dx[d];
                        int ny = pt.Y + dy[d];

                        if (nx >= 0 && nx < width && ny >= 0 && ny < height && !isBackground[nx, ny])
                        {
                            int idx = (ny * stride) + (nx * 4);
                            byte b = rgbValues[idx];
                            byte g = rgbValues[idx + 1];
                            byte r = rgbValues[idx + 2];

                            if (matchesBgColor(r, g, b))
                            {
                                isBackground[nx, ny] = true;
                                queue.Enqueue(new Point(nx, ny));
                            }
                        }
                    }
                }

                // 4. Ubah warna hanya pada pixel yang terdeteksi sebagai background luar
                byte targetB = newBgColor.B;
                byte targetG = newBgColor.G;
                byte targetR = newBgColor.R;

                for (int y = 0; y < height; y++)
                {
                    int rowOffset = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        if (isBackground[x, y])
                        {
                            int idx = rowOffset + (x * 4);
                            byte b = rgbValues[idx];
                            byte g = rgbValues[idx + 1];
                            byte r = rgbValues[idx + 2];

                            // Hitung jarak ke batas untuk antialiasing halus di tepi objek
                            int dr = r - baseR;
                            int dg = g - baseG;
                            int db = b - baseB;
                            double dist = Math.Sqrt(dr * dr + dg * dg + db * db);

                            float blend = (float)(dist / Math.Max(1.0, tolerance));
                            blend = Math.Max(0f, Math.Min(1f, blend));

                            // Ganti warna background
                            rgbValues[idx] = (byte)(targetB * (1 - blend * 0.3f) + b * (blend * 0.3f));
                            rgbValues[idx + 1] = (byte)(targetG * (1 - blend * 0.3f) + g * (blend * 0.3f));
                            rgbValues[idx + 2] = (byte)(targetR * (1 - blend * 0.3f) + r * (blend * 0.3f));
                        }
                    }
                }

                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, data.Scan0, bytes);
            }
            finally
            {
                if (data != null) bmp.UnlockBits(data);
            }

            return bmp;
        }

        /// <summary>
        /// Memotong gambar sesuai koordinat kotak area pilihan manual pengguna.
        /// </summary>
        /// <summary>
        /// Menjalankan model AI segmentasi latar belakang (U2Net / remove.bg offline)
        /// </summary>
        private static Bitmap ReplaceBackgroundColorWithAI(Image source, Color newBgColor)
        {
            string tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "FotocopyAI");
            if (!System.IO.Directory.Exists(tempDir))
            {
                System.IO.Directory.CreateDirectory(tempDir);
            }

            string inputPath = System.IO.Path.Combine(tempDir, $"in_{Guid.NewGuid():N}.png");
            string outputPath = System.IO.Path.Combine(tempDir, $"out_{Guid.NewGuid():N}.png");

            try
            {
                // Simpan image ke file sementara
                source.Save(inputPath, ImageFormat.Png);

                // Cari lokasi change_bg.py
                string scriptPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "change_bg.py");
                if (!System.IO.File.Exists(scriptPath))
                {
                    scriptPath = @"c:\lara\www\Fotocopy\change_bg.py";
                }

                if (!System.IO.File.Exists(scriptPath)) return null;

                // Cari path executable python (Windows & Linux)
                string pythonExe = DeviceServiceFactory.IsLinuxPlatform() ? "python3" : "python.exe";
                string[] potentialPythonPaths = new string[]
                {
                    @"/usr/bin/python3",
                    @"python3",
                    @"C:\lara\bin\python\python-3.13\python.exe",
                    @"python.exe"
                };

                foreach (var p in potentialPythonPaths)
                {
                    if (System.IO.File.Exists(p))
                    {
                        pythonExe = p;
                        break;
                    }
                }

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = pythonExe,
                    Arguments = $"\"{scriptPath}\" \"{inputPath}\" \"{outputPath}\" {newBgColor.R} {newBgColor.G} {newBgColor.B}",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    process.WaitForExit(30000); // batas timeout 30 detik
                    string output = process.StandardOutput.ReadToEnd();

                    if (System.IO.File.Exists(outputPath))
                    {
                        byte[] resultBytes = System.IO.File.ReadAllBytes(outputPath);
                        using (var ms = new System.IO.MemoryStream(resultBytes))
                        {
                            return new Bitmap(ms);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AI Background Error: {ex.Message}");
            }
            finally
            {
                try { if (System.IO.File.Exists(inputPath)) System.IO.File.Delete(inputPath); } catch { }
                try { if (System.IO.File.Exists(outputPath)) System.IO.File.Delete(outputPath); } catch { }
            }

            return null;
        }

        public static Bitmap CropByRectangle(Image source, Rectangle cropArea)
        {
            if (source == null) return null;
            if (cropArea.Width <= 0 || cropArea.Height <= 0) return new Bitmap(source);

            // Batasi ke dimensi gambar asli
            int x = Math.Max(0, Math.Min(cropArea.X, source.Width - 1));
            int y = Math.Max(0, Math.Min(cropArea.Y, source.Height - 1));
            int w = Math.Min(cropArea.Width, source.Width - x);
            int h = Math.Min(cropArea.Height, source.Height - y);

            if (w <= 0 || h <= 0) return new Bitmap(source);

            Bitmap cropped = new Bitmap(w, h);
            using (Graphics g = Graphics.FromImage(cropped))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(source, new Rectangle(0, 0, w, h), new Rectangle(x, y, w, h), GraphicsUnit.Pixel);
            }

            return cropped;
        }
    }
}
