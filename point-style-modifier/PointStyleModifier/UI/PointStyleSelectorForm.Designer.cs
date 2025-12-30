namespace PointStyleModifier.UI
{
    partial class PointStyleSelectorForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListBox listBoxStyles;
        private System.Windows.Forms.Button buttonOk;
        private System.Windows.Forms.Button buttonCancel;
        private System.Windows.Forms.Label labelInstructions;
        private System.Windows.Forms.Panel panelButtons;

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
            this.listBoxStyles = new System.Windows.Forms.ListBox();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.labelInstructions = new System.Windows.Forms.Label();
            this.panelButtons = new System.Windows.Forms.Panel();
            this.panelButtons.SuspendLayout();
            this.SuspendLayout();
            //
            // labelInstructions
            //
            this.labelInstructions.AutoSize = false;
            this.labelInstructions.Dock = System.Windows.Forms.DockStyle.Top;
            this.labelInstructions.Location = new System.Drawing.Point(10, 10);
            this.labelInstructions.Name = "labelInstructions";
            this.labelInstructions.Padding = new System.Windows.Forms.Padding(0, 0, 0, 10);
            this.labelInstructions.Size = new System.Drawing.Size(364, 30);
            this.labelInstructions.TabIndex = 0;
            this.labelInstructions.Text = "Select a point style to apply to the selected points:";
            //
            // listBoxStyles
            //
            this.listBoxStyles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listBoxStyles.FormattingEnabled = true;
            this.listBoxStyles.ItemHeight = 15;
            this.listBoxStyles.Location = new System.Drawing.Point(10, 40);
            this.listBoxStyles.Name = "listBoxStyles";
            this.listBoxStyles.Size = new System.Drawing.Size(364, 260);
            this.listBoxStyles.TabIndex = 1;
            this.listBoxStyles.DoubleClick += new System.EventHandler(this.listBoxStyles_DoubleClick);
            //
            // panelButtons
            //
            this.panelButtons.Controls.Add(this.buttonCancel);
            this.panelButtons.Controls.Add(this.buttonOk);
            this.panelButtons.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelButtons.Location = new System.Drawing.Point(10, 300);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(364, 40);
            this.panelButtons.TabIndex = 2;
            //
            // buttonOk
            //
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.Location = new System.Drawing.Point(192, 8);
            this.buttonOk.Name = "buttonOk";
            this.buttonOk.Size = new System.Drawing.Size(80, 28);
            this.buttonOk.TabIndex = 0;
            this.buttonOk.Text = "OK";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
            //
            // buttonCancel
            //
            this.buttonCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(280, 8);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(80, 28);
            this.buttonCancel.TabIndex = 1;
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.Click += new System.EventHandler(this.buttonCancel_Click);
            //
            // PointStyleSelectorForm
            //
            this.AcceptButton = this.buttonOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(384, 350);
            this.Controls.Add(this.listBoxStyles);
            this.Controls.Add(this.panelButtons);
            this.Controls.Add(this.labelInstructions);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PointStyleSelectorForm";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Select Point Style";
            this.panelButtons.ResumeLayout(false);
            this.ResumeLayout(false);
        }
    }
}
