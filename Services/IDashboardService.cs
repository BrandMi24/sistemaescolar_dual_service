using ControlEscolar.Models.Dashboard;

namespace ControlEscolar.Services
{
    public interface IDashboardService
    {
        Task<RectorateViewModel> GetRectorateDataAsync(int? year = null, int? cuatrimestre = null);
        Task<AdmissionsViewModel> GetAdmissionsDataAsync(int? year = null, int? cuatrimestre = null);
        Task<TramitesViewModel> GetTramitesDataAsync(int? year = null, int? cuatrimestre = null);
        Task<MedicalViewModel> GetMedicalDataAsync(int? year = null, int? cuatrimestre = null);
        Task<VinculacionViewModel> GetVinculacionDataAsync(int? year = null, int? cuatrimestre = null);
        Task<AcademicQualityViewModel> GetAcademicQualityDataAsync(int? year = null, int? cuatrimestre = null);
    }
}
