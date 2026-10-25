using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Text;

namespace Fotocopy
{
    /// <summary>
    /// Layanan untuk mengekspor gambar dan kanvas ke file PNG, JPG, dan PDF standar industri tanpa dependensi pihak ketiga eksternal.
    /// </summary>
    public static class ExportService
    {
        /// <summary>
        /// Menyimpan gambar ke format PNG dengan kompresi lossless kualitas tinggi.
        /// </summary>
        public static void SaveAsPng(Image image, string filePath)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));
            image.Save(filePath, ImageFormat.Png);
        }

        /// <summary>
        /// Menyimpan gambar ke format JPG/JPEG dengan kualitas maksimal (95%).
        /// </summary>
        public static void SaveAsJpg(Image image, string filePath, long quality = 95L)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            if (jpgEncoder == null)
            {
                image.Save(filePath, ImageFormat.Jpeg);
                return;
            }

            using (EncoderParameters encoderParams = new EncoderParameters(1))
            {
                encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, new long[] { quality });
                image.Save(filePath, jpgEncoder, encoderParams);
            }
        }

        /// <summary>
        /// Mengekspor gambar ke dokumen PDF standar (PDF 1.4) dengan ukuran halaman presisi sesuai DPI (default 300 DPI).
        /// </summary>
        public static void SaveAsPdf(Image image, string filePath, float dpi = 300f)
        {
            if (image == null) throw new ArgumentNullException(nameof(image));

            // Kompres gambar menjadi JPEG stream kualitas tinggi untuk XObject PDF
            byte[] jpegBytes;
            using (var ms = new MemoryStream())
            {
                ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
                if (jpgEncoder != null)
                {
                    using (EncoderParameters encoderParams = new EncoderParameters(1))
                    {
                        encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, new long[] { 95L });
                        image.Save(ms, jpgEncoder, encoderParams);
                    }
                }
                else
                {
                    image.Save(ms, ImageFormat.Jpeg);
                }
                jpegBytes = ms.ToArray();
            }

            // Hitung ukuran halaman PDF dalam satuan point (72 points per inch)
            float widthPt = (image.Width * 72.0f) / dpi;
            float heightPt = (image.Height * 72.0f) / dpi;

            string widthStr = widthPt.ToString("F2", CultureInfo.InvariantCulture);
            string heightStr = heightPt.ToString("F2", CultureInfo.InvariantCulture);

            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(fs, Encoding.ASCII))
            {
                long[] offsets = new long[5];

                // Header PDF
                writer.Write(Encoding.ASCII.GetBytes("%PDF-1.4\n%\xE2\xE3\xCF\xD3\n"));

                // Objek 1: Catalog
                offsets[0] = fs.Position;
                writer.Write(Encoding.ASCII.GetBytes("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n"));

                // Objek 2: Pages tree
                offsets[1] = fs.Position;
                writer.Write(Encoding.ASCII.GetBytes("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n"));

                // Objek 3: Page definition
                offsets[2] = fs.Position;
                string pageObj = $"3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {widthStr} {heightStr}] /Contents 4 0 R /Resources << /XObject << /Im1 5 0 R >> >> >>\nendobj\n";
                writer.Write(Encoding.ASCII.GetBytes(pageObj));

                // Objek 4: Page Content Stream (menempatkan gambar di kanvas halaman)
                offsets[3] = fs.Position;
                string streamCmd = $"q\n{widthStr} 0 0 {heightStr} 0 0 cm\n/Im1 Do\nQ\n";
                byte[] streamCmdBytes = Encoding.ASCII.GetBytes(streamCmd);
                string contentObjHeader = $"4 0 obj\n<< /Length {streamCmdBytes.Length} >>\nstream\n";
                writer.Write(Encoding.ASCII.GetBytes(contentObjHeader));
                writer.Write(streamCmdBytes);
                writer.Write(Encoding.ASCII.GetBytes("\nendstream\nendobj\n"));

                // Objek 5: Image XObject (menggunakan filter /DCTDecode bawaan PDF untuk JPEG)
                offsets[4] = fs.Position;
                string imgObjHeader = $"5 0 obj\n<< /Type /XObject /Subtype /Image /Width {image.Width} /Height {image.Height} /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /DCTDecode /Length {jpegBytes.Length} >>\nstream\n";
                writer.Write(Encoding.ASCII.GetBytes(imgObjHeader));
                writer.Write(jpegBytes);
                writer.Write(Encoding.ASCII.GetBytes("\nendstream\nendobj\n"));

                // Tabel Cross-Reference (XRef)
                long xrefOffset = fs.Position;
                writer.Write(Encoding.ASCII.GetBytes("xref\n0 6\n0000000000 65535 f \n"));
                for (int i = 0; i < offsets.Length; i++)
                {
                    string entry = $"{offsets[i]:D10} 00000 n \n";
                    writer.Write(Encoding.ASCII.GetBytes(entry));
                }

                // Trailer & EOF
                string trailer = $"trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n{xrefOffset}\n%%EOF\n";
                writer.Write(Encoding.ASCII.GetBytes(trailer));
            }
        }

        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }
}
