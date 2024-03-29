﻿using stocks_common.Enums;

namespace stocks_core.Models
{
    public class AssetIncomeTaxes
    {
        public AssetIncomeTaxes(string month, string assetName, IEnumerable<OperationDetails> tradedAssets)
        {
            Month = month;
            AssetName = assetName;
            TradedAssets = tradedAssets;
        }

        /// <summary>
        /// O mês em que o ativo foi negociado.
        /// </summary>
        public string Month { get; init; }

        /// <summary>
        /// O id do tipo de ativo sendo negociado.
        /// </summary>
        public Asset AssetTypeId { get; set; }

        /// <summary>
        /// O nome do tipo de ativo sendo negociado.
        /// </summary>
        public string AssetName { get; set; }

        /// <summary>
        /// Total a ser pago em imposto de renda referente a um ativo.
        /// </summary>
        public double Taxes { get; set; } = 0;

        /// <summary>
        /// Total vendido do ativo.
        /// </summary>
        public double TotalSold { get; set; } = 0;

        /// <summary>
        /// O total de lucro ou prejuízo de um determinado ativo movimentado por swing trade.
        /// </summary>
        public double SwingTradeProfit { get; set; } = 0;

        /// <summary>
        /// O total de lucro ou prejuízo de um determinado ativo movimentado por day trade.
        /// </summary>
        public double DayTradeProfit { get; set; } = 0;

        /// <summary>
        /// Uma lista em formato JSON que representa os ativos negociados.
        /// </summary>
        public IEnumerable<OperationDetails> TradedAssets { get; set; }
    }
}
