namespace Fotocopy
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.gbSettings = new System.Windows.Forms.GroupBox();
            this.lblScanner = new System.Windows.Forms.Label();
            this.cbScanner = new System.Windows.Forms.ComboBox();
            this.btnScannerProp = new System.Windows.Forms.Button();
            this.lblPrinter = new System.Windows.Forms.Label();
            this.cbPrinter = new System.Windows.Forms.ComboBox();
            this.btnPrinterProp = new System.Windows.Forms.Button();
            this.btnRefreshDevices = new System.Windows.Forms.Button();
            this.lblCopies = new System.Windows.Forms.Label();
            this.numCopies = new System.Windows.Forms.NumericUpDown();
            this.lblItemsPerSheet = new System.Windows.Forms.Label();
            this.numItemsPerSheet = new System.Windows.Forms.NumericUpDown();
            this.chkFitPage = new System.Windows.Forms.CheckBox();
            this.chkShowScannerUI = new System.Windows.Forms.CheckBox();
            this.chkCleanDarkBg = new System.Windows.Forms.CheckBox();
            this.chkAutoCrop = new System.Windows.Forms.CheckBox();
            this.lblPaperSize = new System.Windows.Forms.Label();
            this.cbPaperSize = new System.Windows.Forms.ComboBox();
            this.lblMargin = new System.Windows.Forms.Label();
            this.lblMarginTop = new System.Windows.Forms.Label();
            this.numMarginTop = new System.Windows.Forms.NumericUpDown();
            this.lblMarginBottom = new System.Windows.Forms.Label();
            this.numMarginBottom = new System.Windows.Forms.NumericUpDown();
            this.lblMarginLeft = new System.Windows.Forms.Label();
            this.numMarginLeft = new System.Windows.Forms.NumericUpDown();
            this.lblMarginRight = new System.Windows.Forms.Label();
            this.numMarginRight = new System.Windows.Forms.NumericUpDown();
            this.btnSaveSettings = new System.Windows.Forms.Button();
            this.lblScale = new System.Windows.Forms.Label();
            this.numScale = new System.Windows.Forms.NumericUpDown();
            this.gbPreview = new System.Windows.Forms.GroupBox();
            this.pnlCropToolbar = new System.Windows.Forms.Panel();
            this.btnCropSelection = new System.Windows.Forms.Button();
            this.btnPhotoPresets = new System.Windows.Forms.Button();
            this.contextMenuPhotoPresets = new System.Windows.Forms.ContextMenuStrip();
            this.btnResetCrop = new System.Windows.Forms.Button();
            this.btnAutoEnhance = new System.Windows.Forms.Button();
            this.btnAdjustments = new System.Windows.Forms.Button();
            this.btnUndo = new System.Windows.Forms.Button();
            this.btnRedo = new System.Windows.Forms.Button();
            this.picPreview = new System.Windows.Forms.PictureBox();
            this.btnUploadImage = new System.Windows.Forms.Button();
            this.btnChangeBg = new System.Windows.Forms.Button();
            this.colorDialogBg = new System.Windows.Forms.ColorDialog();
            this.btnScanPreview = new System.Windows.Forms.Button();
            this.btnScanOnly = new System.Windows.Forms.Button();
            this.btnCopy = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.lblStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusSpring = new System.Windows.Forms.ToolStripStatusLabel();
            this.btnAbout = new System.Windows.Forms.ToolStripDropDownButton();
            this.menuAboutInfo = new System.Windows.Forms.ToolStripMenuItem();
            this.gbSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCopies)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numItemsPerSheet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMarginTop)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMarginBottom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMarginLeft)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMarginRight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numScale)).BeginInit();
            this.gbPreview.SuspendLayout();
            this.pnlCropToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbSettings
            // 
            this.gbSettings.Controls.Add(this.lblScale);
            this.gbSettings.Controls.Add(this.numScale);
            this.gbSettings.Controls.Add(this.btnSaveSettings);
            this.gbSettings.Controls.Add(this.numMarginRight);
            this.gbSettings.Controls.Add(this.lblMarginRight);
            this.gbSettings.Controls.Add(this.numMarginLeft);
            this.gbSettings.Controls.Add(this.lblMarginLeft);
            this.gbSettings.Controls.Add(this.numMarginBottom);
            this.gbSettings.Controls.Add(this.lblMarginBottom);
            this.gbSettings.Controls.Add(this.numMarginTop);
            this.gbSettings.Controls.Add(this.lblMarginTop);
            this.gbSettings.Controls.Add(this.lblMargin);
            this.gbSettings.Controls.Add(this.cbPaperSize);
            this.gbSettings.Controls.Add(this.lblPaperSize);
            this.gbSettings.Controls.Add(this.chkAutoCrop);
            this.gbSettings.Controls.Add(this.chkCleanDarkBg);
            this.gbSettings.Controls.Add(this.chkShowScannerUI);
            this.gbSettings.Controls.Add(this.chkFitPage);
            this.gbSettings.Controls.Add(this.numItemsPerSheet);
            this.gbSettings.Controls.Add(this.lblItemsPerSheet);
            this.gbSettings.Controls.Add(this.numCopies);
            this.gbSettings.Controls.Add(this.lblCopies);
            this.gbSettings.Controls.Add(this.btnRefreshDevices);
            this.gbSettings.Controls.Add(this.btnPrinterProp);
            this.gbSettings.Controls.Add(this.cbPrinter);
            this.gbSettings.Controls.Add(this.lblPrinter);
            this.gbSettings.Controls.Add(this.btnScannerProp);
            this.gbSettings.Controls.Add(this.cbScanner);
            this.gbSettings.Controls.Add(this.lblScanner);
            this.gbSettings.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbSettings.Location = new System.Drawing.Point(16, 12);
            this.gbSettings.Name = "gbSettings";
            this.gbSettings.Size = new System.Drawing.Size(350, 560);
            this.gbSettings.TabIndex = 0;
            this.gbSettings.TabStop = false;
            this.gbSettings.Text = "Pengaturan Koneksi & Fotokopi";
            // 
            // lblScanner
            // 
            this.lblScanner.AutoSize = true;
            this.lblScanner.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblScanner.Location = new System.Drawing.Point(16, 26);
            this.lblScanner.Name = "lblScanner";
            this.lblScanner.Size = new System.Drawing.Size(127, 20);
            this.lblScanner.TabIndex = 0;
            this.lblScanner.Text = "Koneksi Scanner :";
            // 
            // cbScanner
            // 
            this.cbScanner.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbScanner.FormattingEnabled = true;
            this.cbScanner.Location = new System.Drawing.Point(20, 49);
            this.cbScanner.Name = "cbScanner";
            this.cbScanner.Size = new System.Drawing.Size(210, 28);
            this.cbScanner.TabIndex = 1;
            // 
            // btnScannerProp
            // 
            this.btnScannerProp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(248)))));
            this.btnScannerProp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnScannerProp.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnScannerProp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.btnScannerProp.Location = new System.Drawing.Point(236, 48);
            this.btnScannerProp.Name = "btnScannerProp";
            this.btnScannerProp.Size = new System.Drawing.Size(92, 30);
            this.btnScannerProp.TabIndex = 2;
            this.btnScannerProp.Text = "⚙ Setting";
            this.btnScannerProp.UseVisualStyleBackColor = false;
            this.btnScannerProp.Click += new System.EventHandler(this.btnScannerProp_Click);
            // 
            // lblPrinter
            // 
            this.lblPrinter.AutoSize = true;
            this.lblPrinter.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPrinter.Location = new System.Drawing.Point(16, 86);
            this.lblPrinter.Name = "lblPrinter";
            this.lblPrinter.Size = new System.Drawing.Size(121, 20);
            this.lblPrinter.TabIndex = 3;
            this.lblPrinter.Text = "Koneksi Printer :";
            // 
            // cbPrinter
            // 
            this.cbPrinter.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPrinter.FormattingEnabled = true;
            this.cbPrinter.Location = new System.Drawing.Point(20, 109);
            this.cbPrinter.Name = "cbPrinter";
            this.cbPrinter.Size = new System.Drawing.Size(210, 28);
            this.cbPrinter.TabIndex = 4;
            // 
            // btnPrinterProp
            // 
            this.btnPrinterProp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(248)))));
            this.btnPrinterProp.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrinterProp.Font = new System.Drawing.Font("Segoe UI", 8.5F);
            this.btnPrinterProp.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.btnPrinterProp.Location = new System.Drawing.Point(236, 108);
            this.btnPrinterProp.Name = "btnPrinterProp";
            this.btnPrinterProp.Size = new System.Drawing.Size(92, 30);
            this.btnPrinterProp.TabIndex = 5;
            this.btnPrinterProp.Text = "⚙ Setting";
            this.btnPrinterProp.UseVisualStyleBackColor = false;
            this.btnPrinterProp.Click += new System.EventHandler(this.btnPrinterProp_Click);
            // 
            // btnRefreshDevices
            // 
            this.btnRefreshDevices.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(248)))));
            this.btnRefreshDevices.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRefreshDevices.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.btnRefreshDevices.Location = new System.Drawing.Point(20, 146);
            this.btnRefreshDevices.Name = "btnRefreshDevices";
            this.btnRefreshDevices.Size = new System.Drawing.Size(308, 30);
            this.btnRefreshDevices.TabIndex = 6;
            this.btnRefreshDevices.Text = "🔄 Refresh Scanner & Printer";
            this.btnRefreshDevices.UseVisualStyleBackColor = false;
            this.btnRefreshDevices.Click += new System.EventHandler(this.btnRefreshDevices_Click);
            // 
            // lblCopies
            // 
            this.lblCopies.AutoSize = true;
            this.lblCopies.Location = new System.Drawing.Point(16, 186);
            this.lblCopies.Name = "lblCopies";
            this.lblCopies.Size = new System.Drawing.Size(107, 20);
            this.lblCopies.TabIndex = 7;
            this.lblCopies.Text = "Jumlah Copy :";
            // 
            // numCopies
            // 
            this.numCopies.Location = new System.Drawing.Point(220, 184);
            this.numCopies.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numCopies.Maximum = new decimal(new int[] { 99, 0, 0, 0 });
            this.numCopies.Name = "numCopies";
            this.numCopies.Size = new System.Drawing.Size(108, 27);
            this.numCopies.TabIndex = 8;
            this.numCopies.Value = new decimal(new int[] { 1, 0, 0, 0 });
            // 
            // lblItemsPerSheet
            // 
            this.lblItemsPerSheet.AutoSize = true;
            this.lblItemsPerSheet.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblItemsPerSheet.Location = new System.Drawing.Point(16, 219);
            this.lblItemsPerSheet.Name = "lblItemsPerSheet";
            this.lblItemsPerSheet.Size = new System.Drawing.Size(186, 20);
            this.lblItemsPerSheet.TabIndex = 9;
            this.lblItemsPerSheet.Text = "Ganda Gambar per Kertas :";
            // 
            // 
            // numItemsPerSheet
            // 
            this.numItemsPerSheet.Location = new System.Drawing.Point(220, 218);
            this.numItemsPerSheet.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            this.numItemsPerSheet.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numItemsPerSheet.Name = "numItemsPerSheet";
            this.numItemsPerSheet.Size = new System.Drawing.Size(108, 27);
            this.numItemsPerSheet.TabIndex = 10;
            this.numItemsPerSheet.Value = new decimal(new int[] { 1, 0, 0, 0 });
            this.numItemsPerSheet.ValueChanged += new System.EventHandler(this.numItemsPerSheet_ValueChanged);
            // 
            // chkFitPage
            // 
            this.chkFitPage.AutoSize = true;
            this.chkFitPage.Checked = false;
            this.chkFitPage.CheckState = System.Windows.Forms.CheckState.Unchecked;
            this.chkFitPage.Location = new System.Drawing.Point(20, 278);
            this.chkFitPage.Name = "chkFitPage";
            this.chkFitPage.Size = new System.Drawing.Size(175, 24);
            this.chkFitPage.TabIndex = 11;
            this.chkFitPage.Text = "Sesuaikan kertas";
            this.chkFitPage.UseVisualStyleBackColor = true;
            this.chkFitPage.CheckedChanged += new System.EventHandler(this.chkFitPage_CheckedChanged);
            // 
            // lblScale
            // 
            this.lblScale.AutoSize = true;
            this.lblScale.Location = new System.Drawing.Point(200, 279);
            this.lblScale.Name = "lblScale";
            this.lblScale.Size = new System.Drawing.Size(65, 20);
            this.lblScale.TabIndex = 12;
            this.lblScale.Text = "Skala %:";
            // 
            // numScale
            // 
            this.numScale.Location = new System.Drawing.Point(268, 276);
            this.numScale.Maximum = new decimal(new int[] { 300, 0, 0, 0 });
            this.numScale.Minimum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numScale.Name = "numScale";
            this.numScale.Size = new System.Drawing.Size(60, 27);
            this.numScale.TabIndex = 13;
            this.numScale.Value = new decimal(new int[] { 100, 0, 0, 0 });
            this.numScale.ValueChanged += new System.EventHandler(this.numScale_ValueChanged);
            // 
            // chkShowScannerUI
            // 
            this.chkShowScannerUI.AutoSize = true;
            this.chkShowScannerUI.Location = new System.Drawing.Point(20, 306);
            this.chkShowScannerUI.Name = "chkShowScannerUI";
            this.chkShowScannerUI.Size = new System.Drawing.Size(186, 24);
            this.chkShowScannerUI.TabIndex = 12;
            this.chkShowScannerUI.Text = "Tampilkan UI Scanner";
            this.chkShowScannerUI.UseVisualStyleBackColor = true;
            // 
            // chkCleanDarkBg
            // 
            this.chkCleanDarkBg.AutoSize = true;
            this.chkCleanDarkBg.Checked = true;
            this.chkCleanDarkBg.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkCleanDarkBg.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.chkCleanDarkBg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.chkCleanDarkBg.Location = new System.Drawing.Point(20, 334);
            this.chkCleanDarkBg.Name = "chkCleanDarkBg";
            this.chkCleanDarkBg.Size = new System.Drawing.Size(271, 24);
            this.chkCleanDarkBg.TabIndex = 13;
            this.chkCleanDarkBg.Text = "✨ Bersihkan background gelap/abu";
            this.chkCleanDarkBg.UseVisualStyleBackColor = true;
            this.chkCleanDarkBg.CheckedChanged += new System.EventHandler(this.chkCleanDarkBg_CheckedChanged);
            // 
            // chkAutoCrop
            // 
            this.chkAutoCrop.AutoSize = true;
            this.chkAutoCrop.Checked = true;
            this.chkAutoCrop.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkAutoCrop.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.chkAutoCrop.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(15)))), ((int)(((byte)(23)))), ((int)(((byte)(42)))));
            this.chkAutoCrop.Location = new System.Drawing.Point(20, 362);
            this.chkAutoCrop.Name = "chkAutoCrop";
            this.chkAutoCrop.Size = new System.Drawing.Size(262, 24);
            this.chkAutoCrop.TabIndex = 14;
            this.chkAutoCrop.Text = "✂️ Potong otomatis jika teks habis";
            this.chkAutoCrop.UseVisualStyleBackColor = true;
            this.chkAutoCrop.CheckedChanged += new System.EventHandler(this.chkAutoCrop_CheckedChanged);
            // 
            // lblPaperSize
            // 
            this.lblPaperSize.AutoSize = true;
            this.lblPaperSize.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblPaperSize.Location = new System.Drawing.Point(16, 394);
            this.lblPaperSize.Name = "lblPaperSize";
            this.lblPaperSize.Size = new System.Drawing.Size(109, 20);
            this.lblPaperSize.TabIndex = 15;
            this.lblPaperSize.Text = "Ukuran Kertas :";
            // 
            // cbPaperSize
            // 
            this.cbPaperSize.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPaperSize.FormattingEnabled = true;
            this.cbPaperSize.Items.AddRange(new object[] {
            "A4 (210 x 297 mm)",
            "F4 / Folio (215 x 330 mm)",
            "Letter (215 x 279 mm)",
            "Legal (215 x 356 mm)",
            "A3 (297 x 420 mm)",
            "A5 (148 x 210 mm)"});
            this.cbPaperSize.Location = new System.Drawing.Point(135, 391);
            this.cbPaperSize.Name = "cbPaperSize";
            this.cbPaperSize.Size = new System.Drawing.Size(193, 28);
            this.cbPaperSize.TabIndex = 16;
            this.cbPaperSize.SelectedIndexChanged += new System.EventHandler(this.cbPaperSize_SelectedIndexChanged);
            // 
            // lblMargin
            // 
            this.lblMargin.AutoSize = true;
            this.lblMargin.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.lblMargin.Location = new System.Drawing.Point(16, 428);
            this.lblMargin.Name = "lblMargin";
            this.lblMargin.Size = new System.Drawing.Size(139, 20);
            this.lblMargin.TabIndex = 17;
            this.lblMargin.Text = "Margin Print (mm) :";
            // 
            // lblMarginTop
            // 
            this.lblMarginTop.AutoSize = true;
            this.lblMarginTop.Location = new System.Drawing.Point(18, 456);
            this.lblMarginTop.Name = "lblMarginTop";
            this.lblMarginTop.Size = new System.Drawing.Size(26, 20);
            this.lblMarginTop.TabIndex = 18;
            this.lblMarginTop.Text = "Atas";
            // 
            // numMarginTop
            // 
            this.numMarginTop.Location = new System.Drawing.Point(20, 478);
            this.numMarginTop.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numMarginTop.Name = "numMarginTop";
            this.numMarginTop.Size = new System.Drawing.Size(68, 27);
            this.numMarginTop.TabIndex = 19;
            this.numMarginTop.Value = new decimal(new int[] { 10, 0, 0, 0 });
            this.numMarginTop.ValueChanged += new System.EventHandler(this.numMargin_ValueChanged);
            // 
            // lblMarginBottom
            // 
            this.lblMarginBottom.AutoSize = true;
            this.lblMarginBottom.Location = new System.Drawing.Point(98, 456);
            this.lblMarginBottom.Name = "lblMarginBottom";
            this.lblMarginBottom.Size = new System.Drawing.Size(37, 20);
            this.lblMarginBottom.TabIndex = 20;
            this.lblMarginBottom.Text = "Bwh";
            // 
            // numMarginBottom
            // 
            this.numMarginBottom.Location = new System.Drawing.Point(100, 478);
            this.numMarginBottom.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numMarginBottom.Name = "numMarginBottom";
            this.numMarginBottom.Size = new System.Drawing.Size(68, 27);
            this.numMarginBottom.TabIndex = 21;
            this.numMarginBottom.Value = new decimal(new int[] { 10, 0, 0, 0 });
            this.numMarginBottom.ValueChanged += new System.EventHandler(this.numMargin_ValueChanged);
            // 
            // lblMarginLeft
            // 
            this.lblMarginLeft.AutoSize = true;
            this.lblMarginLeft.Location = new System.Drawing.Point(178, 456);
            this.lblMarginLeft.Name = "lblMarginLeft";
            this.lblMarginLeft.Size = new System.Drawing.Size(28, 20);
            this.lblMarginLeft.TabIndex = 22;
            this.lblMarginLeft.Text = "Kiri";
            // 
            // numMarginLeft
            // 
            this.numMarginLeft.Location = new System.Drawing.Point(180, 478);
            this.numMarginLeft.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numMarginLeft.Name = "numMarginLeft";
            this.numMarginLeft.Size = new System.Drawing.Size(68, 27);
            this.numMarginLeft.TabIndex = 23;
            this.numMarginLeft.Value = new decimal(new int[] { 10, 0, 0, 0 });
            this.numMarginLeft.ValueChanged += new System.EventHandler(this.numMargin_ValueChanged);
            // 
            // lblMarginRight
            // 
            this.lblMarginRight.AutoSize = true;
            this.lblMarginRight.Location = new System.Drawing.Point(258, 456);
            this.lblMarginRight.Name = "lblMarginRight";
            this.lblMarginRight.Size = new System.Drawing.Size(38, 20);
            this.lblMarginRight.TabIndex = 24;
            this.lblMarginRight.Text = "Kanan";
            // 
            // numMarginRight
            // 
            this.numMarginRight.Location = new System.Drawing.Point(260, 478);
            this.numMarginRight.Maximum = new decimal(new int[] { 100, 0, 0, 0 });
            this.numMarginRight.Name = "numMarginRight";
            this.numMarginRight.Size = new System.Drawing.Size(68, 27);
            this.numMarginRight.TabIndex = 25;
            this.numMarginRight.Value = new decimal(new int[] { 10, 0, 0, 0 });
            this.numMarginRight.ValueChanged += new System.EventHandler(this.numMargin_ValueChanged);
            // 
            // btnSaveSettings
            // 
            this.btnSaveSettings.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(231)))), ((int)(((byte)(255)))));
            this.btnSaveSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnSaveSettings.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(67)))), ((int)(((byte)(56)))), ((int)(((byte)(202)))));
            this.btnSaveSettings.Location = new System.Drawing.Point(20, 516);
            this.btnSaveSettings.Name = "btnSaveSettings";
            this.btnSaveSettings.Size = new System.Drawing.Size(308, 34);
            this.btnSaveSettings.TabIndex = 26;
            this.btnSaveSettings.Text = "💾 Simpan Sebagai Default";
            this.btnSaveSettings.UseVisualStyleBackColor = false;
            this.btnSaveSettings.Click += new System.EventHandler(this.btnSaveSettings_Click);
            // 
            // gbPreview
            // 
            this.gbPreview.Controls.Add(this.picPreview);
            this.gbPreview.Controls.Add(this.pnlCropToolbar);
            this.gbPreview.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbPreview.Location = new System.Drawing.Point(380, 12);
            this.gbPreview.Name = "gbPreview";
            this.gbPreview.Size = new System.Drawing.Size(460, 560);
            this.gbPreview.TabIndex = 1;
            this.gbPreview.TabStop = false;
            this.gbPreview.Text = "Preview Hasil Scan & Fotokopi (Tarik mouse untuk pilih area)";
            // 
            // pnlCropToolbar
            // 
            this.pnlCropToolbar.Controls.Add(this.btnAdjustments);
            this.pnlCropToolbar.Controls.Add(this.btnRedo);
            this.pnlCropToolbar.Controls.Add(this.btnUndo);
            this.pnlCropToolbar.Controls.Add(this.btnResetCrop);
            this.pnlCropToolbar.Controls.Add(this.btnPhotoPresets);
            this.pnlCropToolbar.Controls.Add(this.btnCropSelection);
            this.pnlCropToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlCropToolbar.Location = new System.Drawing.Point(3, 23);
            this.pnlCropToolbar.Name = "pnlCropToolbar";
            this.pnlCropToolbar.Size = new System.Drawing.Size(454, 38);
            this.pnlCropToolbar.TabIndex = 1;
            // 
            // btnPhotoPresets
            // 
            this.btnPhotoPresets.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(238)))), ((int)(((byte)(242)))), ((int)(((byte)(255)))));
            this.btnPhotoPresets.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPhotoPresets.Font = new System.Drawing.Font("Segoe UI Semibold", 8.5F, System.Drawing.FontStyle.Bold);
            this.btnPhotoPresets.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(67)))), ((int)(((byte)(56)))), ((int)(((byte)(202)))));
            this.btnPhotoPresets.Location = new System.Drawing.Point(82, 3);
            this.btnPhotoPresets.Name = "btnPhotoPresets";
            this.btnPhotoPresets.Size = new System.Drawing.Size(110, 32);
            this.btnPhotoPresets.TabIndex = 1;
            this.btnPhotoPresets.Text = "📐 Ukuran Foto ▾";
            this.btnPhotoPresets.UseVisualStyleBackColor = false;
            this.btnPhotoPresets.Click += new System.EventHandler(this.btnPhotoPresets_Click);
            // 
            // contextMenuPhotoPresets
            // 
            this.contextMenuPhotoPresets.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuPhotoPresets.Name = "contextMenuPhotoPresets";
            this.contextMenuPhotoPresets.Size = new System.Drawing.Size(210, 320);
            // 
            // 
            // btnCropSelection
            // 
            this.btnCropSelection.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(253)))), ((int)(((byte)(244)))));
            this.btnCropSelection.Enabled = false;
            this.btnCropSelection.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCropSelection.Font = new System.Drawing.Font("Segoe UI Semibold", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnCropSelection.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(22)))), ((int)(((byte)(101)))), ((int)(((byte)(52)))));
            this.btnCropSelection.Location = new System.Drawing.Point(3, 3);
            this.btnCropSelection.Name = "btnCropSelection";
            this.btnCropSelection.Size = new System.Drawing.Size(76, 32);
            this.btnCropSelection.TabIndex = 0;
            this.btnCropSelection.Text = "✂️ Potong";
            this.btnCropSelection.UseVisualStyleBackColor = false;
            this.btnCropSelection.Click += new System.EventHandler(this.btnCropSelection_Click);
            // 
            // btnResetCrop
            // 
            this.btnResetCrop.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.btnResetCrop.Enabled = false;
            this.btnResetCrop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnResetCrop.Font = new System.Drawing.Font("Segoe UI", 8.25F);
            this.btnResetCrop.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(71)))), ((int)(((byte)(85)))), ((int)(((byte)(105)))));
            this.btnResetCrop.Location = new System.Drawing.Point(195, 3);
            this.btnResetCrop.Name = "btnResetCrop";
            this.btnResetCrop.Size = new System.Drawing.Size(62, 32);
            this.btnResetCrop.TabIndex = 2;
            this.btnResetCrop.Text = "↺ Reset";
            this.btnResetCrop.UseVisualStyleBackColor = false;
            this.btnResetCrop.Click += new System.EventHandler(this.btnResetCrop_Click);
            // 
            // btnUndo
            // 
            this.btnUndo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.btnUndo.Enabled = false;
            this.btnUndo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUndo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnUndo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.btnUndo.Location = new System.Drawing.Point(260, 3);
            this.btnUndo.Name = "btnUndo";
            this.btnUndo.Size = new System.Drawing.Size(30, 32);
            this.btnUndo.TabIndex = 3;
            this.btnUndo.Text = "↶";
            this.btnUndo.UseVisualStyleBackColor = false;
            this.btnUndo.Click += new System.EventHandler(this.btnUndo_Click);
            // 
            // btnRedo
            // 
            this.btnRedo.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(241)))), ((int)(((byte)(245)))), ((int)(((byte)(249)))));
            this.btnRedo.Enabled = false;
            this.btnRedo.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnRedo.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.btnRedo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(51)))), ((int)(((byte)(65)))), ((int)(((byte)(85)))));
            this.btnRedo.Location = new System.Drawing.Point(293, 3);
            this.btnRedo.Name = "btnRedo";
            this.btnRedo.Size = new System.Drawing.Size(30, 32);
            this.btnRedo.TabIndex = 4;
            this.btnRedo.Text = "↷";
            this.btnRedo.UseVisualStyleBackColor = false;
            this.btnRedo.Click += new System.EventHandler(this.btnRedo_Click);
            // 
            // 
            // btnAdjustments
            // 
            this.btnAdjustments.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(244)))), ((int)(((byte)(248)))));
            this.btnAdjustments.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnAdjustments.Font = new System.Drawing.Font("Segoe UI Semibold", 8.25F, System.Drawing.FontStyle.Bold);
            this.btnAdjustments.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(41)))), ((int)(((byte)(59)))));
            this.btnAdjustments.Location = new System.Drawing.Point(326, 3);
            this.btnAdjustments.Name = "btnAdjustments";
            this.btnAdjustments.Size = new System.Drawing.Size(125, 32);
            this.btnAdjustments.TabIndex = 5;
            this.btnAdjustments.Text = "🎛️ Edit & Jernihkan";
            this.btnAdjustments.UseVisualStyleBackColor = false;
            this.btnAdjustments.Click += new System.EventHandler(this.btnAdjustments_Click);
            // 
            // picPreview
            // 
            this.picPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.picPreview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picPreview.Cursor = System.Windows.Forms.Cursors.Cross;
            this.picPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picPreview.Location = new System.Drawing.Point(3, 61);
            this.picPreview.Name = "picPreview";
            this.picPreview.Size = new System.Drawing.Size(454, 496);
            this.picPreview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picPreview.TabIndex = 0;
            this.picPreview.TabStop = false;
            this.picPreview.MouseDown += new System.Windows.Forms.MouseEventHandler(this.picPreview_MouseDown);
            this.picPreview.MouseMove += new System.Windows.Forms.MouseEventHandler(this.picPreview_MouseMove);
            this.picPreview.MouseUp += new System.Windows.Forms.MouseEventHandler(this.picPreview_MouseUp);
            this.picPreview.Paint += new System.Windows.Forms.PaintEventHandler(this.picPreview_Paint);
            // 
            // 
            // btnUploadImage
            // 
            this.btnUploadImage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(232)))), ((int)(((byte)(255)))));
            this.btnUploadImage.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnUploadImage.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnUploadImage.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(126)))), ((int)(((byte)(34)))), ((int)(((byte)(206)))));
            this.btnUploadImage.Location = new System.Drawing.Point(16, 585);
            this.btnUploadImage.Name = "btnUploadImage";
            this.btnUploadImage.Size = new System.Drawing.Size(145, 55);
            this.btnUploadImage.TabIndex = 2;
            this.btnUploadImage.Text = "📁 Upload Gambar";
            this.btnUploadImage.UseVisualStyleBackColor = false;
            this.btnUploadImage.Click += new System.EventHandler(this.btnUploadImage_Click);
            // 
            // btnScanPreview
            // 
            this.btnScanPreview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.btnScanPreview.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnScanPreview.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnScanPreview.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(2)))), ((int)(((byte)(132)))), ((int)(((byte)(199)))));
            this.btnScanPreview.Location = new System.Drawing.Point(168, 585);
            this.btnScanPreview.Name = "btnScanPreview";
            this.btnScanPreview.Size = new System.Drawing.Size(145, 55);
            this.btnScanPreview.TabIndex = 3;
            this.btnScanPreview.Text = "👁️ Scan Preview";
            this.btnScanPreview.UseVisualStyleBackColor = false;
            this.btnScanPreview.Click += new System.EventHandler(this.btnScanPreview_Click);
            // 
            // btnScanOnly
            // 
            this.btnScanOnly.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(234)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.btnScanOnly.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnScanOnly.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnScanOnly.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(14)))), ((int)(((byte)(116)))), ((int)(((byte)(144)))));
            this.btnScanOnly.Location = new System.Drawing.Point(320, 585);
            this.btnScanOnly.Name = "btnScanOnly";
            this.btnScanOnly.Size = new System.Drawing.Size(145, 55);
            this.btnScanOnly.TabIndex = 4;
            this.btnScanOnly.Text = "📄 Scan Dokumen";
            this.btnScanOnly.UseVisualStyleBackColor = false;
            this.btnScanOnly.Click += new System.EventHandler(this.btnScanOnly_Click);
            // 
            // btnChangeBg
            // 
            this.btnChangeBg.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(254)))), ((int)(((byte)(242)))), ((int)(((byte)(242)))));
            this.btnChangeBg.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnChangeBg.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnChangeBg.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(185)))), ((int)(((byte)(28)))), ((int)(((byte)(28)))));
            this.btnChangeBg.Location = new System.Drawing.Point(472, 585);
            this.btnChangeBg.Name = "btnChangeBg";
            this.btnChangeBg.Size = new System.Drawing.Size(140, 55);
            this.btnChangeBg.TabIndex = 5;
            this.btnChangeBg.Text = "🎨 Ganti Warna BG";
            this.btnChangeBg.UseVisualStyleBackColor = false;
            this.btnChangeBg.Click += new System.EventHandler(this.btnChangeBg_Click);
            // 
            // btnCopy
            // 
            this.btnCopy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(37)))), ((int)(((byte)(99)))), ((int)(((byte)(235)))));
            this.btnCopy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopy.Font = new System.Drawing.Font("Segoe UI", 11F, System.Drawing.FontStyle.Bold);
            this.btnCopy.ForeColor = System.Drawing.Color.White;
            this.btnCopy.Location = new System.Drawing.Point(620, 585);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(220, 55);
            this.btnCopy.TabIndex = 6;
            this.btnCopy.Text = "⚡ CETAK / COPY";
            this.btnCopy.UseVisualStyleBackColor = false;
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblStatus,
            this.statusSpring,
            this.btnAbout});
            this.statusStrip1.Location = new System.Drawing.Point(0, 653);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(856, 28);
            this.statusStrip1.TabIndex = 7;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // lblStatus
            // 
            this.lblStatus.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(49, 22);
            this.lblStatus.Text = "Siap...";
            // 
            // statusSpring
            // 
            this.statusSpring.Name = "statusSpring";
            this.statusSpring.Size = new System.Drawing.Size(712, 22);
            this.statusSpring.Spring = true;
            // 
            // btnAbout
            // 
            this.btnAbout.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.btnAbout.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.menuAboutInfo});
            this.btnAbout.Font = new System.Drawing.Font("Segoe UI Semibold", 9F, System.Drawing.FontStyle.Bold);
            this.btnAbout.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(79)))), ((int)(((byte)(70)))), ((int)(((byte)(229)))));
            this.btnAbout.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnAbout.Name = "btnAbout";
            this.btnAbout.Size = new System.Drawing.Size(80, 26);
            this.btnAbout.Text = "ℹ️ About";
            this.btnAbout.Click += new System.EventHandler(this.btnAbout_Click);
            // 
            // menuAboutInfo
            // 
            this.menuAboutInfo.Name = "menuAboutInfo";
            this.menuAboutInfo.Size = new System.Drawing.Size(224, 26);
            this.menuAboutInfo.Text = "Tentang Aplikasi...";
            this.menuAboutInfo.Click += new System.EventHandler(this.btnAbout_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(250)))), ((int)(((byte)(252)))));
            this.ClientSize = new System.Drawing.Size(856, 681);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.btnCopy);
            this.Controls.Add(this.btnChangeBg);
            this.Controls.Add(this.btnScanOnly);
            this.Controls.Add(this.btnScanPreview);
            this.Controls.Add(this.btnUploadImage);
            this.Controls.Add(this.gbPreview);
            this.Controls.Add(this.gbSettings);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Aplikasi Fotokopi .NET (Scanner & Printer Connector)";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.gbSettings.ResumeLayout(false);
            this.gbSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCopies)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numItemsPerSheet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMarginTop)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMarginBottom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMarginLeft)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numMarginRight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numScale)).EndInit();
            this.gbPreview.ResumeLayout(false);
            this.pnlCropToolbar.ResumeLayout(false);
            this.pnlCropToolbar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picPreview)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox gbSettings;
        private System.Windows.Forms.Label lblScanner;
        private System.Windows.Forms.ComboBox cbScanner;
        private System.Windows.Forms.Button btnScannerProp;
        private System.Windows.Forms.Label lblPrinter;
        private System.Windows.Forms.ComboBox cbPrinter;
        private System.Windows.Forms.Button btnPrinterProp;
        private System.Windows.Forms.Button btnRefreshDevices;
        private System.Windows.Forms.Label lblCopies;
        private System.Windows.Forms.NumericUpDown numCopies;
        private System.Windows.Forms.Label lblItemsPerSheet;
        private System.Windows.Forms.NumericUpDown numItemsPerSheet;
        private System.Windows.Forms.CheckBox chkFitPage;
        private System.Windows.Forms.Label lblScale;
        private System.Windows.Forms.NumericUpDown numScale;
        private System.Windows.Forms.CheckBox chkShowScannerUI;
        private System.Windows.Forms.CheckBox chkCleanDarkBg;
        private System.Windows.Forms.CheckBox chkAutoCrop;
        private System.Windows.Forms.Label lblPaperSize;
        private System.Windows.Forms.ComboBox cbPaperSize;
        private System.Windows.Forms.Label lblMargin;
        private System.Windows.Forms.Label lblMarginTop;
        private System.Windows.Forms.NumericUpDown numMarginTop;
        private System.Windows.Forms.Label lblMarginBottom;
        private System.Windows.Forms.NumericUpDown numMarginBottom;
        private System.Windows.Forms.Label lblMarginLeft;
        private System.Windows.Forms.NumericUpDown numMarginLeft;
        private System.Windows.Forms.Label lblMarginRight;
        private System.Windows.Forms.NumericUpDown numMarginRight;
        private System.Windows.Forms.Button btnSaveSettings;
        private System.Windows.Forms.GroupBox gbPreview;
        private System.Windows.Forms.Panel pnlCropToolbar;
        private System.Windows.Forms.Button btnCropSelection;
        private System.Windows.Forms.Button btnPhotoPresets;
        private System.Windows.Forms.ContextMenuStrip contextMenuPhotoPresets;
        private System.Windows.Forms.Button btnResetCrop;
        private System.Windows.Forms.Button btnUndo;
        private System.Windows.Forms.Button btnRedo;
        private System.Windows.Forms.Button btnAutoEnhance;
        private System.Windows.Forms.Button btnAdjustments;
        private System.Windows.Forms.PictureBox picPreview;
        private System.Windows.Forms.Button btnUploadImage;
        private System.Windows.Forms.Button btnScanPreview;
        private System.Windows.Forms.Button btnScanOnly;
        private System.Windows.Forms.Button btnChangeBg;
        private System.Windows.Forms.ColorDialog colorDialogBg;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel lblStatus;
        private System.Windows.Forms.ToolStripStatusLabel statusSpring;
        private System.Windows.Forms.ToolStripDropDownButton btnAbout;
        private System.Windows.Forms.ToolStripMenuItem menuAboutInfo;
    }
}
