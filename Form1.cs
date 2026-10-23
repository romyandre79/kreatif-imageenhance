using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
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
                    SelectItemsPerSheetCombo(items);

                    string savedPaper = Settings.Default.PaperSize ?? "A4";
                    SelectPaperSizeCombo(savedPaper);

                    numMarginTop.Value = Math.Max(0, Math.Min(100, Settings.Default.MarginTop));
                    numMarginBottom.Value = Math.Max(0, Math.Min(100, Settings.Default.MarginBottom));
                    numMarginLeft.Value = Math.Max(0, Math.Min(100, Settings.Default.MarginLeft));
                    numMarginRight.Value = Math.Max(0, Math.Min(100, Settings.Default.MarginRight));

                    int savedScale = Settings.Default.ImageScale;
                    numScale.Value = Math.Max(10, Math.Min(300, savedScale > 0 ? savedScale : 100));
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

        private void SelectItemsPerSheetCombo(int count)
        {
            switch (count)
            {
                case 2: cbItemsPerSheet.SelectedIndex = 1; break;
                case 3: cbItemsPerSheet.SelectedIndex = 2; break;
                case 4: cbItemsPerSheet.SelectedIndex = 3; break;
                case 6: cbItemsPerSheet.SelectedIndex = 4; break;
                case 8: cbItemsPerSheet.SelectedIndex = 5; break;
                default: cbItemsPerSheet.SelectedIndex = 0; break;
            }
        }

        private int GetCurrentItemsPerSheet()
        {
            switch (cbItemsPerSheet.SelectedIndex)
            {
                case 1: return 2;
                case 2: return 3;
                case 3: return 4;
                case 4: return 6;
                case 5: return 8;
                default: return 1;
            }
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
                Settings.Default.Save();

                MessageBox.Show(
                    "Pengaturan koneksi scanner, printer, ukuran kertas, margin, skala gambar, dan pengganda gambar berhasil disimpan!",
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

        private void cbItemsPerSheet_SelectedIndexChanged(object sender, EventArgs e)
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
            NumericUpDown[] controls = new NumericUpDown[] { numMarginTop, numMarginBottom, numMarginLeft, numMarginRight, numScale, numCopies };
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
                        scale);
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

                        // Reset skala ke 100%
                        numScale.Value = 100;

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

            // Reset skala ke 100% (tanpa scale) untuk hasil scan baru
            numScale.Value = 100;

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
        /// Mengkonversi koordinat kotak seleksi mouse pada PictureBox (Zoom mode) ke koordinat pixel gambar asli.
        /// </summary>
        private Rectangle GetImageCoordinatesFromPictureBox(Rectangle clientRect, Image img)
        {
            if (img == null || clientRect.Width <= 0 || clientRect.Height <= 0)
                return Rectangle.Empty;

            float pbW = picPreview.ClientSize.Width;
            float pbH = picPreview.ClientSize.Height;
            float imgW = img.Width;
            float imgH = img.Height;

            // Hitung aspect ratio zoom
            float ratio = Math.Min(pbW / imgW, pbH / imgH);
            float displayedW = imgW * ratio;
            float displayedH = imgH * ratio;

            float offsetX = (pbW - displayedW) / 2f;
            float offsetY = (pbH - displayedH) / 2f;

            // Batasi kotak ke area tampilan gambar sebenarnya
            float x1 = Math.Max(offsetX, Math.Min(clientRect.Left, offsetX + displayedW));
            float y1 = Math.Max(offsetY, Math.Min(clientRect.Top, offsetY + displayedH));
            float x2 = Math.Max(offsetX, Math.Min(clientRect.Right, offsetX + displayedW));
            float y2 = Math.Max(offsetY, Math.Min(clientRect.Bottom, offsetY + displayedH));

            float selW = x2 - x1;
            float selH = y2 - y1;

            if (selW <= 0 || selH <= 0) return Rectangle.Empty;

            // Skalakan ke resolusi pixel gambar asli
            int origX = (int)Math.Round((x1 - offsetX) / ratio);
            int origY = (int)Math.Round((y1 - offsetY) / ratio);
            int origW = (int)Math.Round(selW / ratio);
            int origH = (int)Math.Round(selH / ratio);

            origX = Math.Max(0, Math.Min(origX, img.Width - 1));
            origY = Math.Max(0, Math.Min(origY, img.Height - 1));
            origW = Math.Min(origW, img.Width - origX);
            origH = Math.Min(origH, img.Height - origY);

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
                    scale);
                UpdateStatus($"Fotokopi selesai dicetak ({copies} lembar, layout {items} gambar ke {selectedPrinter}).");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Terjadi kesalahan saat mencetak: {ex.Message}", "Error Cetak", MessageBoxButtons.OK, MessageBoxIcon.Error);
                UpdateStatus($"Gagal cetak: {ex.Message}");
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
            cbItemsPerSheet.Enabled = !isBusy;
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
