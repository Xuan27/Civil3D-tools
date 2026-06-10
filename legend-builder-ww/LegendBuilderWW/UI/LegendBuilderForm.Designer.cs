namespace LegendBuilderWW.UI
{
    partial class LegendBuilderForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label labelShow;
        private System.Windows.Forms.ComboBox comboShow;
        private System.Windows.Forms.Label labelType;
        private System.Windows.Forms.ComboBox comboType;
        private System.Windows.Forms.Label labelSearch;
        private System.Windows.Forms.TextBox textSearch;
        private System.Windows.Forms.Button buttonSettings;

        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.DataGridViewCheckBoxColumn colInclude;
        private System.Windows.Forms.DataGridViewTextBoxColumn colType;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDescription;
        private System.Windows.Forms.DataGridViewTextBoxColumn colKey;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCount;

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.FlowLayoutPanel buttonFlow;
        private System.Windows.Forms.Label labelSummary;
        private System.Windows.Forms.Button buttonSelectUsed;
        private System.Windows.Forms.Button buttonClear;
        private System.Windows.Forms.Button buttonPreview;
        private System.Windows.Forms.Button buttonGenerate;
        private System.Windows.Forms.Button buttonCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.panelTop = new System.Windows.Forms.Panel();
            this.labelShow = new System.Windows.Forms.Label();
            this.comboShow = new System.Windows.Forms.ComboBox();
            this.labelType = new System.Windows.Forms.Label();
            this.comboType = new System.Windows.Forms.ComboBox();
            this.labelSearch = new System.Windows.Forms.Label();
            this.textSearch = new System.Windows.Forms.TextBox();
            this.buttonSettings = new System.Windows.Forms.Button();

            this.grid = new System.Windows.Forms.DataGridView();
            this.colInclude = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDescription = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colKey = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCount = new System.Windows.Forms.DataGridViewTextBoxColumn();

            this.panelBottom = new System.Windows.Forms.Panel();
            this.buttonFlow = new System.Windows.Forms.FlowLayoutPanel();
            this.labelSummary = new System.Windows.Forms.Label();
            this.buttonSelectUsed = new System.Windows.Forms.Button();
            this.buttonClear = new System.Windows.Forms.Button();
            this.buttonPreview = new System.Windows.Forms.Button();
            this.buttonGenerate = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();

            this.panelTop.SuspendLayout();
            this.panelBottom.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.SuspendLayout();

            //
            // panelTop
            //
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Height = 44;
            this.panelTop.Controls.Add(this.labelShow);
            this.panelTop.Controls.Add(this.comboShow);
            this.panelTop.Controls.Add(this.labelType);
            this.panelTop.Controls.Add(this.comboType);
            this.panelTop.Controls.Add(this.labelSearch);
            this.panelTop.Controls.Add(this.textSearch);
            this.panelTop.Controls.Add(this.buttonSettings);

            //
            // labelShow
            //
            this.labelShow.AutoSize = true;
            this.labelShow.Location = new System.Drawing.Point(8, 14);
            this.labelShow.Text = "Show:";

            //
            // comboShow
            //
            this.comboShow.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboShow.Location = new System.Drawing.Point(50, 11);
            this.comboShow.Size = new System.Drawing.Size(130, 21);
            this.comboShow.SelectedIndexChanged += new System.EventHandler(this.OnFilterChanged);

            //
            // labelType
            //
            this.labelType.AutoSize = true;
            this.labelType.Location = new System.Drawing.Point(190, 14);
            this.labelType.Text = "Type:";

            //
            // comboType
            //
            this.comboType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.comboType.Location = new System.Drawing.Point(228, 11);
            this.comboType.Size = new System.Drawing.Size(120, 21);
            this.comboType.SelectedIndexChanged += new System.EventHandler(this.OnFilterChanged);

            //
            // labelSearch
            //
            this.labelSearch.AutoSize = true;
            this.labelSearch.Location = new System.Drawing.Point(360, 14);
            this.labelSearch.Text = "Search:";

            //
            // textSearch
            //
            this.textSearch.Location = new System.Drawing.Point(410, 11);
            this.textSearch.Size = new System.Drawing.Size(200, 20);
            this.textSearch.TextChanged += new System.EventHandler(this.OnFilterChanged);

            //
            // buttonSettings
            //
            this.buttonSettings.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
            this.buttonSettings.Location = new System.Drawing.Point(720, 10);
            this.buttonSettings.Size = new System.Drawing.Size(85, 23);
            this.buttonSettings.Text = "Settings...";
            this.buttonSettings.UseVisualStyleBackColor = true;
            this.buttonSettings.Click += new System.EventHandler(this.OnSettingsClicked);

            //
            // grid
            //
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.AllowUserToAddRows = false;
            this.grid.AllowUserToDeleteRows = false;
            this.grid.AutoGenerateColumns = false;
            this.grid.RowHeadersVisible = false;
            this.grid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.grid.MultiSelect = false;
            this.grid.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.grid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
                this.colInclude, this.colType, this.colDescription, this.colKey, this.colCount });
            this.grid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.OnGridCellValueChanged);
            this.grid.CurrentCellDirtyStateChanged += new System.EventHandler(this.OnGridDirtyStateChanged);

            //
            // colInclude
            //
            this.colInclude.HeaderText = "Include";
            this.colInclude.DataPropertyName = "IncludeInOutput";
            this.colInclude.Width = 60;

            //
            // colType
            //
            this.colType.HeaderText = "Type";
            this.colType.DataPropertyName = "RowType";
            this.colType.ReadOnly = true;
            this.colType.Width = 80;

            //
            // colDescription
            //
            this.colDescription.HeaderText = "Description (editable)";
            this.colDescription.DataPropertyName = "Description";
            this.colDescription.ReadOnly = false;
            this.colDescription.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;

            //
            // colKey
            //
            this.colKey.HeaderText = "Key";
            this.colKey.DataPropertyName = "Key";
            this.colKey.ReadOnly = true;
            this.colKey.Width = 180;

            //
            // colCount
            //
            this.colCount.HeaderText = "In Drawing";
            this.colCount.DataPropertyName = "CountInDrawing";
            this.colCount.ReadOnly = true;
            this.colCount.Width = 85;

            //
            // panelBottom
            //
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Height = 56;
            this.panelBottom.Controls.Add(this.buttonFlow);
            this.panelBottom.Controls.Add(this.labelSummary);

            //
            // buttonFlow — docks to the right side of panelBottom, AutoSize takes the actual
            // width of the contained buttons; FlowDirection.RightToLeft means the first control
            // added is rightmost, so Generate appears in the dominant position.
            //
            this.buttonFlow.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonFlow.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.buttonFlow.AutoSize = true;
            this.buttonFlow.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.buttonFlow.Padding = new System.Windows.Forms.Padding(0, 12, 12, 12);
            this.buttonFlow.WrapContents = false;
            this.buttonFlow.Controls.Add(this.buttonGenerate);
            this.buttonFlow.Controls.Add(this.buttonPreview);
            this.buttonFlow.Controls.Add(this.buttonCancel);
            this.buttonFlow.Controls.Add(this.buttonClear);
            this.buttonFlow.Controls.Add(this.buttonSelectUsed);

            //
            // labelSummary
            //
            this.labelSummary.Dock = System.Windows.Forms.DockStyle.Left;
            this.labelSummary.AutoSize = false;
            this.labelSummary.Width = 360;
            this.labelSummary.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.labelSummary.Padding = new System.Windows.Forms.Padding(8, 0, 0, 0);
            this.labelSummary.Text = "0 rows";

            //
            // buttonGenerate (primary action — rightmost in flow)
            //
            this.buttonGenerate.Size = new System.Drawing.Size(140, 32);
            this.buttonGenerate.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.buttonGenerate.Text = "Generate Legend";
            this.buttonGenerate.Font = new System.Drawing.Font(System.Drawing.SystemFonts.DefaultFont, System.Drawing.FontStyle.Bold);
            this.buttonGenerate.UseVisualStyleBackColor = true;
            this.buttonGenerate.Click += new System.EventHandler(this.OnGenerateClicked);

            //
            // buttonCancel
            //
            this.buttonCancel.Size = new System.Drawing.Size(80, 32);
            this.buttonCancel.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            //
            // buttonPreview
            //
            this.buttonPreview.Size = new System.Drawing.Size(130, 32);
            this.buttonPreview.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.buttonPreview.Text = "Preview Legend";
            this.buttonPreview.UseVisualStyleBackColor = true;
            this.buttonPreview.Click += new System.EventHandler(this.OnPreviewClicked);

            //
            // buttonClear
            //
            this.buttonClear.Size = new System.Drawing.Size(80, 32);
            this.buttonClear.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.buttonClear.Text = "Clear All";
            this.buttonClear.UseVisualStyleBackColor = true;
            this.buttonClear.Click += new System.EventHandler(this.OnClearClicked);

            //
            // buttonSelectUsed
            //
            this.buttonSelectUsed.Size = new System.Drawing.Size(130, 32);
            this.buttonSelectUsed.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.buttonSelectUsed.Text = "Select Used";
            this.buttonSelectUsed.UseVisualStyleBackColor = true;
            this.buttonSelectUsed.Click += new System.EventHandler(this.OnSelectUsedClicked);

            //
            // LegendBuilderForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AcceptButton = this.buttonGenerate;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(980, 560);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.panelTop);
            this.Controls.Add(this.panelBottom);
            this.MinimumSize = new System.Drawing.Size(640, 400);
            this.Name = "LegendBuilderForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Legend Builder (Westwood)";

            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelBottom.ResumeLayout(false);
            this.panelBottom.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.ResumeLayout(false);
        }
    }
}
