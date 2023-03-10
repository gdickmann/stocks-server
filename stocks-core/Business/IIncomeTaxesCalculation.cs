using stocks_core.DTOs.B3;
using stocks_core.Response;

namespace stocks_core.Business
{
    public interface IIncomeTaxesCalculation
    {
        /// <summary>
        /// Adiciona no objeto CalculateAssetsIncomeTaxesResponse os ativos e seus respectivos impostos de renda a serem pagos.
        /// </summary>
        Task<CalculateAssetsIncomeTaxesResponse?> AddAllIncomeTaxesToObject(CalculateAssetsIncomeTaxesResponse? response,
            Movement.EquitMovement? movement);
    }
}
