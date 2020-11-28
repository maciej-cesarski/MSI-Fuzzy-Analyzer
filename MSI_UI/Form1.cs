using System;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using MSI_Logic;
using System.Data;

namespace MSI_UI
{
    public partial class Form1 : Form
    {
        GroupBox projectsGroupBox;
        GroupBox revisorsGroupBox;
        GroupBox resultsGroupBox;

        DataFrame projectsDataFrame;
        DataFrame revisorsDataFrame;
        DataFrame resultsDataFrame;

        DataGridView projectsDataGrid = GetInitialDataGrid();
        DataGridView revisorsDataGrid = GetInitialDataGrid();
        DataGridView resultsDataGrid = GetInitialDataGrid();

        public Form1()
        {
            InitializeComponent();

            IntializeProjectsComponent();
            IntializeRevisorsComponent();
            IntializeResultsComponent();

            RecalculateUI();
        }

        public GroupBox GetProjectsGroupBox(DataFrame df, DataGridView grid)
        {
            GroupBox box = new GroupBox();
            box.Size = new Size(100, 500);
            box.Parent = this;
            box.BackColor = Color.AntiqueWhite;
            Button loadButton = GetProjectsLoadButton();
            loadButton.Parent = box;
            grid.Parent = box;

            return box;
        }

        public GroupBox GetRevisorsGroupBox(DataFrame df, DataGridView grid)
        {
            GroupBox box = new GroupBox();
            box.Size = new Size(100, 500);
            box.Parent = this;
            box.BackColor = Color.FloralWhite;
            Button loadButton = GetRevisorsLoadButton();
            loadButton.Parent = box;
            grid.Parent = box;

            return box;
        }

        public Button GetProjectsLoadButton()
        {
            Button loadButton = new Button();
            loadButton.Anchor = AnchorStyles.Top;
            loadButton.Text = "Wczytaj z pliku";
            loadButton.Click += LoadProjects;
            loadButton.Size = new Size(100, 30);

            return loadButton;
        }

        private void LoadProjects(object sender, EventArgs e)
        {
            LoadFileToDataFrameAndFillDataGrid(ref projectsDataFrame, projectsDataGrid, "Projekt");
            RecalculateUI();
        }

        public Button GetRevisorsLoadButton()
        {
            Button loadButton = new Button();
            loadButton.Anchor = AnchorStyles.Top;
            loadButton.Text = "Wczytaj z pliku";
            loadButton.Click += LoadRevisors;
            loadButton.Size = new Size(100, 30);

            return loadButton;
        }

        private void LoadRevisors(object sender, EventArgs e)
        {
            LoadFileToDataFrameAndFillDataGrid(ref revisorsDataFrame, revisorsDataGrid, "Osoba");
            RecalculateUI();
        }

        public GroupBox GetOutputGroupBox(DataFrame df, DataGridView grid)
        {
            GroupBox box = new GroupBox();
            box.Size = new Size(100, 500);
            box.Parent = this;
            box.BackColor = Color.LightGray;
            Button loadButton = GetBaseCalculateButton(() =>
            {
                CalculateResults();
                RecalculateUI();
            });
            loadButton.Parent = box;
            loadButton.Anchor = AnchorStyles.Top | AnchorStyles.Left;

            Button saveButton = new Button();
            saveButton.Click += (object o, EventArgs ea) => { SaveOutputToFile(); };
            saveButton.Parent = box;
            saveButton.Text = "Zapisz do pliku";
            loadButton.Anchor = AnchorStyles.Right | AnchorStyles.Top;
            saveButton.Size = new Size(100, 30);

            grid.Parent = box;
            grid.RowHeadersWidth = 80;

            return box;
        }

        public void SaveOutputToFile()
        {
            var dialog = new SaveFileDialog();

            var result = dialog.ShowDialog();

            if (result == DialogResult.OK || result == DialogResult.Yes)
            {
                try
                {
                    var lines = resultsDataFrame.ToStringTable();
                    System.IO.File.WriteAllLines(dialog.FileName, lines);
                }
                catch(Exception ex)
                {
                    string message = "Nie udało się zapisać danych do wybranego pliku.";
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        message += $"\n\n{ex}";
                    }
                    MessageBox.Show(message, "Błąd przy próbie zapisu pliku");
                    return;
                }

                MessageBox.Show("Poprawnie zapisano dane do pliku.", "Sukces!");
            }

            
        }

