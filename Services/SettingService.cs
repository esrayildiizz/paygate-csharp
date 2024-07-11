using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.CastleFeatures;
using PayosferCastle.CastleService.Dtos.MasterpassIntegration;
using PayosferCastle.CastleService.Dtos.Merchant;
using PayosferCastle.CastleService.Dtos.PilotNotification;
using PayosferCastle.CastleService.Dtos.Response;
using PayosferCastle.CastleService.Dtos.Setting;
using PayosferCastle.CastleService.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace PayosferCastle.CastleService.Services
{
    public class SettingService : CastleAppService, ISettingService
    {

        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<Feature> _featuresRepository;
        private readonly IRepository<PilotNotification> _pilotNotificationRepository;
        private readonly IRepository<MasterpassIntegration> _masterPassIntegrationRepository;
        private readonly IRepository<Setting> _settingsRepository;
        private readonly IRepository<Merchant> _merchantRepository;

        public SettingService(
            IUnitOfWorkManager unitOfWorkManager,
            IRepository<Feature> castleFeaturesRepository,
            IRepository<PilotNotification> pilotNotificationRepository,
            IRepository<MasterpassIntegration> masterPassIntegrationRepository,
            IRepository<Setting> settingsRepository,
            IRepository<Merchant> merchantRepository)
        {
            this._unitOfWorkManager = unitOfWorkManager;
            this._featuresRepository = castleFeaturesRepository;
            this._pilotNotificationRepository = pilotNotificationRepository;
            this._masterPassIntegrationRepository = masterPassIntegrationRepository;
            this._settingsRepository = settingsRepository;
            this._merchantRepository = merchantRepository;

        }

        public async Task<SettingsDto> GetSettingsAsync()
        {
            var responseDto = new SettingsDto();
            var merchantQueryable = await _merchantRepository.GetQueryableAsync();
            var merchantEntity = await merchantQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id);

            responseDto.Information = ObjectMapper.Map<Merchant, MerchantDto>(merchantEntity);

            var setting = await _settingsRepository.GetAsync(x => x.TenantId == CurrentTenant.Id);
            responseDto.Id = setting.Id;
            responseDto.DailyReport = setting.DailyReport;
            responseDto.DeficitAccountLimit = setting.DeficitAccountLimit;
            responseDto.CheckoutNon3DSLimit = setting.CheckoutNon3DSLimit;
            responseDto.MerchantLogoURL = setting.MerchantLogoURL;
            responseDto.MerchantWebhookURL = setting.MerchantWebhookURL;
            responseDto.Merchant3DSCallbackKey = setting.Merchant3DSCallbackKey;
            responseDto.DailyReport = setting.DailyReport;
            responseDto.ThreeDSType = setting.ThreeDSType;
            responseDto.PayAlgorithm = setting.PayAlgorithm;
            responseDto.DisregardPOSAlias = setting.DisregardPOSAlias;

            var featureEntitiesQueryable = await _featuresRepository.GetQueryableAsync();
            var featureEntity = await featureEntitiesQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id);
            responseDto.Features = ObjectMapper.Map<Feature, FeatureDto>(featureEntity);

            var notificationQueryable = await _pilotNotificationRepository.GetQueryableAsync();
            var notification = await notificationQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id);
            responseDto.Notification = ObjectMapper.Map<PilotNotification, PilotNotificationDto>(notification);

            var masterPassesQueryable = await _masterPassIntegrationRepository.GetQueryableAsync();
            var masterPass = await masterPassesQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id);
            responseDto.MasterPassIntegration = ObjectMapper.Map<MasterpassIntegration, MasterPassIntegrationDto>(masterPass);

            return responseDto;
        }


        public async Task UpdateSettingsAsync(UpdateSettingsDto input)
        {
            var settingsQueryable = await _settingsRepository.GetQueryableAsync();
            var settings = await settingsQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id);

            settings.CheckoutNon3DSLimit = input.CheckoutNon3DSLimit;
            settings.DailyReport = input.DailyReport;
            settings.DisregardPOSAlias = input.DisregardPOSAlias;
            settings.DeficitAccountLimit = input.DeficitAccountLimit;
            settings.Merchant3DSCallbackKey = input.Merchant3DSCallbackKey;
            settings.MerchantLogoURL = input.MerchantLogoURL;
            settings.MerchantWebhookURL = input.MerchantWebhookURL;
            settings.PayAlgorithm = input.PayAlgorithm;
            settings.ThreeDSType = input.ThreeDSType;
            await _settingsRepository.UpdateAsync(settings);

        }
    }
}
