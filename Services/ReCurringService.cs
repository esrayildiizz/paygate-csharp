using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Dependency;
using Abp.Domain.Repositories;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PayosferCastle.CastleService.Business;
using PayosferCastle.CastleService.Dtos.Request;
using PayosferCastle.CastleService.Dtos.Response;
using PayosferCastle.CastleService.EmailProviders;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.HangfireJobs;
using PayosferCastle.CastleService.Helpers;
using PayosferCastle.CastleService.Models;
using PayosferCastle.CastleService.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.ObjectMapping;
using static PayosferCastle.CastleService.Permissions.CastleServicePermissions;

namespace PayosferCastle.CastleService.Services
{
    public class ReCurringService : CastleAppService, IReCurringService
    {
        private readonly IScheduledJobEngine _scheduledTaskEngine;
  
        private readonly IReCurringRepository _reCurringRepository;

        public ReCurringService(IReCurringRepository recurringRepository, IScheduledJobEngine scheduledTaskEngine)
        {

            _scheduledTaskEngine = scheduledTaskEngine;
            this._reCurringRepository = recurringRepository;

        }
        public async Task<List<ReCurringDto>> GetReCurringListAsync()
        {
            var responseDto = new List<ReCurringDto>();
            var response = await _reCurringRepository.GetQueryableAsync();

            var recurring = await response.OrderByDescending(x => x.Id).ToListAsync();

            responseDto = ObjectMapper.Map<List<ReCurring>, List<ReCurringDto>>(recurring);
            return responseDto;
        }
        public async Task<ReCurringDto> CreateReCurringAsync(CreateReCurringInput input)
        {
            var entity = ObjectMapper.Map<CreateReCurringInput, ReCurring>(input);
            entity.RecurringDateTime = input.FirstPaymentDate;
            var response = await _reCurringRepository.InsertAsync(entity);
            var responseDto = ObjectMapper.Map<ReCurring, ReCurringDto>(response);

            string cronDate = "";
            cronDate = CreateCron(input.RecurringPeriodType, input.FirstPaymentDate.Value);

            RecurringJob.RemoveIfExists(string.Format(ScheduledTaskConsts.ReCurringPaymentPlan, input.MerchantId, input.SubscriptionMerchantCode));
            RecurringJob.AddOrUpdate(string.Format(ScheduledTaskConsts.ReCurringPaymentPlan, input.MerchantId, input.SubscriptionMerchantCode), () => _scheduledTaskEngine.ReCurringPaymentPlan((int)input.MerchantId, entity.Id), cronDate , TimeZoneInfo.Local);



            return responseDto;
        }

        public async Task<ReCurringDto> ReCurringUpdateAsync(UpdateReCurringInput input)
        {
            var recurring = await _reCurringRepository.GetAsync(x => x.Id == input.Id);

            recurring.SubscriptionMerchantCode = input.SubscriptionMerchantCode;
            recurring.CurrencyId = input.CurrencyId;
            recurring.Amount = input.Amount;
            recurring.CallbackUrl = input.CallbackUrl;
            recurring.RecurringPeriodType = input.RecurringPeriodType;
            recurring.FailAttempt = input.FailAttempt;
            recurring.FailAttemptPendingHour = input.FailAttemptPendingHour;
            recurring.Status = input.Status;

            var response = await _reCurringRepository.UpdateAsync(recurring);

            var responseDto = ObjectMapper.Map<ReCurring, ReCurringDto>(response);

            return responseDto;
        }

        public async Task DeleteReCurringAsync(long id)
        {
            await _reCurringRepository.DeleteAsync(id);
        }

        #region Private Helper Methods
        private string CreateCron(int types, DateTime startDate)
        {
            var cron = "";
            switch (types)
            {
                case 0://weekly
                    cron = String.Format("{0} {1} ? * {2} *",startDate.Minute, (startDate.Hour),(int)startDate.DayOfWeek);
                    break;                
                case 1://monthly          
                    cron = String.Format("{0} {1} {2} * ?", startDate.Minute, (startDate.Hour), (int)startDate.DayOfWeek);
                    break;                
                case 2://year             
                    cron = String.Format("{0} {1} {2} {3} ? *", startDate.Minute, (startDate.Hour), startDate.Day, startDate.Month);
                    break;
            }
            return cron;
        }
        #endregion
    }
}
