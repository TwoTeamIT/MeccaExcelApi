using Azure;
using DucatiMeccaExcelApi.Engine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Xml;

namespace DucatiExcelApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    //[Authorize]
    public class Functions : ControllerBase
    {
        private readonly DataExportService _dataExportService;
        private readonly ILogger<Functions> _logger;

        public Functions(IConfiguration configuration, ILogger<Functions> logger)
        {
            var connStr = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException("La stringa di connessione 'DefaultConnection' non è stata trovata o è nulla.");

            _dataExportService = new DataExportService(connStr);
            _logger = logger;
        }

        [HttpGet("efn_ARTICOLI_XML")]
        public IActionResult GetEfn_ARTICOLI_XML([FromQuery] string Distinta)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Distinta", Distinta) };
            var stopwatch = Stopwatch.StartNew();
            return GetXmlStream("efn_ARTICOLI", parameters, stopwatch);
        }

        [HttpGet("{mode}/efn_ARTICOLI")]
        public IActionResult GetEfn_ARTICOLI([FromRoute] string mode, [FromQuery] string Distinta)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Distinta", Distinta) };
            var stopwatch = Stopwatch.StartNew();
            return GetExportStream(mode, "efn_ARTICOLI", parameters, stopwatch);
        }

        #region Private

        private IActionResult GetExportStream(string mode, string functionName, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            if (mode.Equals("Xml", StringComparison.OrdinalIgnoreCase))
            {
                return GetXmlStream(functionName, parameters, stopwatch);
            }
            else if (mode.Equals("JsonStream", StringComparison.OrdinalIgnoreCase))
            {
                return GetJsonStream(functionName, parameters, stopwatch);
            }
            else if (mode.Equals("JsonResult", StringComparison.OrdinalIgnoreCase))
            {
                return GetJsonResult(functionName, parameters, stopwatch);
            }
            else
            {
                return BadRequest("Invalid mode. Use 'Xml', 'Json', or 'JsonStream'.");
            }
        }

        private IActionResult GetJsonStream(string functionName, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            var dt = _dataExportService.ExecuteFunction(functionName, parameters);

            string json = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.None);

            stopwatch.Stop();
            _logger.LogInformation("[{Function}] JsonStream - Tempo totale: {Elapsed}", functionName, stopwatch.Elapsed);

            var bytes = Encoding.UTF8.GetBytes(json);
            var stream = new MemoryStream(bytes);
            return File(stream, "application/json", $"{functionName}.json", enableRangeProcessing: false);
        }

        private IActionResult GetJsonResult(string functionName, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            var dt = _dataExportService.ExecuteFunction(functionName, parameters);

            var rows = new List<Dictionary<string, object?>>();
            foreach (DataRow dr in dt.Rows)
            {
                var dict = new Dictionary<string, object?>();
                foreach (DataColumn col in dt.Columns)
                {
                    dict[col.ColumnName] = dr[col] == DBNull.Value ? null : dr[col];
                }
                rows.Add(dict);
            }

            stopwatch.Stop();
            _logger.LogInformation("[{Function}] GetJsonResult - Tempo totale: {Elapsed}", functionName, stopwatch.Elapsed);

            return new JsonResult(rows)
            {
                ContentType = "application/json; charset=utf-8"
            };
        }

        private IActionResult GetXmlStream(string functionName, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            var dt = _dataExportService.ExecuteFunction(functionName, parameters);

            // Scrivi tutto in memoria
            using var ms = new MemoryStream();
            dt.WriteXml(ms, XmlWriteMode.WriteSchema);
            ms.Position = 0;

            stopwatch.Stop();
            _logger.LogInformation("[{Function}] GetXmlStream - Tempo totale: {Elapsed}", functionName, stopwatch.Elapsed);

            // Converti in stringa (Excel legge meglio da UTF-8)
            var xmlContent = Encoding.UTF8.GetString(ms.ToArray());

            // Rimuovi header che possono attivare lo streaming
            Response.Headers.Remove("Transfer-Encoding");
            Response.Headers["Cache-Control"] = "private, max-age=300";
            Response.Headers["Content-Length"] = xmlContent.Length.ToString();

            // ✅ Risposta completa, con Content-Length fisso: Excel aspetta fino alla fine
            return Content(xmlContent, "application/xml", Encoding.UTF8);
        }



        #endregion
    }
}
