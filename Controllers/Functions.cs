using Azure;
using DucatiMeccaExcelApi.Engine;
using DucatiMeccaExcelApi.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using System.Collections.Concurrent;
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
            return GetXmlStream("efn_ARTICOLI", ProgramType.Function, parameters, stopwatch);
        }

        [HttpGet("{mode}/efn_ARTICOLI")]
        public IActionResult GetEfn_ARTICOLI([FromRoute] string mode, [FromQuery] string Distinta)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Distinta", Distinta) };
            var stopwatch = Stopwatch.StartNew();
            return GetExportStream(mode, "efn_ARTICOLI", ProgramType.Function, parameters, stopwatch);
        }

        #region Private

        private IActionResult GetExportStream(string mode, string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            if (mode.Equals("Xml", StringComparison.OrdinalIgnoreCase))
            {
                return GetXmlStreamRedirect(functionName, functionType, parameters, stopwatch);
            }
            else if (mode.Equals("XmlRedirect", StringComparison.OrdinalIgnoreCase))
            {
                return GetXmlStreamRedirect(functionName, functionType, parameters, stopwatch);
            }
            else if (mode.Equals("JsonStream", StringComparison.OrdinalIgnoreCase))
            {
                return GetJsonStream(functionName, functionType, parameters, stopwatch);
            }
            else if (mode.Equals("JsonResult", StringComparison.OrdinalIgnoreCase))
            {
                return GetJsonResult(functionName, functionType, parameters, stopwatch);
            }
            else
            {
                return BadRequest("Invalid mode. Use 'Xml', 'Json', or 'JsonStream'.");
            }
        }

        /// <summary>
        /// Serializza direttamente l’intero DataTable in JSON con JsonConvert.SerializeObject.
        /// Poi lo scrive su un file stream(MemoryStream) e lo restituisce come download.
        /// È semplice e funziona bene per esportare in file .json.
        /// Excel o Power Query possono leggerlo se il JSON è lineare.
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="parameters"></param>
        /// <param name="stopwatch"></param>
        /// <returns></returns>
        private IActionResult GetJsonStream(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            var dt = _dataExportService.Execute(functionName, functionType, parameters);

            string json = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.None);

            stopwatch.Stop();
            _logger.LogInformation("[{Function}] JsonStream - Tempo totale: {Elapsed}", functionName, stopwatch.Elapsed);

            var bytes = Encoding.UTF8.GetBytes(json);
            var stream = new MemoryStream(bytes);
            return File(stream, "application/json", $"{functionName}.json", enableRangeProcessing: false);
        }

        /// <summary>
        /// Converte il DataTable in una lista di oggetti JSON puri, tipo:
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="parameters"></param>
        /// <param name="stopwatch"></param>
        /// <returns></returns>
        private IActionResult GetJsonResult(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            var dt = _dataExportService.Execute(functionName, functionType, parameters);

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

        /// <summary>
        /// restituisce un file XML downloadabile
        /// </summary>
        /// <param name="functionName"></param>
        /// <param name="parameters"></param>
        /// <param name="stopwatch"></param>
        /// <returns></returns>
        private IActionResult GetXmlStream(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            var dt = _dataExportService.Execute(functionName, functionType, parameters);

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


        // Cache in memoria: per ogni chiave (funzione + utente + parametri)
        // memorizza il percorso del file XML e la sua data di scadenza
        private static readonly Dictionary<string, (string FilePath, DateTime Expiration)> _cache
            = new Dictionary<string, (string FilePath, DateTime Expiration)>();

        // Contiene i task in corso, per evitare che due chiamate simultanee allo stesso endpoint
        // eseguano la query al DB due volte (una sola la genera, le altre aspettano)
        private static readonly ConcurrentDictionary<string, Lazy<Task<string>>> _generationTasks
            = new ConcurrentDictionary<string, Lazy<Task<string>>>();


        private IActionResult GetXmlStreamRedirect(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            string upn = "user@example.com";

            var keyParams = string.Join(";", parameters.Select(p => $"{p.ParameterName}={p.Value}"));
            var cacheKey = $"{functionName}|{upn}|{keyParams}";

            if (_cache.TryGetValue(cacheKey, out var existing) &&
                System.IO.File.Exists(existing.FilePath) &&
                DateTime.UtcNow < existing.Expiration)
            {
                _logger.LogInformation("[{Function}] Cache HIT (redirect) per {Key}", functionName, cacheKey);
                return PhysicalFile(existing.FilePath, "application/xml", Path.GetFileName(existing.FilePath));
            }

            var lazyTask = _generationTasks.GetOrAdd(cacheKey, k => new Lazy<Task<string>>(() => Task.Run(async () =>
            {
                if (_cache.TryGetValue(cacheKey, out var before) &&
                    System.IO.File.Exists(before.FilePath) &&
                    DateTime.UtcNow < before.Expiration)
                    return before.FilePath;

                _logger.LogInformation("[{Function}] Generazione file INIZIATA per {Key}", functionName, cacheKey);

                // --- QUERY DB (una sola volta per cacheKey) ---
                var dt = _dataExportService.Execute(functionName,functionType,  parameters);

                // --- CREAZIONE XML in memoria ---
                string xmlContent;
                using (var sw = new StringWriter())
                {
                    dt.WriteXml(sw, XmlWriteMode.WriteSchema);
                    xmlContent = sw.ToString();
                }

                // --- VALIDAZIONE XML ---
                bool xmlOk = IsWellFormedXml(xmlContent);

                if (!xmlOk)
                {
                    _logger.LogWarning("[{Function}] XML non well-formed per {Key} — tentativo di pulizia.", functionName, cacheKey);
                    xmlContent = CleanString(xmlContent);

                    if (!IsWellFormedXml(xmlContent))
                    {
                        _logger.LogError("[{Function}] XML ancora non valido dopo la pulizia per {Key}.", functionName, cacheKey);
                        throw new InvalidDataException("XML non valido anche dopo la pulizia.");
                    }
                }

                // --- SCRITTURA FILE UTF-8 SENZA BOM ---
                var fileName = $"{functionName}_{Guid.NewGuid():N}.xml";
                var folderPath = @"C:\temp";
                var tempPath = Path.Combine(folderPath, fileName);

                // Forza UTF-8 senza BOM
                var utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
                await System.IO.File.WriteAllTextAsync(tempPath, xmlContent, utf8NoBom);

                // --- SALVA IN CACHE ---
                _cache[cacheKey] = (tempPath, DateTime.UtcNow.AddMinutes(10));

                _logger.LogInformation("[{Function}] Generazione COMPLETATA per {Key} (path={Path})", functionName, cacheKey, tempPath);

                return tempPath;
            })));

            string filePath = null;
            try
            {
                filePath = lazyTask.Value.Result;
            }
            catch (Exception ex)
            {
                _generationTasks.TryRemove(cacheKey, out _);
                _logger.LogError(ex, "[{Function}] Errore durante la generazione per {Key}", functionName, cacheKey);
                throw;
            }

            stopwatch.Stop();
            _logger.LogInformation("[{Function}] GetXmlStreamRedirect - Tempo totale: {Elapsed}", functionName, stopwatch.Elapsed);

            return PhysicalFile(filePath, "application/xml", Path.GetFileName(filePath));
        }

        private bool IsWellFormedXml(string xml)
        {
            try
            {
                var doc = new System.Xml.XmlDocument();
                doc.LoadXml(xml);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string CleanString(string s)
        {
            // Rimuove caratteri di controllo non ammessi in XML
            return new string(s.Where(c => c == '\n' || c == '\r' || c >= ' ').ToArray());
        }






        #endregion
    }
}
