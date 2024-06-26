﻿# Expresso (Main Project)

Self-contained, modularized.

## Road Map

1. V1: Variable, Condition, Reader, Writer, Processor, Workflow
2. V2: Visualization, Algorithms Toolbox (Reader and Processor)
3. V3: Large numerical data optimization

## Issue

* Notice when fetching from Database it's possible that cell values are NULL - at the moment Parcel Data Grid is not capable of handling that. This is especially common with Microsoft Analysis Service when data are not available. DO NOT HANDLE IT AT THE SITE OF QUERY - IF THE QUERY RETURNS CELL VALUE AS NULL, THEN IT SHOULD BE NULL. AND MICROSOFT.DATA.DATATABLE WILL CONVERT IT AS DbNull which makes sense and ParcelDataGrid can take this as an object (and at the moment ParcelDataGrid can't really take a null actually). So this issue should be handled only at the site of the end user.

## Technical Note

All data transformation generally have the following structure:

1. All readers fetch data from arbitrary resource and store as CSV as final output. This step can be optimized.
2. All CSV output from all readers are serialized into a single in-memory DB context.
3. All variables, conditions, row processors and writers read data from the in-memory DB context.
4. During workflow execution, a dependency list on all readers and variable permutations are computed, then from that we fetch/refetch readers for each input configuration; Only relevant readers and writers are involved in this process.

Additional notes:

* Normal (GUI) application evaluates all queries on-demand, while during workflow execution and headless mode, query results are cached.
* Internally, all intermediate query results (unless transformed within the same in-memory SQLite context) are saved as CSV strings.
* At the moment of implmenetation, all readers fetch data into csv and all data are treated as string during storage time.
* Executable size:
	* Base (Csv, Console Tables): 0-3MB
	* K4os Compression: 3-4MB
	* Avalon Edit: 4-6MB
	* ExcelDataReader: 6-7MB
	* ODBC, Analysis Service: 7-8MB
	* Microsoft.Data.Sqlite: 9-10MB
	* ScottPlot: 10-12MB
	* Python.Net: 12MB-13.5MB
