using DucatiMeccaExcelApi.Engine;
using DucatiMeccaExcelApi.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
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
        private SecurityOptions _securityOptions;

        // Cache in memoria: per ogni chiave (funzione + utente + parametri)
        // memorizza il percorso del file XML e la sua data di scadenza
        private static Dictionary<string, (string FilePath, DateTime Expiration)> _cache
            = new Dictionary<string, (string FilePath, DateTime Expiration)>();

        // Contiene i task in corso, per evitare che due chiamate simultanee allo stesso endpoint
        // eseguano la query al DB due volte (una sola la genera, le altre aspettano)
        private static ConcurrentDictionary<string, Lazy<Task<string>>> _generationTasks
            = new ConcurrentDictionary<string, Lazy<Task<string>>>();

        public Functions(IConfiguration configuration, 
            ILogger<Functions> logger, 
            IOptions<EndPointCacheConfig> cacheOptions, 
            IOptions<SecurityOptions> securityOptions)
        {
            var connStr = configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(connStr))
                throw new InvalidOperationException("La stringa di connessione 'DefaultConnection' non è stata trovata o è nulla.");

            _logger = logger;

            _dataExportEngine = new DataExportEngine(connStr, logger, cacheOptions);

            _securityOptions = securityOptions.Value;

        }

        [HttpGet("{mode}/efn_ARTICOLI")]
        public IActionResult GetEfn_ARTICOLI([FromRoute] string mode, [FromQuery] string Distinta = "", [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>();
            if (Distinta != "")
                parameters.Add(new SqlParameter("@Distinta", Distinta));
            return GetExportStream(mode, "efn_ARTICOLI", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_ARTICOLI_DBEXP")]
        public IActionResult GetEfn_ARTICOLI_DBEXP([FromRoute] string mode, [FromQuery] string Fileoni = "", [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Fileoni", Fileoni) };
            return GetExportStream(mode, "efn_ARTICOLI_DBEXP", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_ARTICOLI_NEW")]
        public IActionResult GetEfn_ARTICOLI_NEW([FromRoute] string mode, [FromQuery] string Parte = "", [FromQuery] string Tipo = "", [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Parte", Parte),
                new SqlParameter("@Tipo", Tipo)
            };
            return GetExportStream(mode, "efn_ARTICOLI_NEW", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_DBEXP")]
        public IActionResult GetEfn_DBEXP([FromRoute] string mode, [FromQuery]string Distinta = "", [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Distinta", Distinta) };
            return GetExportStream(mode, "efn_DBEXP", ProgramType.Function, parameters, Where);
        }
        
        [HttpGet("{mode}/efn_FDMxMatricola")]
        public IActionResult GetEfn_FDMxMatricola([FromRoute] string mode, [FromQuery][Required] string Descrizione, [FromQuery] string Where = "")
        {             
            var parameters = new List<SqlParameter> { new SqlParameter("@Descrizione", Descrizione) };
            return GetExportStream(mode, "efn_FDMxMatricola", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_FILEONE")]
        public IActionResult GetEfn_FILEONE([FromRoute] string mode, [FromQuery][Required] string Distinta, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Distinta", Distinta) };
            return GetExportStream(mode, "efn_FILEONE", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_FILEONE_EXP")]
        public IActionResult GetEfn_FILEONE_EXP([FromRoute] string mode, [FromQuery][Required] string Fileone, [FromQuery][Required] string Divisione, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Fileone", Fileone),
                new SqlParameter("@Divisione", Divisione)
            };
            return GetExportStream(mode, "efn_FILEONE_EXP", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_FILEONE_EXP1")]
        public IActionResult GetEfn_FILEONE_EXP1([FromRoute] string mode, [FromQuery][Required] string Fileone, [FromQuery][Required] string Divisione, [FromQuery][Required] string Mag, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Fileone", Fileone),
                new SqlParameter("@Divisione", Divisione),
                new SqlParameter("@Mag", Mag)
            };
            return GetExportStream(mode, "efn_FILEONE_EXP1", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_FILEONI_CARRELLI")]
        public IActionResult GetEfn_FILEONI_CARRELLI([FromRoute] string mode, [FromQuery][Required] string FileoneRIF, [FromQuery][Required] string FileoneCOMP, [FromQuery][Required] DateTime? DataRif, 
            [FromQuery][Required] string Ordinamento, [FromQuery] string Where = "")
        {   var parameters = new List<SqlParameter>
            {
                new SqlParameter("@FileoneRIF", FileoneRIF),
                new SqlParameter("@FileoneCOMP", FileoneCOMP),
                new SqlParameter("@DataRif", SqlDbType.SmallDateTime)
                {
                    Value = DataRif != null? DataRif : DateTime.Now
                },
                new SqlParameter("@Ordinamento", Ordinamento)
            };
            return GetExportStream(mode, "efn_FILEONI_CARRELLI", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_FILEONI_COMP")]
        public IActionResult GetEfn_FILEONI_COMP([FromRoute] string mode, [FromQuery][Required] string FileoneRIF, [FromQuery][Required] string FileoneCOMP, [FromQuery][Required] string Ordinamento, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@FileoneRIF", FileoneRIF),
                new SqlParameter("@FileoneCOMP", FileoneCOMP),
                new SqlParameter("@Ordinamento", Ordinamento)
            };
            return GetExportStream(mode, "efn_FILEONI_COMP", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_FOGLIO_MONTAGGIO")]
        public IActionResult GetEfn_FOGLIO_MONTAGGIO([FromRoute] string mode, [FromQuery][Required] int Ultimo, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Ultimo", Ultimo)
            };
            return GetExportStream(mode, "efn_FOGLIO_MONTAGGIO", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_FOGLIO_SMONTAGGIO")]
        public IActionResult GetEfn_FOGLIO_SMONTAGGIO([FromRoute] string mode, [FromQuery][Required] string Codice, [FromQuery][Required] string Revisione, 
            [FromQuery][Required] string Progressivo, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Codice", Codice),
                new SqlParameter("@Revisione", Revisione),
                new SqlParameter("@Progressivo", Progressivo)
            };
            return GetExportStream(mode, "efn_FOGLIO_SMONTAGGIO", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_FOGLIO_SMONTAGGIO_2")]
        public IActionResult GetEfn_FOGLIO_SMONTAGGIO_2([FromRoute] string mode, [FromQuery][Required] string Codice, [FromQuery][Required] string Revisione, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Codice", Codice),
                new SqlParameter("@Revisione", Revisione)
            };
            return GetExportStream(mode, "efn_FOGLIO_SMONTAGGIO_2", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_FOGLIO_SMONTAGGIO_3")]
        public IActionResult GetEfn_FOGLIO_SMONTAGGIO_3([FromRoute] string mode, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_FOGLIO_SMONTAGGIO_3", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_GestioneID")]
        public IActionResult GetEfn_GestioneID([FromRoute] string mode, [FromQuery] string Codice = "", [FromQuery] int Id = 0, 
            [FromQuery] string Montati = "",
            [FromQuery] string Esito = "", [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Codice", Codice),
                new SqlParameter("@Id", Id),
                new SqlParameter("@Montati", Montati),
                new SqlParameter("@Esito", Esito)
            };
            return GetExportStream(mode, "efn_GestioneID", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_FOGLIO_MONTAGGIO_NOROW")]
        public IActionResult Getfn_FOGLIO_MONTAGGIO_NOROW([FromRoute] string mode, [FromQuery][Required] int Ultimo, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Ultimo", Ultimo)
            };

            return GetExportStream(mode, "efn_FOGLIO_MONTAGGIO_NOROW", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_GestioneID1")]
        public IActionResult GetEfn_GestioneID1([FromRoute] string mode, [FromQuery][Required] string Fileone, [FromQuery][Required] string TT, 
            [FromQuery][Required] string Intervento, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@Fileone", Fileone),
                new SqlParameter("@TT", TT),
                new SqlParameter("@Intervento", Intervento)
            };
            return GetExportStream(mode, "efn_GestioneID1", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_MADRE_EXP")]
        public IActionResult GetEfn_MADRE_EXP([FromRoute] string mode, [FromQuery][Required] string Madre, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Madre", Madre) };
            return GetExportStream(mode, "efn_MADRE_EXP", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_MAGAZZINO")]
        public IActionResult GetEfn_MAGAZZINO([FromRoute] string mode, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_MAGAZZINO", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_MAGAZZINO_DMH")]
        public IActionResult GetEfn_MAGAZZINO_DMH([FromRoute] string mode, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_MAGAZZINO_DMH", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_MAGAZZINO_PBI")]
        public IActionResult GetEfn_MAGAZZINO_PBI([FromRoute] string mode, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_MAGAZZINO_PBI", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_MAINTENANCE")]
        public IActionResult GetEfn_MAINTENANCE([FromRoute] string mode, [FromQuery][Required] int Id, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", Id) };
            return GetExportStream(mode, "efn_MAINTENANCE", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_MovimentiMAG")]
        public IActionResult GetEfn_MovimentiMAG([FromRoute] string mode, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_MovimentiMAG", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_PDFMapping")]
        public IActionResult GetEfn_PDFMapping([FromRoute] string mode, [FromQuery][Required] string Ambiente, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Ambiente", Ambiente) };
            return GetExportStream(mode, "efn_PDFMapping", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_PREVIEWPL")]
        public IActionResult GetEfn_PREVIEWPL([FromRoute] string mode, [FromQuery][Required] int Id, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Id", Id) };
            return GetExportStream(mode, "efn_PREVIEWPL", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_PRIMIINGRESSI")]
        public IActionResult GetEfn_PRIMIINGRESSI([FromRoute] string mode, [FromQuery][Required] int Tipo, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Tipo", Tipo) };
            return GetExportStream(mode, "efn_PRIMIINGRESSI", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_PROVE")]
        public IActionResult GetEfn_PROVE([FromRoute] string mode, [FromQuery] string TRR = "", [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@TRR", TRR) };
            return GetExportStream(mode, "efn_PROVE", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_RIGHEORDINE")]
        public IActionResult GetEfn_RIGHEORDINE([FromRoute] string mode, [FromQuery] string Magazzini = "", [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Magazzini", Magazzini) };
            return GetExportStream(mode, "efn_RIGHEORDINE", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_RIGHEORDINE1")]
        public IActionResult GetEfn_RIGHEORDINE1([FromRoute] string mode, [FromQuery][Required] string Magazzini, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Magazzini", Magazzini) };
            return GetExportStream(mode, "efn_RIGHEORDINE1", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_RIGHEORDINE2")]
        public IActionResult GetEfn_RIGHEORDINE2([FromRoute] string mode, [FromQuery] string Magazzini = "", [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter> { new SqlParameter("@Magazzini", Magazzini) };
            return GetExportStream(mode, "efn_RIGHEORDINE2", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_SchedeMontaggio")]
        public IActionResult GetEfn_SchedeMontaggio([FromRoute] string mode, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_SchedeMontaggio", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_SPC")]
        public IActionResult GetEfn_SPC([FromRoute] string mode, [FromQuery][Required] string MotoMotore, [FromQuery][Required] string Modello, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>
            {
                new SqlParameter("@MotoMotore", MotoMotore),
                new SqlParameter("@Modello", Modello)
            };
            return GetExportStream(mode, "efn_SPC", ProgramType.Function, parameters, Where);
        }

        [HttpGet("{mode}/efn_UltimoMovimento")]
        public IActionResult GetEfn_UltimoMovimento([FromRoute] string mode, [FromQuery] string Where = "")
        {
            var parameters = new List<SqlParameter>();
            return GetExportStream(mode, "efn_UltimoMovimento", ProgramType.Function, parameters, Where);
        }

        #region Private

        private IActionResult GetExportStream(string mode, string functionName, ProgramType operationType, List<SqlParameter> parameters, string whereClause = "")
        {

            var stopwatch = Stopwatch.StartNew();

            var userName = DomainUpnManager.GetUpnFromActiveDirectory(User.Identity?.Name ?? "anonymous", _securityOptions);

            parameters.Add(new SqlParameter("@upn", userName));

            // Log dell’azione
            _logger.LogInformation("[#ENDPOINT#] User {User} called GetExportStream. Mode={Mode}, Function={Function}, Type={Type}, Params={Params}",
                userName,
                mode,
                functionName,
                operationType,
                string.Join(", ", parameters.Select(p => $"{p.ParameterName}={p.Value}")));

            IActionResult result;

            try
            {

                if (mode.Equals("Xml", StringComparison.OrdinalIgnoreCase))
                    result = GetXmlStream(functionName, operationType, parameters, whereClause, stopwatch);
                else if (mode.Equals("XmlExcel", StringComparison.OrdinalIgnoreCase))
                    result = GetXmlStreamWithCache(functionName, operationType, parameters, whereClause, userName, stopwatch);
                else if (mode.Equals("Json", StringComparison.OrdinalIgnoreCase))
                    result = GetJsonStream(functionName, operationType, parameters, whereClause, stopwatch);
                else if (mode.Equals("JsonExcel", StringComparison.OrdinalIgnoreCase))
                    result = GetJsonStreamWithCache(functionName, operationType, parameters, whereClause, userName, stopwatch);
                else
                    result = BadRequest("Invalid mode. Use 'XmlExcel', 'Json', or 'JsonExcel'.");

                stopwatch.Stop();

                _logger.LogInformation("GetExportStream completed in {ElapsedMilliseconds} ms for user {User}", stopwatch.ElapsedMilliseconds, userName);

                return result;
            }
            catch (Exception ex)
            {
                return BadRequest("GetExportStream broken.Exceptio ex = " + ex.Message);
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
        private IActionResult GetJsonStream(string functionName, ProgramType functionType, List<SqlParameter> parameters,string whereClause, Stopwatch stopwatch)
        {
            MemoryStream stream = _dataExportEngine.GetJsonStream(functionName, functionType, parameters, whereClause, stopwatch);

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
        private IActionResult GetJsonStreamWithCache(string functionName, ProgramType functionType, List<SqlParameter> parameters, string whereClause, string userName, Stopwatch stopwatch)
        {
            var keyParams = string.Join(";", parameters.Select(p => $"{p.ParameterName}={p.Value}"));
            var cacheKey = $"{functionName}|{userName}|{keyParams}|{whereClause}|JsonFriendly";

            // Controllo cache
            if (_cache.TryGetValue(cacheKey, out var existing) &&
                System.IO.File.Exists(existing.FilePath) &&
                DateTime.UtcNow < existing.Expiration)
            {
                _logger.LogInformation("[{Function}] Cache HIT (Excel-friendly JSON) per {Key}", functionName, cacheKey);
                return PhysicalFile(existing.FilePath, "application/json; charset=utf-8", Path.GetFileName(existing.FilePath));
            }

            string filePath = _dataExportEngine.GetJsonStreamWithCache(functionName, functionType, parameters,whereClause, stopwatch, cacheKey, _generationTasks, _cache);

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
        private IActionResult GetXmlStream(string functionName, ProgramType functionType, List<SqlParameter> parameters, string whereClause, Stopwatch stopwatch)
        {
            MemoryStream ms;
            string xmlContent;

            _dataExportEngine.GetXmlStream(functionName, functionType, parameters, whereClause, stopwatch, out ms, out xmlContent);

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
        private IActionResult GetXmlStreamWithCache(string functionName, ProgramType functionType, List<SqlParameter> parameters, 
            string whereClause, string userName, Stopwatch stopwatch)
        {
            var keyParams = string.Join(";", parameters.Select(p => $"{p.ParameterName}={p.Value}"));
            var cacheKey = $"{functionName}|{userName}|{keyParams}|{whereClause}|Xml";

            if (_cache.TryGetValue(cacheKey, out var existing) &&
                System.IO.File.Exists(existing.FilePath) &&
                DateTime.UtcNow < existing.Expiration)
            {
                _logger.LogInformation("[{Function}] Cache HIT (redirect) per {Key}", functionName, cacheKey);
                return PhysicalFile(existing.FilePath, "application/xml", Path.GetFileName(existing.FilePath));
            }

            string filePath = _dataExportEngine.GetXmlStreamWithCache(functionName, functionType, parameters, whereClause, stopwatch, cacheKey, _generationTasks, _cache);

            return PhysicalFile(filePath, "application/xml", Path.GetFileName(filePath));
        }

        


       
        #endregion

        #endregion
    }
}
