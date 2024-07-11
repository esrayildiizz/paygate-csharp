using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;

using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.Merchant;
using PayosferCastle.CastleService.Entities;
using Volo.Abp;
using Volo.Abp.Authorization.Permissions;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.PermissionManagement;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;
using Microsoft.Extensions.Options;
using PayosferCastle.CastleService.Dtos.EmailLog;
using PayosferCastle.CastleService.EmailProviders;
using PayosferCastle.CastleService.Models;

using Volo.Abp.Guids;
using IdentityRole = Volo.Abp.Identity.IdentityRole;
using IdentityUser = Volo.Abp.Identity.IdentityUser;
using IPermissionManager = Volo.Abp.PermissionManagement.IPermissionManager;
using Polly;



namespace PayosferCastle.CastleService.Services
{
    public class MerchantService : CastleAppService, IMerchantService
    {
        protected IGuidGenerator GuidGenerator { get; }
        protected IIdentityRoleRepository RoleRepository { get; }
        protected IIdentityUserRepository UserRepository { get; }
        protected ILookupNormalizer LookupNormalizer { get; }
        protected IdentityRoleManager RoleManager { get; }
        protected ICurrentTenant CurrentTenant { get; }
        protected IOptions<IdentityOptions> IdentityOptions { get; }
        public IPermissionManager _permissionManager { get; }
        protected IPermissionDefinitionManager PermissionDefinitionManager { get; }
        protected IPermissionDataSeeder PermissionDataSeeder { get; }
        protected IdentityUserManager UserManager { get; }
        protected IIdentityRoleRepository _roleRepository { get; }

        protected IRepository<Volo.Abp.Identity.IdentityUser> _identityUserAppService;
        
        private readonly ICurrentTenant _currentTenant;
        private readonly IDataSeeder _dataSeeder;
        private readonly ITenantRepository _tenantRepository;
        private readonly TenantManager _tenantManager;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IRepository<Feature> _featuresRepository;
        private readonly IRepository<Setting> _settingRepository;
        private readonly IRepository<PilotNotification> _pilotNotificationRepository;
        private readonly IRepository<MasterpassIntegration> _masterPassIntegrationRepository;
        private readonly IRepository<Merchant> _merchantRepository;
        private readonly IRepository<Country> _countryRepository;
        private readonly IRepository<State> _stateRepository;
        private IPayosferEmailSender _payosferMailSender;
        private IEmailProvidersService _emailProvider;

        public MerchantService(
            IGuidGenerator guidGenerator,
            IIdentityRoleRepository roleRepository,
            IIdentityUserRepository userRepository,
            ILookupNormalizer lookupNormalizer,
            IdentityUserManager userManager,
            IdentityRoleManager roleManager,
            ICurrentTenant currentTenant,
            IOptions<IdentityOptions> identityOptions,
            IPermissionManager permissionManager,
            IPermissionDataSeeder permissionDataSeeder,
            IPermissionDefinitionManager permissionDefinitionManager,
            IRepository<Volo.Abp.Identity.IdentityUser> identityUserAppService,
            IUnitOfWorkManager unitOfWorkManager,
            IRepository<Feature> castleFeaturesRepository,
            IRepository<PilotNotification> castlePilotNotificationSettingsRepository,
            IRepository<MasterpassIntegration> masterPassIntegrationSettingsRepository,
            IRepository<Merchant> merchantRepository,
            ITenantRepository tenantRepository,
            TenantManager tenantManager,
            IDataSeeder dataSeeder,
            IRepository<Setting> settingRepository,
            IRepository<Country> countryRepository,
            IRepository<State> stateRepository,
            IPayosferEmailSender payosferMailSender,
            IEmailProvidersService emailProvider)
        {
            GuidGenerator = guidGenerator;
            RoleRepository = roleRepository;
            UserRepository = userRepository;
            LookupNormalizer = lookupNormalizer;
            UserManager = userManager;
            RoleManager = roleManager;
            CurrentTenant = currentTenant;
            IdentityOptions = identityOptions;
            _permissionManager = permissionManager;
            PermissionDataSeeder = permissionDataSeeder;
            PermissionDefinitionManager = permissionDefinitionManager;
            _roleRepository = roleRepository;
            _identityUserAppService = identityUserAppService;
            UserManager = userManager;
            this._unitOfWorkManager = unitOfWorkManager;
            this._featuresRepository = castleFeaturesRepository;
            this._pilotNotificationRepository = castlePilotNotificationSettingsRepository;
            this._masterPassIntegrationRepository = masterPassIntegrationSettingsRepository;
            this._merchantRepository = merchantRepository;
            _settingRepository = settingRepository;
            _tenantRepository = tenantRepository;
            _tenantManager = tenantManager;
            _dataSeeder = dataSeeder;
            _currentTenant = currentTenant;
            _settingRepository = settingRepository;
            _countryRepository = countryRepository;
            _stateRepository = stateRepository;
            _payosferMailSender = payosferMailSender;
            _emailProvider = emailProvider;
        }

