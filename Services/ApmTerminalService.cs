using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.ApmTerminal;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Repositories;
using Volo.Abp;


namespace PayosferCastle.CastleService.Services
{
    public class ApmTerminalService : CastleAppService, IApmTerminalService
    {
        private readonly IApmTerminalRepository apmTerminalRepository;
        public ApmTerminalService(IApmTerminalRepository apmTerminalRepository)
        {
            this.apmTerminalRepository = apmTerminalRepository;
        }
        
        public async Task<List<ApmTerminalDto>> GetAllApmTerminalListAsync()
        {
     
            var response = await apmTerminalRepository.GetListAsync(x => x.TenantId == CurrentTenant.Id );
            var responseDto= ObjectMapper.Map<List<ApmTerminal>, List<ApmTerminalDto>>(response.Where(x => x.TenantId == CurrentTenant.Id).ToList());
            return responseDto;
        }

        public async Task<ApmTerminalDto> GetApmTerminalAsync(long id)
        {
            var apmTerminalQueryable= await apmTerminalRepository.GetQueryableAsync();
            var response = await apmTerminalQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id && x.Id == id );
            var responseDto = ObjectMapper.Map<ApmTerminal, ApmTerminalDto>(response);
            return responseDto;
        }
        
      
        public async Task SaveApmTerminalAsync(SaveApmTerminalDto input)
        {
    
            var entity = ObjectMapper.Map<SaveApmTerminalDto, ApmTerminal>(input);
            entity.TenantId = CurrentTenant.Id;
            await apmTerminalRepository.InsertAsync(entity);
            
        }
        
        public async Task UpdateApmTerminalAsync(UpdateApmTerminalDto input)
        {
            
            var apmTerminalQueryable = await apmTerminalRepository.GetQueryableAsync();
            var apmTerminal =await apmTerminalQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id && x.Id == input.Id.Value );
            
            apmTerminal.Status=input.Status;
            apmTerminal.Apm=input.Apm;
            apmTerminal.ApmCode=input.ApmCode;
            apmTerminal.TerminalUserName=input.TerminalUserName;
            apmTerminal.TerminalPassword=input.TerminalPassword;
            apmTerminal.Currency=input.Currency;
            apmTerminal.IsMultiCurrency=input.IsMultiCurrency;
            apmTerminal.IsInstallment=input.IsInstallment;
            apmTerminal.ClientId=input.ClientId;
            apmTerminal.HostName=input.HostName;
            apmTerminal.Path=input.Path;
            apmTerminal.Port=input.Port;
        
            await apmTerminalRepository.UpdateAsync(apmTerminal);
        }
        

        public async Task DeleteApmTerminalAsync(long id)
        {
            var tempRes = await apmTerminalRepository.GetQueryableAsync();
            var filtered = tempRes.Any(x => x.TenantId == CurrentTenant.Id && x.Id == id );
            if(filtered)
            {
                await apmTerminalRepository.DeleteAsync(id);
            }
            else
            {
                throw new UserFriendlyException("APM Terminal not found");
            }
            
        }
        
    }
}