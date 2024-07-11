using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using OpenIddict.Server;
using PayosferCastle.CastleService.Dtos.EmailLog;
using PayosferCastle.CastleService.Dtos.Registrations;
using PayosferCastle.CastleService.EmailProviders;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Models;
using PayosferCastle.CastleService.Repositories;
using Volo.Abp;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using Volo.Abp.TenantManagement;
using Volo.Abp.Uow;



namespace PayosferCastle.CastleService.Services
{
    public class RegistrationService : CastleServiceAppService, IRegistrationService
    {
        private readonly IOptionsMonitor<OpenIddictServerOptions> _oidcOptions;
        protected IRepository<Volo.Abp.Identity.IdentityUser> _identityUserAppService;
        private readonly IdentityUserManager _userManager;
        private readonly IRegistrationRepository _pendingRegistrationRepository;
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
        private readonly IRepository<Volo.Abp.Identity.IdentityUser> _userRepository;
        private readonly IDataFilter _dataFilter;
        private IPayosferEmailSender _payosferMailSender;
        private IEmailProvidersService _emailProvider;

        public RegistrationService(
            IRepository<Volo.Abp.Identity.IdentityUser> identityUserAppService,
            IdentityUserManager userManager,
           IRegistrationRepository registrationRepository,
           IUnitOfWorkManager unitOfWorkManager,
           IRepository<Feature> castleFeaturesRepository,
           IRepository<PilotNotification> castlePilotNotificationSettingsRepository,
           IRepository<MasterpassIntegration> masterPassIntegrationSettingsRepository,
           IRepository<Merchant> merchantRepository,
           ITenantRepository tenantRepository,
           TenantManager tenantManager,
           IDataSeeder dataSeeder,
           ICurrentTenant currentTenant,
           IRepository<Setting> settingRepository,
           IPayosferEmailSender payosferMailSender,
           IEmailProvidersService emailProvider,
            IRepository<Volo.Abp.Identity.IdentityUser> userRepository,

            IDataFilter dataFilter)
        {
            _identityUserAppService = identityUserAppService;
            _userManager = userManager;
            _pendingRegistrationRepository = registrationRepository;
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
            _payosferMailSender = payosferMailSender;
            _emailProvider = emailProvider;
            _userRepository = userRepository;
            _dataFilter = dataFilter;
        }

        public async Task<List<RegistrationsDto>> GetAllPendingRegistrations(RegistrationsFilterDto input)
        {
            
            var response = new List<RegistrationsDto>();

            if (input.ApprovedStatus == "All")
            {
                var tempResult = await _pendingRegistrationRepository.GetListAsync(x =>
                    x.CreationTime >= input.StartDate && x.CreationTime <= input.EndDate);
                response = ObjectMapper.Map<List<Registration>, List<RegistrationsDto>>(tempResult);
            }
            else
            {
                var tempResult = await _pendingRegistrationRepository.GetListAsync(x =>
                    x.IsApproved == input.ApprovedStatus && x.CreationTime >= input.StartDate &&
                    x.CreationTime <= input.EndDate);
                response = ObjectMapper.Map<List<Registration>, List<RegistrationsDto>>(tempResult);
            }

            return response;
        }

        public async Task<bool> Register(RegistrationsDto input)
        {
            var success = false;
            var findEntity = await _pendingRegistrationRepository.FindAsync(x => x.EmailAddress == input.EmailAddress);

            if (findEntity == null)
            {
                var entity = ObjectMapper.Map<RegistrationsDto, Registration>(input);
                await _pendingRegistrationRepository.InsertAsync(entity);
                success = true;
            }

            return success;
        }


        public async Task<bool> CheckUsernameUniqueness(string username)
        {
            var user = await _pendingRegistrationRepository.GetListAsync(x => x.Name == username);
            return !user.Any();
        }


