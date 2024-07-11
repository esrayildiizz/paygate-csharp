using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.APIKey;
using PayosferCastle.CastleService.Dtos.Terminals;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Repositories;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Uow;

namespace PayosferCastle.CastleService.Services
{
    public class ApiKeyService : CastleAppService, IApiKeyService
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IApiKeyRepository _apiKeysRepository;
        

        public ApiKeyService(
            IApiKeyRepository apiKeyRepository,
            IUnitOfWorkManager unitOfWorkManager )

        {
            this._unitOfWorkManager = unitOfWorkManager;
            this._apiKeysRepository = apiKeyRepository;

        }


        public async Task<APIKeysDto> GetAPIKeysAsync(int Id)
        {
            var apiKeyQueryable = await _apiKeysRepository.GetQueryableAsync();
            var entity = await apiKeyQueryable.FirstOrDefaultAsync(x=>x.Id == Id);
            var responseDto = ObjectMapper.Map<APIKeys, APIKeysDto>(entity);
            return responseDto;
        }

        public async Task<List<APIKeysDto>> GetAllAPIKeysAsync()
        {
            var apiList = await _apiKeysRepository.GetListAsync(); /*x => x.IsDeleted == false*/
            var responseDto = ObjectMapper.Map<List<APIKeys>, List<APIKeysDto>>(apiList);
            return responseDto;
        }

        public async Task SaveAPIKeysAsync(SaveAPIKeysDto input)
        {
            var entity = ObjectMapper.Map<SaveAPIKeysDto, APIKeys>(input);
            entity.TenantId = CurrentTenant.Id;
            await _apiKeysRepository.InsertAsync(entity);
        }

        public async Task UpdateAPIKeysAsync(UpdateAPIKeysDto input)
        {
            var apiKeyQueryable = await _apiKeysRepository.GetQueryableAsync();
            var apiKey = await apiKeyQueryable.FirstOrDefaultAsync(x=>x.Id==input.Id );
            apiKey.Status = input.Status;
            apiKey.Name = input.Name;
            apiKey.APIURL = input.APIURL;
            apiKey.CreatePayment = input.CreatePayment;
            apiKey.PreAuthorization = input.PreAuthorization;
            apiKey.PreAuthorizationClose = input.PreAuthorizationClose;
            apiKey.CancelRefund = input.CancelRefund;
            apiKey.BINInquiry = input.BINInquiry;
            apiKey.InstallmentInquiry = input.InstallmentInquiry;

            await _apiKeysRepository.UpdateAsync(apiKey);
        }

        public async Task DeleteAPIKeysAsync(long id)
        {
            var apiKeyQueryable = await _apiKeysRepository.GetQueryableAsync();
             var entity = await apiKeyQueryable.FirstOrDefaultAsync(x => x.Id == id);
              await _apiKeysRepository.DeleteAsync(entity);
        }
    }
}
