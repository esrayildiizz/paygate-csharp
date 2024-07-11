using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.PilotNotification;
using PayosferCastle.CastleService.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace PayosferCastle.CastleService.Services
{
    public class PilotNotificationService : CastleAppService, IPilotNotificationService
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<PilotNotification> _castlePilotNotificationSettingsRepository;
        private readonly IRepository<Merchant> _merchantRepository;

        public PilotNotificationService(
            IUnitOfWorkManager unitOfWorkManager,
            IRepository<PilotNotification> castlePilotNotificationSettingsRepository,
            IRepository<Merchant> merchantRepository)
        {
            this._unitOfWorkManager = unitOfWorkManager;
            this._castlePilotNotificationSettingsRepository = castlePilotNotificationSettingsRepository;
            this._merchantRepository = merchantRepository;

        }

        public async Task<PilotNotificationDto> GetPilotNotificationSettingsAsync()
        {
            var pilotNotificationQueryable = await _castlePilotNotificationSettingsRepository.GetQueryableAsync();
            var pilotNotification = await pilotNotificationQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id);
            var responseDto = ObjectMapper.Map<PilotNotification, PilotNotificationDto>(pilotNotification);
            return responseDto;
        }

        public async Task UpdatePilotNotificationSettingsAsync(UpdatePilotNotificationDto input)
        {
            var pilotNotificationQueryable = await _castlePilotNotificationSettingsRepository.GetQueryableAsync();
            var pilotNotification = await pilotNotificationQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id);
            pilotNotification.WebhookURL = input.WebhookURL;
            pilotNotification.SlackWebhookURL = input.SlackWebhookURL;
            pilotNotification.PhoneNumber = input.PhoneNumber;
            pilotNotification.Email = input.Email;
            pilotNotification.Status = input.Status;

            await _castlePilotNotificationSettingsRepository.UpdateAsync(pilotNotification);
        }
    }
}
