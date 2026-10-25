using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fotocopy.Properties;

namespace Fotocopy
{
    public partial class Form1 : Form
    {
        private IScannerService _scannerService;
        private IPrinterService _printerService;

        // Image state
        private Image _rawScannedImage;          // Gambar asli persis dari scanner
        private Image _enhancedImage;            // Gambar setelah dibersihkan / di-crop
        private Image _renderedPreviewImage;     // Gambar preview layout kertas multi-copy

        // Workflow state
        private bool _isCopyRequested;
        private bool _isPreviewScanRequested;

        // Area selection state on PictureBox
        private bool _isSelecting = false;
        private Point _selectionStart;
        private Rectangle _selectionRect = Rectangle.Empty;

        // Undo & Redo history stacks
        private readonly System.Collections.Generic.Stack<Bitmap> _undoHistory = new System.Collections.Generic.Stack<Bitmap>();
        private readonly System.Collections.Generic.Stack<Bitmap> _redoHistory = new System.Collections.Generic.Stack<Bitmap>();

        private void PushUndoState(Image current)
        {
            if (current == null) return;
            try
            {
                // Simpan salinan bitmap ke stack undo
                _undoHistory.Push(new Bitmap(current));
                // Bersihkan redo stack setiap ada aksi baru
                while (_redoHistory.Count > 0)
                {
                    _redoHistory.Pop()?.Dispose();
                }
                UpdateUndoRedoButtonState();
            }
            catch { }
        }

        private void ClearUndoRedo()
        {
            while (_undoHistory.Count > 0) _undoHistory.Pop()?.Dispose();
            while (_redoHistory.Count > 0) _redoHistory.Pop()?.Dispose();
            UpdateUndoRedoButtonState();
        }

        private void UpdateUndoRedoButtonState()
        {
            btnUndo.Enabled = (_undoHistory.Count > 0);
            btnRedo.Enabled = (_redoHistory.Count > 0);
        }

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);
        private const int WM_SETICON = 0x0080;
        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        public Form1()
        {
            InitializeComponent();
            ApplyAppIcon();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyAppIcon();
        }

        private void ApplyAppIcon()
        {
            try
            {
                this.ShowIcon = true;

                Icon loadedIcon = null;

                // 1. Coba muat dari app.ico di folder aplikasi atau direktori project
                string[] searchPaths = new string[]
                {
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico"),
                    System.IO.Path.Combine(Application.StartupPath, "app.ico"),
                    @"c:\lara\www\Fotocopy\app.ico"
                };

                foreach (string p in searchPaths)
                {
                    if (System.IO.File.Exists(p))
                    {
                        using (var stream = new System.IO.FileStream(p, System.IO.FileMode.Open, System.IO.FileAccess.Read))
                        {
                            loadedIcon = new Icon(stream);
                            break;
                        }
                    }
                }

                // 2. Jika belum, coba ekstrak dari executable .exe
                if (loadedIcon == null)
                {
                    string exePath = Application.ExecutablePath;
                    if (System.IO.File.Exists(exePath))
                    {
                        loadedIcon = Icon.ExtractAssociatedIcon(exePath);
                    }
                }

                // 3. Fallback buat icon dari rafa-atk.jpg
                if (loadedIcon == null)
                {
                    string jpgPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rafa-atk.jpg");
                    if (!System.IO.File.Exists(jpgPath)) jpgPath = @"c:\lara\www\Fotocopy\rafa-atk.jpg";

                    if (System.IO.File.Exists(jpgPath))
                    {
                        using (Bitmap bmp = new Bitmap(jpgPath))
                        {
                            IntPtr hIcon = bmp.GetHicon();
                            loadedIcon = Icon.FromHandle(hIcon);
                        }
                    }
                }

                if (loadedIcon != null)
                {
                    this.Icon = loadedIcon;
                    if (this.IsHandleCreated)
                    {
                        SendMessage(this.Handle, WM_SETICON, (IntPtr)ICON_SMALL, loadedIcon.Handle);
                        SendMessage(this.Handle, WM_SETICON, (IntPtr)ICON_BIG, loadedIcon.Handle);
                    }
                }
            }
            catch { }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                _printerService = DeviceServiceFactory.CreatePrinterService();
            }
            catch (Exception ex)
            {
                UpdateStatus($"Inisialisasi printer service gagal: {ex.Message}");
            }

            try
            {
                _scannerService = DeviceServiceFactory.CreateScannerService(this.Handle);
                _scannerService.ImageScanned += ScannerService_ImageScanned;
                _scannerService.StatusChanged += ScannerService_StatusChanged;
                _scannerService.ErrorOccurred += ScannerService_ErrorOccurred;
            }
            catch (Exception ex)
            {
                UpdateStatus($"Inisialisasi scanner gagal: {ex.Message}");
            }