        public async Task<List<MerchantDto>> GetAllMerchantListAsync()
        {
            var merchantList = await _merchantRepository.GetListAsync();
            var responseDto = ObjectMapper.Map<List<Merchant>, List<MerchantDto>>(merchantList);
            return responseDto;
        }

        public async Task<MerchantDto> GetMerchantAsync(long id)
        {
            var merchantQueryable = await _merchantRepository.GetQueryableAsync();
            var response = await merchantQueryable.FirstOrDefaultAsync(x => x.Id == id);
            var responseDto = ObjectMapper.Map<Merchant, MerchantDto>(response);
            return responseDto;
        }

        public async Task UpdateMerchantAsync(UpdateMerchantDto input)
        {
            var merchantQueryable = await _merchantRepository.GetQueryableAsync();

            var merchant =
                await merchantQueryable.FirstOrDefaultAsync(x => x.Id == input.Id);

            merchant.Status = input.Status;
            merchant.IsBuyer = input.IsBuyer;
            merchant.IsSubMerchant = input.IsSubMerchant;
            merchant.MemberType = input.MemberType;
            merchant.MemberExternalId = input.MemberExternalId;
            merchant.Name = input.Name;
            merchant.Surname = input.Surname;
            merchant.Email = input.Email;
            merchant.Country = input.Country;
            merchant.State = input.State;

            merchant.Password = input.Password;
            merchant.Storekey = input.Storekey;
            merchant.Address = input.Address;
            merchant.PostCode = input.PostCode;
            merchant.PhoneNumber = input.PhoneNumber;
            merchant.IdentityNumber = input.IdentityNumber;
            merchant.ContactName = input.ContactName;
            merchant.ContactSurname = input.ContactSurname;
            merchant.LegalCompanyTitle = input.LegalCompanyTitle;
            merchant.TaxOffice = input.TaxOffice;
            merchant.TaxNumber = input.TaxNumber;
            merchant.Iban = input.Iban;
            merchant.WebSite = input.WebSite;

            var entity = ObjectMapper.Map<UpdateMerchantDto, Merchant>(input);

            var hostId = CurrentTenant.Id;


            using (var unitOfWork = _unitOfWorkManager.Begin())
            {

                var findTenant = await _tenantRepository.FindByNameAsync(merchant.UserName);
                _tenantManager.ChangeNameAsync(findTenant, input.UserName);
                using (_currentTenant.Change(findTenant.Id))
                {
                    var userEntity = await _identityUserAppService.GetAsync(x => x.Name == merchant.UserName);
                    await UserManager.SetUserNameAsync(userEntity, input.UserName);
                }
            }
            merchant.UserName = input.UserName;
            await _merchantRepository.UpdateAsync(merchant);
        }

