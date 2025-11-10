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
using Formatting = Newtonsoft.Json.Formatting;

namespace DucatiExcelApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    //[Authorize]
    public class Functions : ControllerBase
    {
        private readonly DataExportService _dataExportService;
        private readonly ILogger<Functions> _logger;

        private readonly string folderPath = @"C:\temp";
        private readonly double cacheDurationMinutes = 5;

        // Cache in memoria: per ogni chiave (funzione + utente + parametri)
        // memorizza il percorso del file XML e la sua data di scadenza
        private static readonly Dictionary<string, (string FilePath, DateTime Expiration)> _cache
            = new Dictionary<string, (string FilePath, DateTime Expiration)>();

        // Contiene i task in corso, per evitare che due chiamate simultanee allo stesso endpoint
        // eseguano la query al DB due volte (una sola la genera, le altre aspettano)
        private static readonly ConcurrentDictionary<string, Lazy<Task<string>>> _generationTasks
            = new ConcurrentDictionary<string, Lazy<Task<string>>>();

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
                return GetXmlStream(functionName, functionType, parameters, stopwatch);
            }
            else if (mode.Equals("XmlExcel", StringComparison.OrdinalIgnoreCase))
            {
                return GetXmlStreamWithCache(functionName, functionType, parameters, stopwatch);
            }
            else if (mode.Equals("Json", StringComparison.OrdinalIgnoreCase))
            {
                return GetJsonStream(functionName, functionType, parameters, stopwatch);
            }
            else if (mode.Equals("JsonExcel", StringComparison.OrdinalIgnoreCase))
            {
                return GetJsonStreamWithCache(functionName, functionType, parameters, stopwatch);
            }
            else
            {
                return BadRequest("Invalid mode. Use 'XmlExcel', 'Json', or 'JsonExcel'.");
            }
        }

        #region Json
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
        /// Genera o restituisce da cache un file JSON basato su un DataSet ottenuto dal database.
        /// Supporta cache su file fisico per evitare rigenerazioni multiple.
        /// Gestisce DataSet vuoti, serializzazione UTF-8 senza BOM e compatibilità con client come Excel o PowerQuery.
        /// </summary>
        /// <param name="functionName">Nome della funzione/endpoint di esportazione dati.</param>
        /// <param name="functionType">Tipo di funzione (ProgramType).</param>
        /// <param name="parameters">Lista di parametri SQL per la query.</param>
        /// <param name="stopwatch">Stopwatch per misurare il tempo di generazione.</param>
        /// <returns>IActionResult con il file JSON, leggibile da client esterni.</returns>

        /// <summary>
        /// Genera o restituisce da cache un file JSON “Excel-friendly”
        /// (array di oggetti piatti, apribile direttamente da Excel senza Power Query).
        /// </summary>
        private IActionResult GetJsonStreamWithCache(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            string upn = "user@example.com";
            var keyParams = string.Join(";", parameters.Select(p => $"{p.ParameterName}={p.Value}"));
            var cacheKey = $"{functionName}|{upn}|{keyParams}|JsonFriendly";

            // Controllo cache
            if (_cache.TryGetValue(cacheKey, out var existing) &&
                System.IO.File.Exists(existing.FilePath) &&
                DateTime.UtcNow < existing.Expiration)
            {
                _logger.LogInformation("[{Function}] Cache HIT (Excel-friendly JSON) per {Key}", functionName, cacheKey);
                return PhysicalFile(existing.FilePath, "application/json; charset=utf-8", Path.GetFileName(existing.FilePath));
            }

            // Generazione file
            var lazyTask = _generationTasks.GetOrAdd(cacheKey, k => new Lazy<Task<string>>(async () =>
            {
                if (_cache.TryGetValue(cacheKey, out var before) &&
                    System.IO.File.Exists(before.FilePath) &&
                    DateTime.UtcNow < before.Expiration)
                    return before.FilePath;

                _logger.LogInformation("[{Function}] Generazione JSON Excel-friendly INIZIATA per {Key}", functionName, cacheKey);

                // Query DB
                var dt = _dataExportService.Execute(functionName, functionType, parameters);

                // Converti il DataTable in una lista di dizionari piatti
                var rows = new List<Dictionary<string, object?>>();
                foreach (DataRow dr in dt.Rows)
                {
                    var dict = new Dictionary<string, object?>();
                    foreach (DataColumn col in dt.Columns)
                        dict[col.ColumnName] = dr[col] == DBNull.Value ? null : dr[col];
                    rows.Add(dict);
                }

                // Serializzazione “Excel-friendly”
                var wrapper = new { rows };
                string json = JsonConvert.SerializeObject(wrapper, Formatting.Indented);

                // Scrittura file
                var fileName = $"{functionName}_{Guid.NewGuid():N}.json";
                var tempPath = Path.Combine(folderPath, fileName);
                var utf8NoBom = new UTF8Encoding(false);
                await System.IO.File.WriteAllTextAsync(tempPath, json, utf8NoBom);

                _cache[cacheKey] = (tempPath, DateTime.UtcNow.AddMinutes(cacheDurationMinutes));
                _logger.LogInformation("[{Function}] JSON Excel-friendly COMPLETATO per {Key} (path={Path})", functionName, cacheKey, tempPath);

                return tempPath;
            }));

            string filePath;
            try
            {
                filePath = lazyTask.Value.Result;
            }
            catch (Exception ex)
            {
                _generationTasks.TryRemove(cacheKey, out _);
                _logger.LogError(ex, "[{Function}] Errore durante la generazione JSON Excel-friendly per {Key}", functionName, cacheKey);
                throw;
            }

            stopwatch.Stop();
            _logger.LogInformation("[{Function}] GetJsonStreamWithCache - Tempo totale: {Elapsed}", functionName, stopwatch.Elapsed);

            return PhysicalFile(filePath, "application/json; charset=utf-8", Path.GetFileName(filePath));
        }


        #endregion

        #region Xml
        /// <summary>
        /// Genera uno stream XML direttamente in memoria a partire da un DataSet ottenuto dal database.
        /// Restituisce il contenuto come stringa UTF-8, con Content-Length fisso, ottimizzato per client come Excel.
        /// Non utilizza file su disco; la scrittura è completamente in memoria.
        /// </summary>
        /// <param name="functionName">Nome della funzione/endpoint di esportazione dati.</param>
        /// <param name="functionType">Tipo di funzione (ProgramType).</param>
        /// <param name="parameters">Lista di parametri SQL per la query.</param>
        /// <param name="stopwatch">Stopwatch per misurare il tempo totale di generazione.</param>
        /// <returns>IActionResult con il contenuto XML in memoria pronto per il download o lettura da client.</returns>

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

        /// <summary>
        /// Genera o restituisce da cache un file XML basato su un DataSet ottenuto dal database.
        /// Utilizza una cache su file fisico per evitare rigenerazioni multiple e supporta chiamate concorrenti tramite Lazy<Task>.
        /// Il file XML è scritto in UTF-8 senza BOM, con dichiarazione XML e line endings Windows, ottimizzato per client come Excel.
        /// La serializzazione ignora lo schema per garantire compatibilità durante l’importazione da API.
        /// Gestisce DataSet vuoti e validazione XML per prevenire errori di parsing.
        /// </summary>
        /// <param name="functionName">Nome della funzione/endpoint di esportazione dati.</param>
        /// <param name="functionType">Tipo di funzione (ProgramType).</param>
        /// <param name="parameters">Lista di parametri SQL per la query.</param>
        /// <param name="stopwatch">Stopwatch per misurare il tempo totale di generazione.</param>
        /// <returns>IActionResult che restituisce il file XML fisico pronto per il download o per la lettura da client esterni.</returns>

        private IActionResult GetXmlStreamWithCache(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            string upn = "user@example.com";

            var keyParams = string.Join(";", parameters.Select(p => $"{p.ParameterName}={p.Value}"));
            var cacheKey = $"{functionName}|{upn}|{keyParams}|Xml";

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
                var dt = _dataExportService.Execute(functionName, functionType, parameters);

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


        /// <summary>
        /// Verifica se una stringa è un XML ben formato.
        /// Restituisce true se il parsing XML ha successo, false in caso contrario.
        /// </summary>
        /// <param name="xml">Stringa contenente l'XML da validare.</param>
        /// <returns>Booleano che indica se l'XML è ben formato.</returns>
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

        /// <summary>
        /// Pulisce una stringa rimuovendo caratteri di controllo non ammessi in XML.
        /// Mantiene solo caratteri validi per XML e line endings (\r, \n).
        /// </summary>
        /// <param name="s">Stringa da pulire.</param>
        /// <returns>Stringa filtrata pronta per essere scritta in XML.</returns>
        private string CleanString(string s)
        {
            return new string(s.Where(c => c == '\n' || c == '\r' || c >= ' ').ToArray());
        }
        #endregion

        #endregion
    }
}
