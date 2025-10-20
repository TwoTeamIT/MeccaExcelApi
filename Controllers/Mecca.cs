using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Xml;

namespace DucatiExcelApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class Mecca : ControllerBase
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private readonly ILogger<Mecca> _logger;

        public Mecca(ILogger<Mecca> logger)
        {
            _logger = logger;
        }

        

        [HttpGet("efn_ARTICOLI")]
        [AllowAnonymous]
        public IActionResult efn_ARTICOLI([FromQuery] string Distinta)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_ARTICOLI", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Distinta", Distinta);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_ARTICOLI_DBEXP")]
        public IActionResult efn_ARTICOLI_DBEXP([FromQuery] string Fileoni)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_ARTICOLI_DBEXP", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Fileoni", Fileoni);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_ARTICOLI_NEW")]
        public IActionResult efn_ARTICOLI_NEW([FromQuery] string Parte)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_ARTICOLI_NEW", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Parte", Parte);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_DBEXP")]
        public IActionResult efn_DBEXP([FromQuery] string Distinta)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_DBEXP", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Distinta", Distinta);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_FDMxMatricola")]
        public IActionResult efn_FDMxMatricola([FromQuery] string Descrizione)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_FDMxMatricola", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Descrizione", Descrizione);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_FILEONE")]
        public IActionResult efn_FILEONE([FromQuery] string Distinta)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_FILEONE", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Distinta", Distinta);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_FILEONE_EXP")]
        public IActionResult efn_FILEONE_EXP([FromQuery] string Fileone)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_FILEONE_EXP", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Fileone", Fileone);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_FILEONE_EXP1")]
        public IActionResult efn_FILEONE_EXP1([FromQuery] string Fileone)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_FILEONE_EXP1", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Fileone", Fileone);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_FILEONI_CARRELLI")]
        public IActionResult efn_FILEONI_CARRELLI([FromQuery] string FileoneRIF)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_FILEONI_CARRELLI", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FileoneRIF", FileoneRIF);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_FILEONI_COMP")]
        public IActionResult efn_FILEONI_COMP([FromQuery] string FileoneRIF)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_FILEONI_COMP", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FileoneRIF", FileoneRIF);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_FOGLIO_MONTAGGIO")]
        public IActionResult efn_FOGLIO_MONTAGGIO([FromQuery] int Ultimo)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_FOGLIO_MONTAGGIO", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Ultimo", Ultimo);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_FOGLIO_SMONTAGGIO")]
        public IActionResult efn_FOGLIO_SMONTAGGIO([FromQuery] string Codice)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_FOGLIO_SMONTAGGIO", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Codice", Codice);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_FOGLIO_SMONTAGGIO_2")]
        public IActionResult efn_FOGLIO_SMONTAGGIO_2([FromQuery] string Codice)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_FOGLIO_SMONTAGGIO_2", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Codice", Codice);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_GestioneID")]
        public IActionResult efn_GestioneID([FromQuery] string Codice)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_GestioneID", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Codice", Codice);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_GestioneID1")]
        public IActionResult efn_GestioneID1([FromQuery] string Fileone)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_GestioneID1", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Fileone", Fileone);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_MADRE_EXP")]
        public IActionResult efn_MADRE_EXP([FromQuery] string Madre)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_MADRE_EXP", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Madre", Madre);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_MAINTENANCE")]
        public IActionResult efn_MAINTENANCE([FromQuery] int Id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_MAINTENANCE", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", Id);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_PDFMapping")]
        public IActionResult efn_PDFMapping([FromQuery] string Ambiente)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_PDFMapping", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Ambiente", Ambiente);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_PREVIEWPL")]
        public IActionResult efn_PREVIEWPL([FromQuery] int Id)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_PREVIEWPL", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Id", Id);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_PRIMIINGRESSI")]
        public IActionResult efn_PRIMIINGRESSI([FromQuery] int Tipo)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_PRIMIINGRESSI", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Tipo", Tipo);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_PROVE")]
        public IActionResult efn_PROVE([FromQuery] string TRR)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_PROVE", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@TRR", TRR);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_RIGHEORDINE")]
        public IActionResult efn_RIGHEORDINE([FromQuery] string Magazzini)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_RIGHEORDINE", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Magazzini", Magazzini);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_RIGHEORDINE1")]
        public IActionResult efn_RIGHEORDINE1([FromQuery] string Magazzini)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_RIGHEORDINE1", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Magazzini", Magazzini);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_RIGHEORDINE2")]
        public IActionResult efn_RIGHEORDINE2([FromQuery] string Magazzini)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_RIGHEORDINE2", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Magazzini", Magazzini);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("efn_SPC")]
        public IActionResult efn_SPC([FromQuery] string MotoMotore)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("efn_SPC", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MotoMotore", MotoMotore);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("v4_ADDIN_CompareFileoni")]
        public IActionResult v4_ADDIN_CompareFileoni([FromQuery] string FileoneRIF)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("v4_ADDIN_CompareFileoni", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@FileoneRIF", FileoneRIF);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("v4_ADDIN_DistintaMadreExp")]
        public IActionResult v4_ADDIN_DistintaMadreExp([FromQuery] string Madre)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("v4_ADDIN_DistintaMadreExp", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Madre", Madre);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("v4_ADDIN_ElencoEsaurimentixCodice")]
        public IActionResult v4_ADDIN_ElencoEsaurimentixCodice([FromQuery] string Codice)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("v4_ADDIN_ElencoEsaurimentixCodice", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Codice", Codice);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("v4_ADDIN_EsplodiFileoni_Figli")]
        public IActionResult v4_ADDIN_EsplodiFileoni_Figli([FromQuery] string Fileoni)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("v4_ADDIN_EsplodiFileoni_Figli", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Fileoni", Fileoni);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("v4_ADDIN_EsplodiMaintenance_Figli")]
        public IActionResult v4_ADDIN_EsplodiMaintenance_Figli([FromQuery] string Fileoni)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("v4_ADDIN_EsplodiMaintenance_Figli", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Fileoni", Fileoni);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("v4_ADDIN_FOGLIO_MONTAGGIO")]
        public IActionResult v4_ADDIN_FOGLIO_MONTAGGIO([FromQuery] string Nome)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("v4_ADDIN_FOGLIO_MONTAGGIO", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Nome", Nome);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("v4_ADDIN_GetFileName_PRM")]
        public IActionResult v4_ADDIN_GetFileName_PRM([FromQuery] string Parti)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("v4_ADDIN_GetFileName_PRM", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Parti", Parti);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("v4_ADDIN_InterrogaDistinte")]
        public IActionResult v4_ADDIN_InterrogaDistinte([FromQuery] string Madre)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("v4_ADDIN_InterrogaDistinte", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Madre", Madre);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("v4_ADDIN_ListaCronologiaCerchiSPX")]
        public IActionResult v4_ADDIN_ListaCronologiaCerchiSPX([FromQuery] int Season)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("v4_ADDIN_ListaCronologiaCerchiSPX", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Season", Season);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("v4_ADDIN_ListaRigheListinixDistinta")]
        public IActionResult v4_ADDIN_ListaRigheListinixDistinta([FromQuery] string Fileone)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("v4_ADDIN_ListaRigheListinixDistinta", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Fileone", Fileone);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("v4_ADDIN_RicercaCodiceinDistinteAttive")]
        public IActionResult v4_ADDIN_RicercaCodiceinDistinteAttive([FromQuery] string Parte)
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            using (SqlCommand cmd = new SqlCommand("v4_ADDIN_RicercaCodiceinDistinteAttive", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@Parte", Parte);

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataTable dt = new DataTable();
                da.Fill(dt);

                string json = JsonConvert.SerializeObject(dt, Formatting.Indented);
                return Content(json, "application/json");
            }
        }

        [HttpGet("public")]
        [AllowAnonymous]
        public IActionResult PublicInfo()
        {
            return Ok("Questa è pubblica");
        }
    }
}
