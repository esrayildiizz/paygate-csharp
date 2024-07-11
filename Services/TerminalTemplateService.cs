using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.TerminalTemplates;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Repositories;
using Volo.Abp;

namespace PayosferCastle.CastleService.Services
{
    
    public class TerminalTemplateService : CastleAppService, ITerminalTemplateService
    {
        private readonly ITerminalTemplateRepository _terminalTemplateRepository;

        public TerminalTemplateService(ITerminalTemplateRepository terminalTemplateRepository)
        {
            _terminalTemplateRepository = terminalTemplateRepository;
        }

        public async Task<List<TerminalTemplateDto>> GetAllTerminalTemplates()
        {
            var response = await _terminalTemplateRepository.GetListAsync();
            var responseDto = ObjectMapper.Map<List<TerminalTemplate>, List<TerminalTemplateDto>>(response);

            return responseDto;
        }

        public async Task Save(TerminalTemplateSaveDto input)
        {
            var entity = ObjectMapper.Map<TerminalTemplateSaveDto, TerminalTemplate>(input);
            entity.TenantId = CurrentTenant.Id;
            await _terminalTemplateRepository.InsertAsync(entity);

        }

        public async Task Update(TerminalTemplateUpdateDto input)
        {
            var terminalQueryable = await _terminalTemplateRepository.GetQueryableAsync();
            var terminal = await terminalQueryable.FirstOrDefaultAsync(x => x.Id == input.Id );

            terminal.TemplateName = input.TemplateName;
            terminal.BankTerminalId = input.BankTerminalId;
            terminal.Status = input.Status;
            terminal.Name = input.Name;
            terminal.Bank = input.Bank;
            terminal.Alias = input.Alias;
            terminal.BankCode = input.BankCode;
            terminal.CastlePilotStatus = input.CastlePilotStatus;
            terminal.ClientId = input.ClientId;
            terminal.ThreeDSecureKey = input.ThreeDSecureKey;
            terminal.TerminalUserName = input.TerminalUserName;
            terminal.TerminalPassword = input.TerminalPassword;
            terminal.Currency = input.Currency;
            terminal.MultiCurrency = input.MultiCurrency;
            terminal.Installment = input.Installment;
            terminal.NonCVV = input.NonCVV;
            terminal.Mandatory3DS = input.Mandatory3DS;
            terminal.Mode = input.Mode;
            terminal.HostName = input.HostName;
            terminal.Path = input.Path;
            terminal.Port = input.Port;
            terminal.ThreeDSsPath = input.ThreeDSsPath;
            terminal.SupportedCardAssociations = input.SupportedCardAssociations;
            // terminal.PROVAUTUserName = input.PROVAUTUserName;
            // terminal.PROVAUTPassword = input.PROVAUTPassword;
            // terminal.PROVRFNUserName = input.PROVRFNUserName;
            // terminal.PROVRFNPassword = input.PROVRFNPassword;
            terminal.PosnetId = input.PosnetId;
            terminal.ThreeDTerminalId = input.ThreeDTerminalId;
            terminal.ThreeDPosnetId = input.ThreeDPosnetId;
            // terminal.IsNewPosnet = input.IsNewPosnet;
            terminal.MarketNumber = input.MarketNumber;
            await _terminalTemplateRepository.UpdateAsync(terminal);
            
        }

        public async Task<TerminalTemplateDto> GetTemplate(long id)
        {
            var terminalQueryable = await _terminalTemplateRepository.GetQueryableAsync();
            var response = terminalQueryable.FirstOrDefault(x => x.TenantId == CurrentTenant.Id && x.Id == id );
            var responseDto = ObjectMapper.Map<TerminalTemplate, TerminalTemplateDto>(response);
            return responseDto;
        }
        
        
        public async Task Delete(long id)
        {
            var tempRes = await _terminalTemplateRepository.GetQueryableAsync();
            var filtered = tempRes.Any(x =>  x.Id == id);
            if(filtered)
            {
                await _terminalTemplateRepository.DeleteAsync(id);
            }
            else
            {
                throw new UserFriendlyException("Terminal not found");
            }
        }
    }
}
