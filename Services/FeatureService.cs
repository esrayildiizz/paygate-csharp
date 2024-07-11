using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.CastleFeatures;
using PayosferCastle.CastleService.Entities;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Uow;

namespace PayosferCastle.CastleService.Services
{
    public class FeaturesService : CastleAppService, IFeatureService
    {
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<Feature> _featuresRepository;
        private readonly IRepository<Merchant> _merchantRepository;

        public FeaturesService(
            IUnitOfWorkManager unitOfWorkManager,
            IRepository<Feature> featureRepository,
            IRepository<Merchant> merchantRepository)
        {
            this._unitOfWorkManager = unitOfWorkManager;
            this._featuresRepository = featureRepository;
            this._merchantRepository = merchantRepository;

        }

        public async Task<FeatureDto> GetFeaturesAsync()
        {
            var castleFeatureQueryable = await _featuresRepository.GetQueryableAsync();
            var response = await castleFeatureQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id);
            var responseDto = ObjectMapper.Map<Feature, FeatureDto>(response);
            return responseDto;
        }

        public async Task UpdateFeaturesAsync(UpdateFeatureDto input)
        {
            var castleFeatureQueryable = await _featuresRepository.GetQueryableAsync();
            var castleFeature = await castleFeatureQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id);
            castleFeature.AutoCastle = input.AutoCastle;
            castleFeature.Fraud = input.Fraud;
            castleFeature.Marketplace = input.Marketplace;
            castleFeature.Masterpass = input.Masterpass;
            castleFeature.CardStorage = input.CardStorage;
            castleFeature.ProactiveMonitoring = input.ProactiveMonitoring;
            castleFeature.TryPay = input.TryPay;
            castleFeature.APM = input.APM;
            castleFeature.ClosedLoopWallet = input.ClosedLoopWallet;
            castleFeature.VPOS = input.VPOS;
            castleFeature.SmartDynamicPay = input.SmartDynamicPay;
            castleFeature.MultiCurrencyPay = input.MultiCurrencyPay;
            castleFeature.InstitutionReadyIntegration = input.InstitutionReadyIntegration;
            castleFeature.LinkQR = input.LinkQR;

            await _featuresRepository.UpdateAsync(castleFeature);
        }
    }
}
