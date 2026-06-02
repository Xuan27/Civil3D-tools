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

        public List<MatchedRow> SelectedRows { get; private set; }
        public bool SettingsChanged { get; private set; }

        public LegendBuilderForm(List<MatchedRow> rows, Settings settings)
        {
            InitializeComponent();
            _settings = settings;

            _allRows = new BindingList<MatchedRow>(rows ?? new List<MatchedRow>());
            _visibleRows = new BindingList<MatchedRow>();
            grid.DataSource = _visibleRows;

            comboShow.Items.AddRange(new object[] { "Used (in drawing)", "All rows", "Unused only", "Checked only" });
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
            if (e.ColumnIndex == colInclude.Index)
            {
                UpdateSummary();
            }
        }

        private void OnGridDirtyStateChanged(object sender, EventArgs e)
        {
            // Commit checkbox edits immediately so the model + summary stay in sync.
            if (grid.IsCurrentCellDirty)
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
    }
}
