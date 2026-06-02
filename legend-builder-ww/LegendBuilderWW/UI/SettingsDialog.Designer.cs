namespace LegendBuilderWW.UI
{
    partial class SettingsDialog
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label labelPath;
        private System.Windows.Forms.TextBox textPath;
        private System.Windows.Forms.Button buttonBrowse;
        private System.Windows.Forms.Label labelBlockName;
        private System.Windows.Forms.TextBox textBlockName;
        private System.Windows.Forms.Label labelPrefix;
        private System.Windows.Forms.TextBox textPrefix;
        private System.Windows.Forms.Label labelTolerance;
        private System.Windows.Forms.TextBox textTolerance;
        private System.Windows.Forms.Label labelSavedTo;
        private System.Windows.Forms.Button buttonOk;
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
            this.labelPath = new System.Windows.Forms.Label();
            this.textPath = new System.Windows.Forms.TextBox();
            this.buttonBrowse = new System.Windows.Forms.Button();
            this.labelBlockName = new System.Windows.Forms.Label();
            this.textBlockName = new System.Windows.Forms.TextBox();
            this.labelPrefix = new System.Windows.Forms.Label();
            this.textPrefix = new System.Windows.Forms.TextBox();
            this.labelTolerance = new System.Windows.Forms.Label();
            this.textTolerance = new System.Windows.Forms.TextBox();
            this.labelSavedTo = new System.Windows.Forms.Label();
            this.buttonOk = new System.Windows.Forms.Button();
            this.buttonCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();

            //
            // labelPath
            //
            this.labelPath.AutoSize = true;
            this.labelPath.Location = new System.Drawing.Point(12, 15);
            this.labelPath.Text = "Source DWG path (contains the Vertical Legend block):";

            //
            // textPath
            //
            this.textPath.Location = new System.Drawing.Point(12, 34);
            this.textPath.Size = new System.Drawing.Size(540, 20);

            //
            // buttonBrowse
            //
            this.buttonBrowse.Location = new System.Drawing.Point(558, 33);
            this.buttonBrowse.Size = new System.Drawing.Size(75, 23);
            this.buttonBrowse.Text = "Browse...";
            this.buttonBrowse.UseVisualStyleBackColor = true;
            this.buttonBrowse.Click += new System.EventHandler(this.OnBrowseClicked);

            //
            // labelBlockName
            //
            this.labelBlockName.AutoSize = true;
            this.labelBlockName.Location = new System.Drawing.Point(12, 70);
            this.labelBlockName.Text = "Source block name:";

            //
            // textBlockName
            //
            this.textBlockName.Location = new System.Drawing.Point(150, 67);
            this.textBlockName.Size = new System.Drawing.Size(280, 20);

            //
            // labelPrefix
            //
            this.labelPrefix.AutoSize = true;
            this.labelPrefix.Location = new System.Drawing.Point(12, 100);
            this.labelPrefix.Text = "Output block prefix:";

            //
            // textPrefix
            //
            this.textPrefix.Location = new System.Drawing.Point(150, 97);
            this.textPrefix.Size = new System.Drawing.Size(280, 20);

            //
            // labelTolerance
            //
            this.labelTolerance.AutoSize = true;
            this.labelTolerance.Location = new System.Drawing.Point(12, 130);
            this.labelTolerance.Text = "Row grouping tolerance:";

            //
            // textTolerance
            //
            this.textTolerance.Location = new System.Drawing.Point(150, 127);
            this.textTolerance.Size = new System.Drawing.Size(100, 20);

            //
            // labelSavedTo
            //
            this.labelSavedTo.AutoSize = false;
            this.labelSavedTo.Location = new System.Drawing.Point(12, 160);
            this.labelSavedTo.Size = new System.Drawing.Size(620, 30);
            this.labelSavedTo.ForeColor = System.Drawing.Color.Gray;

            //
            // buttonOk
            //
            this.buttonOk.Location = new System.Drawing.Point(478, 200);
            this.buttonOk.Size = new System.Drawing.Size(75, 25);
            this.buttonOk.Text = "Save";
            this.buttonOk.UseVisualStyleBackColor = true;
            this.buttonOk.Click += new System.EventHandler(this.OnSaveClicked);

            //
            // buttonCancel
            //
            this.buttonCancel.Location = new System.Drawing.Point(558, 200);
            this.buttonCancel.Size = new System.Drawing.Size(75, 25);
            this.buttonCancel.Text = "Cancel";
            this.buttonCancel.UseVisualStyleBackColor = true;
            this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;

            //
            // SettingsDialog
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(648, 240);
            this.Controls.Add(this.labelPath);
            this.Controls.Add(this.textPath);
            this.Controls.Add(this.buttonBrowse);
            this.Controls.Add(this.labelBlockName);
            this.Controls.Add(this.textBlockName);
            this.Controls.Add(this.labelPrefix);
            this.Controls.Add(this.textPrefix);
            this.Controls.Add(this.labelTolerance);
            this.Controls.Add(this.textTolerance);
            this.Controls.Add(this.labelSavedTo);
            this.Controls.Add(this.buttonOk);
            this.Controls.Add(this.buttonCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsDialog";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Legend Builder Settings";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
