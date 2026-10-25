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

                // Fungsi mengukur selisih warna Euclidean distance terhadap warna sampel border
                Func<int, int, int, bool> matchesBgColor = (pxR, pxG, pxB) =>
                {
                    int dr = pxR - baseR;
                    int dg = pxG - baseG;
                    int db = pxB - baseB;
                    double dist = Math.Sqrt(dr * dr + dg * dg + db * db);
                    
                    // Hanya cocok jika warna benar-benar mendekati warna dasar border
                    // JANGAN gunakan aturan lum >= 225 karena akan bocor ke baju putih / highlight wajah / kerah!
                    return dist <= Math.Min(tolerance, 40.0);
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
                            rgbValues[idx] = targetB;
                            rgbValues[idx + 1] = targetG;
                            rgbValues[idx + 2] = targetR;
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
        /// Menjalankan model AI segmentasi latar belakang (U2Net / rembg offline)
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

                // Cari path executable python dengan memprioritaskan python yang memiliki modul rembg
                string pythonExe = @"C:\lara\bin\python\python-3.13\python.exe";
                if (!System.IO.File.Exists(pythonExe))
                {
                    string[] potentialPythonPaths = new string[]
                    {
                        @"C:\lara\bin\python\python-3.13\python.exe",
                        @"python.exe",
                        @"python3",
                        @"/usr/bin/python3"
                    };

                    foreach (var p in potentialPythonPaths)
                    {
                        if (System.IO.File.Exists(p) || p == "python.exe" || p == "python3")
                        {
                            pythonExe = p;
                            break;
                        }
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
                    // Beri waktu hingga 60 detik jika model pertama kali load
                    bool finished = process.WaitForExit(60000);
                    if (!finished)
                    {
                        try { process.Kill(); } catch { }
                        return null;
                    }

                    string stdout = process.StandardOutput.ReadToEnd();
                    string stderr = process.StandardError.ReadToEnd();

                    if (System.IO.File.Exists(outputPath))
                    {
                        byte[] resultBytes = System.IO.File.ReadAllBytes(outputPath);
                        using (var ms = new System.IO.MemoryStream(resultBytes))
                        {
                            return new Bitmap(ms);
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"AI process output: {stdout}; Error: {stderr}");
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

        /// <summary>
        /// Mendapatkan dimensi fisik standar dalam milimeter (mm) untuk ukuran pas foto dan foto R.
        /// </summary>
        public static (float WidthMm, float HeightMm) GetStandardDimensionsMm(string presetName)
        {
            switch ((presetName ?? "").ToUpperInvariant().Trim())
            {
                case "2X3":
                    return (21.6f, 27.9f); // Pas foto 2x3 (standar studio Indonesia)
                case "3X4":
                    return (27.9f, 38.1f); // Pas foto 3x4
                case "4X6":
                    return (38.1f, 55.9f); // Pas foto 4x6
                case "2R":
                    return (60.0f, 90.0f); // 6 x 9 cm (Wallet size)
                case "3R":
                    return (89.0f, 127.0f); // 8.9 x 12.7 cm (3.5 x 5 inch)
                case "4R":
                    return (102.0f, 152.0f); // 10.2 x 15.2 cm (4 x 6 inch)
                case "5R":
                    return (127.0f, 178.0f); // 12.7 x 17.8 cm (5 x 7 inch)
                case "6R":
                    return (152.0f, 203.0f); // 15.2 x 20.3 cm (6 x 8 inch)
                case "8R":
                    return (203.0f, 254.0f); // 20.3 x 25.4 cm (8 x 10 inch)
                case "10R":
                    return (254.0f, 305.0f); // 25.4 x 30.5 cm (10 x 12 inch)
                case "KTP":
                case "E-KTP":
                case "ID CARD":
                    return (85.6f, 53.98f); // ISO/IEC 7810 ID-1 standard KTP/SIM
                default:
                    return (0f, 0f);
            }
        }

        /// <summary>
        /// Memotong dan mengubah gambar objek sesuai ukuran preset (3x4, 4x6, 2R, 3R, 4R, dll)
        /// dengan menjaga proporsi rasio aspek (crop tengah / center-crop) dan meresize pada resolusi target (default 300 DPI).
        /// </summary>
        public static Bitmap CropToPresetSize(Image source, string presetName, int dpi = 300)
        {
            if (source == null) return null;

            var dims = GetStandardDimensionsMm(presetName);
            if (dims.WidthMm <= 0 || dims.HeightMm <= 0)
            {
                return new Bitmap(source);
            }

            int targetW = (int)Math.Round((dims.WidthMm / 25.4f) * dpi);
            int targetH = (int)Math.Round((dims.HeightMm / 25.4f) * dpi);

            // Jika gambar horizontal tapi preset vertikal, atau sebaliknya
            bool isSourceLandscape = source.Width > source.Height;
            bool isPresetLandscape = targetW > targetH;
            if (isSourceLandscape != isPresetLandscape)
            {
                // Putar orientasi target agar sesuai dengan orientasi gambar sumber
                int temp = targetW;
                targetW = targetH;
                targetH = temp;
            }

            float targetAspect = (float)targetW / targetH;
            float srcAspect = (float)source.Width / source.Height;

            int cropX = 0, cropY = 0, cropW = source.Width, cropH = source.Height;

            if (srcAspect > targetAspect)
            {
                // Sumber lebih lebar: potong kiri dan kanan secara seimbang
                cropW = (int)Math.Round(source.Height * targetAspect);
                cropX = (source.Width - cropW) / 2;
            }
            else
            {
                // Sumber lebih tinggi: potong atas dan bawah (sedikit bias ke atas untuk pas foto wajah)
                cropH = (int)Math.Round(source.Width / targetAspect);
                // Untuk pas foto manusia, kepala berada di bagian atas, beri margin 1/4 dari atas dan 3/4 dari bawah
                cropY = Math.Max(0, (int)((source.Height - cropH) * 0.25f));
                if (cropY + cropH > source.Height)
                {
                    cropY = source.Height - cropH;
                }
            }

            Rectangle srcRect = new Rectangle(cropX, cropY, cropW, cropH);

            Bitmap result = new Bitmap(targetW, targetH);
            result.SetResolution(dpi, dpi);

            using (Graphics g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.CompositingQuality = CompositingQuality.HighQuality;
                g.DrawImage(source, new Rectangle(0, 0, targetW, targetH), srcRect, GraphicsUnit.Pixel);
            }

            return result;
        }

        /// <summary>
        /// Mengatur Brightness, Contrast, dan Sharpness secara manual pada gambar.
        /// </summary>
        /// <param name="source">Gambar sumber</param>
        /// <param name="brightness">Nilai kecerahan (-100 s/d +100, 0 = normal)</param>
        /// <param name="contrast">Nilai kontras (-100 s/d +100, 0 = normal)</param>
        /// <param name="sharpness">Nilai ketajaman (0 s/d 100, 0 = tanpa penajaman)</param>
        public static Bitmap AdjustImageAdjustments(Image source, int brightness, int contrast, int sharpness)
        {
            if (source == null) return null;

            Bitmap working = new Bitmap(source);

            // 1. Terapkan Brightness dan Contrast jika tidak 0
            if (brightness != 0 || contrast != 0)
            {
                Bitmap bcResult = new Bitmap(working.Width, working.Height, PixelFormat.Format32bppArgb);
                bcResult.SetResolution(working.HorizontalResolution, working.VerticalResolution);

                float cFactor = (contrast + 100f) / 100f; // 0.0 s/d 2.0 (1.0 = normal)
                float bOffset = (brightness / 100f) + (0.5f * (1.0f - cFactor));

                ColorMatrix cm = new ColorMatrix(new float[][]
                {
                    new float[] { cFactor, 0, 0, 0, 0 },
                    new float[] { 0, cFactor, 0, 0, 0 },
                    new float[] { 0, 0, cFactor, 0, 0 },
                    new float[] { 0, 0, 0, 1, 0 },
                    new float[] { bOffset, bOffset, bOffset, 0, 1 }
                });

                using (ImageAttributes attr = new ImageAttributes())
                {
                    attr.SetColorMatrix(cm);
                    using (Graphics g = Graphics.FromImage(bcResult))
                    {
                        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                        g.DrawImage(working, new Rectangle(0, 0, working.Width, working.Height),
                            0, 0, working.Width, working.Height, GraphicsUnit.Pixel, attr);
                    }
                }

                working.Dispose();
                working = bcResult;
            }

            // 2. Terapkan Sharpness (Unsharp Masking filter 3x3 kernel) jika sharpness > 0
            if (sharpness > 0)
            {
                Bitmap sharpResult = ApplySharpnessFilter(working, sharpness);
                working.Dispose();
                working = sharpResult;
            }

            return working;
        }

        /// <summary>
        /// Filter penajaman gambar (Unsharp Mask Convolution) menggunakan LockBits untuk performa tinggi.
        /// </summary>
        public static Bitmap ApplySharpnessFilter(Bitmap source, int sharpnessAmount)
        {
            if (source == null) return null;

            int width = source.Width;
            int height = source.Height;

            Bitmap result = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            result.SetResolution(source.HorizontalResolution, source.VerticalResolution);

            BitmapData srcData = source.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            BitmapData dstData = result.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                int stride = Math.Abs(srcData.Stride);
                int bytes = stride * height;
                byte[] srcBuffer = new byte[bytes];
                byte[] dstBuffer = new byte[bytes];

                System.Runtime.InteropServices.Marshal.Copy(srcData.Scan0, srcBuffer, 0, bytes);
                // Copy isi awal buffer
                Buffer.BlockCopy(srcBuffer, 0, dstBuffer, 0, bytes);

                // Bobot kernel penajaman berdasarkan nilai sharpness (0..100)
                // Kernel 3x3:
                // [ 0,  -k,   0 ]
                // [ -k, 1+4k, -k ]
                // [ 0,  -k,   0 ]
                float k = (sharpnessAmount / 100f) * 0.75f; // rentang 0.0 s/d 0.75
                float centerWeight = 1.0f + (4f * k);

                for (int y = 1; y < height - 1; y++)
                {
                    int rowPrev = (y - 1) * stride;
                    int rowCurr = y * stride;
                    int rowNext = (y + 1) * stride;

                    for (int x = 1; x < width - 1; x++)
                    {
                        int p = rowCurr + (x * 4);
                        int pTop = rowPrev + (x * 4);
                        int pBottom = rowNext + (x * 4);
                        int pLeft = rowCurr + ((x - 1) * 4);
                        int pRight = rowCurr + ((x + 1) * 4);

                        // B, G, R
                        for (int c = 0; c < 3; c++)
                        {
                            float val = (srcBuffer[p + c] * centerWeight)
                                        - (k * (srcBuffer[pTop + c] + srcBuffer[pBottom + c] + srcBuffer[pLeft + c] + srcBuffer[pRight + c]));

                            dstBuffer[p + c] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(val)));
                        }

                        // Alpha tetap sama
                        dstBuffer[p + 3] = srcBuffer[p + 3];
                    }
                }

                System.Runtime.InteropServices.Marshal.Copy(dstBuffer, 0, dstData.Scan0, bytes);
            }
            finally
            {
                source.UnlockBits(srcData);
                result.UnlockBits(dstData);
            }

            return result;
        }

        /// <summary>
        /// Fitur Otomatis Memperbagus Foto (Auto-Enhance De-blur):
        /// Melakukan penajaman adaptive unsharp mask, perbaikan auto-contrast histogram stretching,
        /// dan perbaikan saturasi warna agar foto tidak buram/kusam dan langsung terlihat tajam & hidup.
        /// </summary>
        public static Bitmap AutoEnhancePhoto(Image source)
        {
            if (source == null) return null;

            Bitmap bmp = new Bitmap(source);
            int width = bmp.Width;
            int height = bmp.Height;

            BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
            try
            {
                int stride = Math.Abs(data.Stride);
                int bytes = stride * height;
                byte[] buffer = new byte[bytes];
                System.Runtime.InteropServices.Marshal.Copy(data.Scan0, buffer, 0, bytes);

                // 1. Hitung histogram luminance untuk mendeteksi batas minimum dan maksimum gelap-terang
                int[] hist = new int[256];
                int totalPixels = width * height;

                for (int i = 0; i < bytes; i += 4)
                {
                    int lum = (buffer[i + 2] * 299 + buffer[i + 1] * 587 + buffer[i] * 114) / 1000;
                    hist[lum]++;
                }

                // Ambil persentil 1% dan 99% untuk clipping titik hitam & putih yang optimal (Auto Levels)
                int clipLow = (int)(totalPixels * 0.015f);
                int clipHigh = (int)(totalPixels * 0.985f);

                int minVal = 0;
                int sum = 0;
                for (int i = 0; i < 256; i++)
                {
                    sum += hist[i];
                    if (sum >= clipLow) { minVal = i; break; }
                }

                int maxVal = 255;
                sum = 0;
                for (int i = 255; i >= 0; i--)
                {
                    sum += hist[i];
                    if (sum >= (totalPixels - clipHigh)) { maxVal = i; break; }
                }

                if (maxVal <= minVal) { maxVal = 255; minVal = 0; }
                float scaleRange = 255f / Math.Max(1, (maxVal - minVal));

                // 2. Terapkan Dynamic Range Stretching & sedikit saturasi warna
                for (int i = 0; i < bytes; i += 4)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        float stretched = (buffer[i + c] - minVal) * scaleRange;
                        buffer[i + c] = (byte)Math.Max(0, Math.Min(255, (int)Math.Round(stretched)));
                    }
                }

                System.Runtime.InteropServices.Marshal.Copy(buffer, 0, data.Scan0, bytes);
            }
            finally
            {
                bmp.UnlockBits(data);
            }

            // 3. Terapkan penajaman unsharp mask berkualitas tinggi (sharpness 45%) untuk menghilangkan efek buram
            Bitmap sharpened = ApplySharpnessFilter(bmp, 45);
            bmp.Dispose();

            return sharpened;
        }
    }
}
