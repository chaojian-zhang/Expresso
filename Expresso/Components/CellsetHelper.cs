/* Author: Charles Zhang 📧Email: charles_zhang@totalimagine.com
 * Version: v0.1.3
 * ALL MODIFICATIONS MUST INCREMENT VERSION NUMBER.
 *
 * Notice for AdomdClient, generally only Microsoft.AnalysisServices.AdomdClient.NetCore.retail.amd64 v19.46.0 works.
 * 
 * # Version Changes
 * 
 * - Pre v0.1.0: Draft definition
 * - v0.1.1 (2023-02-24): Deal with multi-dimensional name columns.
 * - v0.1.2 (2023-02-24): Change contact email address.
 * - v0.1.3 (2023-02-24): Migrate changes from Expresso project.
 */

using Microsoft.AnalysisServices.AdomdClient;
using System;
using System.Data;
using System.Text.RegularExpressions;

namespace DataHelpers
{
    public static class CellsetHelper
    {
        #region Transformations
        public static DataTable CellSetToTableNew(this CellSet cellset)
        {
            DataTable table = new DataTable("cellset");

            if (cellset.Axes.Count == 2)
            {
                Axis columns = cellset.Axes[0];
                Axis rows = cellset.Axes[1];
                if (rows.Set.Tuples.Count == 0)
                    return null;

                CellCollection valuesCell = cellset.Cells;
                for (int i = 0; i < rows.Set.Hierarchies.Count; i++)
                    table.Columns.Add(CaptureMDXName(rows.Set.Hierarchies[i].UniqueName));
                for (int i = 0; i < columns.Set.Tuples.Count; i++)
                    table.Columns.Add(CaptureMDXName(columns.Set.Tuples[i].Members[0].Caption));

                int rowDimensionCount = rows.Set.Tuples[0].Members.Count;
                int rowCellValuesCount = columns.Set.Tuples.Count;
                int rowElementSize = rowDimensionCount + rowCellValuesCount;
                for (int row = 0; row < rows.Set.Tuples.Count; row++)
                {
                    DataRow tableRow = table.NewRow();

                    for (int i = 0; i < rows.Set.Tuples[row].Members.Count; i++)
                        tableRow[i] = CaptureMDXName(rows.Set.Tuples[row].Members[i].Caption);
                    for (int i = 0; i < columns.Set.Tuples.Count; i++)
                        tableRow[i + rows.Set.Hierarchies.Count] = cellset.Cells[row * rowCellValuesCount + i].Value;

                    table.Rows.Add(tableRow);
                }

                return table;
            }
            else if (cellset.Axes.Count == 1)
            {
                Axis columns = cellset.Axes[0];
                CellCollection valuesCell = cellset.Cells;

                for (int i = 0; i < columns.Set.Tuples.Count; i++)
                    table.Columns.Add(new System.Data.DataColumn(columns.Set.Tuples[i].Members[0].Caption));

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

            string CaptureMDXName(string original)
            {
                return Regex.Replace(original, @".*\[(.*?)\]", "$1");
            }
        }
        public static DataTable CellSetToTable(this CellSet cellset)
        {
            DataTable table = new DataTable("cellset");

            Axis columns = cellset.Axes[0];
            Axis rows = cellset.Axes[1];
            CellCollection valuesCell = cellset.Cells;

            table.Columns.Add(rows.Set.Hierarchies[0].Caption);
            for (int i = 0; i < columns.Set.Tuples.Count; i++)
                table.Columns.Add(new System.Data.DataColumn(columns.Set.Tuples[i].Members[0].Caption));
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
        #endregion
    }
}
