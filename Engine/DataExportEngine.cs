using Azure;
using DucatiExcelApi.Controllers;
using DucatiMeccaExcelApi.Utility;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace DucatiMeccaExcelApi.Engine
{
    public class DataExportEngine
    {
        private DataExportService _dataExportService;
        private ILogger<Functions> _logger;
        private string _folderPath;
        private double _cacheDurationMinutes;

        public DataExportEngine(string connStr, ILogger<Functions> logger, IOptions<EndPointCacheConfig> options) {
            _logger = logger;

            _folderPath = options.Value.FolderPath;
            _cacheDurationMinutes = options.Value.CacheDurationMinutes;

            _dataExportService = new DataExportService(connStr);
        }

        public MemoryStream GetJsonStream(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch)
        {
            var dt = _dataExportService.Execute(functionName, functionType, parameters);

            string json = JsonConvert.SerializeObject(dt, Newtonsoft.Json.Formatting.None);

            stopwatch.Stop();
            _logger.LogInformation("[{Function}] JsonStream - Tempo totale: {Elapsed}", functionName, stopwatch.Elapsed);

            var bytes = Encoding.UTF8.GetBytes(json);
            var stream = new MemoryStream(bytes);
            return stream;
        }

        public string GetJsonStreamWithCache(string functionName, 
            ProgramType functionType, List<SqlParameter> parameters,
            Stopwatch stopwatch, string cacheKey,
            ConcurrentDictionary<string, Lazy<Task<string>>> generationTasks, 
            Dictionary<string, (string FilePath, DateTime Expiration)> cache)
        {
            // Generazione file
            var lazyTask = generationTasks.GetOrAdd(cacheKey, k => new Lazy<Task<string>>(async () =>
            {
                if (cache.TryGetValue(cacheKey, out var before) &&
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
                var tempPath = Path.Combine(_folderPath, fileName);
                var utf8NoBom = new UTF8Encoding(false);
                await System.IO.File.WriteAllTextAsync(tempPath, json, utf8NoBom);

                cache[cacheKey] = (tempPath, DateTime.UtcNow.AddMinutes(_cacheDurationMinutes));
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
                generationTasks.TryRemove(cacheKey, out _);
                _logger.LogError(ex, "[{Function}] Errore durante la generazione JSON Excel-friendly per {Key}", functionName, cacheKey);
                throw;
            }

            stopwatch.Stop();
            _logger.LogInformation("[{Function}] GetJsonStreamWithCache - Tempo totale: {Elapsed}", functionName, stopwatch.Elapsed);
            return filePath;
        }

        public void GetXmlStream(string functionName, ProgramType functionType, List<SqlParameter> parameters, Stopwatch stopwatch, out MemoryStream ms, out string xmlContent)
        {
            var dt = _dataExportService.Execute(functionName, functionType, parameters);
            ms = new MemoryStream();
            dt.WriteXml(ms, XmlWriteMode.WriteSchema);
            
            ms.Position = 0;

            stopwatch.Stop();
            _logger.LogInformation("[{Function}] GetXmlStream - Tempo totale: {Elapsed}", functionName, stopwatch.Elapsed);

            // Converti in stringa (Excel legge meglio da UTF-8)
            xmlContent = Encoding.UTF8.GetString(ms.ToArray());

            
        }

        public string GetXmlStreamWithCache(string functionName, 
            ProgramType functionType, List<SqlParameter> parameters, 
            Stopwatch stopwatch, string cacheKey,
            ConcurrentDictionary<string, Lazy<Task<string>>> generationTasks,
            Dictionary<string, (string FilePath, DateTime Expiration)> cache)
        {
            var lazyTask = generationTasks.GetOrAdd(cacheKey, k => new Lazy<Task<string>>(() => Task.Run(async () =>
            {
                if (cache.TryGetValue(cacheKey, out var before) &&
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
                    //dt.WriteXml(sw, XmlWriteMode.WriteSchema);
                    dt.WriteXml(sw, XmlWriteMode.IgnoreSchema);
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
                cache[cacheKey] = (tempPath, DateTime.UtcNow.AddMinutes(10));

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
                generationTasks.TryRemove(cacheKey, out _);
                _logger.LogError(ex, "[{Function}] Errore durante la generazione per {Key}", functionName, cacheKey);
                throw;
            }

            stopwatch.Stop();
            _logger.LogInformation("[{Function}] GetXmlStreamRedirect - Tempo totale: {Elapsed}", functionName, stopwatch.Elapsed);
            return filePath;
        }

        
        #region Private
        
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
    }
}
