using System;
using System.Threading.Tasks;
using PayosferCastle.CastleService.ApmModels;
using PayosferCastle.CastleService.BankModels;
using PayosferCastle.CastleService.Business;
using PayosferCastle.CastleService.Dtos.Request;
using PayosferCastle.CastleService.Dtos.Response;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Helpers;
using PayosferCastle.CastleService.Models;
using PayosferCastle.CastleService.Repositories;

namespace PayosferCastle.CastleService.Services
{
    public class ApmPaymentService : CastleAppService, IApmPaymentService
    {
        private readonly IApmClientFactory _apmClientFactory;

        private readonly IApmRepository _apmRepository;
        private readonly ITerminalRepository _terminalRepository;
        private readonly IMerchantRepository _merchantRepository;

        public ApmPaymentService(IApmClientFactory apmClientFactory, 
            IApmRepository apmRepository, 
            ITerminalRepository terminalRepository,
             IMerchantRepository merchantRepository
             )
        {
            _apmClientFactory = apmClientFactory;
            _apmRepository = apmRepository;
            _terminalRepository = terminalRepository;
            _merchantRepository = merchantRepository;

        }


        public async Task<CompleteApmPaymentDto> CompleteApmPaymentAsync(CompleteApmPaymentInput input)
        {
            var entity = await _apmRepository.GetAsync((long)input.PaymentId);
           
            Entities.ApmPayment inputs = new ();

            entity.Action = (int)PaymentAction.POST_AUTH;
            Card card = new()
            {
                CardHolderName = entity.CardHolderName,
                CardNumber=entity.CardNumber,
                ExpireMonth= entity.ExpireMonth,
                ExpireYear= entity.ExpireYear,
                Cvc=entity.Cvc
               
            };
                inputs.Amount =entity.Amount;
                inputs.PaidAmount =entity.PaidAmount;
                inputs.Currency = entity.Currency;
                inputs.Installment =entity.Installment;

            var terminalID = entity.TerminalId;
            var responseTerminal = await _terminalRepository.GetQueryableAsync();
            //Terminal? terminal = responseTerminal.Where(x => x.Id == terminalId).FirstOrDefault();

            //entity.ApmPaymentStatus = (int)result.Status;
            var response = await _apmRepository.InsertAsync(entity);

            var responseDto = new CompleteApmPaymentDto();

            var error = new PaymentError();
            error.ErrorDescription = entity.ErrorDescription;
            error.ErrorCode = entity.ErrorCode;
            error.ErrorGroup = entity.ErrorGroup;
            responseDto.PaymentError = error;
            return responseDto;
        }

        public async Task<InitApmPaymentDto> InitApmPaymentAsync(InitApmPaymentInput input)
        {
            
            var entity = ObjectMapper.Map<InitApmPaymentInput, ApmPayment>(input);
          
            
            entity.ApmTransId = Guid.NewGuid().ToString(); // Örnek olarak TransactionId oluşturuluyor

            entity.TranDate = DateTime.Now;
            entity.PaidAmount = (input.PaidAmount == null || input.PaidAmount == 0) ? entity.Amount : input.PaidAmount;


            //Ödeme Bilgileri
            entity.ApmPaymentSource = (int)PaymentSource.API;
            entity.ApmPaymentStatus = (int)ApmStatus.INIT_APM;
            entity.ApmPaymentProvider = (int)input.ApmType;
            
            // ApmPaymentInit nesnesi üzerinde gerekli işlemleri gerçekleştirin
            #region ApmPaymentInit Özel İşlemler

            #endregion

            // Özel ApmPaymentInit nesnesini kullanarak ödeme yöntemini başlatın
            var result = await DoApmPaymentAsync(input);

            // Sonucu değerlendirerek ApmPaymentInit nesnesini güncelleyin
            if (result.ApmData.Status != (int)PaymentResponseStatus.Success)
            {
                entity.ErrorDescription = "";
                entity.ErrorCode = "";
                entity.ErrorGroup = "";
            }
            entity.ApmPaymentStatus = (int)result.ApmData.Status;

            // ApmPaymentInit nesnesini veritabanına kaydedin
            var response = await _apmRepository.InsertAsync(entity);

            // ApmPaymentInit nesnesini InitApmPaymentDto'ya dönüştürerek yanıt için
            var responseDto = new InitApmPaymentDto();

            // Yanıt DTO'suna özel özellikleri ekleyin
            //responseDto.RedirectUrl = "https://www.example.com/redirect"; // Örnek bir RedirectUrl

            var error = new PaymentError();
            error.ErrorDescription = entity.ErrorDescription;
            error.ErrorCode = entity.ErrorCode;
            error.ErrorGroup = entity.ErrorGroup;
            return responseDto;
           
        }


        private async Task<ApmPaymentResponse> DoApmPaymentAsync(InitApmPaymentInput input)
        {
            ApmPOSAuth _apm = new()
            {
                ApmType = ApmType.PAPARA,
                MerchantId = "",
                MerchantUser = null,
                MerchantPassword = null,
                MerchantStorekey = null,
                TestPlatform = true,
            };
            var responseMerchant = await _merchantRepository.GetAsync(input.MerchantId.Value); //?

            ApmCustomerInfo customerInfo = new ApmCustomerInfo()
            {
                TaxNumber = responseMerchant.TaxNumber,
                EmailAddress = responseMerchant.Email,
                Name = responseMerchant.Name,
                Surname = responseMerchant.Surname,
                PhoneNumber = responseMerchant.PhoneNumber,
                Address = responseMerchant.Address,
                State = responseMerchant.State,
                Country = responseMerchant.Country,
                PostCode = responseMerchant.PostCode,
                TaxOffice = responseMerchant.TaxOffice,
            };

            ApmPaymentRequest saleRequest = new ApmPaymentRequest()
            {
                RedirectUrl = input.RedirectUrl,
                //FailNotificationUrl=input
                
                //ReferenceId =
                //NotificationUrl=
                //OrderDescription=

                ApmPaymentInfo = new ApmPaymentInfo
                {
                    CardNumber = input.ApmCard.CardNumber,
                    CardNameSurname = input.ApmCard.CardHolderName,
                    CardExpiryDateMonth = input.ApmCard.ExpireMonth.cpToShort(),
                    CardExpiryDateYear = input.ApmCard.ExpireYear.cpToShort(),
                    Amount = input.Amount,
                    CardCVV = input.ApmCard.Cvc,
                    Currency = input.Currency,
                    Installment = input.Installment,                  
                    PaymentDescription = input.PaymentDescription,
                   
                },

            };

            var ApmClient = _apmClientFactory.Create(input.ApmType);
            ApmPaymentResponse result = null;
            int maxRetryCount = 3;
            int retryCount = 0;
            while (retryCount < maxRetryCount)
            {
                result = ApmClient.ApmPayment(saleRequest, _apm);

                //if (entity == null)
                //    entity.RetryCount++;
                //else
                //    entity.RetryCount++;

                if (result.ApmData.Status == (int)(PaymentResponseStatus.Success))
                {
                    return result;
                }
            }
            result.ApmData.Status =(int) PaymentResponseStatus.Error;
            return result;
        }
    }
}