        public bool RevalidateDataFromUI()
        {
            return RevalidateDataFromGrid(projectsDataGrid, projectsDataFrame, "Projekty")
                && RevalidateDataFromGrid(revisorsDataGrid, revisorsDataFrame, "Recenzenci");
        }

        public bool RevalidateDataFromGrid(DataGridView input, DataFrame output, string gridName)
        {
            int lastRowIndex = input.Rows.Count - 1;
            int errorsCount = 0;
            //validating last row in input
            for (int i=0; i<output.Cols; i++)
            {
                try
                {
                    string value = input.Rows[lastRowIndex].Cells[i].Value.ToString();
                    if (value == null || value == "")
                    {
                        errorsCount++;
                    }
                }
                catch
                {
                    errorsCount++;
                }                
            }

            if (errorsCount != output.Cols)
            {
                string messageBase = $"Błąd podczas walidacji danych wejściowych.\n\nTabela: {gridName}\n\nPrzyczyna błędu: ostatni wiersz jest niekompletny.";
                MessageBox.Show(messageBase, "Błąd w danych");
                return false;
            }

            int newRowCount = input.Rows.Count - 1;

            float[][] newData = new float[newRowCount][];

            for (int i = 0; i < newRowCount; i++)
            {
                newData[i] = new float[output.Cols];
            }

            for (int i=0; i< newRowCount; i++)
            {
                for(int j = 1; j< output.Cols; j++)
                {
                    bool stringFormatError = false;
                    
                    string valueToParse = "";
                    string trimmedValueToParse = "";

                    try
                    {
                        valueToParse = input.Rows[i].Cells[j].Value.ToString();
                        trimmedValueToParse = String.Format("{0:0.00}", valueToParse).Replace(',','.');
                    }
                    catch
                    {
                        stringFormatError = true;
                    }
                    
                    bool parseError = !float.TryParse(trimmedValueToParse, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed);
                    bool outside01RangeError = parsed > 1 || parsed < 0;

                    string messageBase = $"Błąd podczas walidacji danych wejściowych.\n\nTabela: {gridName}\nWiersz: {i+1}\nKolumna: {j+1}\nBłędna wartość: {valueToParse} \n\nPrzyczyna błędu: ";
                    string messageReason = "";

                    if (stringFormatError) messageReason = "Nie udało się zamienić wartości na napis.";
                    if (parseError) messageReason = "Nie udało się zamienić wartości na liczbę zmiennoprzecinkową.";
                    if (outside01RangeError) messageReason = "Wartość jest spoza zakresu [0, 1].";

                    if (!string.IsNullOrEmpty(messageReason))
                    {
                        MessageBox.Show(messageBase + messageReason, "Błąd w danych");
                        return false;
                    }

                    newData[i][j-1] = parsed;
                }
            }

            output.data = newData;
            return true;
        }

        public void CalculateResults()
        {
            if (projectsDataFrame == null)
            {
                MessageBox.Show("Najpierw wczytaj dane projektów.", "Brak danych!");
                return;
            }

            if (revisorsDataFrame == null)
            {
                MessageBox.Show("Najpierw wczytaj dane recenzentów.", "Brak danych!");
                return;
            }

            if (projectsDataFrame.Cols != revisorsDataFrame.Cols)
            {
                MessageBox.Show($"Plik projektów ma inną liczbę kolumn {projectsDataFrame.Cols} niż plik recenzentów {revisorsDataFrame.Cols}.", "Brak danych!");
                return;
            }

            if (RevalidateDataFromUI() == false) return;

            resultsDataFrame = DataFrame.Calculate(projectsDataFrame, revisorsDataFrame);
            string[] headers = new string[resultsDataFrame.Cols];
            for (int i = 0; i < resultsDataFrame.Cols; i++)
            {
                headers[i] = $"Praca dyplomowa {i + 1}";
            }
            //FillDataGrid(resultsDataGrid, resultsDataFrame, GetRevisorNames(), GetProjectNames());
            FillOutputDataGrid(resultsDataGrid, resultsDataFrame, GetProjectNames(), GetRevisorNames());
            MarkBestRowsInResults();
        }

