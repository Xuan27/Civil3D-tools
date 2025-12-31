namespace PointStyleModifier.UI
{
    partial class PointStyleSelectorForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.CheckBox checkBoxPointStyle;
        private System.Windows.Forms.CheckBox checkBoxLabelStyle;
        private System.Windows.Forms.ListBox listBoxPointStyles;
        private System.Windows.Forms.ListBox listBoxLabelStyles;
        private System.Windows.Forms.Label labelPointStyle;
        private System.Windows.Forms.Label labelLabelStyle;
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
            this.checkBoxPointStyle = new System.Windows.Forms.CheckBox();
            this.checkBoxLabelStyle = new System.Windows.Forms.CheckBox();
            this.listBoxPointStyles = new System.Windows.Forms.ListBox();
            this.listBoxLabelStyles = new System.Windows.Forms.ListBox();
            this.labelPointStyle = new System.Windows.Forms.Label();
            this.labelLabelStyle = new System.Windows.Forms.Label();
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
            this.labelInstructions.Location = new System.Drawing.Point(12, 12);
            this.labelInstructions.Name = "labelInstructions";
            this.labelInstructions.Size = new System.Drawing.Size(550, 30);
            this.labelInstructions.TabIndex = 0;
            this.labelInstructions.Text = "Select which styles to modify for the selected points:";
            //
            // checkBoxPointStyle
            //
            this.checkBoxPointStyle.AutoSize = true;
            this.checkBoxPointStyle.Checked = true;
            this.checkBoxPointStyle.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxPointStyle.Location = new System.Drawing.Point(12, 50);
            this.checkBoxPointStyle.Name = "checkBoxPointStyle";
            this.checkBoxPointStyle.Size = new System.Drawing.Size(150, 17);
            this.checkBoxPointStyle.TabIndex = 1;
            this.checkBoxPointStyle.Text = "Modify Point Style (Marker)";
            this.checkBoxPointStyle.UseVisualStyleBackColor = true;
            this.checkBoxPointStyle.CheckedChanged += new System.EventHandler(this.checkBoxPointStyle_CheckedChanged);
            //
            // labelPointStyle
            //
            this.labelPointStyle.AutoSize = true;
            this.labelPointStyle.Location = new System.Drawing.Point(12, 75);
            this.labelPointStyle.Name = "labelPointStyle";
            this.labelPointStyle.Size = new System.Drawing.Size(100, 13);
            this.labelPointStyle.TabIndex = 2;
            this.labelPointStyle.Text = "Available Point Styles:";
            //
            // listBoxPointStyles
            //
            this.listBoxPointStyles.FormattingEnabled = true;
            this.listBoxPointStyles.Location = new System.Drawing.Point(12, 95);
            this.listBoxPointStyles.Name = "listBoxPointStyles";
            this.listBoxPointStyles.Size = new System.Drawing.Size(260, 200);
            this.listBoxPointStyles.TabIndex = 3;
            //
            // checkBoxLabelStyle
            //
            this.checkBoxLabelStyle.AutoSize = true;
            this.checkBoxLabelStyle.Location = new System.Drawing.Point(290, 50);
            this.checkBoxLabelStyle.Name = "checkBoxLabelStyle";
            this.checkBoxLabelStyle.Size = new System.Drawing.Size(145, 17);
            this.checkBoxLabelStyle.TabIndex = 4;
            this.checkBoxLabelStyle.Text = "Modify Label Style (Text)";
            this.checkBoxLabelStyle.UseVisualStyleBackColor = true;
            this.checkBoxLabelStyle.CheckedChanged += new System.EventHandler(this.checkBoxLabelStyle_CheckedChanged);
            //
            // labelLabelStyle
            //
            this.labelLabelStyle.AutoSize = true;
            this.labelLabelStyle.Location = new System.Drawing.Point(290, 75);
            this.labelLabelStyle.Name = "labelLabelStyle";
            this.labelLabelStyle.Size = new System.Drawing.Size(105, 13);
            this.labelLabelStyle.TabIndex = 5;
            this.labelLabelStyle.Text = "Available Label Styles:";
            //
            // listBoxLabelStyles
            //
            this.listBoxLabelStyles.Enabled = false;
            this.listBoxLabelStyles.FormattingEnabled = true;
            this.listBoxLabelStyles.Location = new System.Drawing.Point(290, 95);
            this.listBoxLabelStyles.Name = "listBoxLabelStyles";
            this.listBoxLabelStyles.Size = new System.Drawing.Size(260, 200);
            this.listBoxLabelStyles.TabIndex = 6;
            //
            // panelButtons
            //
            this.panelButtons.Controls.Add(this.buttonCancel);
            this.panelButtons.Controls.Add(this.buttonOk);
            this.panelButtons.Location = new System.Drawing.Point(12, 310);
            this.panelButtons.Name = "panelButtons";
            this.panelButtons.Size = new System.Drawing.Size(538, 40);
            this.panelButtons.TabIndex = 7;
            //
            // buttonOk
            //
            this.buttonOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonOk.Location = new System.Drawing.Point(370, 8);
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
            this.buttonCancel.Location = new System.Drawing.Point(458, 8);
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
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(564, 362);
            this.Controls.Add(this.panelButtons);
            this.Controls.Add(this.listBoxLabelStyles);
            this.Controls.Add(this.labelLabelStyle);
            this.Controls.Add(this.checkBoxLabelStyle);
            this.Controls.Add(this.listBoxPointStyles);
            this.Controls.Add(this.labelPointStyle);
            this.Controls.Add(this.checkBoxPointStyle);
            this.Controls.Add(this.labelInstructions);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PointStyleSelectorForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Modify Point and Label Styles";
            this.panelButtons.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
