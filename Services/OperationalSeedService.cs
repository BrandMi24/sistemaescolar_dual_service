using ControlEscolar.Data;
using ControlEscolar.Models.ModuleCommon;
using ControlEscolar.Models.Operational;
using Microsoft.EntityFrameworkCore;

namespace ControlEscolar.Services;

public class OperationalSeedService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<OperationalSeedService> _logger;

    public OperationalSeedService(ApplicationDbContext context, ILogger<OperationalSeedService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        var firstCareerId = await _context.CareersOperational
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync();

        if (!await _context.OperationalPrograms.AnyAsync(x => x.Type == ProgramTypes.PRACTICAS_PROFESIONALES && x.Status && x.IsActive))
        {
            _context.OperationalPrograms.Add(new OperationalProgram
            {
                Code = "DUAL-DEFAULT",
                Name = "Modelo Dual General",
                Type = ProgramTypes.PRACTICAS_PROFESIONALES,
                Period = DateTime.Now.Month <= 6 ? "ENERO-JUNIO" : "AGOSTO-DICIEMBRE",
                Year = DateTime.Now.Year,
                CareerId = firstCareerId,
                RequiredHours = 480,
                IsActive = true,
                Status = true
            });

            _logger.LogInformation("Seed: se agrego programa base de Modelo Dual.");
        }

        if (!await _context.OperationalPrograms.AnyAsync(x => x.Type == ProgramTypes.SERVICIO_SOCIAL && x.Status && x.IsActive))
        {
            _context.OperationalPrograms.Add(new OperationalProgram
            {
                Code = "SS-DEFAULT",
                Name = "Servicio Social General",
                Type = ProgramTypes.SERVICIO_SOCIAL,
                Period = DateTime.Now.Month <= 6 ? "ENERO-JUNIO" : "AGOSTO-DICIEMBRE",
                Year = DateTime.Now.Year,
                CareerId = firstCareerId,
                RequiredHours = 480,
                IsActive = true,
                Status = true
            });

            _logger.LogInformation("Seed: se agrego programa base de Servicio Social.");
        }

        await _context.SaveChangesAsync();
    }
}
