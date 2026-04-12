using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace ControlEscolar.Data
{
    public class DapperContext
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DapperContext(IConfiguration configuration)
        {
            _configuration = configuration;

            // Aquí lee exactamente el "DefaultConnection" de tu appsettings.json
            _connectionString = _configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("La cadena de conexión 'DefaultConnection' no se encontró.");
        }

        // Este es el motor que usará el DashboardService para hacer consultas a la velocidad de la luz
        public IDbConnection CreateConnection()
            => new SqlConnection(_connectionString);
    }
}