        public async Task SaveMerchantAsync(SaveMerchantDto input)
        {
            var entity = ObjectMapper.Map<SaveMerchantDto, Merchant>(input);

            var hostId = CurrentTenant.Id;


            using (var unitOfWork = _unitOfWorkManager.Begin())
            {
                var tenantEntity = await _tenantManager.CreateAsync(entity.UserName);
                var tenant = await _tenantRepository.InsertAsync(tenantEntity, true);

               
                using (_currentTenant.Change(tenant.Id))
                {
                    await IdentityOptions.SetAsync();
                    var result = new IdentityDataSeedResult();
                    var adminUser = new IdentityUser(
                        GuidGenerator.Create(),
                        entity.UserName,
                        entity.Email,
                        tenant.Id
                    )
                    {
                        Name = entity.Name
                    };

                    (await UserManager.CreateAsync(adminUser, entity.Password, validatePassword: false)).CheckErrors();
                    result.CreatedAdminUser = true;
                    //"admin" role

                    var adminRole = await RoleRepository.FindByNormalizedNameAsync(LookupNormalizer.NormalizeName(entity.UserName));
                    if (adminRole == null)
                    {
                        adminRole = new IdentityRole(
                            GuidGenerator.Create(),
                            "admin",
                            tenant.Id
                        )
                        {
                            IsStatic = true,
                            IsPublic = true
                        };
                        (await RoleManager.CreateAsync(adminRole)).CheckErrors();
                        result.CreatedAdminRole = true;
                    }

                    var multiTenancySide = CurrentTenant.GetMultiTenancySide();
                    var permissionNames = (await PermissionDefinitionManager.GetPermissionsAsync())
                        .Where(p => p.MultiTenancySide.HasFlag(multiTenancySide))
                        .Where(p => !p.Providers.Any() || p.Providers.Contains(RolePermissionValueProvider.ProviderName))
                        .Select(p => p.Name)
                        .ToArray();
                  


                    await PermissionDataSeeder.SeedAsync(
                        RolePermissionValueProvider.ProviderName,
                        "admin",
                        permissionNames,
                        tenant.Id
                    );

                    (await UserManager.AddToRoleAsync(adminUser, "admin")).CheckErrors();
                    var userEntity = await UserManager.GetByIdAsync(adminUser.Id);

                    // var roleEntity = await UserManager.SetRolesAsync(userEntity, new string[] { "admin" });
                    userEntity.SetIsActive(false);
                    // (await UserManager.UpdateAsync(userEntity)).CheckErrors();
                    // var userResult  = await _identityUserAppService.UpdateAsync(user, true);

                    entity.CreatorId = hostId;
                    entity.TenantId = tenant.Id;
                    var options = new PasswordHasherOptions();
                    options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2;
                    var user2 = new Microsoft.AspNetCore.Identity.IdentityUser

                        { UserName = userEntity.UserName, Email = userEntity.Email };
                    var hasher = new PasswordHasher<Microsoft.AspNetCore.Identity.IdentityUser>();
                    var reP = hasher.HashPassword(user2, entity.Password);
                    entity.Password = reP;
                    
                    var merchant = await _merchantRepository.InsertAsync(entity, true);

                    var settingEntity = new Setting() {

                        TenantId = tenant.Id,
                        CreatorId = hostId,
                    };
                    var setting = await _settingRepository.InsertAsync(settingEntity, true);


                    var featureEntity = new Feature() {

                        TenantId = tenant.Id,
                        SettingId = setting.Id,
                        CreatorId = hostId
                    };


                    var pilotNotificationEntity = new PilotNotification() {

                        TenantId = tenant.Id,
                        SettingId = setting.Id,
                        CreatorId = hostId
                    };


                    var masterPassIntegrationEntity = new MasterpassIntegration() {

                        TenantId = tenant.Id,
                        SettingId = setting.Id,
                        CreatorId = hostId
                    };


                    var feature = await _featuresRepository.InsertAsync(featureEntity, true);
                    var pilotNotification =
                        await _pilotNotificationRepository.InsertAsync(pilotNotificationEntity, true);
                    var masterPassIntegration =
                        await _masterPassIntegrationRepository.InsertAsync(masterPassIntegrationEntity, true);

                    setting.MasterPassIntegrationId = masterPassIntegration.Id;
                    setting.PilotNotificationId = pilotNotification.Id;
                    setting.CastleFeaturesId = feature.Id;

                    await _settingRepository.UpdateAsync(setting);

                    var code = await UserManager.GenerateEmailConfirmationTokenAsync(userEntity);
                    code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                    string userId = await UserManager.GetUserIdAsync(userEntity);

                    if (!userEntity.EmailConfirmed)
                    {
                        var response = new EmailSendStatusDto();
                        var emailProviderList = await _emailProvider.GetAllEmailProviders();
                        var emailProvider = emailProviderList.FirstOrDefault(x => x.IsActive == Status.ACTIVE);

                        var link =
                            $"https://localhost:7600/Account/ConfirmEmail?UserId={userId}&Code={UrlEncoder.Default.Encode(code)}";
                        //"https://auth.payosfer.com/Account/ConfirmEmail?UserId={userId}&Code={UrlEncoder.Default.Encode(code)}";


                        await _payosferMailSender.SendAsync(emailProvider, userEntity.Email, "Activation",
                            $"Please confirm your by <a href='{link}'>clicking here</a>.", "", true);
                    }

                }
            }
        }

        public async Task DeleteMerchantAsync(long id)
        {
            var merchantQueryable = await _merchantRepository.GetQueryableAsync();
            var merchant = merchantQueryable.Any(x => x.TenantId == CurrentTenant.Id && x.Id == id);

            if (merchant)
            {
                await _merchantRepository.DeleteAsync(x => x.Id == id);
            }
            else
            {
                throw new UserFriendlyException("Merchant not found");
            }
        }

        public async Task<List<CountryDto>> GetAllCountry()
        {
            var countries = await _countryRepository.GetListAsync();
            var response = ObjectMapper.Map<List<Country>, List<CountryDto>>(countries);
            return response;
        }

        public async Task<List<StateDto>> GetStates(int countryId)
        {
            var country = await _countryRepository.GetAsync(x => x.Id == countryId);
            var states = await _stateRepository.GetListAsync(x => x.CountryId == country.Id);
            var response = ObjectMapper.Map<List<State>, List<StateDto>>(states);
            return response;
        }

        public async Task<List<StateDto>> GetAllStates()
        {
            var states = await _stateRepository.GetListAsync();
            var response = ObjectMapper.Map<List<State>, List<StateDto>>(states);
            return response;
        }


        public async Task<bool> CheckUsernameUniqueness(string username)
        {
            var merchant = await _merchantRepository.GetListAsync(x => x.UserName == username);
            return !merchant.Any();
        }

    }
}