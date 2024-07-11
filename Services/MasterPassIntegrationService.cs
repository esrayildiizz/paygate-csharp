using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.MasterpassIntegration;
using PayosferCastle.CastleService.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace PayosferCastle.CastleService.Services
{
    public class MasterPassIntegrationService : CastleAppService, IMasterPassIntegrationService
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<MasterpassIntegration> _masterPassIntegrationSettingsRepository;
        private readonly IRepository<Merchant> _merchantRepository;

        public MasterPassIntegrationService(
            IUnitOfWorkManager unitOfWorkManager,
            IRepository<Feature> featuresRepository,
            IRepository<PilotNotification> pilotNotificationSettingsRepository,
            IRepository<MasterpassIntegration> masterPassIntegrationSettingsRepository,
            IRepository<Merchant> merchantRepository)
        {
            this._unitOfWorkManager = unitOfWorkManager;
            this._masterPassIntegrationSettingsRepository = masterPassIntegrationSettingsRepository;
            this._merchantRepository = merchantRepository;

        }

        public async Task<MasterPassIntegrationDto> GetMasterPassIntegrationAsync()
        {
            var masterPassQueryable = await _masterPassIntegrationSettingsRepository.GetQueryableAsync();
            var response = await masterPassQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id);
            var responseDto = ObjectMapper.Map<MasterpassIntegration, MasterPassIntegrationDto>(response);
            return responseDto;
        }

        public async Task UpdateMasterPassIntegrationAsync(UpdateMasterPassIntegrationDto input)
        {
            var masterPassQueryable = await _masterPassIntegrationSettingsRepository.GetQueryableAsync();
            var masterPass = await masterPassQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id);
            masterPass.ClientId = input.ClientId;
            masterPass.EncKey = input.EncKey;
            masterPass.MacKey = input.MacKey;
            await _masterPassIntegrationSettingsRepository.UpdateAsync(masterPass);

        }
    }
}
