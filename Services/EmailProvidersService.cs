
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using PayosferCastle.CastleService.Dtos.EmailProviders;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Localization;
using PayosferCastle.CastleService.Models;
using PayosferCastle.CastleService.Repositories;
using UserFriendlyException = Abp.UI.UserFriendlyException;

namespace PayosferCastle.CastleService.Services
{
    public class EmailProvidersService: CastleAppService, IEmailProvidersService
    {
        private readonly IEmailProviderRepository _emailProviderRepository;
        private readonly IStringLocalizer<CastleServiceResource> _localizer;
        public EmailProvidersService(IStringLocalizer<
            CastleServiceResource> localizer,
            IEmailProviderRepository emailProviderRepository)
        {
            this._localizer = localizer;
            this._emailProviderRepository = emailProviderRepository;
        }


        public async Task<List<EmailProviderDto>> GetAllEmailProviders()
        {
            var response = await _emailProviderRepository.GetListAsync();
            var responseDto = ObjectMapper.Map<List<EmailProvider>, List<EmailProviderDto>>(response);
            return responseDto;
        }

        public async Task<EmailProviderDto> GetEmailProvider(long id)
        {
            var emailProviderQueryable = await _emailProviderRepository.GetQueryableAsync();
            var response = await emailProviderQueryable.FirstOrDefaultAsync(x => x.Id == id );
            var responseDto = ObjectMapper.Map<EmailProvider, EmailProviderDto>(response);
            return responseDto;;
        }


        public async Task SaveEmailProvider(SaveEmailProviderDto input)
        {
            var activeEmailProviders = await _emailProviderRepository.GetListAsync(x => x.IsActive == Status.ACTIVE); 

            
            var entity = ObjectMapper.Map<SaveEmailProviderDto, EmailProvider>(input);
            await _emailProviderRepository.InsertAsync(entity);

            if (entity.IsActive == Status.ACTIVE)
            {
                foreach (var provider in activeEmailProviders)
                {
                    if (entity.Id == provider.Id)
                    {
                        return;
                    }
                    provider.IsActive = Status.PASSIVE;
                }
            }

        }
        public async Task UpdateTerminal(UpdateEmailProviderDto input)
        {
            var activeEmailProviders = await _emailProviderRepository.GetListAsync(x => x.IsActive == Status.ACTIVE); 
            
            var emailProviderQueryable = await _emailProviderRepository.GetQueryableAsync();
            var emailProvider =await emailProviderQueryable.FirstOrDefaultAsync(x => x.Id == input.Id );
            
            emailProvider.IsActive = input.IsActive;
            emailProvider.DefaultFromDisplayName = input.DefaultFromDisplayName;
            emailProvider.DefaultFromAddress = input.DefaultFromAddress;
            emailProvider.SmtpDomain = input.SmtpDomain;
            emailProvider.SmtpHost = input.SmtpHost;
            emailProvider.SmtpUserName = input.SmtpUserName;
            emailProvider.SmtpPassword = input.SmtpPassword;
            emailProvider.SmtpPort = input.SmtpPort;
            emailProvider.SmtpEnableSsl = input.SmtpEnableSsl;
            emailProvider.SmtpUseDefaultCredentials = input.SmtpUseDefaultCredentials;
    

            await _emailProviderRepository.UpdateAsync(emailProvider);
            
            if (emailProvider.IsActive == Status.ACTIVE)
            {
                foreach (var provider in activeEmailProviders)
                {
                    if (emailProvider.Id == provider.Id)
                    {
                        return;
                    }
                    provider.IsActive = Status.PASSIVE;
                }
            }
  
        }
        
        

        public async Task Delete(long id)
        {
       
            
            var emailProviderQueryable = await _emailProviderRepository.GetQueryableAsync();
            var emailProvider = emailProviderQueryable.Any(x => x.Id == id);

            if (emailProvider)
            {
                await _emailProviderRepository.DeleteAsync(x => x.Id == id);
            }
            else
            {
                throw new UserFriendlyException("Merchant not found");
            }
            
        }

        public Task SendTestEmail(SendTestTenantEmailInput input)
        {
            throw new System.NotImplementedException();
        }


        // public async Task SendTestEmail(SendTestTenantEmailInput input)
        // {
        //     EmailProviderDto emailProvider = ObjectMapper.Map<CreateOrEditEmailProviderDto,EmailProviderDto>(input.EmailProvider);
        //
        //
        //     IPayosferEmailSender _mtEmailSender = IocManager.Instance.Resolve<IPayosferEmailSender>();
        //     await _mtEmailSender.SendAsync(
        //         emailProvider,
        //         input.EmailProvider.DefaultFromAddress,
        //         input.EmailAddress,
        //         L["TestEmail_Subject"],
        //         L["TestEmail_Body"],
        //         true);
        // }
      
    }
}
