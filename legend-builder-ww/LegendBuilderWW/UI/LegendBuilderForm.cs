using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using LegendBuilderWW.Config;
using LegendBuilderWW.Models;

namespace LegendBuilderWW.UI
{
    public partial class LegendBuilderForm : Form
    {
        private BindingList<MatchedRow> _allRows;
        private BindingList<MatchedRow> _visibleRows;
        private readonly Settings _settings;
        private readonly System.Func<List<MatchedRow>, System.Drawing.Image> _previewProvider;

        public List<MatchedRow> SelectedRows { get; private set; }
        public bool SettingsChanged { get; private set; }

        public LegendBuilderForm(
            List<MatchedRow> rows,
            Settings settings,
            System.Func<List<MatchedRow>, System.Drawing.Image> previewProvider)
        {
            InitializeComponent();
            _settings = settings;
            _previewProvider = previewProvider;

            _allRows = new BindingList<MatchedRow>(rows ?? new List<MatchedRow>());
            _visibleRows = new BindingList<MatchedRow>();
            grid.DataSource = _visibleRows;

            comboShow.Items.AddRange(new object[] { "All rows", "Used (in drawing)", "Unused only", "Checked only" });
            comboShow.SelectedIndex = 0;

            comboType.Items.AddRange(new object[] { "All types", "Block", "Linetype", "Hatch" });
            comboType.SelectedIndex = 0;

            ApplyFilters();
        }

        private void ApplyFilters()
        {
            string showMode = comboShow.SelectedItem as string ?? "All rows";
            string typeMode = comboType.SelectedItem as string ?? "All types";
            string search = (textSearch.Text ?? string.Empty).Trim();

            IEnumerable<MatchedRow> query = _allRows;

            switch (showMode)
            {
                case "Used (in drawing)":
                    query = query.Where(r => r.IsUsedInDrawing);
                    break;
                case "Unused only":
                    query = query.Where(r => !r.IsUsedInDrawing);
                    break;
                case "Checked only":
                    query = query.Where(r => r.IncludeInOutput);
                    break;
                case "All rows":
                default:
                    break;
            }

            if (!string.Equals(typeMode, "All types", StringComparison.OrdinalIgnoreCase))
            {
                RowType filter;
                if (Enum.TryParse(typeMode, out filter))
                {
                    query = query.Where(r => r.RowType == filter);
                }
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(r =>
                    (r.Description != null && r.Description.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (r.Key != null && r.Key.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0));
            }

            _visibleRows.RaiseListChangedEvents = false;
            _visibleRows.Clear();
            foreach (MatchedRow row in query)
            {
                _visibleRows.Add(row);
            }
            _visibleRows.RaiseListChangedEvents = true;
            _visibleRows.ResetBindings();

            UpdateSummary();
        }

        private void UpdateSummary()
        {
            // DataGridView raises cell-value-changed events from InitializeComponent (e.g. when
            // HeaderText is set), so this can fire before the constructor has assigned _allRows.
            if (_allRows == null) return;

            int total = _allRows.Count;
            int used = _allRows.Count(r => r.IsUsedInDrawing);
            int checkedCount = _allRows.Count(r => r.IncludeInOutput);
            labelSummary.Text = string.Format(
                "{0} of {1} rows shown   |   Used in drawing: {2}   |   Checked: {3}",
                _visibleRows.Count, total, used, checkedCount);
        }

        private void OnFilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void OnSelectUsedClicked(object sender, EventArgs e)
        {
            foreach (MatchedRow row in _allRows)
            {
                row.IncludeInOutput = row.IsUsedInDrawing;
            }
            _visibleRows.ResetBindings();
            UpdateSummary();
        }

        private void OnClearClicked(object sender, EventArgs e)
        {
            foreach (MatchedRow row in _allRows)
            {
                row.IncludeInOutput = false;
            }
            _visibleRows.ResetBindings();
            UpdateSummary();
        }

        private void OnGridCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            // RowIndex == -1 is a header-cell event; ignore it. These also fire during
            // InitializeComponent before the form has any data.
            if (e.RowIndex < 0) return;
            if (e.ColumnIndex == colInclude.Index)
            {
                UpdateSummary();
            }
        }

        private void OnGridDirtyStateChanged(object sender, EventArgs e)
        {
            // Commit the checkbox immediately so the model + summary stay in sync. The editable
            // Description column commits on cell-leave so typing isn't interrupted per keystroke.
            if (grid.IsCurrentCellDirty &&
                grid.CurrentCell != null &&
                grid.CurrentCell.ColumnIndex == colInclude.Index)
            {
                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            }
        }

        private void OnGenerateClicked(object sender, EventArgs e)
        {
            SelectedRows = _allRows.Where(r => r.IncludeInOutput).ToList();
            if (SelectedRows.Count == 0)
            {
                MessageBox.Show(
                    this,
                    "No rows are checked. Tick the rows you want in the legend and try again.",
                    "Legend Builder",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }
            DialogResult = DialogResult.OK;
            Close();
        }

        private void OnSettingsClicked(object sender, EventArgs e)
        {
            using (SettingsDialog dlg = new SettingsDialog(_settings))
            {
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    SettingsChanged = true;
                }
            }
        }

        private void OnPreviewClicked(object sender, EventArgs e)
        {
            if (_previewProvider == null) return;

            if (!_allRows.Any(r => r.IncludeInOutput))
            {
                MessageBox.Show(this, "Check at least one row to preview.", "Legend Builder",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            System.Drawing.Image image;
            using (new WaitCursorScope())
            {
                try { image = _previewProvider(_allRows.ToList()); }
                catch { image = null; }
            }

            if (image == null)
            {
                MessageBox.Show(this,
                    "Preview is unavailable (the symbols could not be rendered).",
                    "Legend Builder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (Form window = BuildPreviewWindow(image))
            {
                window.ShowDialog(this);
            }
            image.Dispose();
        }

        private Form BuildPreviewWindow(System.Drawing.Image image)
        {
            Form window = new Form();
            window.Text = "Legend Preview";
            window.StartPosition = FormStartPosition.CenterParent;
            window.ShowIcon = false;
            window.MinimizeBox = false;
            window.Width = System.Math.Min(1000, image.Width + 40);
            window.Height = System.Math.Min(800, image.Height + 60);

            PictureBox picture = new PictureBox();
            picture.Dock = DockStyle.Fill;
            picture.SizeMode = PictureBoxSizeMode.Zoom;
            picture.BackColor = System.Drawing.Color.White;
            picture.Image = image;

            window.Controls.Add(picture);
            return window;
        }

        private sealed class WaitCursorScope : IDisposable
        {
            private readonly Cursor _previous;
            public WaitCursorScope() { _previous = Cursor.Current; Cursor.Current = Cursors.WaitCursor; }
            public void Dispose() { Cursor.Current = _previous; }
        }
    }
}