            SetupNumericControlSync();
            SetupPhotoPresetsMenu();
            LoadSavedSettings();
            RefreshDeviceList();
            UpdatePreviewDisplay();
        }

        private void LoadSavedSettings()
        {
            try
            {
                if (Settings.Default != null)
                {
                    numCopies.Value = Math.Max(1, Math.Min(99, Settings.Default.Copies));
                    chkFitPage.Checked = Settings.Default.PaperFit;
                    chkShowScannerUI.Checked = Settings.Default.ShowScannerUI;
                    chkCleanDarkBg.Checked = Settings.Default.CleanDarkBg;
                    chkAutoCrop.Checked = Settings.Default.AutoCrop;

                    int items = Settings.Default.ItemsPerSheet;
                    numItemsPerSheet.Value = Math.Max(1, Math.Min(100, items > 0 ? items : 1));

                    string savedPaper = Settings.Default.PaperSize ?? "A4";
                    SelectPaperSizeCombo(savedPaper);

                    numMarginTop.Value = Math.Max(0, Math.Min(100, Settings.Default.MarginTop));
                    numMarginBottom.Value = Math.Max(0, Math.Min(100, Settings.Default.MarginBottom));
                    numMarginLeft.Value = Math.Max(0, Math.Min(100, Settings.Default.MarginLeft));
                    numMarginRight.Value = Math.Max(0, Math.Min(100, Settings.Default.MarginRight));

                    int savedScale = Settings.Default.ImageScale;
                    numScale.Value = Math.Max(10, Math.Min(300, savedScale > 0 ? savedScale : 100));

                    int gapX = Settings.Default.GapX;
                    numGapX.Value = Math.Max(0, Math.Min(100, gapX >= 0 ? gapX : 3));

                    int gapY = Settings.Default.GapY;
                    numGapY.Value = Math.Max(0, Math.Min(100, gapY >= 0 ? gapY : 3));
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"Gagal memuat preferensi: {ex.Message}");
            }
        }

        private void SelectPaperSizeCombo(string paperName)
        {
            if (string.IsNullOrEmpty(paperName)) { cbPaperSize.SelectedIndex = 0; return; }

            for (int i = 0; i < cbPaperSize.Items.Count; i++)
            {
                if (cbPaperSize.Items[i].ToString().StartsWith(paperName, StringComparison.OrdinalIgnoreCase))
                {
                    cbPaperSize.SelectedIndex = i;
                    return;
                }
            }
            cbPaperSize.SelectedIndex = 0;
        }

        private string GetSelectedPaperSizeName()
        {
            string selected = cbPaperSize.SelectedItem?.ToString() ?? "A4";
            if (selected.StartsWith("F4", StringComparison.OrdinalIgnoreCase) || selected.StartsWith("Folio", StringComparison.OrdinalIgnoreCase)) return "F4";
            if (selected.StartsWith("Letter", StringComparison.OrdinalIgnoreCase)) return "Letter";
            if (selected.StartsWith("Legal", StringComparison.OrdinalIgnoreCase)) return "Legal";
            if (selected.StartsWith("A3", StringComparison.OrdinalIgnoreCase)) return "A3";
            if (selected.StartsWith("A5", StringComparison.OrdinalIgnoreCase)) return "A5";
            return "A4";
        }

        private int GetCurrentItemsPerSheet()
        {
            return (int)numItemsPerSheet.Value;
        }

        private void RefreshDeviceList()
        {
            UpdateStatus("Mencari scanner dan printer yang terhubung...");

            // 1. Scanners
            cbScanner.Items.Clear();
            var scanners = _scannerService != null ? _scannerService.GetScanners() : new List<string>();
            if (scanners != null)
            {
                foreach (var scanner in scanners)
                {
                    if (!string.IsNullOrEmpty(scanner))
                    {
                        cbScanner.Items.Add(scanner);
                    }
                }
            }

            if (cbScanner.Items.Count > 0)
            {
                string savedScanner = null;
                try { savedScanner = Settings.Default?.SelectedScanner; } catch { }

                int idx = !string.IsNullOrEmpty(savedScanner) ? cbScanner.FindStringExact(savedScanner) : -1;
                cbScanner.SelectedIndex = idx >= 0 ? idx : 0;
            }
            else
            {
                cbScanner.Items.Add("(Tidak ada scanner terdeteksi)");
                cbScanner.SelectedIndex = 0;
            }

            // 2. Printers
            cbPrinter.Items.Clear();
            var printers = _printerService != null ? _printerService.GetPrinters() : new List<string>();
            if (printers != null)
            {
                foreach (var printer in printers)
                {
                    if (!string.IsNullOrEmpty(printer))
                    {
                        cbPrinter.Items.Add(printer);
                    }
                }
            }

            if (cbPrinter.Items.Count > 0)
            {
                string savedPrinter = null;
                try { savedPrinter = Settings.Default?.SelectedPrinter; } catch { }

                int pIdx = !string.IsNullOrEmpty(savedPrinter) ? cbPrinter.FindStringExact(savedPrinter) : -1;
                if (pIdx >= 0)
                {
                    cbPrinter.SelectedIndex = pIdx;
                }
                else
                {
                    string defPrinter = _printerService != null ? _printerService.GetDefaultPrinter() : string.Empty;
                    int defIdx = !string.IsNullOrEmpty(defPrinter) ? cbPrinter.FindStringExact(defPrinter) : -1;
                    cbPrinter.SelectedIndex = defIdx >= 0 ? defIdx : 0;
                }
            }
            else
            {
                cbPrinter.Items.Add("(Tidak ada printer terpasang)");
                cbPrinter.SelectedIndex = 0;
            }

            int scanCount = scanners != null ? scanners.Count : 0;
            int printCount = printers != null ? printers.Count : 0;
            UpdateStatus($"Siap. Scanner: {scanCount} ditemukan, Printer: {printCount} ditemukan.");
        }

        private void btnRefreshDevices_Click(object sender, EventArgs e)
        {
            RefreshDeviceList();
        }

        private void btnScannerProp_Click(object sender, EventArgs e)
        {
            if (_scannerService == null)
            {
                MessageBox.Show("Layanan scanner belum terinisialisasi.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedScanner = cbScanner.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedScanner) || selectedScanner.StartsWith("("))
            {
                MessageBox.Show("Silakan pilih scanner yang valid terlebih dahulu.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UpdateStatus($"Membuka dialog konfigurasi scanner '{selectedScanner}'...");
            _scannerService.ShowScannerProperties(selectedScanner);
        }

        private void btnPrinterProp_Click(object sender, EventArgs e)
        {
            if (_printerService == null)
            {
                MessageBox.Show("Layanan printer belum siap.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedPrinter = cbPrinter.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedPrinter) || selectedPrinter.StartsWith("("))
            {
                MessageBox.Show("Silakan pilih printer yang valid terlebih dahulu.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            UpdateStatus($"Membuka properties printer '{selectedPrinter}'...");
            _printerService.ShowPrinterProperties(selectedPrinter, this.Handle);
        }

        private void btnSaveSettings_Click(object sender, EventArgs e)
        {
            try
            {
                if (Settings.Default == null) return;

                string scanner = cbScanner.SelectedItem?.ToString();
                if (scanner != null && !scanner.StartsWith("("))
                {
                    Settings.Default.SelectedScanner = scanner;
                }

                string printer = cbPrinter.SelectedItem?.ToString();
                if (printer != null && !printer.StartsWith("("))
                {
                    Settings.Default.SelectedPrinter = printer;
                }

                Settings.Default.Copies = (int)numCopies.Value;
                Settings.Default.PaperFit = chkFitPage.Checked;
                Settings.Default.ShowScannerUI = chkShowScannerUI.Checked;
                Settings.Default.CleanDarkBg = chkCleanDarkBg.Checked;
                Settings.Default.AutoCrop = chkAutoCrop.Checked;
                Settings.Default.PaperSize = GetSelectedPaperSizeName();
                Settings.Default.MarginTop = (int)numMarginTop.Value;
                Settings.Default.MarginBottom = (int)numMarginBottom.Value;
                Settings.Default.MarginLeft = (int)numMarginLeft.Value;
                Settings.Default.MarginRight = (int)numMarginRight.Value;
                Settings.Default.ImageScale = (int)numScale.Value;
                Settings.Default.ItemsPerSheet = GetCurrentItemsPerSheet();
                Settings.Default.GapX = (int)numGapX.Value;
                Settings.Default.GapY = (int)numGapY.Value;
                Settings.Default.Save();

                MessageBox.Show(
                    "Pengaturan koneksi scanner, printer, ukuran kertas, margin, skala gambar, jarak antar foto, dan pengganda gambar berhasil disimpan!",
                    "Pengaturan Tersimpan",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                UpdateStatus("Pengaturan dan preferensi kertas berhasil disimpan.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan pengaturan: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void numItemsPerSheet_ValueChanged(object sender, EventArgs e)
        {
            UpdatePreviewDisplay();
        }

        private void numGap_ValueChanged(object sender, EventArgs e)
        {
            UpdatePreviewDisplay();
        }

        private void cbPaperSize_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdatePreviewDisplay();
        }

        private bool _isUpdatingNumeric = false;

        private void SetupNumericControlSync()
        {
            NumericUpDown[] controls = new NumericUpDown[] { numMarginTop, numMarginBottom, numMarginLeft, numMarginRight, numScale, numCopies, numItemsPerSheet, numGapX, numGapY };
            foreach (var num in controls)
            {
                num.InterceptArrowKeys = true;
                
                num.KeyUp += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter)
                    {
                        CommitNumericInput(s as NumericUpDown);
                        e.Handled = true;
                        e.SuppressKeyPress = true;
                    }
                };
                num.Leave += (s, e) =>
                {
                    CommitNumericInput(s as NumericUpDown);
                };
            }
        }

        private void CommitNumericInput(NumericUpDown num)
        {
            if (num == null || _isUpdatingNumeric) return;
            try
            {
                if (decimal.TryParse(num.Text, out decimal val))
                {
                    decimal clamped = Math.Max(num.Minimum, Math.Min(num.Maximum, val));
                    if (num.Value != clamped)
                    {
                        _isUpdatingNumeric = true;
                        num.Value = clamped;
                        _isUpdatingNumeric = false;
                        UpdatePreviewDisplay();
                    }
                }
            }
            catch
            {
                _isUpdatingNumeric = false;
            }
        }

        private void numMargin_ValueChanged(object sender, EventArgs e)
        {
            UpdatePreviewDisplay();
        }

        private void numScale_ValueChanged(object sender, EventArgs e)
        {
            UpdatePreviewDisplay();
        }

        private void chkFitPage_CheckedChanged(object sender, EventArgs e)
        {
            UpdatePreviewDisplay();
        }

        private void chkCleanDarkBg_CheckedChanged(object sender, EventArgs e)
        {
            ReprocessScannedImage();
        }

        private void chkAutoCrop_CheckedChanged(object sender, EventArgs e)
        {
            ReprocessScannedImage();
        }

        /// <summary>
        /// Mengulang proses pembersihan background atau auto-crop dari gambar scan asli
        /// </summary>
        private void ReprocessScannedImage()
        {
            if (_rawScannedImage == null) return;

            Image processed = new Bitmap(_rawScannedImage);

            // 1. Hilangkan background gelap / abu-abu scanner
            if (chkCleanDarkBg.Checked)
            {
                var cleaned = ImageEnhanceService.CleanDarkBackground(processed);
                processed.Dispose();
                processed = cleaned;
            }

            // 2. Potong otomatis tepi gambar jika tulisan sudah habis
            if (chkAutoCrop.Checked)
            {
                var cropped = ImageEnhanceService.AutoCropContent(processed);
                processed.Dispose();
                processed = cropped;
            }

            _enhancedImage?.Dispose();
            _enhancedImage = processed;

            _selectionRect = Rectangle.Empty;
            btnCropSelection.Enabled = false;
            btnResetCrop.Enabled = (_rawScannedImage != null);

            UpdatePreviewDisplay();
        }

        private void UpdatePreviewDisplay()
        {
            try
            {
                int count = GetCurrentItemsPerSheet();
                string paperName = GetSelectedPaperSizeName();

                // Hitung ukuran pixel kanvas kertas untuk preview
                Size paperPx = PrinterService.GetPaperDimensions(paperName, 150); // 150 DPI cukup cepat dan tajam untuk preview

                int mTop = (int)Math.Round(((float)numMarginTop.Value / 25.4f) * 150);
                int mBottom = (int)Math.Round(((float)numMarginBottom.Value / 25.4f) * 150);
                int mLeft = (int)Math.Round(((float)numMarginLeft.Value / 25.4f) * 150);
                int mRight = (int)Math.Round(((float)numMarginRight.Value / 25.4f) * 150);
                int scale = (int)numScale.Value;

                Image activeImage = _enhancedImage ?? _rawScannedImage;
                Bitmap pageCanvas;

                if (activeImage != null)
                {
                    pageCanvas = PrinterService.CreateMultiCopyCanvas(
                        activeImage,
                        count,
                        paperPx.Width,
                        paperPx.Height,
                        mTop,
                        mBottom,
                        mLeft,
                        mRight,
                        chkFitPage.Checked,
                        true,
                        scale,
                        (int)numGapX.Value,
                        (int)numGapY.Value);
                }
                else
                {
                    // Belum ada dokumen yang di-scan: buat lembaran kertas kosong dengan garis batas margin aktif
                    pageCanvas = new Bitmap(paperPx.Width, paperPx.Height);
                    using (Graphics g = Graphics.FromImage(pageCanvas))
                    {
                        g.Clear(Color.White);

                        // Garis tepi luar kertas
                        using (Pen paperBorderPen = new Pen(Color.FromArgb(203, 213, 225), 2))
                        {
                            g.DrawRectangle(paperBorderPen, 0, 0, paperPx.Width - 1, paperPx.Height - 1);
                        }

                        // Garis batas margin (printable area)
                        int printW = Math.Max(20, paperPx.Width - (mLeft + mRight));
                        int printH = Math.Max(20, paperPx.Height - (mTop + mBottom));
                        using (Pen marginGuidePen = new Pen(Color.FromArgb(99, 102, 241), 1)) // Indigo soft
                        {
                            marginGuidePen.DashStyle = DashStyle.Dash;
                            g.DrawRectangle(marginGuidePen, mLeft, mTop, printW, printH);
                        }

                        // Teks panduan di tengah kertas
                        string guideText = $"Area Cetak ({paperName})\nMargin: Atas {numMarginTop.Value}mm, Bwh {numMarginBottom.Value}mm\nKiri {numMarginLeft.Value}mm, Kanan {numMarginRight.Value}mm\n\nSiap untuk Scan Dokumen";
                        using (Font f = new Font("Segoe UI", 18, FontStyle.Regular, GraphicsUnit.Pixel))
                        using (Brush b = new SolidBrush(Color.FromArgb(148, 163, 184)))
                        using (StringFormat sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString(guideText, f, b, new RectangleF(mLeft, mTop, printW, printH), sf);
                        }
                    }
                }

                _renderedPreviewImage?.Dispose();
                _renderedPreviewImage = pageCanvas;
                picPreview.Image = _renderedPreviewImage;

                picPreview.Invalidate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdatePreviewDisplay error: {ex.Message}");
            }
        }

        // ==========================================
        // SCAN & IMAGE ACTION BUTTONS
        // ==========================================

        private void btnUploadImage_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog ofd = new OpenFileDialog())
                {
                    ofd.Title = "Pilih Gambar untuk Fotokopi / Preview";
                    ofd.Filter = "File Gambar (*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff)|*.jpg;*.jpeg;*.png;*.bmp;*.tif;*.tiff|Semua File (*.*)|*.*";
                    ofd.Multiselect = false;

                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        // Muat gambar ke memory stream agar file tidak terkunci oleh Windows
                        byte[] fileBytes = System.IO.File.ReadAllBytes(ofd.FileName);
                        using (var ms = new System.IO.MemoryStream(fileBytes))
                        {
                            Image uploaded = Image.FromStream(ms);
                            _rawScannedImage?.Dispose();
                            _rawScannedImage = new Bitmap(uploaded);
                        }

                        // Reset skala ke 100% dan matikan fit page agar ukuran gambar apa adanya (WYSIWYG)
                        numScale.Value = 100;
                        chkFitPage.Checked = false;

                        // Bersihkan riwayat undo/redo untuk gambar baru
                        ClearUndoRedo();

                        // Terapkan enhance awal (crop & clean)
                        ReprocessScannedImage();

                        btnResetCrop.Enabled = true;
                        int items = GetCurrentItemsPerSheet();
                        UpdateStatus($"Gambar '{System.IO.Path.GetFileName(ofd.FileName)}' berhasil di-upload ({_rawScannedImage.Width} x {_rawScannedImage.Height} px).");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal membuka gambar: {ex.Message}", "Error Gambar", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Gagal membuka gambar: {ex.Message}");
            }
        }

        private async void btnChangeBg_Click(object sender, EventArgs e)
        {
            Image activeImage = _enhancedImage ?? _rawScannedImage;
            if (activeImage == null)
            {
                MessageBox.Show("Silakan scan dokumen atau upload gambar terlebih dahulu sebelum mengganti background.", "Pemberitahuan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            colorDialogBg.FullOpen = true;
            colorDialogBg.Color = Color.White;

            if (colorDialogBg.ShowDialog() == DialogResult.OK)
            {
                Color chosen = colorDialogBg.Color;
                PushUndoState(activeImage);

                // Buat salinan image untuk diproses di thread terpisah agar thread-safe
                Bitmap inputCopy = new Bitmap(activeImage);

                // Indikator loading agar UI tidak hang/Not Responding
                btnChangeBg.Enabled = false;
                Cursor = Cursors.WaitCursor;
                UpdateStatus("Sedang memproses AI ganti background (mohon tunggu)...");

                try
                {
                    Bitmap recolored = await Task.Run(() =>
                    {
                        try
                        {
                            return ImageEnhanceService.ReplaceBackgroundColor(inputCopy, chosen);
                        }
                        finally
                        {
                            inputCopy.Dispose();
                        }
                    });

                    if (recolored != null)
                    {
                        _enhancedImage?.Dispose();
                        _enhancedImage = recolored;

                        UpdatePreviewDisplay();
                        btnResetCrop.Enabled = true;
                        UpdateStatus($"Warna background berhasil diubah menjadi RGB({chosen.R}, {chosen.G}, {chosen.B}).");
                    }
                    else
                    {
                        UpdateStatus("AI background replacement tidak menghasilkan gambar atau dibatalkan.");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Gagal mengganti warna background: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    UpdateStatus("Gagal mengganti background.");
                }
                finally
                {
                    btnChangeBg.Enabled = true;
                    Cursor = Cursors.Default;
                }
            }
        }

        private void btnUndo_Click(object sender, EventArgs e)
        {
            if (_undoHistory.Count == 0) return;

            Image currentActive = _enhancedImage ?? _rawScannedImage;
            if (currentActive != null)
            {
                _redoHistory.Push(new Bitmap(currentActive));
            }

            Bitmap previous = _undoHistory.Pop();
            _enhancedImage?.Dispose();
            _enhancedImage = previous;

            UpdateUndoRedoButtonState();
            btnResetCrop.Enabled = true;
            UpdatePreviewDisplay();
            UpdateStatus("Perubahan berhasil di-Undo.");
        }

        private void btnRedo_Click(object sender, EventArgs e)
        {
            if (_redoHistory.Count == 0) return;

            Image currentActive = _enhancedImage ?? _rawScannedImage;
            if (currentActive != null)
            {
                _undoHistory.Push(new Bitmap(currentActive));
            }

            Bitmap next = _redoHistory.Pop();
            _enhancedImage?.Dispose();
            _enhancedImage = next;

            UpdateUndoRedoButtonState();
            btnResetCrop.Enabled = true;
            UpdatePreviewDisplay();
            UpdateStatus("Perubahan berhasil di-Redo.");
        }

        private void btnAbout_Click(object sender, EventArgs e)
        {
            string aboutMessage =
                "Aplikasi Fotokopi & Scan Dokumen\n" +
                "Versi 1.0.0\n\n" +
                "Copyright © Prisma Data Abadi\n" +
                "Alamat: Jl Mrutu Kalianyar 1c Surabaya\n\n" +
                "Fitur:\n" +
                "• Pengganda KTP / Dokumen Otomatis\n" +
                "• Auto-Crop & Pembersih Background Gelap\n" +
                "• Ganti Background Pas Foto / Dokumen berbasis AI\n" +
                "• Undo / Redo Riwayat Edit\n" +
                "• Dukungan Ukuran Kertas & Margin Presisi";

            MessageBox.Show(
                aboutMessage,
                "Tentang Aplikasi - Prisma Data Abadi",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnScanPreview_Click(object sender, EventArgs e)
        {
            _isCopyRequested = false;
            _isPreviewScanRequested = true;
            ExecuteScan();
        }

        private void btnScanOnly_Click(object sender, EventArgs e)
        {
            _isCopyRequested = false;
            _isPreviewScanRequested = false;
            ExecuteScan();
        }

        private void btnCopy_Click(object sender, EventArgs e)
        {
            // Jika sudah ada hasil scan di preview, langsung cetak tanpa perlu scan ulang
            Image activeImage = _enhancedImage ?? _rawScannedImage;
            if (activeImage != null)
            {
                PrintCurrentPreview();
                return;
            }

            // Jika belum ada gambar scan sama sekali, lakukan scan lalu cetak
            _isCopyRequested = true;
            _isPreviewScanRequested = false;
            ExecuteScan();
        }

        private void ExecuteScan()
        {
            try
            {
                if (_scannerService == null)
                {
                    MessageBox.Show("Layanan scanner belum terinisialisasi.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                string selectedScanner = cbScanner.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(selectedScanner) || selectedScanner.StartsWith("("))
                {
                    MessageBox.Show("Silakan pilih scanner yang valid terlebih dahulu.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                if (_isCopyRequested)
                {
                    string selectedPrinter = cbPrinter.SelectedItem?.ToString();
                    if (string.IsNullOrEmpty(selectedPrinter) || selectedPrinter.StartsWith("("))
                    {
                        MessageBox.Show("Silakan pilih printer tujuan cetak fotokopi.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                }

                SetBusyState(true);
                string modeDesc = _isPreviewScanRequested ? "Scan Preview" : (_isCopyRequested ? "Scan & Cetak Copy" : "Scan");
                UpdateStatus($"Menghubungkan ke scanner untuk {modeDesc}...");

                bool started = _scannerService.StartScan(selectedScanner, chkShowScannerUI.Checked);
                if (!started)
                {
                    SetBusyState(false);
                }
            }
            catch (Exception ex)
            {
                SetBusyState(false);
                MessageBox.Show($"Error saat memulai scan:\n{ex.Message}\n\nDetail:\n{ex.StackTrace}", "Error Scan", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Error scan: {ex.Message}");
            }
        }

        private void ScannerService_ImageScanned(object sender, Image img)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, Image>(ScannerService_ImageScanned), sender, img);
                return;
            }

            _rawScannedImage?.Dispose();
            _rawScannedImage = img;

            // Reset skala ke 100% dan matikan fit page agar hasil scan berukuran asli (WYSIWYG)
            numScale.Value = 100;
            chkFitPage.Checked = false;

            // Bersihkan riwayat undo/redo untuk hasil scan baru
            ClearUndoRedo();

            // Proses enhancement awal (bersihkan gelap + auto crop jika aktif)
            ReprocessScannedImage();

            btnResetCrop.Enabled = (_rawScannedImage != null);

            int items = GetCurrentItemsPerSheet();

            if (_isCopyRequested)
            {
                UpdateStatus($"Scan selesai. Menyiapkan layout ({items} gambar per lembar) & mencetak...");
                PrintCurrentPreview();
            }
            else if (_isPreviewScanRequested)
            {
                UpdateStatus("Preview dokumen berhasil dimuat. Tarik kursor mouse pada gambar untuk memilih area yang diinginkan.");
            }
            else
            {
                UpdateStatus($"Scan dokumen berhasil ditampilkan pada preview ({items} per lembar).");
            }

            SetBusyState(false);
        }

        // ==========================================
        // MANUAL SELECTION CROP (AREA SCAN SELECTION)
        // ==========================================

        private void picPreview_MouseDown(object sender, MouseEventArgs e)
        {
            Image activeImage = _enhancedImage ?? _rawScannedImage;
            if (activeImage == null || e.Button != MouseButtons.Left) return;

            // Hanya aktifkan seleksi bebas jika mode 1 gambar aktif (bukan kompilasi lembaran multi-copy)
            if (GetCurrentItemsPerSheet() > 1) return;

            _isSelecting = true;
            _selectionStart = e.Location;
            _selectionRect = new Rectangle(e.Location, new Size(0, 0));
            picPreview.Invalidate();
        }

        private void picPreview_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isSelecting) return;

            int x = Math.Min(_selectionStart.X, e.X);
            int y = Math.Min(_selectionStart.Y, e.Y);
            int w = Math.Abs(_selectionStart.X - e.X);
            int h = Math.Abs(_selectionStart.Y - e.Y);

            _selectionRect = new Rectangle(x, y, w, h);
            btnCropSelection.Enabled = (w > 10 && h > 10);
            picPreview.Invalidate();
        }

        private void picPreview_MouseUp(object sender, MouseEventArgs e)
        {
            if (!_isSelecting) return;
            _isSelecting = false;

            if (_selectionRect.Width > 15 && _selectionRect.Height > 15)
            {
                btnCropSelection.Enabled = true;
                UpdateStatus($"Area dipilih ({_selectionRect.Width}x{_selectionRect.Height} px pada layar). Klik 'Potong Area Pilihan' untuk memotong.");
            }
            else
            {
                _selectionRect = Rectangle.Empty;
                btnCropSelection.Enabled = false;
            }
            picPreview.Invalidate();
        }

        private void picPreview_Paint(object sender, PaintEventArgs e)
        {
            if (_selectionRect.Width > 0 && _selectionRect.Height > 0)
            {
                using (Pen dashedPen = new Pen(Color.FromArgb(220, 37, 99, 235), 2))
                {
                    dashedPen.DashStyle = DashStyle.Dash;
                    e.Graphics.DrawRectangle(dashedPen, _selectionRect);
                }

                using (Brush fillBrush = new SolidBrush(Color.FromArgb(40, 37, 99, 235)))
                {
                    e.Graphics.FillRectangle(fillBrush, _selectionRect);
                }
            }
        }

        /// <summary>
        /// Mengkonversi koordinat kotak seleksi mouse pada PictureBox ke koordinat pixel gambar objek asli.
        /// </summary>
        private Rectangle GetImageCoordinatesFromPictureBox(Rectangle clientRect, Image sourceImg)
        {
            if (sourceImg == null || clientRect.Width <= 0 || clientRect.Height <= 0)
                return Rectangle.Empty;

            Image renderedImg = _renderedPreviewImage;
            if (renderedImg == null) return Rectangle.Empty;

            float pbW = picPreview.ClientSize.Width;
            float pbH = picPreview.ClientSize.Height;
            float canvasW = renderedImg.Width;
            float canvasH = renderedImg.Height;

            // 1. Skala PictureBoxSizeMode.Zoom untuk kanvas kertas
            float ratio = Math.Min(pbW / canvasW, pbH / canvasH);
            float displayedW = canvasW * ratio;
            float displayedH = canvasH * ratio;

            float offsetX = (pbW - displayedW) / 2f;
            float offsetY = (pbH - displayedH) / 2f;

            // Konversi seleksi mouse di PictureBox ke koordinat pixel kanvas preview (150 DPI)
            float cX1 = (clientRect.Left - offsetX) / ratio;
            float cY1 = (clientRect.Top - offsetY) / ratio;
            float cX2 = (clientRect.Right - offsetX) / ratio;
            float cY2 = (clientRect.Bottom - offsetY) / ratio;

            // 2. Cari posisi (destX, destY) dan ukuran (destW, destH) gambar di atas kanvas preview
            int mTop = (int)Math.Round(((float)numMarginTop.Value / 25.4f) * 150);
            int mBottom = (int)Math.Round(((float)numMarginBottom.Value / 25.4f) * 150);
            int mLeft = (int)Math.Round(((float)numMarginLeft.Value / 25.4f) * 150);
            int mRight = (int)Math.Round(((float)numMarginRight.Value / 25.4f) * 150);
            int scale = (int)numScale.Value;
            float scaleMultiplier = Math.Max(0.1f, Math.Min(3.0f, scale / 100f));

            int printableW = Math.Max(100, (int)canvasW - (mLeft + mRight));
            int printableH = Math.Max(100, (int)canvasH - (mTop + mBottom));

            float canvasDpi = canvasW / (210f / 25.4f);
            float srcDpiX = sourceImg.HorizontalResolution > 0 ? sourceImg.HorizontalResolution : 300f;
            float srcDpiY = sourceImg.VerticalResolution > 0 ? sourceImg.VerticalResolution : 300f;
            float dpiRatioX = canvasDpi / srcDpiX;
            float dpiRatioY = canvasDpi / srcDpiY;

            int naturalW = (int)Math.Round(sourceImg.Width * dpiRatioX * scaleMultiplier);
            int naturalH = (int)Math.Round(sourceImg.Height * dpiRatioY * scaleMultiplier);

            int imgDestX, imgDestY, imgDestW, imgDestH;

            if (chkFitPage.Checked)
            {
                float imgAspect = (float)sourceImg.Width / Math.Max(1, sourceImg.Height);
                float printAspect = (float)printableW / Math.Max(1, printableH);

                if (imgAspect > printAspect)
                {
                    imgDestW = printableW;
                    imgDestH = (int)(printableW / imgAspect);
                }
                else
                {
                    imgDestH = printableH;
                    imgDestW = (int)(printableH * imgAspect);
                }

                if (scale != 100)
                {
                    imgDestW = (int)Math.Round(imgDestW * scaleMultiplier);
                    imgDestH = (int)Math.Round(imgDestH * scaleMultiplier);
                }

                imgDestX = mLeft + Math.Max(0, (printableW - imgDestW) / 2);
                imgDestY = mTop;
            }
            else
            {
                imgDestX = mLeft;
                imgDestY = mTop;
                imgDestW = naturalW;
                imgDestH = naturalH;
            }

            // 3. Batasi koordinat seleksi ke dalam bounding box gambar pada kanvas
            float cropX1 = Math.Max(imgDestX, Math.Min(cX1, imgDestX + imgDestW));
            float cropY1 = Math.Max(imgDestY, Math.Min(cY1, imgDestY + imgDestH));
            float cropX2 = Math.Max(imgDestX, Math.Min(cX2, imgDestX + imgDestW));
            float cropY2 = Math.Max(imgDestY, Math.Min(cY2, imgDestY + imgDestH));

            float cropW = cropX2 - cropX1;
            float cropH = cropY2 - cropY1;

            if (cropW <= 0 || cropH <= 0 || imgDestW <= 0 || imgDestH <= 0)
                return Rectangle.Empty;

            // 4. Skalakan kembali ke pixel resolusi asli sourceImg
            float scaleToOrigX = (float)sourceImg.Width / imgDestW;
            float scaleToOrigY = (float)sourceImg.Height / imgDestH;

            int origX = (int)Math.Round((cropX1 - imgDestX) * scaleToOrigX);
            int origY = (int)Math.Round((cropY1 - imgDestY) * scaleToOrigY);
            int origW = (int)Math.Round(cropW * scaleToOrigX);
            int origH = (int)Math.Round(cropH * scaleToOrigY);

            origX = Math.Max(0, Math.Min(origX, sourceImg.Width - 1));
            origY = Math.Max(0, Math.Min(origY, sourceImg.Height - 1));
            origW = Math.Max(1, Math.Min(origW, sourceImg.Width - origX));
            origH = Math.Max(1, Math.Min(origH, sourceImg.Height - origY));

            return new Rectangle(origX, origY, origW, origH);
        }

        private void btnCropSelection_Click(object sender, EventArgs e)
        {
            Image activeImage = _enhancedImage ?? _rawScannedImage;
            if (activeImage == null || _selectionRect.Width <= 0 || _selectionRect.Height <= 0) return;

            Rectangle imgCropRect = GetImageCoordinatesFromPictureBox(_selectionRect, activeImage);
            if (imgCropRect.Width <= 10 || imgCropRect.Height <= 10)
            {
                MessageBox.Show("Area seleksi terlalu kecil.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            PushUndoState(activeImage);
            var cropped = ImageEnhanceService.CropByRectangle(activeImage, imgCropRect);
            _enhancedImage?.Dispose();
            _enhancedImage = cropped;

            _selectionRect = Rectangle.Empty;
            btnCropSelection.Enabled = false;
            btnResetCrop.Enabled = true;

            UpdatePreviewDisplay();
            UpdateStatus($"Gambar berhasil dipotong ke area pilihan ({cropped.Width} x {cropped.Height} px).");
        }

        private void btnResetCrop_Click(object sender, EventArgs e)
        {
            if (_rawScannedImage == null) return;

            Image currentActive = _enhancedImage ?? _rawScannedImage;
            PushUndoState(currentActive);

            _selectionRect = Rectangle.Empty;
            btnCropSelection.Enabled = false;

            ReprocessScannedImage();
            UpdateStatus("Area gambar dikembalikan ke hasil scan asli.");
        }

        private void btnAutoEnhance_Click(object sender, EventArgs e)
        {
            Image activeImage = _enhancedImage ?? _rawScannedImage;
            if (activeImage == null)
            {
                MessageBox.Show("Silakan scan dokumen atau upload gambar terlebih dahulu.", "Pemberitahuan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                PushUndoState(activeImage);
                UpdateStatus("Sedang mengoptimalkan foto agar jernih dan tajam...");

                var enhanced = ImageEnhanceService.AutoEnhancePhoto(activeImage);
                if (enhanced != null)
                {
                    _enhancedImage?.Dispose();
                    _enhancedImage = enhanced;
                    btnResetCrop.Enabled = true;
                    UpdatePreviewDisplay();
                    UpdateStatus("Foto berhasil diperbagus otomatis (Auto-contrast, de-blur, dan penajaman diterapkan).");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal memproses gambar: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnAdjustments_Click(object sender, EventArgs e)
        {
            Image activeImage = _enhancedImage ?? _rawScannedImage;
            if (activeImage == null)
            {
                MessageBox.Show("Silakan scan dokumen atau upload gambar terlebih dahulu.", "Pemberitahuan", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            ShowAdjustmentsDialog(activeImage);
        }

        /// <summary>
        /// Menampilkan dialog popup interaktif untuk mengatur Brightness, Contrast, dan Sharpness secara langsung.
        /// </summary>
        private void ShowAdjustmentsDialog(Image targetImage)
        {
            using (Form dlg = new Form())
            {
                dlg.Text = "Pengaturan Kontras, Kecerahan & Ketajaman";
                dlg.Size = new Size(390, 340);
                dlg.FormBorderStyle = FormBorderStyle.FixedDialog;
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.MaximizeBox = false;
                dlg.MinimizeBox = false;
                dlg.BackColor = Color.FromArgb(248, 250, 252);
                dlg.Font = new Font("Segoe UI", 9F);

                // Label & TrackBar Brightness (-100 s/d 100)
                Label lblBTitle = new Label { Text = "☀️ Kecerahan (Brightness):", Location = new Point(20, 15), AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) };
                Label lblBVal = new Label { Text = "0", Location = new Point(310, 15), AutoSize = true, ForeColor = Color.FromArgb(79, 70, 229), Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) };
                TrackBar tbBrightness = new TrackBar { Minimum = -100, Maximum = 100, Value = 0, TickFrequency = 20, SmallChange = 5, LargeChange = 20, Location = new Point(20, 38), Width = 330 };

                // Label & TrackBar Contrast (-100 s/d 100)
                Label lblCTitle = new Label { Text = "🌓 Kontras (Contrast):", Location = new Point(20, 85), AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) };
                Label lblCVal = new Label { Text = "0", Location = new Point(310, 85), AutoSize = true, ForeColor = Color.FromArgb(79, 70, 229), Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) };
                TrackBar tbContrast = new TrackBar { Minimum = -100, Maximum = 100, Value = 0, TickFrequency = 20, SmallChange = 5, LargeChange = 20, Location = new Point(20, 108), Width = 330 };

                // Label & TrackBar Sharpness (0 s/d 100)
                Label lblSTitle = new Label { Text = "🔍 Ketajaman (Sharpness):", Location = new Point(20, 155), AutoSize = true, Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) };
                Label lblSVal = new Label { Text = "0", Location = new Point(310, 155), AutoSize = true, ForeColor = Color.FromArgb(79, 70, 229), Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold) };
                TrackBar tbSharpness = new TrackBar { Minimum = 0, Maximum = 100, Value = 0, TickFrequency = 10, SmallChange = 5, LargeChange = 10, Location = new Point(20, 178), Width = 330 };

                // Live Preview Debounce Timer
                Timer previewTimer = new Timer { Interval = 150 };
                Bitmap previewOriginal = new Bitmap(targetImage);
                Bitmap liveAdjusted = null;

                Action updateValues = () =>
                {
                    lblBVal.Text = tbBrightness.Value > 0 ? $"+{tbBrightness.Value}" : tbBrightness.Value.ToString();
                    lblCVal.Text = tbContrast.Value > 0 ? $"+{tbContrast.Value}" : tbContrast.Value.ToString();
                    lblSVal.Text = tbSharpness.Value.ToString();
                    previewTimer.Stop();
                    previewTimer.Start();
                };

                tbBrightness.ValueChanged += (s, e) => updateValues();
                tbContrast.ValueChanged += (s, e) => updateValues();
                tbSharpness.ValueChanged += (s, e) => updateValues();

                previewTimer.Tick += (s, e) =>
                {
                    previewTimer.Stop();
                    try
                    {
                        var adjusted = ImageEnhanceService.AdjustImageAdjustments(previewOriginal, tbBrightness.Value, tbContrast.Value, tbSharpness.Value);
                        if (adjusted != null)
                        {
                            liveAdjusted?.Dispose();
                            liveAdjusted = adjusted;
                            _enhancedImage?.Dispose();
                            _enhancedImage = new Bitmap(liveAdjusted);
                            UpdatePreviewDisplay();
                        }
                    }
                    catch { }
                };

                // Tombol Otomatis Jernihkan di dalam dialog
                Button btnAuto = new Button
                {
                    Text = "✨ Otomatis Perbagus / Jernihkan",
                    Location = new Point(20, 225),
                    Size = new Size(330, 32),
                    BackColor = Color.FromArgb(254, 249, 195),
                    ForeColor = Color.FromArgb(133, 77, 14),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 8.5F, FontStyle.Bold)
                };

                btnAuto.Click += (s, e) =>
                {
                    try
                    {
                        var autoImg = ImageEnhanceService.AutoEnhancePhoto(previewOriginal);
                        if (autoImg != null)
                        {
                            previewOriginal.Dispose();
                            previewOriginal = autoImg;
                            tbBrightness.Value = 0;
                            tbContrast.Value = 0;
                            tbSharpness.Value = 0;
                            liveAdjusted?.Dispose();
                            liveAdjusted = new Bitmap(previewOriginal);
                            _enhancedImage?.Dispose();
                            _enhancedImage = new Bitmap(liveAdjusted);
                            UpdatePreviewDisplay();
                        }
                    }
                    catch { }
                };

                dlg.Size = new Size(390, 360);

                // Tombol Terapkan & Batal
                Button btnApply = new Button
                {
                    Text = "✓ Terapkan",
                    DialogResult = DialogResult.OK,
                    Location = new Point(160, 270),
                    Size = new Size(100, 36),
                    BackColor = Color.FromArgb(37, 99, 235),
                    ForeColor = Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold)
                };

                Button btnCancel = new Button
                {
                    Text = "Batal",
                    DialogResult = DialogResult.Cancel,
                    Location = new Point(270, 270),
                    Size = new Size(80, 36),
                    BackColor = Color.FromArgb(241, 245, 249),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    FlatStyle = FlatStyle.Flat
                };

                Button btnReset = new Button
                {
                    Text = "↺ Reset",
                    Location = new Point(20, 270),
                    Size = new Size(80, 36),
                    BackColor = Color.FromArgb(241, 245, 249),
                    ForeColor = Color.FromArgb(71, 85, 105),
                    FlatStyle = FlatStyle.Flat
                };

                btnReset.Click += (s, e) =>
                {
                    tbBrightness.Value = 0;
                    tbContrast.Value = 0;
                    tbSharpness.Value = 0;
                };

                dlg.Controls.Add(lblBTitle); dlg.Controls.Add(lblBVal); dlg.Controls.Add(tbBrightness);
                dlg.Controls.Add(lblCTitle); dlg.Controls.Add(lblCVal); dlg.Controls.Add(tbContrast);
                dlg.Controls.Add(lblSTitle); dlg.Controls.Add(lblSVal); dlg.Controls.Add(tbSharpness);
                dlg.Controls.Add(btnAuto);
                dlg.Controls.Add(btnReset); dlg.Controls.Add(btnApply); dlg.Controls.Add(btnCancel);

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    PushUndoState(previewOriginal);
                    if (liveAdjusted != null)
                    {
                        _enhancedImage?.Dispose();
                        _enhancedImage = new Bitmap(liveAdjusted);
                    }
                    else
                    {
                        _enhancedImage?.Dispose();
                        _enhancedImage = ImageEnhanceService.AdjustImageAdjustments(previewOriginal, tbBrightness.Value, tbContrast.Value, tbSharpness.Value);
                    }

                    btnResetCrop.Enabled = true;
                    UpdatePreviewDisplay();
                    UpdateStatus($"Penyesuaian diterapkan: Kecerahan {tbBrightness.Value}, Kontras {tbContrast.Value}, Ketajaman {tbSharpness.Value}.");
                }
                else
                {
                    // Kembalikan ke gambar sebelum dialog dibuka
                    _enhancedImage?.Dispose();
                    _enhancedImage = new Bitmap(previewOriginal);
                    UpdatePreviewDisplay();
                }

                previewOriginal.Dispose();
                liveAdjusted?.Dispose();
            }
        }

        // ==========================================
        // PRESET UKURAN FOTO (2x3, 3x4, 4x6, 2R, 3R, 4R, KTP, dll)
        // ==========================================

        private void SetupPhotoPresetsMenu()
        {
            if (contextMenuPhotoPresets == null) return;
            contextMenuPhotoPresets.Items.Clear();

            // Kategori 1: Pas Foto Standar
            var headerPasFoto = new ToolStripMenuItem("--- PAS FOTO STANDAR ---") { Enabled = false };
            headerPasFoto.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            contextMenuPhotoPresets.Items.Add(headerPasFoto);

            AddPresetMenuItem("Pas Foto 2 x 3 cm (21.6 x 27.9 mm)", "2x3");
            AddPresetMenuItem("Pas Foto 3 x 4 cm (27.9 x 38.1 mm)", "3x4");
            AddPresetMenuItem("Pas Foto 4 x 6 cm (38.1 x 55.9 mm)", "4x6");

            contextMenuPhotoPresets.Items.Add(new ToolStripSeparator());

            // Kategori 2: Ukuran Foto Seri R (Cetak Foto Studio)
            var headerFotoR = new ToolStripMenuItem("--- CETAK FOTO R ---") { Enabled = false };
            headerFotoR.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            contextMenuPhotoPresets.Items.Add(headerFotoR);

            AddPresetMenuItem("Foto 2R (6 x 9 cm)", "2R");
            AddPresetMenuItem("Foto 3R (8.9 x 12.7 cm / 3.5 x 5 in)", "3R");
            AddPresetMenuItem("Foto 4R (10.2 x 15.2 cm / 4 x 6 in)", "4R");
            AddPresetMenuItem("Foto 5R (12.7 x 17.8 cm / 5 x 7 in)", "5R");
            AddPresetMenuItem("Foto 6R (15.2 x 20.3 cm / 6 x 8 in)", "6R");
            AddPresetMenuItem("Foto 8R (20.3 x 25.4 cm / 8 x 10 in)", "8R");
            AddPresetMenuItem("Foto 10R (25.4 x 30.5 cm / 10 x 12 in)", "10R");

            contextMenuPhotoPresets.Items.Add(new ToolStripSeparator());

            // Kategori 3: Kartu Identitas
            var headerKartu = new ToolStripMenuItem("--- KARTU IDENTITAS ---") { Enabled = false };
            headerKartu.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            contextMenuPhotoPresets.Items.Add(headerKartu);

            AddPresetMenuItem("Ukuran KTP / SIM (85.6 x 54 mm)", "KTP");
        }

        private void AddPresetMenuItem(string label, string presetCode)
        {
            var item = new ToolStripMenuItem(label);
            item.Tag = presetCode;
            item.Click += (s, e) =>
            {
                if (s is ToolStripMenuItem mi && mi.Tag is string code)
                {
                    ApplyPhotoPreset(code, mi.Text);
                }
            };
            contextMenuPhotoPresets.Items.Add(item);
        }

        private void btnPhotoPresets_Click(object sender, EventArgs e)
        {
            Image activeImage = _enhancedImage ?? _rawScannedImage;
            if (activeImage == null)
            {
                MessageBox.Show(
                    "Silakan scan dokumen atau upload gambar terlebih dahulu sebelum memilih ukuran foto.",
                    "Pemberitahuan",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            if (contextMenuPhotoPresets != null && btnPhotoPresets != null)
            {
                contextMenuPhotoPresets.Show(btnPhotoPresets, new Point(0, btnPhotoPresets.Height));
            }
        }

        private void ApplyPhotoPreset(string presetCode, string presetLabel)
        {
            Image activeImage = _enhancedImage ?? _rawScannedImage;
            if (activeImage == null) return;

            try
            {
                // Jika pengguna sedang membuat seleksi kotak bebas di layar, potong area seleksi tersebut dahulu
                if (_selectionRect.Width > 10 && _selectionRect.Height > 10)
                {
                    Rectangle imgCropRect = GetImageCoordinatesFromPictureBox(_selectionRect, activeImage);
                    if (imgCropRect.Width > 10 && imgCropRect.Height > 10)
                    {
                        var userSelected = ImageEnhanceService.CropByRectangle(activeImage, imgCropRect);
                        activeImage = userSelected;
                    }
                    _selectionRect = Rectangle.Empty;
                    btnCropSelection.Enabled = false;
                }

                PushUndoState(_enhancedImage ?? _rawScannedImage);

                // Potong dan sesuaikan ke preset ukuran
                Bitmap formatted = ImageEnhanceService.CropToPresetSize(activeImage, presetCode, 300);
                if (formatted != null)
                {
                    _enhancedImage?.Dispose();
                    _enhancedImage = formatted;

                    // Nonaktifkan 'fit to page' dan pastikan skala 100% agar ukuran fisik preset presisi 1:1 di layar & cetak
                    chkFitPage.Checked = false;
                    numScale.Value = 100;

                    btnResetCrop.Enabled = true;
                    UpdatePreviewDisplay();
                    UpdateStatus($"Objek gambar berhasil diatur ke ukuran {presetLabel} ({formatted.Width} x {formatted.Height} px 300 DPI).");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal mengatur ukuran preset: {ex.Message}", "Error Preset", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // ==========================================
        // PRINTING
        // ==========================================

        private void PrintCurrentPreview()
        {
            if (_printerService == null)
            {
                MessageBox.Show("Layanan printer belum siap.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string selectedPrinter = cbPrinter.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(selectedPrinter) || selectedPrinter.StartsWith("("))
            {
                MessageBox.Show("Silakan pilih printer yang valid.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Image imageToPrint = _enhancedImage ?? _rawScannedImage;
            if (imageToPrint == null)
            {
                MessageBox.Show("Tidak ada gambar untuk dicetak.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                int items = GetCurrentItemsPerSheet();
                short copies = (short)numCopies.Value;
                bool fit = chkFitPage.Checked;
                string paperName = GetSelectedPaperSizeName();
                int mTop = (int)numMarginTop.Value;
                int mBottom = (int)numMarginBottom.Value;
                int mLeft = (int)numMarginLeft.Value;
                int mRight = (int)numMarginRight.Value;
                int scale = (int)numScale.Value;

                UpdateStatus($"Mencetak ke '{selectedPrinter}' (Kertas: {paperName}, {items} gambar/lembar, Skala: {scale}%)...");
                _printerService.PrintImage(
                    imageToPrint,
                    selectedPrinter,
                    copies,
                    fit,
                    items,
                    paperName,
                    mTop,
                    mBottom,
                    mLeft,
                    mRight,
                    scale,
                    (int)numGapX.Value,
                    (int)numGapY.Value);
                UpdateStatus($"Fotokopi selesai dicetak ({copies} lembar, layout {items} gambar ke {selectedPrinter}).");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan saat mencetak: {ex.Message}", "Error Cetak", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Gagal cetak: {ex.Message}");
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            Image activeImage = _enhancedImage ?? _rawScannedImage;
            if (activeImage == null)
            {
                MessageBox.Show("Belum ada gambar yang discan atau diunggah.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                saveFileDialogExport.FileName = $"Hasil_Edit_{DateTime.Now:yyyyMMdd_HHmmss}";
                if (saveFileDialogExport.ShowDialog() != DialogResult.OK)
                    return;

                string filePath = saveFileDialogExport.FileName;
                string ext = Path.GetExtension(filePath)?.ToLowerInvariant() ?? "";

                int items = GetCurrentItemsPerSheet();
                Image imageToExport;
                bool shouldDispose = false;

                // Jika ganda gambar > 1, simpan seluruh kanvas tata letak lembaran kertas (300 DPI high resolution)
                if (items > 1)
                {
                    string paperName = GetSelectedPaperSizeName();
                    Size paperPx = PrinterService.GetPaperDimensions(paperName, 300);
                    int mTop = (int)Math.Round(((float)numMarginTop.Value / 25.4f) * 300);
                    int mBottom = (int)Math.Round(((float)numMarginBottom.Value / 25.4f) * 300);
                    int mLeft = (int)Math.Round(((float)numMarginLeft.Value / 25.4f) * 300);
                    int mRight = (int)Math.Round(((float)numMarginRight.Value / 25.4f) * 300);
                    int scale = (int)numScale.Value;

                    imageToExport = PrinterService.CreateMultiCopyCanvas(
                        activeImage,
                        items,
                        paperPx.Width,
                        paperPx.Height,
                        mTop,
                        mBottom,
                        mLeft,
                        mRight,
                        chkFitPage.Checked,
                        false, // jangan gambar border panduan saat simpan file hasil
                        scale,
                        (int)numGapX.Value,
                        (int)numGapY.Value);
                    shouldDispose = true;
                }
                else
                {
                    // 1 gambar: simpan foto hasil edit langsung dengan kualitas asli
                    imageToExport = activeImage;
                }

                try
                {
                    UpdateStatus($"Menyimpan file ke '{Path.GetFileName(filePath)}'...");

                    if (ext == ".pdf")
                    {
                        ExportService.SaveAsPdf(imageToExport, filePath);
                    }
                    else if (ext == ".jpg" || ext == ".jpeg")
                    {
                        ExportService.SaveAsJpg(imageToExport, filePath, 95);
                    }
                    else
                    {
                        ExportService.SaveAsPng(imageToExport, filePath);
                    }

                    UpdateStatus($"Berhasil disimpan: {Path.GetFileName(filePath)}");
                    MessageBox.Show(
                        $"File berhasil disimpan ke:\n{filePath}",
                        "Simpan Berhasil",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                finally
                {
                    if (shouldDispose)
                    {
                        imageToExport.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Gagal menyimpan file: {ex.Message}", "Error Simpan", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Gagal simpan file: {ex.Message}");
            }
        }

        private void ScannerService_StatusChanged(object sender, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, string>(ScannerService_StatusChanged), sender, message);
                return;
            }
            UpdateStatus(message);
        }

        private void ScannerService_ErrorOccurred(object sender, string message)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<object, string>(ScannerService_ErrorOccurred), sender, message);
                return;
            }

            SetBusyState(false);
            UpdateStatus($"Status: {message}");
            MessageBox.Show(message, "Info Scanner / TWAIN", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void SetBusyState(bool isBusy)
        {
            btnCopy.Enabled = !isBusy;
            btnScanOnly.Enabled = !isBusy;
            btnScanPreview.Enabled = !isBusy;
            btnRefreshDevices.Enabled = !isBusy;
            btnScannerProp.Enabled = !isBusy;
            btnPrinterProp.Enabled = !isBusy;
            btnSaveSettings.Enabled = !isBusy;
            cbScanner.Enabled = !isBusy;
            cbPrinter.Enabled = !isBusy;
            numItemsPerSheet.Enabled = !isBusy;
            chkCleanDarkBg.Enabled = !isBusy;
            chkAutoCrop.Enabled = !isBusy;
        }

        private void UpdateStatus(string message)
        {
            if (lblStatus != null)
            {
                lblStatus.Text = message ?? string.Empty;
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _rawScannedImage?.Dispose();
                _enhancedImage?.Dispose();
                _renderedPreviewImage?.Dispose();
                _scannerService?.Dispose();
            }
            catch { }
        }
    }
}
