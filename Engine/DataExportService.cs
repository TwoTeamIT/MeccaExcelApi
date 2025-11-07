using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
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

        

        public DataTable ExecuteFunction(string functionName, List<SqlParameter> parameters)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;
                Console.WriteLine($"Inizio ExecuteReader...{DateTime.Now}");
                
                string paramList = string.Join(", ", parameters.Select(p => p.ParameterName));
                cmd.CommandText = $"SELECT top(10) * FROM {functionName}({paramList})";
                cmd.CommandTimeout = 0;

                if (parameters != null && parameters.Count > 0)
                    cmd.Parameters.AddRange(parameters.ToArray());

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                _stopwatch.Stop();
                TimeSpan ts = _stopwatch.Elapsed;

                string elapsedTime = string.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                    ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds);
                Console.WriteLine($"Fine ExecuteReader - Tempo trascorso: {ts} ");

                dt.TableName = functionName;

                return dt;
            }
        }

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


        public string EseguiStoredProcedureJson(string storedName, List<SqlParameter> parameters)
        {
            SqlConnection conn = new SqlConnection(_connectionString);

            using (SqlCommand cmd = new SqlCommand(storedName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandTimeout = 0; // Nessun timeout durante l’esecuzione

                // Aggiungo eventuali parametri
                if (parameters != null && parameters.Count > 0)
                    cmd.Parameters.AddRange(parameters.ToArray());

                // Riempio il DataTable
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                // Serializzo in JSON
                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return json;
            }
        }
    }
}
