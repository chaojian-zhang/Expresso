using Microsoft.AnalysisServices.AdomdClient;
using System;
using System.Data;

namespace Expresso.Services
{
    internal static class CellsetExtension
    {
        public static DataTable CellSetToTable(this CellSet cellset)
        {
            DataTable table = new DataTable("cellset");

            if (cellset.Axes.Count == 2)
            {
                Axis columns = cellset.Axes[0];
                Axis rows = cellset.Axes[1];
                CellCollection valuesCell = cellset.Cells;

                table.Columns.Add(rows.Set.Hierarchies[0].Caption);
                for (int i = 0; i < columns.Set.Tuples.Count; i++)
                    table.Columns.Add(new DataColumn(columns.Set.Tuples[i].Members[0].Caption));
                int valuesIndex = 0;
                DataRow row = null;

                for (int i = 0; i < rows.Set.Tuples.Count; i++)
                {
                    row = table.NewRow();

                    row[0] = rows.Set.Tuples[i].Members[0].Caption;
                    for (int k = 1; k <= columns.Set.Tuples.Count; k++)
                    {
                        row[k] = valuesCell[valuesIndex].Value;
                        valuesIndex++;
                    }
                    table.Rows.Add(row);
                }

                return table;
            }
            else if (cellset.Axes.Count == 1)
            {
                Axis columns = cellset.Axes[0];
                CellCollection valuesCell = cellset.Cells;

                for (int i = 0; i < columns.Set.Tuples.Count; i++)
                    table.Columns.Add(new DataColumn(columns.Set.Tuples[i].Members[0].Caption));

                int valuesIndex = 0;
                DataRow row = row = table.NewRow();
                for (int k = 0; k < columns.Set.Tuples.Count; k++)
                {
                    row[k] = valuesCell[valuesIndex].Value;
                    valuesIndex++;
                }
                table.Rows.Add(row);

                return table;
            }
            else throw new ArgumentException("Unrecognized cellset format.");
        }
    }
}
