using System.ComponentModel;

namespace LegendBuilderWW.Models
{
    /// <summary>
    /// A LegendRow joined with drawing-usage information and the user's include/exclude decision.
    /// INotifyPropertyChanged lets the DataGridView react when the checkbox flips.
    /// </summary>
    public class MatchedRow : INotifyPropertyChanged
    {
        private bool _includeInOutput;

        public LegendRow Source { get; set; }
        public bool IsUsedInDrawing { get; set; }
        public int CountInDrawing { get; set; }

        public bool IncludeInOutput
        {
            get { return _includeInOutput; }
            set
            {
                if (_includeInOutput != value)
                {
                    _includeInOutput = value;
                    OnPropertyChanged("IncludeInOutput");
                }
            }
        }

        public RowType RowType
        {
            get { return Source.RowType; }
        }

        public string Description
        {
            get { return Source.Description; }
            set
            {
                if (Source.Description != value)
                {
                    Source.Description = value;
                    OnPropertyChanged("Description");
                }
            }
        }

        public string Key
        {
            get { return Source.Key; }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