        public async Task<bool> Approve(long id)
        {
            var success = false;
            var emailProviders = await _emailProvider.GetAllEmailProviders();
            var provider = emailProviders.FirstOrDefault(x => x.IsActive == Status.ACTIVE);
            if (provider == null)
            {
                throw new UserFriendlyException("Please add new EmailProvider or Set the IsActive of true");
            }
            
            try
            {
                var registration = await _pendingRegistrationRepository.FindAsync(x => x.Id == id);
                registration.IsApproved = "Approved";
                await _pendingRegistrationRepository.UpdateAsync(registration);

                var entity = new Merchant();
                entity.Password = registration.Password;
                entity.UserName = registration.Name;
                entity.PhoneNumber = registration.Phone;
                entity.Email = registration.EmailAddress;
                entity.Surname = registration.Surname;

                entity.Name = registration.Name;
                entity.Surname = "Surname";
                entity.IdentityNumber = "35440111436";
                entity.Iban = "TR111111111111111111111111";
                entity.Status = Status.ACTIVE;
                entity.MemberType = MemberType.PRIVATE_COMPANY;
                entity.Country = 228;

                entity.State = null;
                entity.ContactName = "Ad";
                entity.ContactSurname = "Soyad";
                entity.LegalCompanyTitle = "Company";
                entity.MemberExternalId = 1;
                entity.WebSite = "www.company.com";
                entity.Storekey = "111000";
                entity.PhoneNumber = "(111) 111-1111";
                entity.Address = "İstanbul";
                entity.PostCode = "34000";
                entity.TaxNumber = "TAX No";
                entity.TaxOffice = "Tax Office";


                var hostId = CurrentTenant.Id;


                using (var unitOfWork = _unitOfWorkManager.Begin())
                {
                    var tenantEntity = await _tenantManager.CreateAsync(entity.UserName);
                    var tenant = await _tenantRepository.InsertAsync(tenantEntity, true);

                    using (_currentTenant.Change(tenant.Id))
                    {
                        await _dataSeeder.SeedAsync(
                            new DataSeedContext(tenant.Id)
                                .WithProperty(IdentityDataSeedContributor.AdminEmailPropertyName, entity.Email)
                                .WithProperty(IdentityDataSeedContributor.AdminPasswordPropertyName, entity.Password)
                        );

                        entity.CreatorId = hostId;
                        entity.TenantId = tenant.Id;

                        var user = await _userManager.FindByEmailAsync(entity.Email);
                        user.SetIsActive(false);
                        user.SetEmailConfirmed(false);
                        var userResult = await _identityUserAppService.UpdateAsync(user, true);

                        var options = new PasswordHasherOptions();
                        options.CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV2;
                        var user2 = new Microsoft.AspNetCore.Identity.IdentityUser { UserName = user.UserName, Email = user.Email };
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
                    }

                    await unitOfWork.SaveChangesAsync();
                }




                using (_dataFilter.Disable<IMultiTenant>())
                {
                    var user = await _userManager.FindByEmailAsync(registration.EmailAddress);
                    if (user != null)
                    {
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                        
                        code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                        string userId = await _userManager.GetUserIdAsync(user);


                        if (!user.EmailConfirmed)
                        {
                            var response = new EmailSendStatusDto();
                            var emailProviderList = await _emailProvider.GetAllEmailProviders();
                            var emailProvider = emailProviderList.FirstOrDefault(x => x.IsActive == Status.ACTIVE);

                            var link =
                                $"https://localhost:7600/Account/ConfirmEmail?UserId={userId}&Code={UrlEncoder.Default.Encode(code)}";
                                //"https://auth.payosfer.com/Account/ConfirmEmail?UserId={userId}&Code={UrlEncoder.Default.Encode(code)}";



                            await _payosferMailSender.SendAsync(emailProvider, registration.EmailAddress, "Activation",
                                $"Please confirm your by <a href='{link}'>clicking here</a>.", "", true);
                        }
                    }
                }

                success = true;
            }
            catch (Exception e)
            {

                success = false;
            }


            return success;
        }


        public async Task<bool> Cancel(long id)
        {
            var success = false;
            
            var registration = await _pendingRegistrationRepository.FindAsync(x => x.Id == id);
            if (registration != null)
            {
                registration.IsApproved = "Rejected";
                await _pendingRegistrationRepository.UpdateAsync(registration);
                success = true;
            }
            
            return success;
        }
    }
}