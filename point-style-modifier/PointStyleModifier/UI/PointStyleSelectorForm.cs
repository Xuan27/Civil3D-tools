using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using PointStyleModifier.Models;

namespace PointStyleModifier.UI
{
    /// <summary>
    /// Windows Forms dialog for selecting point and/or label styles
    /// </summary>
    public partial class PointStyleSelectorForm : Form
    {
        private List<PointStyleInfo> allPointStyles;
        private List<LabelStyleInfo> allLabelStyles;

        /// <summary>
        /// Gets the point style selected by the user
        /// </summary>
        public PointStyleInfo SelectedPointStyle { get; private set; }

        /// <summary>
        /// Gets the label style selected by the user
        /// </summary>
        public LabelStyleInfo SelectedLabelStyle { get; private set; }

        /// <summary>
        /// Gets whether the user wants to modify the point style
        /// </summary>
        public bool ModifyPointStyle => checkBoxPointStyle.Checked;

        /// <summary>
        /// Gets whether the user wants to modify the label style
        /// </summary>
        public bool ModifyLabelStyle => checkBoxLabelStyle.Checked;

        public PointStyleSelectorForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Populates the list boxes with available styles
        /// </summary>
        public void PopulateStyles(List<PointStyleInfo> pointStyles, List<LabelStyleInfo> labelStyles)
        {
            allPointStyles = pointStyles ?? new List<PointStyleInfo>();
            allLabelStyles = labelStyles ?? new List<LabelStyleInfo>();

            FilterPointStyles();
            FilterLabelStyles();
        }

        private void FilterPointStyles()
        {
            string searchText = textBoxSearchPointStyle.Text.ToLower();
            listBoxPointStyles.Items.Clear();

            var filtered = allPointStyles.Where(s => s.Name.ToLower().Contains(searchText)).ToList();

            foreach (var style in filtered)
            {
                listBoxPointStyles.Items.Add(style);
            }

            if (listBoxPointStyles.Items.Count > 0)
            {
                listBoxPointStyles.SelectedIndex = 0;
            }
        }

        private void FilterLabelStyles()
        {
            string searchText = textBoxSearchLabelStyle.Text.ToLower();
            listBoxLabelStyles.Items.Clear();

            var filtered = allLabelStyles.Where(s => s.Name.ToLower().Contains(searchText)).ToList();

            foreach (var style in filtered)
            {
                listBoxLabelStyles.Items.Add(style);
            }

            if (listBoxLabelStyles.Items.Count > 0)
            {
                listBoxLabelStyles.SelectedIndex = 0;
            }
        }

        private void textBoxSearchPointStyle_TextChanged(object sender, EventArgs e)
        {
            FilterPointStyles();
        }

        private void textBoxSearchLabelStyle_TextChanged(object sender, EventArgs e)
        {
            FilterLabelStyles();
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            if (!checkBoxPointStyle.Checked && !checkBoxLabelStyle.Checked)
            {
                MessageBox.Show(
                    "Please select at least one modification option (Point Style or Label Style).",
                    "No Option Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (checkBoxPointStyle.Checked && listBoxPointStyles.SelectedItem == null)
            {
                MessageBox.Show(
                    "Please select a point style.",
                    "No Point Style Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            if (checkBoxLabelStyle.Checked && listBoxLabelStyles.SelectedItem == null)
            {
                MessageBox.Show(
                    "Please select a label style.",
                    "No Label Style Selected",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            SelectedPointStyle = listBoxPointStyles.SelectedItem as PointStyleInfo;
            SelectedLabelStyle = listBoxLabelStyles.SelectedItem as LabelStyleInfo;

            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void checkBoxPointStyle_CheckedChanged(object sender, EventArgs e)
        {
            textBoxSearchPointStyle.Enabled = checkBoxPointStyle.Checked;
            listBoxPointStyles.Enabled = checkBoxPointStyle.Checked;
        }

        private void checkBoxLabelStyle_CheckedChanged(object sender, EventArgs e)
        {
            textBoxSearchLabelStyle.Enabled = checkBoxLabelStyle.Checked;
            listBoxLabelStyles.Enabled = checkBoxLabelStyle.Checked;
        }
    }
}
