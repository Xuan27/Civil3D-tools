using System;
using System.Globalization;
using System.Windows.Forms;
using LegendBuilderWW.Config;

namespace LegendBuilderWW.UI
{
    public partial class SettingsDialog : Form
    {
        private readonly Settings _settings;

        public SettingsDialog(Settings settings)
        {
            InitializeComponent();
            _settings = settings;

            textPath.Text = settings.SourceDwgPath ?? string.Empty;
            textBlockName.Text = settings.SourceBlockName ?? string.Empty;
            textPrefix.Text = settings.OutputBlockNamePrefix ?? string.Empty;
            textTolerance.Text = settings.RowGroupingTolerance.ToString("0.###", CultureInfo.InvariantCulture);
            labelSavedTo.Text = "Settings file: " + Settings.SettingsFilePath;
        }

        private void OnBrowseClicked(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "AutoCAD drawings (*.dwg)|*.dwg|All files (*.*)|*.*";
                dlg.Title = "Select source DWG containing the legend block";
                if (!string.IsNullOrEmpty(textPath.Text))
                {
                    try { dlg.InitialDirectory = System.IO.Path.GetDirectoryName(textPath.Text); }
                    catch { }
                }
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    textPath.Text = dlg.FileName;
                }
            }
        }

        private void OnSaveClicked(object sender, EventArgs e)
        {
            double tolerance;
            if (!double.TryParse(textTolerance.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out tolerance) ||
                tolerance <= 0)
            {
                MessageBox.Show(this, "Row grouping tolerance must be a positive number.", "Settings",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _settings.SourceDwgPath = textPath.Text.Trim();
            _settings.SourceBlockName = textBlockName.Text.Trim();
            _settings.OutputBlockNamePrefix = textPrefix.Text.Trim();
            _settings.RowGroupingTolerance = tolerance;

            try
            {
                _settings.Save();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Failed to save settings: " + ex.Message, "Settings",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
