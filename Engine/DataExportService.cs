using DucatiMeccaExcelApi.Utility;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Formatting = Newtonsoft.Json.Formatting;

namespace DucatiMeccaExcelApi.Engine
{
    public class DataExportService
    {

        private readonly string _connectionString;
        private Stopwatch _stopwatch;

        public DataExportService(string connectionString)
        {
            _connectionString = connectionString;
            _stopwatch = new Stopwatch();
        }

        public DataTable Execute(string programName, ProgramType programType, List<SqlParameter> parameters, string whereClause)
        {
            return programType == ProgramType.StoredProcedure ?
                ExecuteStoredProcedure(programName, parameters) :
                ExecuteFunction(programName, parameters, whereClause);
        }

        private DataTable ExecuteStoredProcedure(string procedureName, List<SqlParameter> parameters)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand(procedureName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0;

                if (parameters != null && parameters.Count > 0)
                    cmd.Parameters.AddRange(parameters.ToArray());

                return ReturnDataTable(procedureName, cmd);
            }
        }        

        private DataTable ExecuteFunction(string functionName, List<SqlParameter> parameters, string whereClause)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                Console.WriteLine($"Inizio ExecuteReader... {DateTime.Now}");

                string paramList = string.Join(", ", parameters.Select(p => p.ParameterName));
                cmd.CommandText = $"SELECT * FROM {functionName}({paramList}) " + whereClause;
                cmd.CommandTimeout = 0;

                if (parameters != null && parameters.Count > 0)
                    cmd.Parameters.AddRange(parameters.ToArray());

                return ReturnDataTable(functionName, cmd);
            }
        }

        private DataTable ReturnDataTable(string sqlObjectName, SqlCommand cmd)
        {
            Console.WriteLine($"Inizio [{sqlObjectName}]... {DateTime.Now}");

            // --- Esegui la stored procedure e riempi la DataTable originale ---
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            // --- Crea una nuova DataTable con tutte le colonne come stringhe ---
            DataTable resultDataTable = new DataTable(sqlObjectName);
            foreach (DataColumn col in dt.Columns)
            {
                resultDataTable.Columns.Add(col.ColumnName, typeof(string));
            }

            // --- Copia tutti i dati convertendoli in stringhe ---
            foreach (DataRow row in dt.Rows)
            {
                DataRow newRow = resultDataTable.NewRow();
                foreach (DataColumn col in dt.Columns)
                {
                    object value = row[col];
                    string safeValue = value == null || value == DBNull.Value ? "" : Convert.ToString(value);
                    newRow[col.ColumnName] = safeValue;

                    //newRow[col.ColumnName] = row[col]?.ToString();
                }
                resultDataTable.Rows.Add(newRow);
            }

            _stopwatch.Stop();
            TimeSpan ts = _stopwatch.Elapsed;
            Console.WriteLine($"Fine [{sqlObjectName}] - Tempo trascorso: {ts}");

            return resultDataTable;
        }

        #region not active
        // 🔹 Nuovo metodo: STREAMING JSON
        public async Task ExecuteFunctionStream(HttpResponse response, string functionName, List<SqlParameter> parameters)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            await using var conn = new SqlConnection(_connectionString);
            await using var cmd = new SqlCommand
            {
                Connection = conn,
                CommandText = $"SELECT * FROM {functionName}({string.Join(", ", parameters.Select(p => p.ParameterName))})",
                CommandTimeout = 0
            };

            if (parameters?.Count > 0)
                cmd.Parameters.AddRange(parameters.ToArray());

            Console.WriteLine($"Inizio ExecuteReader... {DateTime.Now}");
            await conn.OpenAsync();
            await using var reader = await cmd.ExecuteReaderAsync();

            _stopwatch.Stop();
            var ts = _stopwatch.Elapsed;
            Console.WriteLine($"Fine ExecuteReader - Tempo trascorso: {ts:hh\\:mm\\:ss\\.fff}");

            _stopwatch = Stopwatch.StartNew();
            Console.WriteLine($"Inizio StreamWriter... {DateTime.Now}");

            await using var writer = new StreamWriter(response.Body, Encoding.UTF8, leaveOpen: true);

            await writer.WriteLineAsync("[");
            bool first = true;

            while (await reader.ReadAsync())
            {
                if (!first)
                    await writer.WriteLineAsync(",");
                else
                    first = false;

                var row = new Dictionary<string, object?>();
                for (int i = 0; i < reader.FieldCount; i++)
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);

                string jsonRow = JsonConvert.SerializeObject(row);
                await writer.WriteAsync(jsonRow);
            }

            await writer.WriteLineAsync("]");
            await writer.FlushAsync();
            await response.Body.FlushAsync();
            await response.CompleteAsync(); // ✅ chiude correttamente lo stream per Excel, Postman, Swagger

            ts = _stopwatch.Elapsed;
            Console.WriteLine($"Fine StreamWriter - Tempo trascorso: {ts:hh\\:mm\\:ss\\.fff}");
        }

        #endregion
    }
}
