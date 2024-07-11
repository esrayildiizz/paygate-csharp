using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PayosferCastle.CastleService.Business;
using PayosferCastle.CastleService.Dtos.Response;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Models;
using PayosferCastle.CastleService.Repositories;
using Volo.Abp.Domain.Repositories;

namespace PayosferCastle.CastleService.Services
{
    public class DashboardService : CastleAppService, IDashboardService
    {
       
        private readonly IPaymentRepository paymentRepository;
        private readonly IThreeDInitRepository threeDInitRepository;
        private readonly IRefundRepository refundRepository;
        private readonly ICommissionManagementRepository commissionManagementRepository;
        private readonly IBINRepository bINRepository;
        private readonly ISettingRepository settingsRepository;
        private readonly IBankClientFactory bankClientFactory;
        private readonly ITerminalRepository terminalRepository;
        private readonly IMerchantRepository merchantRepository;
        public DashboardService(
           IPaymentRepository paymentRepository, IThreeDInitRepository threeDInitRepository, IRefundRepository refundRepository,
            ICommissionManagementRepository commissionManagementRepository, IBINRepository bINRepository
            , ISettingRepository settingsRepository, IBankClientFactory bankClientFactory
            , ITerminalRepository terminalRepository, IMerchantRepository merchantRepository)
        {
            
            this.paymentRepository = paymentRepository;
            this.threeDInitRepository = threeDInitRepository;
            this.refundRepository = refundRepository;
            this.commissionManagementRepository = commissionManagementRepository;
            this.bINRepository = bINRepository;
            this.settingsRepository = settingsRepository;
            this.bankClientFactory = bankClientFactory;
            this.terminalRepository = terminalRepository;
            this.merchantRepository = merchantRepository;
        }

        public async Task<DashboardDto> DashboardValues()
        {
            var payments = await paymentRepository.GetQueryableAsync();
            var result = new DashboardDto
            {
               
            };
            DateTime todayStart = DateTime.Today;
            DateTime todayEnd = todayStart.AddDays(1).AddTicks(-1);

            DayOfWeek firstDayOfWeek = CultureInfo.CurrentCulture.DateTimeFormat.FirstDayOfWeek;
            int ex = (7 + (todayStart.DayOfWeek - firstDayOfWeek)) % 7;
            DateTime thisWeekStart = todayStart.AddDays(-1 * ex).Date; //olduğun haftanın başlangıç tarihi
            DateTime thisWeekEnd = thisWeekStart.AddDays(7).AddTicks(-1); //olduğun haftanın bitiş tarihi

            var tenantId = CurrentTenant.Id;
            if (tenantId == null)
            {
                var merchants = await merchantRepository.GetQueryableAsync();
                result.MerchantCount = merchants.Count();

                var terminals = await terminalRepository.GetQueryableAsync();
                result.TerminalCount = terminals.Count();
                
                 //O hafta kayıt olan üye iş yerleri
                 result.ThisWeekMerchantCount = merchants.Where(x => x.CreationTime >= thisWeekStart && x.CreationTime <= thisWeekEnd).Count();    
                 //O hafta geçen işlem adedi
                 var thisWeekPayments = payments.Where(x => x.CreationTime >= thisWeekStart && x.CreationTime <= thisWeekEnd);
                 result.ThisWeekPaymentSuccessCount = thisWeekPayments.Where(x => x.PaymentStatus == (int)PaymentStatus.SUCCESS).Count();
                 result.ThisWeekPaymentFailureCount = thisWeekPayments.Where(x => x.PaymentStatus == (int)PaymentStatus.FAILURE).Count();
                
                 //Son Kayıt olan üye iş yerleri(10 tanesi)
                 result.Last10Records = merchants
                     .OrderByDescending(e => e.CreationTime) 
                     .Take(10)
                     .ToList();
                 
            }
            else
            {
                var merchants = await merchantRepository.GetQueryableAsync();
                var currentMerchant = merchants.Where(x => x.TenantId == CurrentTenant.Id).FirstOrDefault();
                var lastTime = DateTime.Today.AddDays(1).AddTicks(-1);
                result.DailyPaymentCount = payments.Where(x => x.CreationTime >= todayStart && x.CreationTime < lastTime).Count();
                result.WeeklyPaymentCount = payments.Where(x => x.CreationTime >= thisWeekStart && x.CreationTime < thisWeekEnd).Count();
                
                DateTime firstDayOfMonth = new DateTime(todayStart.Year, todayStart.Month, 1);
                DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
                
                result.MonthlyPaymentCount = payments.Where(x => x.CreationTime >= firstDayOfMonth && x.CreationTime < lastDayOfMonth).Count();
                
                DateTime firstDayOfYear = new DateTime(todayStart.Year, todayStart.Month, 1);
                DateTime lastDayOfYear = firstDayOfMonth.AddMonths(12).AddDays(-1);
                result.YearlyPaymentCount = payments.Where(x => x.CreationTime >= firstDayOfYear && x.CreationTime < lastDayOfYear).Count();
                // Son 5 İşlem
                //var last5Payment = payments.OrderDescending().Take(5).ToList();
                
                ////Terminal Bazlı İşlem Tutarı
                //var terminalBazliİslem = payments.GroupBy(x => x.TerminalId).ToList();
                //TODO: Select atılıp terminal name'e göre object oluşturulacak.

            }


     
            return result;

        }
    }
}
