using Myra.Graphics2D.UI;
using Myra.Graphics2D.UI.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Myra.Samples.DataGrid;

public partial class MainForm : VerticalStackPanel
{
	public CheckButton _checkShowGridLines { get; set; }
	public CheckButton _checkResizableColumns { get; set; }
	public CheckButton _checkHasHeader { get; set; }
	public CheckButton _checkHasIndexColumn { get; set; }
	public CheckButton _checkSortableHeaders { get; set; }
	public CheckButton _checkHasFilter { get; set; }
	public ComboView _comboFillColumn { get; set; }
	public HorizontalSplitPane _splitPane { get; set; } 
	public PropertyGrid _propertyGrid { get; set; }

    public Myra.Graphics2D.UI.Data.DataGrid _dataGrid { get; set; }
	private List<Record> _records { get; set; }

	public void Init()
	{  
		var csvPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "customers-10000.csv");
		_records = ParseCsv(csvPath);
		_dataGrid.Data = _records;

		_dataGrid.SelectedIndexChanged += (s, a) => _propertyGrid.Object = _dataGrid.SelectedItem;
		   
		_splitPane.SetSplitterPosition(0, 0.8f);

		_checkShowGridLines.IsChecked = _dataGrid.ShowGridLines;
		_checkResizableColumns.IsChecked = _dataGrid.ResizableColumns;
		_checkHasHeader.IsChecked = _dataGrid.HasHeader;
		_checkHasIndexColumn.IsChecked = _dataGrid.HasIndexColumn;
		_checkSortableHeaders.IsChecked = _dataGrid.SortableHeaders;
		_checkHasFilter.IsChecked = _dataGrid.HasFilter;

		_checkShowGridLines.IsCheckedChanged += (s, a) => _dataGrid.ShowGridLines = _checkShowGridLines.IsChecked;
		_checkResizableColumns.IsCheckedChanged += (s, a) => _dataGrid.ResizableColumns = _checkResizableColumns.IsChecked;
		_checkHasHeader.IsCheckedChanged += (s, a) => _dataGrid.HasHeader = _checkHasHeader.IsChecked;
		_checkHasIndexColumn.IsCheckedChanged += (s, a) => _dataGrid.HasIndexColumn = _checkHasIndexColumn.IsChecked;
		_checkSortableHeaders.IsCheckedChanged += (s, a) => _dataGrid.SortableHeaders = _checkSortableHeaders.IsChecked;
		_checkHasFilter.IsCheckedChanged += (s, a) => _dataGrid.HasFilter = _checkHasFilter.IsChecked;

		_comboFillColumn.SelectedIndexChanged += (s, a) =>
		{
			if (_comboFillColumn.SelectedIndex == null || _comboFillColumn.SelectedIndex == 0)
			{
				_dataGrid.FillColumnIndex = null;
			}
			else
			{
				_dataGrid.FillColumnIndex = _comboFillColumn.SelectedIndex.Value - 1;
			}
		};

		_propertyGrid.PropertyChanged += PropertyGrid_PropertyChanged;
	}

	private void PropertyGrid_PropertyChanged(object sender, Events.GenericEventArgs<string> e)
	{
		if (_propertyGrid.Object == null)
		{
			return;
		}

		var record = (Record)_propertyGrid.Object;
		var index = _records.IndexOf(record);
		_dataGrid.InvalidateDataRow(index);
	}

	/// <summary>
	/// Parses a CSV file into a list of <see cref="Record"/> objects, skipping the header line.
	/// </summary>
	/// <param name="path">The full path to the CSV file.</param>
	/// <returns>A list of parsed customer records.</returns>
	private static List<Record> ParseCsv(string path)
	{
		var records = new List<Record>();
		var lines = File.ReadAllLines(path);

		foreach (var line in lines.Skip(1))
		{
			var fields = ParseCsvLine(line);
			if (fields.Count < 12)
				continue;

			records.Add(new Record
			{
				Index = int.Parse(fields[0]),
				CustomerId = fields[1],
				FirstName = fields[2],
				LastName = fields[3],
				Company = fields[4],
				City = fields[5],
				Country = fields[6],
				Phone1 = fields[7],
				Phone2 = fields[8],
				Email = fields[9],
				SubscriptionDate = fields[10],
				Website = fields[11]
			});
		}

		return records;
	}

	/// <summary>
	/// Splits a single CSV line into fields, correctly handling quoted values with escaped double-quotes.
	/// </summary>
	/// <param name="line">A single line of CSV text.</param>
	/// <returns>A list of field values.</returns>
	private static List<string> ParseCsvLine(string line)
	{
		var fields = new List<string>();
		var current = new System.Text.StringBuilder();
		bool inQuotes = false;

		for (int i = 0; i < line.Length; i++)
		{
			char c = line[i];

			if (inQuotes)
			{
				if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
				{
					current.Append('"');
					i++;
				}
				else if (c == '"')
				{
					inQuotes = false;
				}
				else
				{
					current.Append(c);
				}
			}
			else
			{
				if (c == '"')
				{
					inQuotes = true;
				}
				else if (c == ',')
				{
					fields.Add(current.ToString());
					current.Clear();
				}
				else
				{
					current.Append(c);
				}
			}
		}

		fields.Add(current.ToString());
		return fields;
	}
}