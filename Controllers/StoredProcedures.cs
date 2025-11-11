using Azure;
using DucatiMeccaExcelApi.Engine;
using DucatiMeccaExcelApi.Utility;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.Text;
using Formatting = Newtonsoft.Json.Formatting;

namespace DucatiExcelApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    //[Authorize]
    public class StoredProcedures : ControllerBase
    {
        private readonly ILogger<Functions> _logger;
        private DataExportEngine _dataExportEngine;

        // Cache in memoria: per ogni chiave (funzione + utente + parametri)
        // memorizza il percorso del file XML e la sua data di scadenza
        private static readonly Dictionary<string, (string FilePath, DateTime Expiration)> _cache
            = new Dictionary<string, (string FilePath, DateTime Expiration)>();

        // Contiene i task in corso, per evitare che due chiamate simultanee allo stesso endpoint
        // eseguano la query al DB due volte (una sola la genera, le altre aspettano)
        private static readonly ConcurrentDictionary<string, Lazy<Task<string>>> _generationTasks
            = new ConcurrentDictionary<string, Lazy<Task<string>>>();

        public StoredProcedures(IConfiguration configuration, ILogger<Functions> logger, IOptions<EndPointCacheConfig> options)
        {
            var connStr = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException("La stringa di connessione 'DefaultConnection' non è stata trovata o è nulla.");
                        
            _logger = logger;
            
            _dataExportEngine = new DataExportEngine(connStr, logger, options);
        }

        [HttpGet("{mode}/v4_ADDIN_CompareFileoni")]
        public IActionResult  GetV4_ADDIN_CompareFileoni([FromRoute] string mode, [FromQuery] string FileoneRIF, [FromQuery] string FileoneCOMP, [FromQuery] string Ordinamento)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@FileoneRIF", SqlDbType.NVarChar) { Value = FileoneRIF },
                new SqlParameter("@FileoneCOMP", SqlDbType.NVarChar) { Value = FileoneCOMP },
                new SqlParameter("@Ordinamento", SqlDbType.NVarChar) { Value = Ordinamento }
            };
            return GetExportStream(mode, "v4_ADDIN_CompareFileoni", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_DCTeamSite_TestList")]
        public IActionResult  GetV4_ADDIN_DCTeamSite_TestList([FromRoute] string mode)
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "v4_ADDIN_DCTeamSite_TestList", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_DistintaMadreExp")]
        public IActionResult  GetV4_ADDIN_DistintaMadreExp([FromRoute] string mode, [FromQuery] string Madre)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Madre", SqlDbType.NVarChar) { Value = Madre }
            };
            return GetExportStream(mode, "v4_ADDIN_DistintaMadreExp", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_ElencoEsaurimentixCodice")]
        public IActionResult  GetV4_ADDIN_ElencoEsaurimentixCodice([FromRoute] string mode, [FromQuery] string Codice)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Codice", SqlDbType.NVarChar) { Value = Codice }
            };
            return GetExportStream(mode, "v4_ADDIN_ElencoEsaurimentixCodice", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_EsplodiFileoni_Figli")]
        public IActionResult  GetV4_ADDIN_EsplodiFileoni_Figli([FromRoute] string mode, [FromQuery] string Fileoni)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Fileoni", SqlDbType.NVarChar) { Value = Fileoni }
            };
            return GetExportStream(mode, "v4_ADDIN_EsplodiFileoni_Figli", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_EsplodiMaintenance_Figli")]
        public IActionResult  GetV4_ADDIN_EsplodiMaintenance_Figli([FromRoute] string mode, [FromQuery] string Fileoni)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Fileoni", SqlDbType.NVarChar) { Value = Fileoni }
            };
            return GetExportStream(mode, "v4_ADDIN_EsplodiMaintenance_Figli", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_FOGLIO_MONTAGGIO")]
        public IActionResult  GetV4_ADDIN_FOGLIO_MONTAGGIO([FromRoute] string mode, 
            [FromQuery] string Nome,
            [FromQuery] int Rev,
            [FromQuery] int Prog,
            [FromQuery] string @Parte,
            [FromQuery] string TT,
            [FromQuery] int Id,
            [FromQuery] string Tag)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Nome", SqlDbType.NVarChar) { Value = Nome },
                new SqlParameter("@Rev", SqlDbType.Int) { Value = Rev },
                new SqlParameter("@Prog", SqlDbType.Int) { Value = Prog },
                new SqlParameter("@Parte", SqlDbType.NVarChar) { Value = Parte },
                new SqlParameter("@TT", SqlDbType.NVarChar) { Value = TT },
                new SqlParameter("@Id", SqlDbType.Int) { Value = Id },
                new SqlParameter("@Tag", SqlDbType.NVarChar) { Value = Tag }
            };
            return GetExportStream(mode, "v4_ADDIN_FOGLIO_MONTAGGIO", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_GetFileName_PRM")]
        public IActionResult  GetV4_ADDIN_GetFileName_PRM([FromRoute] string mode, [FromQuery] string Parti, [FromQuery] string TipoProva, 
            [FromQuery] string DataFrom, [FromQuery] string DataTo)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Parti", SqlDbType.NVarChar) { Value = Parti },
                new SqlParameter("@TipoProva", SqlDbType.NVarChar) { Value = TipoProva },
                new SqlParameter("@DataFrom", SqlDbType.NVarChar) { Value = DataFrom },
                new SqlParameter("@DataTo", SqlDbType.NVarChar) { Value = DataTo }
            };
            return GetExportStream(mode, "v4_ADDIN_GetFileName_PRM", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_InterrogaDistinte")]
        public IActionResult  GetV4_ADDIN_InterrogaDistinte([FromRoute] string mode, [FromQuery] string Madre)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Madre", SqlDbType.NVarChar) { Value = Madre }
            };
            return GetExportStream(mode, "v4_ADDIN_InterrogaDistinte", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_ListaCronologiaCerchiSPX")]
        public IActionResult  GetV4_ADDIN_ListaCronologiaCerchiSPX([FromRoute] string mode, [FromQuery] int Season)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Season", SqlDbType.Int) { Value = Season }
            };
            return GetExportStream(mode, "v4_ADDIN_ListaCronologiaCerchiSPX", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_ListaFornitori")]
        public IActionResult  GetV4_ADDIN_ListaFornitori([FromRoute] string mode)
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "v4_ADDIN_ListaFornitori", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_ListaMancanteSfuso\r\n")]
        public IActionResult  GetV4_ADDIN_ListaMancanteSfuso([FromRoute] string mode)
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "v4_ADDIN_ListaMancanteSfuso\r\n", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_ListaRigheListinixDistinta")]
        public IActionResult  GetV4_ADDIN_ListaRigheListinixDistinta([FromRoute] string mode, [FromQuery] string Fileone)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Fileone", SqlDbType.NVarChar) { Value = Fileone }
            };
            return GetExportStream(mode, "v4_ADDIN_ListaRigheListinixDistinta", ProgramType.StoredProcedure, parameters);
        }

        [HttpGet("{mode}/v4_ADDIN_RicercaCodiceinDistinteAttive")]
        public IActionResult  GetV4_ADDIN_RicercaCodiceinDistinteAttive([FromRoute] string mode, [FromQuery] string Parte)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Parte", SqlDbType.NVarChar) { Value = Parte }
            };
            return GetExportStream(mode, "v4_ADDIN_RicercaCodiceinDistinteAttive", ProgramType.StoredProcedure, parameters);
        }



        #region Private

        private IActionResult  GetExportStream(string mode, string functionName, ProgramType functionType, List<SqlParameter> parameters)
        {
            var stopwatch = Stopwatch.StartNew();

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
        private IActionResult  GetJsonStream(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            MemoryStream stream = _dataExportEngine.GetJsonStream(functionName, functionType, parameters, stopwatch);

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
        /// <returns>IActionResult  Getcon il file JSON, leggibile da client esterni.</returns>
        private IActionResult  GetJsonStreamWithCache(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
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

            string filePath = _dataExportEngine.GetJsonStreamWithCache(functionName, functionType, parameters, stopwatch, cacheKey, _generationTasks, _cache);

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
        /// <returns>IActionResult  Getcon il contenuto XML in memoria pronto per il download o lettura da client.</returns>
        private IActionResult  GetXmlStream(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            MemoryStream ms;
            string xmlContent;

            _dataExportEngine.GetXmlStream(functionName, functionType, parameters, stopwatch, out ms, out xmlContent);

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
        /// <returns>IActionResult  Getche restituisce il file XML fisico pronto per il download o per la lettura da client esterni.</returns>
        private IActionResult  GetXmlStreamWithCache(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
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

            string filePath = _dataExportEngine.GetXmlStreamWithCache(functionName, functionType, parameters, stopwatch, cacheKey, _generationTasks, _cache);

            return PhysicalFile(filePath, "application/xml", Path.GetFileName(filePath));
        }





        #endregion

        #endregion
    }
}
