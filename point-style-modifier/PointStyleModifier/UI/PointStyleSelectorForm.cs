using System;
using System.Collections.Generic;
using System.Windows.Forms;
using PointStyleModifier.Models;

namespace PointStyleModifier.UI
{
    /// <summary>
    /// Windows Forms dialog for selecting a point style
    /// </summary>
    public partial class PointStyleSelectorForm : Form
    {
        /// <summary>
        /// Gets the point style selected by the user
        /// </summary>
        public PointStyleInfo SelectedPointStyle { get; private set; }

        public PointStyleSelectorForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Populates the list box with available point styles
        /// </summary>
        /// <param name="styles">List of available point styles</param>
        public void PopulatePointStyles(List<PointStyleInfo> styles)
        {
            listBoxStyles.Items.Clear();

            if (styles != null && styles.Count > 0)
            {
                foreach (PointStyleInfo style in styles)
                {
                    listBoxStyles.Items.Add(style);
                }

                // Select the first item by default
                if (listBoxStyles.Items.Count > 0)
                {
                    listBoxStyles.SelectedIndex = 0;
                }
            }
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            if (listBoxStyles.SelectedItem == null)
            {
                MessageBox.Show(
                    "Please select a point style.",
                    "No Selection",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return;
            }

            SelectedPointStyle = listBoxStyles.SelectedItem as PointStyleInfo;
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
            Close();
        }

        private void listBoxStyles_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxStyles.SelectedItem != null)
            {
                buttonOk_Click(sender, e);
            }
        }
    }
}
