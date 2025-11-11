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
    public class Functions : ControllerBase
    {

        private readonly ILogger<Functions> _logger;
        private DataExportEngine _dataExportEngine;

        // Cache in memoria: per ogni chiave (funzione + utente + parametri)
        // memorizza il percorso del file XML e la sua data di scadenza
        private static Dictionary<string, (string FilePath, DateTime Expiration)> _cache
            = new Dictionary<string, (string FilePath, DateTime Expiration)>();

        // Contiene i task in corso, per evitare che due chiamate simultanee allo stesso endpoint
        // eseguano la query al DB due volte (una sola la genera, le altre aspettano)
        private static ConcurrentDictionary<string, Lazy<Task<string>>> _generationTasks
            = new ConcurrentDictionary<string, Lazy<Task<string>>>();

        public Functions(IConfiguration configuration, ILogger<Functions> logger, IOptions<EndPointCacheConfig> options)
        {
            var connStr = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException("La stringa di connessione 'DefaultConnection' non è stata trovata o è nulla.");

            _logger = logger;

            _dataExportEngine = new DataExportEngine(connStr, logger, options);
        }

        //[HttpGet("efn_ARTICOLI_XML")]
        //public IActionResult GetEfn_ARTICOLI_XML([FromQuery] string Distinta)
        //{
        //    var parameters = new List<SqlParameter> { new SqlParameter("@Distinta", Distinta) };
        //    var stopwatch = Stopwatch.StartNew();
        //    return GetXmlStream("efn_ARTICOLI", ProgramType.Function, parameters, stopwatch);
        //}

        [HttpGet("{mode}/efn_ARTICOLI")]
        public IActionResult GetEfn_ARTICOLI([FromRoute] string mode, [FromQuery] string Distinta)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Distinta", Distinta) };            
            return GetExportStream(mode, "efn_ARTICOLI", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_ARTICOLI_DBEXP")]
        public IActionResult GetEfn_ARTICOLI_DBEXP([FromRoute] string mode, [FromQuery] string Fileoni)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Fileoni", Fileoni) };
            return GetExportStream(mode, "efn_ARTICOLI_DBEXP", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_ARTICOLI_NEW")]
        public IActionResult GetEfn_ARTICOLI_NEW([FromRoute] string mode, [FromQuery] string Parte, [FromQuery] string Tipo)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Parte", Parte),
                new SqlParameter("@Tipo", Tipo)
            };
            return GetExportStream(mode, "efn_ARTICOLI_NEW", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_DBEXP")]
        public IActionResult GetEfn_DBEXP([FromRoute] string mode, [FromQuery]string Distinta)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Distinta", Distinta) };
            return GetExportStream(mode, "efn_DBEXP", ProgramType.Function, parameters);
        }
        [HttpGet("{mode}/efn_FDMxMatricola")]
        public IActionResult GetEfn_FDMxMatricola([FromRoute] string mode, [FromQuery] string Descrizione)
        {             
            var parameters = new List<SqlParameter> { new SqlParameter("@Descrizione", Descrizione) };
            return GetExportStream(mode, "efn_FDMxMatricola", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_FILEONE")]
        public IActionResult GetEfn_FILEONE([FromRoute] string mode, [FromQuery] string Distinta)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Distinta", Distinta) };
            return GetExportStream(mode, "efn_FILEONE", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_FILEONE_EXP")]
        public IActionResult GetEfn_FILEONE_EXP([FromRoute] string mode, [FromQuery] string Fileone, [FromQuery] string Divisione)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Fileone", Fileone),
                new SqlParameter("@Divisione", Divisione)
            };
            return GetExportStream(mode, "efn_FILEONE_EXP", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_FILEONE_EXP1")]
        public IActionResult GetEfn_FILEONE_EXP1([FromRoute] string mode, [FromQuery] string Fileone, [FromQuery] string Divisione, [FromQuery] string Mag)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Fileone", Fileone),
                new SqlParameter("@Divisione", Divisione),
                new SqlParameter("@Mag", Mag)
            };
            return GetExportStream(mode, "efn_FILEONE_EXP1", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_FILEONI_CARRELLI")]
        public IActionResult GetEfn_FILEONI_CARRELLI([FromRoute] string mode, [FromQuery] string FileoneRIF, [FromQuery] string FileoneCOMP, [FromQuery] string DataRif)
        {   var parameters = new List<SqlParameter>
            {
                new SqlParameter("@FileoneRIF", FileoneRIF),
                new SqlParameter("@FileoneCOMP", FileoneCOMP),
                new SqlParameter("@DataRif", DataRif)
            };
            return GetExportStream(mode, "efn_FILEONI_CARRELLI", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_FILEONI_COMP")]
        public IActionResult GetEfn_FILEONI_COMP([FromRoute] string mode, [FromQuery] string FileoneRIF, [FromQuery] string FileoneCOMP, [FromQuery] string Ordinamento)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@FileoneRIF", FileoneRIF),
                new SqlParameter("@FileoneCOMP", FileoneCOMP),
                new SqlParameter("@Ordinamento", Ordinamento)
            };
            return GetExportStream(mode, "efn_FILEONI_COMP", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_FOGLIO_MONTAGGIO")]
        public IActionResult GetEfn_FOGLIO_MONTAGGIO([FromRoute] string mode, [FromQuery] int Ultimo)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Ultimo", Ultimo)
            };
            return GetExportStream(mode, "efn_FOGLIO_MONTAGGIO", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_FOGLIO_SMONTAGGIO")]
        public IActionResult GetEfn_FOGLIO_SMONTAGGIO([FromRoute] string mode, [FromQuery] string Codice, [FromQuery] string Revisione, [FromQuery] string Progressivo)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Codice", Codice),
                new SqlParameter("@Revisione", Revisione),
                new SqlParameter("@Progressivo", Progressivo)
            };
            return GetExportStream(mode, "efn_FOGLIO_SMONTAGGIO", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_FOGLIO_SMONTAGGIO_2")]
        public IActionResult GetEfn_FOGLIO_SMONTAGGIO_2([FromRoute] string mode, [FromQuery] string Codice, [FromQuery] string Revisione)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Codice", Codice),
                new SqlParameter("@Revisione", Revisione)
            };
            return GetExportStream(mode, "efn_FOGLIO_SMONTAGGIO_2", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_FOGLIO_SMONTAGGIO_3")]
        public IActionResult GetEfn_FOGLIO_SMONTAGGIO_3([FromRoute] string mode)
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_FOGLIO_SMONTAGGIO_3", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_GestioneID")]
        public IActionResult GetEfn_GestioneID([FromRoute] string mode, [FromQuery] string Codice, [FromQuery] int Id, [FromQuery] string Montati, [FromQuery] string Esito)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Codice", Codice),
                new SqlParameter("@Id", Id),
                new SqlParameter("@Montati", Montati),
                new SqlParameter("@Esito", Esito)
            };
            return GetExportStream(mode, "efn_GestioneID", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_GestioneID1")]
        public IActionResult GetEfn_GestioneID1([FromRoute] string mode, [FromQuery] string Fileone, [FromQuery] string TT, [FromQuery] string Intervento)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Fileone", Fileone),
                new SqlParameter("@TT", TT),
                new SqlParameter("@Intervento", Intervento)
            };
            return GetExportStream(mode, "efn_GestioneID1", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_MADRE_EXP")]
        public IActionResult GetEfn_MADRE_EXP([FromRoute] string mode, [FromQuery] string Madre)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Madre", Madre) };
            return GetExportStream(mode, "efn_MADRE_EXP", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_MAGAZZINO")]
        public IActionResult GetEfn_MAGAZZINO([FromRoute] string mode)
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_MAGAZZINO", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_MAGAZZINO_DMH")]
        public IActionResult GetEfn_MAGAZZINO_DMH([FromRoute] string mode)
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_MAGAZZINO_DMH", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_MAGAZZINO_PBI")]
        public IActionResult GetEfn_MAGAZZINO_PBI([FromRoute] string mode)
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_MAGAZZINO_PBI", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_MAINTENANCE")]
        public IActionResult GetEfn_MAINTENANCE([FromRoute] string mode, [FromQuery] int Id)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", Id) };
            return GetExportStream(mode, "efn_MAINTENANCE", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_MovimentiMAG")]
        public IActionResult GetEfn_MovimentiMAG([FromRoute] string mode)
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_MovimentiMAG", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_PDFMapping")]
        public IActionResult GetEfn_PDFMapping([FromRoute] string mode, [FromQuery] string Ambiente)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Ambiente", Ambiente) };
            return GetExportStream(mode, "efn_PDFMapping", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_PREVIEWPL")]
        public IActionResult GetEfn_PREVIEWPL([FromRoute] string mode, [FromQuery] int Id)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", Id) };
            return GetExportStream(mode, "efn_PREVIEWPL", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_PRIMIINGRESSI")]
        public IActionResult GetEfn_PRIMIINGRESSI([FromRoute] string mode, [FromQuery] int Tipo)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Tipo", Tipo) };
            return GetExportStream(mode, "efn_PRIMIINGRESSI", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_PROVE")]
        public IActionResult GetEfn_PROVE([FromRoute] string mode, [FromQuery] string TRR)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@TRR", TRR) };
            return GetExportStream(mode, "efn_PROVE", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_RIGHEORDINE")]
        public IActionResult GetEfn_RIGHEORDINE([FromRoute] string mode, [FromQuery] string Magazzini)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Magazzini", Magazzini) };
            return GetExportStream(mode, "efn_RIGHEORDINE", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_RIGHEORDINE1")]
        public IActionResult GetEfn_RIGHEORDINE1([FromRoute] string mode, [FromQuery] string Magazzini)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Magazzini", Magazzini) };
            return GetExportStream(mode, "efn_RIGHEORDINE1", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_RIGHEORDINE2")]
        public IActionResult GetEfn_RIGHEORDINE2([FromRoute] string mode, [FromQuery] string Magazzini)
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Magazzini", Magazzini) };
            return GetExportStream(mode, "efn_RIGHEORDINE2", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_SchedeMontaggio")]
        public IActionResult GetEfn_SchedeMontaggio([FromRoute] string mode)
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_SchedeMontaggio", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_SPC")]
        public IActionResult GetEfn_SPC([FromRoute] string mode, [FromQuery] string MotoMotore, [FromQuery] string Modello)
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@MotoMotore", MotoMotore),
                new SqlParameter("@Modello", Modello)
            };
            return GetExportStream(mode, "efn_SPC", ProgramType.Function, parameters);
        }

        [HttpGet("{mode}/efn_UltimoMovimento")]
        public IActionResult GetEfn_UltimoMovimento([FromRoute] string mode)
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_UltimoMovimento", ProgramType.Function, parameters);
        }

        #region Private

        private IActionResult GetExportStream(string mode, string functionName, ProgramType functionType, List<SqlParameter> parameters)
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
        private IActionResult GetJsonStream(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
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
        /// <returns>IActionResult con il file JSON, leggibile da client esterni.</returns>
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
        /// <returns>IActionResult con il contenuto XML in memoria pronto per il download o lettura da client.</returns>
        private IActionResult GetXmlStream(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            MemoryStream ms;
            string xmlContent;

            _dataExportEngine.GetXmlStream(functionName, functionType, parameters, stopwatch, out ms, out xmlContent);

            // Rimuovi header che possono attivare lo streaming
            Response.Headers.Remove("Transfer-Encoding");
            Response.Headers["Cache-Control"] = "private, max-age=300";
            Response.Headers["Content-Length"] = xmlContent.Length.ToString();

            // ✅ Risposta completa, con Content-Length fisso: Excel aspetta fino alla fine
            var bytes = Encoding.UTF8.GetBytes(xmlContent);
            return File(bytes, "application/xml; charset=utf-8", "export.xml");
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

            string filePath = _dataExportEngine.GetXmlStreamWithCache(functionName, functionType, parameters, stopwatch, cacheKey, _generationTasks, _cache);

            return PhysicalFile(filePath, "application/xml", Path.GetFileName(filePath));
        }

        


       
        #endregion

        #endregion
    }
}
