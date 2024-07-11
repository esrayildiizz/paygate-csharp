using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayosferCastle.CastleService.Dtos.Request;
using PayosferCastle.CastleService.Dtos.Response;

namespace PayosferCastle.CastleService.Services
{
    public class ReportingService : CastleAppService, IReportingService
    {
        public Task<PayoutDetailDto> RetrievePayoutDetailsAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<PayoutBouncedTransactionListDto> SearchBouncedPayoutTransactionsAsync(SearchPayoutBouncedTransactionsInput input)
        {
            throw new NotImplementedException();
        }

        public Task<PayoutCompletedTransactionListDto> SearchPayoutCompletedTransactionsAsync(SearchPayoutCompletedTransactionsInput input)
        {
            throw new NotImplementedException();
        }

        //public Task<PayoutRowListDto> SearchPayoutRowsAsync(SearchPayoutRowsInput input)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
