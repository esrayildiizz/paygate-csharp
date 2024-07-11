using System;
using System.Threading.Tasks;
using PayosferCastle.CastleService.Dtos.Request;
using PayosferCastle.CastleService.Entities;
using Volo.Abp.Domain.Repositories;

namespace PayosferCastle.CastleService.Services
{
    public class MailOrderService : CastleAppService, IMailOrderService
    {
        private readonly IRepository<Feature> _featuresRepository;
        private readonly IRepository<PilotNotification> _castlePilotNotificationSettingsRepository;
        private readonly IRepository<MasterpassIntegration> _masterPassIntegrationSettingsRepository;
        private readonly IRepository<APIKeys> _apiKeysRepository;
        private readonly IRepository<Merchant> _merchantRepository;
        public MailOrderService(

            IRepository<Feature> featuresRepository,
            IRepository<PilotNotification> castlePilotNotificationSettingsRepository,
            IRepository<MasterpassIntegration> masterPassIntegrationSettingsRepository,
            IRepository<APIKeys> apiKeysRepository,
            IRepository<Merchant> merchantRepository)
        {

            this._featuresRepository = featuresRepository;
            this._castlePilotNotificationSettingsRepository = castlePilotNotificationSettingsRepository;
            this._masterPassIntegrationSettingsRepository = masterPassIntegrationSettingsRepository;
            this._apiKeysRepository = apiKeysRepository;
            this._merchantRepository = merchantRepository;
        }

        public Task<CreatePaymentInput> GetPaymentInput()
        {

            throw new NotImplementedException();
        }
    }
}