        public string[] GetRevisorNames()
        {
            List<string> names = new List<string>();

            for(int i=0; i<revisorsDataGrid.Rows.Count-1; i++)
            {
                names.Add(revisorsDataGrid.Rows[i].Cells[0].Value.ToString());
            }

            return names.ToArray();
        }

        public string[] GetProjectNames()
        {
            List<string> names = new List<string>();

            for (int i = 0; i < projectsDataGrid.Rows.Count-1; i++)
            {
                names.Add(projectsDataGrid.Rows[i].Cells[0].Value.ToString());
            }

            return names.ToArray();
        }

        public static DataGridView GetInitialDataGrid()
        {
            DataGridView dataGrid = new DataGridView();
            return dataGrid;
        }

        public DataGridView FillDataGrid(DataGridView dataGrid, DataFrame df, string[] header, string[] rowNames, string firstColumnName = null)
        {
            DataTable dataTable = new DataTable();

            if (firstColumnName != null)
            {
                dataTable.Columns.Add(firstColumnName, typeof(string));
            }            

            for (int i=0; i<header.Length; i++)
            {
                dataTable.Columns.Add(header[i], typeof(float));
            }

            for(int i=0; i<df.Rows; i++)
            {
                var row = df.GetRow(i);

                List<object> entities = new List<object>();
                entities.Add(rowNames[i]);
                
                foreach(var v in row)
                {
                    entities.Add(v);
                }
                
                dataTable.Rows.Add(entities.ToArray());
            }

            dataGrid.DataSource = dataTable;

            return dataGrid;
        }

        public DataGridView FillOutputDataGrid(DataGridView dataGrid, DataFrame df, string[] colNames, string[] rowNames)
        {
            DataTable dataTable = new DataTable();

            for (int i = 0; i < colNames.Length; i++)
            {
                dataTable.Columns.Add(colNames[i], typeof(float));
            }

            for (int i = 0; i < df.Rows; i++)
            {
                var row = df.GetRow(i);

                List<object> entities = new List<object>();

                foreach (var v in row)
                {
                    entities.Add(v);
                }

                dataTable.Rows.Add(entities.ToArray());
            }

            dataGrid.DataSource = dataTable;

            for(int i=0; i<rowNames.Length; i++)
            {
                dataGrid.Rows[i].HeaderCell.Value = rowNames[i];
            }

            return dataGrid;
        }

        public void ClearDataGrid(DataGridView dataGrid)
        {
            dataGrid.Rows.Clear();
            dataGrid.Columns.Clear();
        }      

        public Button GetBaseCalculateButton(Action action)
        {
            Button loadButton = new Button();
            loadButton.Anchor = AnchorStyles.Top;
            loadButton.Text = "Oblicz";
            loadButton.Click += (object sender, EventArgs e) => { action.Invoke(); };
            loadButton.Size = new Size(100, 30);

            return loadButton;
        }

