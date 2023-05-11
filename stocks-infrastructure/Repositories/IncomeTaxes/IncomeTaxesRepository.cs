﻿using stocks.Database;

namespace stocks_infrastructure.Repositories.IncomeTaxes
{
    public class IncomeTaxesRepository : IIncomeTaxesRepository
    {
        private readonly StocksContext context;

        public IncomeTaxesRepository(StocksContext context)
        {
            this.context = context;
        }

        public async Task AddAllAsync(List<Models.IncomeTaxes> incomeTaxes)
        {
            context.AddRange(incomeTaxes);

            context.AttachRange(incomeTaxes.Select(x => x.Account));

            await context.SaveChangesAsync();
        }

        public async Task AddAsync(Models.IncomeTaxes incomeTaxes)
        {
            context.Add(incomeTaxes);

            context.Attach(incomeTaxes.Account);

            await context.SaveChangesAsync();
        }
    }
}