using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Domain.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using PayosferCastle.CastleService.Dtos.EmailLog;
using PayosferCastle.CastleService.Dtos.Request;
using PayosferCastle.CastleService.Dtos.Response;
using PayosferCastle.CastleService.EmailProviders;
using PayosferCastle.CastleService.Entities;

using PayosferCastle.CastleService.Localization;
using PayosferCastle.CastleService.Models;
using PayosferCastle.CastleService.Repositories;
using Volo.Abp;
using Volo.Abp.Validation;


namespace PayosferCastle.CastleService.Services
{
    public class EmailSenderService : CastleAppService, IEmailSenderService
    {
        private IMerchantRepository _merchantRepository;
        private IServiceProvider _serviceProvider;
        private IEmailProvidersService _emailProvider;
        private IEmailLogRepository _emailLogRepository;
        private IPayosferEmailSender _payosferMailSender;
        private readonly IStringLocalizer<CastleServiceResource> _localizer;
        public EmailSenderService(IMerchantRepository merchantRepository,
            IServiceProvider serviceProvider,
            IEmailProvidersService emailProvider,
            IEmailLogRepository emailLogRepository,
            IPayosferEmailSender payosferMailSender,
            IStringLocalizer<CastleServiceResource> localizer)
        {
            _merchantRepository = merchantRepository;
            _serviceProvider = serviceProvider;
            _emailProvider = emailProvider;
            _emailLogRepository = emailLogRepository;
            _payosferMailSender = payosferMailSender;
            _localizer = localizer;
        }

        public async Task<List<EmailListDto>> GetMerchantEmails()
        {
            var merchants = await _merchantRepository.GetListAsync();
            var emailList = merchants.Select(x => new EmailListDto()
            {
                Email = x.Email,
                MerchantName = x.Name
            }).ToList();
            return emailList;
        }

        public async Task<EmailSendStatusDto> SendMail(EmailLogDto input)
        {

            var emailProviders = await _emailProvider.GetAllEmailProviders();
            var provider = emailProviders.FirstOrDefault(x => x.IsActive == Status.ACTIVE);
            if (provider == null)
            {
                throw new AbpValidationException("CastleService::00001");
            }
            
            var response = new EmailSendStatusDto();
            var emailProviderList = await _emailProvider.GetAllEmailProviders();

            var emailProvider = emailProviderList.FirstOrDefault(x => x.IsActive == Status.ACTIVE);

            try
            {
                await _payosferMailSender.SendAsync(emailProvider, input.ToAddress, input.Subject, input.Body, input.CcAddress, true);
                response.IsEmailSend = true;
                var emailLog = ObjectMapper.Map<EmailLogDto, EmailLog>(input);
                await _emailLogRepository.InsertAsync(emailLog);
            }
            catch (Exception ex) 
            {
                throw new UserFriendlyException("Deneme");
            }

            return response;

        }
    }
}