        public void LoadFileToDataFrameAndFillDataGrid(ref DataFrame df, DataGridView dataGrid, string firstColumnName)
        {
            OpenFileDialog dialog = new OpenFileDialog();

            var result = dialog.ShowDialog();

            if (result == DialogResult.OK || result == DialogResult.Yes)
            {
                try
                {
                    string[] dataRows = File.ReadAllLines(dialog.FileName);
                    string[] headers = null;
                    headers = dataRows[0].Split(';');
                    List<string> rowNames = new List<string>();
                    List<string> numberData = new List<string>();

                    foreach(var row in dataRows.Skip(1))
                    {
                        var splitted = row.Split(';').ToList();
                        rowNames.Add(splitted[0]);
                        numberData.Add(string.Join(";", splitted.Skip(1).ToArray()));
                    }

                    df = new DataFrame(numberData.ToArray());

                    FillDataGrid(dataGrid, df, headers.Skip(1).ToArray(), rowNames.ToArray(), firstColumnName);
                }
                catch (Exception ex)
                {
                    string message = $"Nie udało się wczytać pliku {System.IO.Path.GetFileName(dialog.FileName)}.";
                    string caption = "Błąd przy wczytywaniu pliku.";
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        message += $"\n\n{ex}";
                    }
                    MessageBox.Show(message, caption);
                }
            }
        }

        public void IntializeProjectsComponent()
        {
            projectsGroupBox = GetProjectsGroupBox(projectsDataFrame, projectsDataGrid);
            projectsGroupBox.Text = "Projekty";
        }

        public void IntializeRevisorsComponent()
        {
            revisorsGroupBox = GetRevisorsGroupBox(revisorsDataFrame, revisorsDataGrid);
            revisorsGroupBox.Text = "Recenzenci";
        }

        public void IntializeResultsComponent()
        {
            resultsGroupBox = GetOutputGroupBox(resultsDataFrame, resultsDataGrid);
            resultsGroupBox.Text = "Wyniki";
        }

        public void RecalculateUI()
        {
            int width = this.Width;
            int height = this.Height;
            int panelSpacingX = 30;
            int panelSpacingY = 30;
            int panelCount = 3;

            int singlePanelWidth = (width - 4 * panelSpacingX) / panelCount;
            int singlePanelHeight = this.Height - panelSpacingY * 3;

            projectsGroupBox.Size = new Size(singlePanelWidth, singlePanelHeight);
            revisorsGroupBox.Size = new Size(singlePanelWidth, singlePanelHeight);
            resultsGroupBox.Size = new Size(singlePanelWidth, singlePanelHeight);

            projectsGroupBox.Location = new Point(panelSpacingX * 1 + singlePanelWidth * 0, panelSpacingY);
            revisorsGroupBox.Location = new Point(panelSpacingX * 2 + singlePanelWidth * 1, panelSpacingY);
            resultsGroupBox.Location = new Point(panelSpacingX * 3 + singlePanelWidth * 2, panelSpacingY);

            int dataGridOffsetX = 20;
            int dataGridOffsetY = 50;

            int dataGridWidth = singlePanelWidth - dataGridOffsetX * 2;
            int dataGridHeight = singlePanelHeight - dataGridOffsetY * 2;

            if (projectsDataGrid != null)
            {
                projectsDataGrid.Location = new Point(dataGridOffsetX, dataGridOffsetY);
                projectsDataGrid.Size = new Size(dataGridWidth, dataGridHeight);
                SetColumnWidthInDataGrid(projectsDataGrid);
            }

            if (revisorsDataGrid != null)
            {
                revisorsDataGrid.Location = new Point(dataGridOffsetX, dataGridOffsetY);
                revisorsDataGrid.Size = new Size(dataGridWidth, dataGridHeight);
                SetColumnWidthInDataGrid(revisorsDataGrid);
            }

            if (resultsDataGrid != null)
            {
                resultsDataGrid.Location = new Point(dataGridOffsetX, dataGridOffsetY);
                resultsDataGrid.Size = new Size(dataGridWidth, dataGridHeight);
                SetColumnWidthInDataGrid(resultsDataGrid);
            }
        }

        public void MarkBestRowsInResults()
        {
            for (int column = 0; column < resultsDataFrame.Cols; column++)
            {
                int bestRow = 0;

                for (int row = 0; row < resultsDataFrame.Rows; row++)
                {
                    if (resultsDataFrame.data[row][column] < resultsDataFrame.data[bestRow][column])
                    {
                        bestRow = row;
                    }
                }

                var marked = resultsDataGrid.Rows[bestRow].Cells[column];
                marked.Style.BackColor = Color.BurlyWood;
            }
        }

        public void SetColumnWidthInDataGrid(DataGridView grid)
        {
            int width = GetColumnWidth(grid);
            for (int i = 0; i < grid.Columns.Count; i++)
            {
                grid.Columns[i].Width = width;
            }
        }

        private readonly int MinColumnWidth = 40;

        public int GetColumnWidth(DataGridView grid)
        {
            if (grid.Columns.Count == 0) return MinColumnWidth;
            int calculatedWidth = (grid.Width - 50) / grid.Columns.Count;
            return calculatedWidth > MinColumnWidth ? calculatedWidth : MinColumnWidth;
        }

        private void Form1_ResizeEnd(object sender, EventArgs e)
        {
            RecalculateUI();
        }

        private void Form1_MaximumSizeChanged(object sender, EventArgs e)
        {
            RecalculateUI();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            RecalculateUI();
        }
    }
}
