using Newtonsoft.Json;
using Microsoft.Data.SqlClient;

using Formatting = Newtonsoft.Json.Formatting;
using System.Data;

namespace DucatiMeccaExcelApi.Engine
{
    public class Sql_Engine
    {

        private readonly string _connectionString = "YourConnectionStringHere";


        public Sql_Engine()
        {
        }

        public string EseguiStoredProcedureJson(string storedName,  List<SqlParameter> parameters)
        {
            SqlConnection conn = new SqlConnection(_connectionString);

            using (SqlCommand cmd = new SqlCommand(storedName, conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

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

        public string EseguiFunzioneJson(string functionName, List<SqlParameter> parameters)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand())
            {
                cmd.Connection = conn;

                // Costruisco la chiamata tipo: SELECT * FROM dbo.MyFunction(@param1, @param2)
                string paramList = string.Join(", ", parameters.Select(p => p.ParameterName));
                cmd.CommandText = $"SELECT * FROM {functionName}({paramList})";

                // Aggiungo i parametri
                if (parameters != null && parameters.Count > 0)
                    cmd.Parameters.AddRange(parameters.ToArray());

                // Eseguo e converto in JSON
                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return json;
            }
        }
    }
}
