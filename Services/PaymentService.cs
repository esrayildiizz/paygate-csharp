using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PayosferCastle.CastleService.AcquirerSelect;
using PayosferCastle.CastleService.BankModels;
using PayosferCastle.CastleService.Business;
using PayosferCastle.CastleService.Dtos.Cards;
using PayosferCastle.CastleService.Dtos.Payment;
using PayosferCastle.CastleService.Dtos.Request;
using PayosferCastle.CastleService.Dtos.Response;
using PayosferCastle.CastleService.Dtos.TerminalCommissions;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Helpers;
using PayosferCastle.CastleService.Models;
using PayosferCastle.CastleService.Repositories;
using Volo.Abp.Data;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.MultiTenancy;
using Volo.Abp.ObjectMapping;
using Currency = PayosferCastle.CastleService.Models.Currency;
using HttpClient = System.Net.Http.HttpClient;
using Payment = PayosferCastle.CastleService.Entities.Payment;
using Refund = PayosferCastle.CastleService.Entities.Refund;

namespace PayosferCastle.CastleService.Services
{
    public class PaymentService : CastleAppService, IPaymentService
    {
        private readonly IDataFilter _dataFilter;
        private readonly ILogger<PaymentService> _logger;
        private readonly IPaymentRepository paymentRepository;
        private readonly IThreeDInitRepository threeDInitRepository;
        private readonly IRefundRepository refundRepository;
        private readonly ICommissionManagementRepository commissionManagementRepository;
        private readonly IBINRepository bINRepository;
        private readonly ISettingRepository settingsRepository;
        private readonly IBankClientFactory bankClientFactory;
        private readonly IBankPOSAuthFactory bankPOSAuthFactory;
        private readonly ITerminalRepository terminalRepository;
        private readonly IMerchantRepository merchantRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public PaymentService(IPaymentRepository paymentRepository,
            IThreeDInitRepository threeDInitRepository,
            IRefundRepository refundRepository,
            ICommissionManagementRepository commissionManagementRepository,
            IBINRepository bINRepository,
            ISettingRepository _settingsRepository,
            IBankClientFactory bankClientFactory,
            IBankPOSAuthFactory bankPOSAuthFactory,
            ITerminalRepository terminalRepository,
            IMerchantRepository merchantRepository,
            IHttpContextAccessor _httpContextAccessor,
            ILogger<PaymentService> logger,
            IDataFilter dataFilter)
        {
            this.paymentRepository = paymentRepository;
            this.threeDInitRepository = threeDInitRepository;
            this.refundRepository = refundRepository;
            this.commissionManagementRepository = commissionManagementRepository;
            this.bINRepository = bINRepository;
            this.settingsRepository = _settingsRepository;
            this.bankClientFactory = bankClientFactory;
            this.bankPOSAuthFactory = bankPOSAuthFactory;
            this.terminalRepository = terminalRepository;
            this.merchantRepository = merchantRepository;
            this._httpContextAccessor = _httpContextAccessor;
            _logger = logger;
            _dataFilter = dataFilter;
        }

        #region ****Ödeme Alma****

        public async Task<PaymentDto> CreatePaymentAsync(CreatePaymentInput input)
        {
            //TODO:KONTOL CHECK KOYULACAK.

            var entity = ObjectMapper.Map<CreatePaymentInput, Payment>(input);


            var terminal = await terminalRepository.FirstOrDefaultAsync(x =>
                x.TenantId == CurrentTenant.Id && x.BankTerminalId == input.MerchantId);
            var merchant = await merchantRepository.FirstOrDefaultAsync(x => x.TenantId == terminal.TenantId);
            entity.MerchantId = merchant.Id;
            input.MerchantId = merchant.Id;

            //Şuan burası kapalı sonraki iş

            #region Card Save

            //if (input.Card.SaveCard != null && input.Card.SaveCard == true)
            //{
            //    //Kart kaydetme işlemleri yapılacak.
            //}

            #endregion

            entity.CardHolderName = input.Card.CardHolderName;
            entity.CardNumber = input.Card.CardNumber;
            entity.ExpireYear = input.Card.ExpireYear;
            entity.ExpireMonth = input.Card.ExpireMonth;
            entity.Cvc = input.Card.Cvc;
            entity.TranDate = DateTime.Now;
            entity.PaidAmount = (input.PaidAmount == null || input.PaidAmount == 0) ? entity.Amount : input.PaidAmount;
            entity.WalletAmount = (input.WalletAmount == null || input.WalletAmount == 0) ? 0 : input.WalletAmount;
            entity.RRN = Random.Shared.Next().ToString();
            input.BankOrderId = Random.Shared.Next().ToString();

            //şuan wallet_patment ları geç.
            if (entity.WalletAmount != 0)
            {
                if (entity.PaidAmount != 0)
                {
                    entity.PaymentType = (int)PaymentType.CARD_AND_WALLET_PAYMENT;
                }

                entity.PaymentType = (int)PaymentType.WALLET_PAYMENT;
            }
            //bizi şuan card_payment ilgilendiriyor.
            else
            {
                entity.PaymentType = (int)PaymentType.CARD_PAYMENT;
            }

            entity.PaymentSource = (int)PaymentSource.API;
            entity.PaymentStatus = (int)PaymentStatus.WAITING;
            entity.Installment = (entity.Installment == null || entity.Installment == 0) ? 1 : entity.Installment;
            entity.RefundStatus = (int)RefundStatus.NONE;
            entity.RefundableAmount = input.PaidAmount;
            entity.PaymentProvider = (int)PaymentProvider.BANK;
            entity.Action = (int)PaymentAction.AUTH;

            #region BIN bilgileri ile kart detayları çekilir

            var bINNumber = entity.CardNumber.Substring(0, 8);
            var responseBIN = await bINRepository.GetQueryableAsync();
            BIN? bIN = responseBIN.Where(x => x.BinNumber == bINNumber).FirstOrDefault();
            entity.CardType = (int)bIN.CardType;
            entity.CardAssociation = (int)bIN.CardAssociation;
            entity.CardBrand = bIN.CardBrand.ToString();
            entity.BankCardHolderName = "";
            entity.CardIssuerBankName = "";

            #endregion

            var commissionDto = ObjectMapper.Map<Commission, CommissionDto>(await GetCommissionManagement(input, bIN));

            entity.TerminalId = commissionDto.TerminalId;

            #region Comission bilgileri çekilir

            //entity.MerchantCommissionRate = commission.NotOnUsCreditCardRate;//Üye işyerinin komisyonu
            //entity.MerchantCommissionRateAmount = entity.PaidAmount - entity.Amount;
            //entity.BankCommissionRate = commission.NotOnUsCreditCardRate;
            //entity.BankCommissionRateAmount = commission.NotOnUsCreditCardRate * entity.Amount;
            //TODO: Commisyon bilgilerine göre düzenlenicek

            #endregion

            #region Bank Client

            var result = await DoPaymentAsync(input, (int)commissionDto.BankCode, false, entity, null);
            if (result.Status != PaymentResponseStatus.Success)
            {
                entity.ErrorDescription = "Hata";
                entity.ErrorCode = "Hata";
                entity.ErrorGroup = "Hata";
            }

            entity.PaymentStatus = (int)result.Status;
       //     entity.RRN = result.TransactionId;
            entity.RRN = result.OrderNumber;
            entity.TransId = result.TransactionId;
            //entity.TransId = result.PrivateResponse["Stan"].ToString();  //kuveytturk test için yapıldı.
            entity.OrderId = result.OrderNumber;
            //entity.ExternalId = result.PrivateResponse["ProvisionNumber"].ToString(); //kuveytturk test için yapıldı.

            #endregion

            var response = await paymentRepository.InsertAsync(entity);

            var responseDto = ObjectMapper.Map<Payment, PaymentDto>(response);

            var error = new PaymentError();
            error.ErrorDescription = entity.ErrorDescription;
            error.ErrorCode = entity.ErrorCode;
            error.ErrorGroup = entity.ErrorGroup;
            responseDto.PaymentError = error;
            return responseDto;
        }

        //Ödeme Sorgulama
        public async Task<PaymentSummaryDto> RetrievePaymentAsync(long id)
        {
            var response = await paymentRepository.GetAsync(id);

            var responseDto = ObjectMapper.Map<Payment, PaymentSummaryDto>(response);

            return responseDto;
        }

        //Gönderilen iki tarih arasındaki ödemeleri sorgulama
        public async Task<List<PaymentSummaryDto>> RetrievePaymentsAsync(DateTime startDate, DateTime endDate)
        {
            var response = await paymentRepository.GetPaymentsAsync(startDate, endDate);

            var responseDto = ObjectMapper.Map<List<Payment>, List<PaymentSummaryDto>>(response);

            return responseDto;
        }


        #endregion

        #region ****İade****

        public async Task<PaymentRefundDto> RefundPaymentAsync(RefundPaymentInput input)
        {
            Refund entity = new Refund();

            //TODO:İade tutarı 0 ve null olmamalı 
            if (input.RefundAmount == null || input.RefundAmount == 0)
            {
                entity.Status = (int)RefundStatus.NONE;
            }

            entity.PaymentId = input.PaymentId.Value;
            entity.TranDate = DateTime.Now;
            entity.RRN = input.RRN;
            entity.TransId = input.RRN;
            entity.RefundDestinationType = (int)input.RefundDestinationType;
            entity.RefundType = (int)RefundType.REFUND;
            var responseTransaction = await paymentRepository.GetAsync(input.PaymentId.Value);
            entity.RRN = responseTransaction.OrderId;


            #region Set Amount

            if (input.RefundAmount == 0 || input.RefundAmount == responseTransaction.Amount)
            {
                entity.RefundActionType = (int)RefundActionType.FULL;
                //İade yapılmış mı kontrol edilecek.
                entity.RefundAmount = responseTransaction.Amount;
                entity.RefundBankAmount = 0;
                entity.RefundWalletAmount = 0;
            }
            else
            {
                if (input.RefundAmount > 0)
                {
                    //kısmı iade sıfırdan büyük olması gerek.
                }

                entity.RefundActionType = (int)RefundActionType.PARTIAL;

                if (responseTransaction.RefundableAmount < input.RefundAmount)
                {
                    //kısmı iade geldiğinde fazla mı iade ediliyor bakılacak.
                }

                entity.RefundAmount = entity.RefundAmount;
                entity.RefundBankAmount = 0;
                entity.RefundWalletAmount = 0;
            }

            entity.PaymentType = (int)PaymentType.CARD_PAYMENT;

            #endregion

            #region Bank Client

            var responseTerminal = await terminalRepository.GetAsync(responseTransaction.TerminalId.Value);
            var result = await DoRefund(input, entity, responseTerminal.BankCode);
            switch (result.Status)
            {
                case PaymentRefundStatus.NO_REFUND:
                    entity.ErrorDescription = result.Message;
                    entity.ErrorCode = result.Message;
                    entity.ErrorGroup = ""; //Dİctionaryden alınacak // Clientlardaki joinlenebilir.
                    entity.Status = (int)RefundStatus.FAILURE;
                    break;
                case PaymentRefundStatus.NOT_REFUNDED:
                    entity.ErrorDescription = result.Message;
                    entity.ErrorCode = result.Message;
                    entity.ErrorGroup = "";
                    entity.Status = (int)RefundStatus.FAILURE;
                    break;
                case PaymentRefundStatus.PARTIAL_REFUNDED:
                    entity.Status = (int)RefundStatus.SUCCESS; //Banka dönüş değerine göre güncellenecek.
                    break;
                case PaymentRefundStatus.FULLY_REFUNDED:
                    entity.Status = (int)RefundStatus.SUCCESS; //Banka dönüş değerine göre güncellenecek.
                    break;
            }
            //entity.AuthCode = result.PaymentId.ToString();
            //entity.HostReference = result.PaymentId.ToString();
            //entity.TransId = result.PaymentId.ToString();

            #endregion

            #region Insert and Update Table

            var response = await refundRepository.InsertAsync(entity);

            //payment tablosuda update edilecek.
            responseTransaction.RefundableAmount = responseTransaction.RefundableAmount - entity.RefundAmount;
            responseTransaction.RefundStatus = (int)RefundStatus.SUCCESS; //Banka dönüş değerine göre güncellenecek.
            var responseUpdateTran = await paymentRepository.UpdateAsync(responseTransaction);

            #endregion

            #region Set Dto

            var responseDto = ObjectMapper.Map<Refund, PaymentRefundDto>(response);

            responseDto.RefundType = (int)RefundType.REFUND; //Bankadan dönüşe göre bakılacak.
            responseDto.RefundTypeText = Enum.GetName(typeof(RefundType), responseDto.RefundType);
            responseDto.Currency = responseTransaction.Currency;
            responseDto.CurrencyText = Enum.GetName(typeof(Currency), responseDto.Currency);
            //responseDto.StatusText = Enum.GetName(typeof(RefundStatus), responseDto.Status);

            #endregion

            return responseDto;
        }

        public async Task<PaymentRefundDto> RetrievePaymentRefundAsync(long id)
        {
            var response = await refundRepository.GetAsync(id);

            var responseDto = ObjectMapper.Map<Refund, PaymentRefundDto>(response); //TODO bakılacak buna.

            return responseDto;
        }

        public async Task<List<PaymentRefundDto>> RetrievePaymentRefundAsync(DateTime startDate, DateTime endDate)
        {
            var response = await refundRepository.GetRefundsAsync(startDate, endDate);

            var responseDto = ObjectMapper.Map<List<Refund>, List<PaymentRefundDto>>(response);

            return responseDto;
        }

        #endregion

        #region ****İptal****

        public async Task<PaymentRefundDto> CancelPaymentAsync(CancelPaymentInput input)
        {
            Refund entity = new Refund();
            entity.PaymentId = input.PaymentId.Value;
            entity.TranDate = DateTime.Now;
            entity.RRN = input.RRN;
            entity.RefundDestinationType = (int)input.RefundDestinationType;
            entity.RefundType = (int)RefundType.CANCEL;

            var responseTransaction = await paymentRepository.GetAsync(input.PaymentId.Value);
            input.RRN = responseTransaction.OrderId;
            entity.RefundActionType = (int)RefundActionType.FULL;
            //İade yapılmış mı kontrol edilecek.
            entity.RefundAmount = responseTransaction.Amount;
            entity.RefundBankAmount = 0;
            entity.RefundWalletAmount = 0;
            entity.TransId = responseTransaction.TransId;
            entity.EmailAddress = input.EmailAddress;

            entity.PaymentType = (int)PaymentType.CARD_PAYMENT;

            #region Bank Client

            var responseTerminal = await terminalRepository.GetAsync(responseTransaction.TerminalId.Value);
            var result = await DoCancel(input, entity, responseTerminal.BankCode);
            if (result.Status != PaymentCancelStatus.CANCELED)
            {
                entity.ErrorDescription = "";
                entity.ErrorCode = "";
                entity.ErrorGroup = "";
            }

            entity.Status = (int)result.Status;
            //entity.AuthCode = result.PaymentId.ToString();
            //entity.HostReference = result.PaymentId.ToString();
            //entity.TransId = result.PaymentId.ToString();


            #endregion

            #region Insert and Update Table

            var response = await refundRepository.InsertAsync(entity);

            //payment tablosuda update edilecek. 
            responseTransaction.RefundableAmount = responseTransaction.RefundableAmount - entity.RefundAmount;
            responseTransaction.RefundStatus = (int)result.Status; //Banka dönüş değerine göre güncellenecek.
            var responseUpdateTran = await paymentRepository.UpdateAsync(responseTransaction);

            #endregion

            #region Set Dto

            var responseDto = ObjectMapper.Map<Refund, PaymentRefundDto>(response);

            responseDto.Status = (int)response.Status;
            responseDto.RefundType = (int)response.RefundType;
            responseDto.RefundType = (int)RefundType.REFUND; //Bankadan dönüşe göre bakılacak.
            responseDto.RefundTypeText = Enum.GetName(typeof(RefundType), responseDto.RefundType);
            responseDto.Currency = responseTransaction.Currency;
            responseDto.CurrencyText = Enum.GetName(typeof(Currency), responseDto.Currency);
            //responseDto.StatusText = Enum.GetName(typeof(RefundStatus), responseDto.Status);

            #endregion

            return responseDto;
        }

        //public async Task<PaymentRefundDto> RetrievePaymentCancelAsync(long id)
        //{
        //    var response = await refundRepository.GetAsync(id);

        //    var responseDto = ObjectMapper.Map<Refund, PaymentRefundDto>(response); //TODO bakılacak buna.

        //    return responseDto;
        //}

        #endregion

        #region ****3DS****

        public async Task<DepositPaymentDto> Complete3DSDepositPaymentAsync(CompleteThreeDSPaymentInput input)
        {
            throw new NotImplementedException();
        }

        public async Task<PaymentDto> Complete3DSPaymentAsync(CompleteThreeDSPaymentInput input)
        {
            var entity = new Payment();
            var stringBankTerminalId = input.MerchantId.TrimStart('0');
            var bankTerminalId = long.Parse(stringBankTerminalId);
           
            using (_dataFilter.Disable<IMultiTenant>())

            {
                var terminal = await terminalRepository.GetListAsync(x => x.BankTerminalId == bankTerminalId);
                using (StreamWriter writetext = new StreamWriter("terminalList.txt"))
                {
                    writetext.WriteLine("Terminal Sayısı : " + terminal.Count());
                    writetext.WriteLine("Terminal Id:" + terminal.FirstOrDefault().TenantId.ToString());
                }

                var merchant =
                    await merchantRepository.GetListAsync(x => x.TenantId == terminal.FirstOrDefault().TenantId);
                using (StreamWriter writetext = new StreamWriter("merchantList.txt"))
                {
                    writetext.WriteLine("Merchant Sayısı : " + merchant.Count());
                    writetext.WriteLine("Merchant Id:" + merchant.FirstOrDefault().TenantId.ToString());
                }

                entity.MerchantId = merchant.FirstOrDefault().Id;
                entity.TerminalId = terminal.FirstOrDefault().Id;
                entity.CreatorId = CurrentUser.Id;
                entity.PaymentStatus = input.Status == "Y" ? 1 : 0;
            }

            entity.RRN = input.VerifyEnrollmentRequestId;
            entity.Amount = Decimal.Parse(input.PurchAmount);
            entity.Currency = Int32.Parse(input.PurchCurrency);
            entity.MerchantId = Int32.Parse(input.MerchantId);
            entity.OrderId = input.SessionInfo;
            entity.CardNumber = input.Pan;
            entity.PaidAmount = Decimal.Parse(input.PurchAmount);
            var expireYear = (int)(Int32.Parse(input.Expiry) / 100);  
            var expireMonth = (int)(Int32.Parse(input.Expiry) % 100);
            entity.ExpireYear = expireYear.ToString();
            entity.ExpireMonth = expireMonth.ToString();


            var terminalId = entity.TerminalId;
            var responseTerminal = await terminalRepository.GetQueryableAsync();
            Terminal? terminals= responseTerminal.Where(x => x.Id == terminalId).FirstOrDefault();

            var result = await DoPayment3DResponse(input, entity, (int)terminals.BankCode);

            if (result.Status != PaymentResponseStatus.Success)
            {
                entity.ErrorDescription = "";
                entity.ErrorCode = "";
                entity.ErrorGroup = "";
            }

            entity.PaymentStatus = (int)result.Status;
            entity.TransId = result.TransactionId;
            

            var responseDto = new PaymentDto();
            var response = await paymentRepository.InsertAsync(entity);
            responseDto = ObjectMapper.Map<Payment, PaymentDto>(response);

            // var error = new PaymentError();
            // error.ErrorDescription = entity.ErrorDescription;
            // error.ErrorCode = entity.ErrorCode;
            // error.ErrorGroup = entity.ErrorGroup;
            // responseDto.PaymentError = error;
            return responseDto;
        }

        public async Task<InitThreeDSPaymentDto> Init3DSDepositPaymentAsync(CreateDepositPaymentInput input)
        {
            throw new NotImplementedException();
        }

        public async Task<InitThreeDSPaymentDto> Init3DSPaymentAsync(InitThreeDSPaymentInput input,
            bool optional = false)
        {
            var responseDto = new InitThreeDSPaymentDto();

            var entity = ObjectMapper.Map<CreatePaymentInput, ThreeDInit>(input);

            var terminal = await terminalRepository.FirstOrDefaultAsync(x =>
                x.TenantId == CurrentTenant.Id && x.BankTerminalId == input.MerchantId);
            var merchant = await merchantRepository.FirstOrDefaultAsync(x => x.TenantId == terminal.TenantId);
            entity.MerchantId = merchant.Id;
            input.MerchantId = merchant.Id;
            entity.TransId = input.RRN;

            #region Card Save

            //if (input.Card.SaveCard != null && input.Card.SaveCard == true)
            //{
            //    //Kart kaydetme işlemleri yapılacak.
            //}

            #endregion

            entity.CardHolderName = input.Card.CardHolderName;
            entity.CardNumber = input.Card.CardNumber;
            entity.TranDate = DateTime.Now;
            entity.PaidAmount = (input.PaidAmount == null || input.PaidAmount == 0) ? entity.Amount : input.PaidAmount;
            entity.WalletAmount = (input.WalletAmount == null || input.WalletAmount == 0) ? 0 : input.WalletAmount;
            entity.RRN = Random.Shared.Next().ToString();

            entity.WebURL = "";
            input.SuccessUrl = "http://localhost:4200/okurl";
            input.ErrorUrl = "http://localhost:4200/failurl";

            if (entity.WalletAmount != 0)
            {
                if (entity.PaidAmount != 0)
                {
                    entity.PaymentType = (int)PaymentType.CARD_AND_WALLET_PAYMENT;
                }

                entity.PaymentType = (int)PaymentType.WALLET_PAYMENT;
            }
            else
            {
                entity.PaymentType = (int)PaymentType.CARD_PAYMENT;
            }

            entity.PaymentSource = (int)PaymentSource.API;
            entity.PaymentStatus = (int)PaymentStatus.INIT_THREEDS;
            entity.Installment = (entity.Installment == null || entity.Installment == 0) ? 1 : entity.Installment;
            entity.RefundStatus = (int)RefundStatus.NONE;
            entity.RefundableAmount = input.PaidAmount;
            entity.PaymentProvider = (int)PaymentProvider.BANK;


            #region BIN bilgileri ile kart detayları çekilir

            var bINNumber = entity.CardNumber.Substring(0, 8);
            var responseBIN = await bINRepository.GetQueryableAsync();
            BIN? bIN = responseBIN.Where(x => x.BinNumber == bINNumber).FirstOrDefault();
            entity.CardType = (int)bIN.CardType;
            entity.CardAssociation = (int)bIN.CardAssociation;
            entity.CardBrand = bIN.CardBrand.ToString();
            entity.BankCardHolderName = "";
            entity.CardIssuerBankName = "";

            #endregion

            var commissionDto = ObjectMapper.Map<Commission, CommissionDto>(await GetCommissionManagement(input, bIN));

            entity.TerminalId = commissionDto.TerminalId;

            #region Comission bilgileri çekilir

            //entity.MerchantCommissionRate = commission.NotOnUsCreditCardRate;//Üye işyerinin komisyonu
            //entity.MerchantCommissionRateAmount = entity.PaidAmount - entity.Amount;
            //entity.BankCommissionRate = commission.NotOnUsCreditCardRate;
            //entity.BankCommissionRateAmount = commission.NotOnUsCreditCardRate * entity.Amount;
            //TODO: Commisyon bilgilerine göre düzenlenicek

            #endregion

            #region Bank Client

            var result = await DoPaymentAsync(input, (int)commissionDto.BankCode, true, null, entity);

            if (result.Status == PaymentResponseStatus.Error)
            {
                entity.ErrorDescription = "Hata";
                entity.ErrorCode = "Hata";
                entity.ErrorGroup = "Hata";
            }

            entity.PaymentStatus = (int)result.Status;
            entity.TransId = result.TransactionId;

            #endregion


            var response = await threeDInitRepository.InsertAsync(entity);
            //var responseDto = new InitThreeDSPaymentDto(result.Message,result.isIframe);

            string htmlContent = (string)result.Message;
            responseDto.HtmlContent = htmlContent;

            //AHLPay ise
            if (optional == true)
            {
                if (result.Status == PaymentResponseStatus.Success)
                {
                    var newInput = new CreatePaymentInput() {
                        Amount = input.Amount,
                        MerchantId = input.MerchantId,
                        //diğer parametreler setlenmeli
                    };
                    var newPaymentEntity = ObjectMapper.Map<CreatePaymentInput, Payment>(newInput);

                    await paymentRepository.InsertAsync(newPaymentEntity);
                }
            }

            return responseDto;
        }

        [IgnoreAntiforgeryToken]
        public async Task<PaymentDto> Payment3D_Complete_IyzipayAsync(
            [FromForm] Payment3DHtmlRespondModel_Iyzipay input)
        {
            var entity = new Payment();

            if (input.MdStatus != "1") //şifre yanlış

            {
                string errorMessage = IyzicoClient.GetStatusByMdStatus(input.MdStatus);
                _httpContextAccessor.HttpContext.Response.Redirect("/payment?errorMessage=" + errorMessage);
            }

            CreateThreedsPaymentRequest request = new();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = input.ConversationId;
            request.PaymentId = input.PaymentId;
            if (!string.IsNullOrEmpty(request.ConversationData))
            {
                request.ConversationData = input.ConversationData;
            }

            Options options = new();
            options.ApiKey = "sandbox-htEEQwyT9JcaFRN3TKXxvgDOXp3gQ39m";
            options.SecretKey = "DHFrOJSZoUySJwvf2GLURVluRm1Oj0yT";
            options.BaseUrl = "https://sandbox-api.iyzipay.com";

            ThreedsPayment threedsPayment = ThreedsPayment.Create(request, options);

            var responseDto = new PaymentDto();
            if (threedsPayment.Status == "success")
            {
                entity.PaymentStatus = threedsPayment.Status == "success" ? 1 : 0;
                entity.Amount = Math.Round(decimal.Parse(threedsPayment.PaymentItems[0].Price), 2);
                entity.CardBrand = threedsPayment.CardAssociation;
                entity.Installment = threedsPayment.Installment;
                entity.PaidAmount = Math.Round(decimal.Parse(threedsPayment.PaymentItems[0].PaidPrice), 2);
                entity.TransId = threedsPayment.PaymentItems[0].PaymentTransactionId;


                var response = await paymentRepository.InsertAsync(entity);
                responseDto = ObjectMapper.Map<Payment, PaymentDto>(response);


                //başarılı

                //_httpContextAccessor.HttpContext.Response.Redirect("/payment?status=success");
            }


            return responseDto;
            //else
            //{
            //    //başarısız

            //    _httpContextAccessor.HttpContext.Response.Redirect("/payment?errorMessage=" + threedsPayment.ErrorMessage);
            //}

        }


        #endregion

        #region ***** Ön Provizyon - Ön Provizyon Kapama *****

        //Ön Provizyon
        public async Task<PaymentDto> PreAuthPaymentAsync(CreatePaymentInput input)
        {
            //TODO:KONTOL CHECK KOYULACAK.

            var entity = ObjectMapper.Map<CreatePaymentInput, Payment>(input);

            var terminal = await terminalRepository.FirstOrDefaultAsync(x => x.BankTerminalId == input.MerchantId);
            var merchant = await merchantRepository.FirstOrDefaultAsync(x => x.TenantId == terminal.TenantId);
            entity.MerchantId = merchant.Id;
            input.MerchantId = merchant.Id;
            //TODO Ket ile auth kısmında atanacak bu değer.


            #region Card Save

            //if (input.Card.SaveCard != null && input.Card.SaveCard == true)
            //{
            //    //Kart kaydetme işlemleri yapılacak.
            //}

            #endregion

            entity.CardHolderName = input.Card.CardHolderName;
            entity.CardNumber = input.Card.CardNumber;
            entity.TranDate = DateTime.Now;
            entity.PaidAmount = (input.PaidAmount == null || input.PaidAmount == 0) ? entity.Amount : input.PaidAmount;
            entity.WalletAmount = (input.WalletAmount == null || input.WalletAmount == 0) ? 0 : input.WalletAmount;
            entity.RRN = Random.Shared.Next().ToString();

            //şuan wallet_patment ları geç.
            if (entity.WalletAmount != 0)
            {
                if (entity.PaidAmount != 0)
                {
                    entity.PaymentType = (int)PaymentType.CARD_AND_WALLET_PAYMENT;
                }

                entity.PaymentType = (int)PaymentType.WALLET_PAYMENT;
            }
            //bizi şuan card_payment ilgilendiriyor.
            else
            {
                entity.PaymentType = (int)PaymentType.CARD_PAYMENT;
            }

            //ödeme ile alakalı bilgiler
            entity.PaymentSource = (int)PaymentSource.API;
            entity.PaymentStatus = (int)PaymentStatus.WAITING;
            entity.Installment = (entity.Installment == null || entity.Installment == 0) ? 1 : entity.Installment;
            entity.RefundStatus = (int)RefundStatus.NONE;
            entity.RefundableAmount = input.PaidAmount;
            entity.PaymentProvider = (int)PaymentProvider.BANK;
            entity.Action = (int)PaymentAction.PRE_AUTH;

            #region BIN bilgileri ile kart detayları çekilir

            var bINNumber = entity.CardNumber.Substring(0, 8);
            var responseBIN = await bINRepository.GetQueryableAsync();
            BIN? bIN = responseBIN.Where(x => x.BinNumber == bINNumber).FirstOrDefault();
            entity.CardType = (int)bIN.CardType;
            entity.CardAssociation = (int)bIN.CardAssociation;
            entity.CardBrand = bIN.CardBrand.ToString();
            entity.BankCardHolderName = "";
            entity.CardIssuerBankName = "";

            #endregion

            //Uygun algoritme ve Terminal metot haline getirildi ve çağırıldı.
            var commissionDto = ObjectMapper.Map<Commission, CommissionDto>(await GetCommissionManagement(input, bIN));

            entity.TerminalId = commissionDto.TerminalId;

            #region Comission bilgileri çekilir

            //entity.MerchantCommissionRate = commission.NotOnUsCreditCardRate;//Üye işyerinin komisyonu
            //entity.MerchantCommissionRateAmount = entity.PaidAmount - entity.Amount;
            //entity.BankCommissionRate = commission.NotOnUsCreditCardRate;
            //entity.BankCommissionRateAmount = commission.NotOnUsCreditCardRate * entity.Amount;
            //TODO: Commisyon bilgilerine göre düzenlenicek

            #endregion

            #region Bank Client

            var result = await DoPreAuthPaymentAsync(input, (int)commissionDto.BankCode, false, entity, null);
            if (result.Status != PaymentResponseStatus.Success)
            {
                entity.ErrorDescription = "";
                entity.ErrorCode = "";
                entity.ErrorGroup = "";
            }

            entity.PaymentStatus = (int)result.Status;
            entity.RRN = result.TransactionId;
            //entity.TransId = result.PrivateResponse["Stan"].ToString();  //kuveytturk test için yapıldı.
            entity.OrderId = result.OrderNumber;
            //entity.ExternalId = result.PrivateResponse["ProvisionNumber"].ToString();  //kuveytturk test için yapıldı.

            #endregion

            var response = await paymentRepository.InsertAsync(entity);

            var responseDto = ObjectMapper.Map<Payment, PaymentDto>(response);

            var error = new PaymentError();
            error.ErrorDescription = entity.ErrorDescription;
            error.ErrorCode = entity.ErrorCode;
            error.ErrorGroup = entity.ErrorGroup;
            responseDto.PaymentError = error;
            return responseDto;
        }

        //Ön Provizyon Kapama
        public async Task<PaymentDto> PostAuthPaymentAsync(long paymentId, PostAuthPaymentInput postAuthPaymentInput)
        {
            CreatePaymentInput input = new CreatePaymentInput();
            var entity = await paymentRepository.GetAsync(paymentId);

            if (entity.Amount >= (decimal)postAuthPaymentInput.PaidAmount)
            {
                entity.Amount = (decimal)postAuthPaymentInput.PaidAmount;
            }
            else
            {
                Console.WriteLine("Uyarı: PaidAmount öğesi, Amount öğesinden büyük veya eşit olamaz.");
            }

            entity.Action = (int)PaymentAction.POST_AUTH;

            CardDto card = new CardDto {
                CardHolderName = entity.CardHolderName,
                CardNumber = entity.CardNumber,
                ExpireMonth = entity.ExpireMonth,
                ExpireYear = entity.ExpireYear,
                Cvc = entity.Cvc
            };

            input.Card = card;
            input.Amount = entity.Amount;
            input.PaidAmount = entity.PaidAmount;
            input.Currency = (Currency?)entity.Currency;
            input.Installment = entity.Installment;
            input.MerchantId = entity.MerchantId;
            input.Action = (PaymentAction)entity.Action;
            input.RRN = entity.RRN;
            input.BankOrderId = entity.OrderId;


            var terminalId = entity.TerminalId;
            var responseTerminal = await terminalRepository.GetQueryableAsync();
            Terminal? terminal = responseTerminal.Where(x => x.Id == terminalId).FirstOrDefault();

            var result = await DoPaymentAsync(input, (int)terminal.BankCode, false, entity, null);
            if (result.Status != PaymentResponseStatus.Success)
            {
                entity.ErrorDescription = "";
                entity.ErrorCode = "";
                entity.ErrorGroup = "";
            }

            entity.PaymentStatus = (int)result.Status;
            entity.Action = (int)PaymentAction.POST_AUTH;

            var response = await paymentRepository.UpdateAsync(entity);

            var responseDto = ObjectMapper.Map<Payment, PaymentDto>(response);

            var error = new PaymentError();
            error.ErrorDescription = entity.ErrorDescription;
            error.ErrorCode = entity.ErrorCode;
            error.ErrorGroup = entity.ErrorGroup;
            responseDto.PaymentError = error;
            return responseDto;
        }

        //Ön Provizyon İptal
        public async Task<PaymentRefundDto> PreAuthCancelAsync(CancelPaymentInput input)
        {
            Refund entity = new Refund();
            entity.PaymentId = input.PaymentId.Value;
            entity.TranDate = DateTime.Now;
            entity.RRN = input.RRN;
            entity.RefundDestinationType = (int)input.RefundDestinationType;
            entity.RefundType = (int)RefundType.CANCEL;
            var responseTransaction = await paymentRepository.GetAsync(input.PaymentId.Value);
            entity.RefundActionType = (int)RefundActionType.FULL;
            //İade yapılmış mı kontrol edilecek.
            entity.RefundAmount = responseTransaction.Amount;
            entity.RefundBankAmount = 0;
            entity.RefundWalletAmount = 0;
            entity.TransId = responseTransaction.TransId;
            entity.PaymentType = (int)PaymentType.CARD_PAYMENT;

            #region Bank Client

            var responseTerminal = await terminalRepository.GetAsync(responseTransaction.TerminalId.Value);
            var result = await DoCancel(input, entity, responseTerminal.BankCode);
            if (result.Status != PaymentCancelStatus.CANCELED)
            {
                entity.ErrorDescription = "";
                entity.ErrorCode = "";
                entity.ErrorGroup = "";
            }

            entity.Status = (int)result.Status;
            //entity.AuthCode = result.PaymentId.ToString();
            //entity.HostReference = result.PaymentId.ToString();
            //entity.TransId = result.PaymentId.ToString();

            entity.Status = (int)RefundStatus.SUCCESS; //Banka dönüş değerine göre güncellenecek.

            #endregion

            #region Insert and Update Table

            var response = await refundRepository.InsertAsync(entity);

            //payment tablosuda update edilecek.
            responseTransaction.RefundableAmount = responseTransaction.RefundableAmount - entity.RefundAmount;
            responseTransaction.RefundStatus = (int)RefundStatus.SUCCESS; //Banka dönüş değerine göre güncellenecek.
            var responseUpdateTran = await paymentRepository.UpdateAsync(responseTransaction);

            #endregion

            #region Set Dto

            var responseDto = ObjectMapper.Map<Refund, PaymentRefundDto>(response);

            responseDto.RefundType = (int)RefundType.REFUND; //Bankadan dönüşe göre bakılacak.
            responseDto.RefundTypeText = Enum.GetName(typeof(RefundType), responseDto.RefundType);
            responseDto.Currency = responseTransaction.Currency;
            responseDto.CurrencyText = Enum.GetName(typeof(Currency), responseDto.Currency);
            //responseDto.StatusText = Enum.GetName(typeof(RefundStatus), responseDto.Status);

            #endregion

            return responseDto;
        }

        #endregion

        #region *****Ödül/Puan****

        //Ödül sorgulama
        public async Task<RetrieveLoyaltiesDto> RetrieveLoyaltiesAsync(RetrieveLoyaltiesInput input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region *****Cüzdan****

        //Cüzdan Kart ile Para Yatırma
        public async Task<DepositPaymentDto> CreateDepositPaymentAsync(CreateDepositPaymentInput input)
        {
            throw new NotImplementedException();
        }

        //Cüzdana EFT/Havale İle Para Yatırma
        public async Task<FundTransferDepositPaymentDto> CreateFundTransferDepositPaymentAsync(
            CreateFundTransferDepositPaymentInput input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region *****Kırılım****

        //Ödeme Kırılımı İade
        public async Task<PaymentTransactionRefundDto> RefundPaymentTransactionAsync(
            RefundPaymentTransactionInput input)
        {
            throw new NotImplementedException();
        }

        //Ödeme Kırılımı iade Sorgulama
        public async Task<PaymentTransactionRefundDto> RetrievePaymentTransactionRefundAsync(long id)
        {
            throw new NotImplementedException();
        }

        //Ödeme Kırılımı Onay Verme
        public async Task<PaymentTransactionDto> UpdatePaymentTransactionAsync(UpdatePaymentTransactionInput input)
        {
            throw new NotImplementedException();
        }

        //Ödeme Kırılımı Onay Verme
        public async Task<PaymentTransactionApprovalListDto> ApprovePaymentTransactionsAsync(
            ApprovePaymentTransactionsInput input)
        {
            throw new NotImplementedException();
        }

        //Ödeme Kırılımı Onay Geri Alma
        public async Task<PaymentTransactionApprovalListDto> DisapprovePaymentTransactionsAsync(
            DisapprovePaymentTransactionsInput input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ****GarantiPay****

        public async Task<InitGarantiPayPaymentDto> InitGarantiPayPaymentAsync(InitGarantiPayPaymentInput input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ****Checkout****

        //ödeme Başlatma
        public async Task<InitCheckoutPaymentDto> InitCheckoutPaymentAsync(InitCheckoutPaymentInput input)
        {
            throw new NotImplementedException();
        }

        //Ödeme Sorgulama
        public async Task<PaymentDto> RetrieveCheckoutPaymentAsync(string token)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ****Masterpass****

        public async Task<CheckMasterpassUserDto> CheckMasterpassUserAsync(CheckMasterpassUserDto input)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region ****APM****

        //public async Task<ApmDepositPaymentDto> InitApmDepositPaymentAsync(InitApmDepositPaymentInput input)
        //{
        //    throw new NotImplementedException();
        //}

        //public async Task<InitApmPaymentDto> InitApmPaymentAsync(InitApmPaymentInput input)
        //{
        //    throw new NotImplementedException();
        //}

        //public async Task<CompleteApmPaymentDto> CompleteApmPaymentAsync(CompleteApmPaymentInput input)
        //{
        //    throw new NotImplementedException();
        //}

        //public async Task<PaymentDto> CreateApmPaymentAsync(CreateApmPaymentInput input)
        //{
        //    throw new NotImplementedException();
        //}

        #endregion

        #region Private Methods - Yardımcı Methodlar

        #region DoPaymentAsync

        /// <summary>
        /// Karttan çekim yapmak için kullanılır. 3D çekim yapmak için "SaleRequest.payment3D.confirm = true" olarak gönderilmelidir.
        /// </summary>
        /// <param name="request">Kart çekim işlemine ait işlem bilgileri</param>
        /// <param name="auth">Banka API bilgileri</param>
        /// <returns></returns>
        private async Task<BankPaymentResponse> DoPaymentAsync(CreatePaymentInput input, int bankCode, bool is3D, Payment paymentEntity = null, ThreeDInit threedEntity = null)
        {
            var bankPos = bankPOSAuthFactory.Create(bankCode);

            var bankPOSAuth = new BankPOSAuth {
                BankCode = bankPos.GetBankCode(),
                MerchantID = bankPos.GetMerchantID(),
                MerchantUser = bankPos.GetMerchantUser(),
                MerchantPassword = bankPos.GetMerchantPassword(),
                MerchantStorekey = bankPos.GetMerchantStorekey(),
                TestPlatform = bankPos.GetTestPlatform()
            };

            var responseMerchant = await merchantRepository.GetAsync((long)input.MerchantId);

            BankCustomerInfo customerInfo = new BankCustomerInfo();

            customerInfo.EmailAddress = responseMerchant.Email;
            customerInfo.Name = responseMerchant.Name;
            customerInfo.Surname = responseMerchant.Surname;
            customerInfo.PhoneNumber = responseMerchant.PhoneNumber;
            customerInfo.Address = responseMerchant.Address;
            customerInfo.State = responseMerchant.State;
            customerInfo.PostCode = responseMerchant.PostCode;
            customerInfo.TaxOffice = responseMerchant.TaxOffice;
            customerInfo.TaxNumber = responseMerchant.TaxNumber;
            customerInfo.Country = null;


            BankPaymentRequest saleRequest = new BankPaymentRequest {
                InvoiceInfo = customerInfo,
                ShippingInfo = customerInfo,


                PaymentInfo = new BankPaymentInfo {
                    CardNameSurname = input.Card.CardHolderName,
                    CardNumber = input.Card.CardNumber,
                    CardExpiryDateMonth = input.Card.ExpireMonth.cpToShort(),
                    CardExpiryDateYear = input.Card.ExpireYear.cpToShort(),
                    Amount = input.Amount,
                    CardCVV = input.Card.Cvc,
                    Currency = BankCurrency.TRY,
                    Installment = input.Installment.Value,
                    RND = input.RRN,
                    Hash = input.Hash,
                    PaymentDescription = input.PaymentDescription,
                    WebURL = input.WebURL
                },
                Payment3D = new BankPayment3D {
                    Confirm = is3D,

                    //Açılacak olan ACS ekranının nerede gözükmesini istiyorsak o sayfanın url i verilmeli
                    //Üye İşyeri ‘nin sitesine yönlendirilecek olan sayfanın adresi.
                    ReturnURL =
                        "https://castle.payosfer.com/api/app/payment/ok-url" //domain alındıktan sonra doğru adres setlenecek.
                },

                CustomerIPAddress = "81.214.129.252", //TODO bizim ip adresi yazılacak.

                OrderNumber = input.BankOrderId
            };

            var bankClient = bankClientFactory.Create(bankCode);
            BankPaymentResponse result = null;
            int maxRetryCount = 3;
            int retryCount = 0;
            while (retryCount < maxRetryCount)
            {
                if (input.Action == PaymentAction.POST_AUTH)
                {
                    result = bankClient.PostAuthPayment(saleRequest, bankPOSAuth);
                }
                else
                {
                    result = bankClient.Payment(saleRequest, bankPOSAuth);
                }


                if (paymentEntity == null)
                    threedEntity.RetryCount++;
                else
                    paymentEntity.RetryCount++;
                retryCount++;

                if (result.Status != PaymentResponseStatus.Error)
                {
                    return result;
                }
            }

            result.Status = PaymentResponseStatus.Error;
            return result;
        }

        #endregion

        #region DoPayment3DResponse

        /// <summary>
        /// 3D yapılan çekim işlemi sonucunu döner
        /// </summary>
        /// <param name="request"></param>
        private async Task<BankPaymentResponse> DoPayment3DResponse(CompleteThreeDSPaymentInput input, Payment entity,
            int bankCode)
        {
            var bankPos = bankPOSAuthFactory.Create(bankCode);

            var bankPOSAuth = new BankPOSAuth {
                BankCode = bankPos.GetBankCode(),
                MerchantID = bankPos.GetMerchantID(),
                MerchantUser = bankPos.GetMerchantUser(),
                MerchantPassword = bankPos.GetMerchantPassword(),
                MerchantStorekey = bankPos.GetMerchantStorekey(),
                TestPlatform = bankPos.GetTestPlatform()
            };


            BankPayment3DResponseRequest saleRequest = new BankPayment3DResponseRequest {
                ResponseArray = new Dictionary<string, object>
                {
                    { "Pan", input.Pan }, 
                    { "Expiry", input.Expiry }, 
                    { "PurchCurrency", input.PurchCurrency }, 
                    { "Eci", input.Eci }, 
                    { "Cavv", input.Cavv }, 
                    { "VerifyEnrollmentRequestId", input.VerifyEnrollmentRequestId } ,
                    { "SessionInfo", input.SessionInfo },
                    { "PurchAmount", input.PurchAmount },
                    { "Amount",input.PurchAmount },
                    { "Status", input.Status },
                    { "Xid" ,input.Xid},
                    { "BankPacket" ,input.BankPacket},
                }
            };

            var bankClient = bankClientFactory.Create(bankCode);
            BankPaymentResponse result = null;
            int maxRetryCount = 3;
            int retryCount = 0;
            while (retryCount < maxRetryCount)
            {
                result = bankClient.Payment3DResponse(saleRequest, bankPOSAuth);
                entity.RetryCount++;
                retryCount++;
                if (result.Status == PaymentResponseStatus.Success)
                {
                    return result;
                }
            }

            result.Status = PaymentResponseStatus.Error;
            return result;
        }

        #endregion

        #region DoCancel

        /// <summary>
        ///  Ödeme iptal etme. Aynı gün yapılan ödemeler için kullanılabilir. Çekilen tutarın tamamı iptal edilir ve müşteri ekstresine hiçbir işlem yansımaz.
        /// </summary>
        /// <param name="request"></param>
        /// <param name="auth">Banka API bilgileri</param>
        /// <returns></returns>
        private async Task<BankCancelResponse> DoCancel(CancelPaymentInput input, Refund entity, int bankCode)
        {
            var bankPos = bankPOSAuthFactory.Create(bankCode);

            var bankPOSAuth = new BankPOSAuth {
                BankCode = bankPos.GetBankCode(),
                MerchantID = bankPos.GetMerchantID(),
                MerchantUser = bankPos.GetMerchantUser(),
                MerchantPassword = bankPos.GetMerchantPassword(),
                MerchantStorekey = bankPos.GetMerchantStorekey(),
                TestPlatform = bankPos.GetTestPlatform()
            };

            BankCancelRequest cancelRequest = new BankCancelRequest {
                Currency = BankCurrency.TRY,
                CustomerIPAddress = "81.214.129.252",
                OrderNumber = input.RRN,
                TransactionId = entity.TransId,
                RefundAmount = entity.RefundAmount,
                EmailAddress = entity.EmailAddress
            };

            var bankClient = bankClientFactory.Create(bankCode);

            BankCancelResponse result = null;
            int maxRetryCount = 3;
            int retryCount = 0;
            while (retryCount < maxRetryCount)
            {
                result = bankClient.Cancel(cancelRequest, bankPOSAuth);
                entity.RetryCount++;
                retryCount++;
                if (result.Status == PaymentCancelStatus.CANCELED)
                {
                    return result;
                }
            }

            result.Status = PaymentCancelStatus.NOT_CANCELED;
            return result;
        }

        #endregion

        #region DoRefund

        /// <summary>
        /// Ödeme iade etme. Belirtilen tutar kadar kısmi iade işlemi yapılır
        /// </summary>
        /// <param name="request"></param>
        /// <param name="auth"></param>
        /// <returns></returns>
        private async Task<BankRefundResponse> DoRefund(RefundPaymentInput input, Refund entity, int bankCode)
        {
            var bankPos = bankPOSAuthFactory.Create(bankCode);

            var bankPOSAuth = new BankPOSAuth {
                BankCode = bankPos.GetBankCode(),
                MerchantID = bankPos.GetMerchantID(),
                MerchantUser = bankPos.GetMerchantUser(),
                MerchantPassword = bankPos.GetMerchantPassword(),
                MerchantStorekey = bankPos.GetMerchantStorekey(),
                TestPlatform = bankPos.GetTestPlatform()
            };

            BankRefundRequest refundRequest = new BankRefundRequest {
                CustomerIPAddress = "81.214.129.252",
                OrderNumber = entity.RRN,
                TransactionId = entity.TransId,
                RefundAmount = (decimal)input.RefundAmount,
                Currency = BankCurrency.TRY,
                EmailAddress = entity.EmailAddress
            };

            var bankClient = bankClientFactory.Create(bankCode);
            BankRefundResponse result = null;
            int maxRetryCount = 3;
            int retryCount = 0;
            while (retryCount < maxRetryCount)
            {
                result = bankClient.Refund(refundRequest, bankPOSAuth);
                entity.RetryCount++;
                retryCount++;
                if (result.Status == PaymentRefundStatus.PARTIAL_REFUNDED ||
                    result.Status == PaymentRefundStatus.FULLY_REFUNDED)
                {
                    return result;
                }
            }

            result.Status = PaymentRefundStatus.NOT_REFUNDED;
            return result;
        }

        #endregion

        #region DoPreAuthPaymentAsync

        /// <summary>
        /// Kart limitinin belirli bir süre blokede tutulmasına imkan veren ödeme için kullanılır. 3D çekim yapmak için "SaleRequest.payment3D.confirm = true" olarak gönderilmelidir.
        /// </summary>
        /// <param name="request">Kart çekim işlemine ait işlem bilgileri</param>
        /// <param name="auth">Banka API bilgileri</param>
        /// <returns></returns>
        private async Task<BankPaymentResponse> DoPreAuthPaymentAsync(CreatePaymentInput input, int bankCode, bool is3D,
            Payment paymentEntity = null, ThreeDInit threedEntity = null)
        {
            var bankPos = bankPOSAuthFactory.Create(bankCode);

            var bankPOSAuth = new BankPOSAuth {
                BankCode = bankPos.GetBankCode(),
                MerchantID = bankPos.GetMerchantID(),
                MerchantUser = bankPos.GetMerchantUser(),
                MerchantPassword = bankPos.GetMerchantPassword(),
                MerchantStorekey = bankPos.GetMerchantStorekey(),
                TestPlatform = bankPos.GetTestPlatform()
            };

            var responseMerchant = await merchantRepository.GetAsync((long)input.MerchantId);


            BankCustomerInfo customerInfo = new BankCustomerInfo {
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

            BankPaymentRequest preauthRequest = new BankPaymentRequest {
                InvoiceInfo = customerInfo,
                ShippingInfo = customerInfo,
                PaymentInfo = new BankPaymentInfo {
                    CardNameSurname = input.Card.CardHolderName,
                    CardNumber = input.Card.CardNumber,
                    CardExpiryDateMonth = input.Card.ExpireMonth.cpToShort(),
                    CardExpiryDateYear = input.Card.ExpireYear.cpToShort(),
                    Amount = input.Amount,
                    CardCVV = input.Card.Cvc,
                    Currency = BankCurrency.TRY,
                    Installment = input.Installment.Value,
                    RND = input.RRN,
                    Hash = input.Hash,
                    PaymentDescription = input.PaymentDescription,
                    WebURL = input.WebURL
                },
                Payment3D = new BankPayment3D {
                    Confirm = is3D,
                    ReturnURL = "http://localhost:4200/okurl"
                },
                CustomerIPAddress = "1.1.1.1", //TODO bizim ip adresi yazılacak.
                OrderNumber = Random.Shared.Next(100000, 1000000000).ToString(),
            };

            var bankClient = bankClientFactory.Create(bankCode);
            BankPaymentResponse result = null;
            int maxRetryCount = 3;
            int retryCount = 0;
            while (retryCount < maxRetryCount)
            {
                result = bankClient.PreAuthPayment(preauthRequest, bankPOSAuth);

                if (paymentEntity == null)
                    threedEntity.RetryCount++;
                else
                    paymentEntity.RetryCount++;
                retryCount++;

                if (result.Status != PaymentResponseStatus.Error)
                {
                    return result;
                }
            }

            result.Status = PaymentResponseStatus.Error;
            return result;
        }

        #endregion

        #region Uygun algoritme ve Terminal

        // <summary>
        ///  Uygun algoritme ve Terminal Bulma
        /// </summary>
        private async Task<Commission> GetCommissionManagement(CreatePaymentInput input, BIN bIN)
        {
            var entity = ObjectMapper.Map<CreatePaymentInput, Payment>(input);
            entity.CardType = (int)bIN.CardType;
            entity.Installment = input.Installment;
            var responseCommission = await commissionManagementRepository.GetQueryableAsync();
            Commission commissionDto;
            try
            {
                if (input.TerminalId == null || input.TerminalId == 0)
                {
                    //Müşteri ödeme algoritmasına göre uygun pos getirilecek ve ataması yapılacak.
                    var responseSetting = await settingsRepository.GetAsync(entity.MerchantId.Value);
                    if (responseSetting.PayAlgorithm != PayAlgorithmType.OnUsPrefer)
                    {
                        if (responseCommission.Any(x => x.BankCode == bIN.BankCode))
                        {
                            commissionDto = responseCommission.Where(x =>
                                    x.BankCode == bIN.BankCode && x.Installments.Count == entity.Installment)
                                .FirstOrDefault();
                        }
                        else if (entity.CardType == (int)CardType.CREDIT_CARD)
                        {
                            commissionDto = responseCommission.Where(x => x.Installments.Count == entity.Installment)
                                .FirstOrDefault();
                        }
                        else
                        {
                            commissionDto = responseCommission.Where(x => x.Installments.Count == entity.Installment)
                                .FirstOrDefault();
                        }
                    }
                    else
                    {
                        if (entity.CardType == (int)CardType.CREDIT_CARD)
                        {
                            commissionDto = responseCommission.Where(x => x.Installments.Count == entity.Installment)
                                .FirstOrDefault();
                        }
                        else
                        {
                            commissionDto = responseCommission.Where(x => x.Installments.Count == entity.Installment)
                                .FirstOrDefault();
                        }
                    }

                    entity.TerminalId = commissionDto.TerminalId;
                }
                else
                {
                    commissionDto = responseCommission.Where(x =>
                            x.TerminalId == entity.TerminalId && x.Installments.Count== entity.Installment)
                        .FirstOrDefault();
                }
                if (entity.Installment < commissionDto.Installments.Count)
                {
                    throw new Exception("Komisyon oranı giriniz.");
                }

                return commissionDto;

            }
            catch (Exception)
            {
                throw new Exception("Commission bilgisi bulunamadı.");
            }
        }

        #endregion

        #endregion

        [HttpPost, HttpGet]
        public async Task OkUrl()
        {
            //var bodyAsString = _httpContextAccessor.HttpContext.Request.QueryString.Value;
            var stream = _httpContextAccessor.HttpContext.Request.Body;
            using StreamReader reader = new(stream, leaveOpen: false);
            var bodyAsString = await reader.ReadToEndAsync();
            var arrayCollection = bodyAsString.Split("&");

            
            using (StreamWriter writetext = new StreamWriter("response.txt"))
            {
                writetext.WriteLine("Response : " + bodyAsString);
                
            }


            var jsonData = new CompleteThreeDSPaymentInput();

            foreach (var item in arrayCollection)
            {
                var key = item.Split("=")[0];
                var value = item.Split("=")[1];

                switch (key)
                {
                    case "MerchantId":
                        jsonData.MerchantId = value;
                        break;
                    case "Pan":
                        jsonData.Pan = value;
                        break;
                    case "Expiry":
                        jsonData.Expiry = value;
                        break;
                    case "PurchAmount":
                        jsonData.PurchAmount = value;
                        break;
                    case "PurchCurrency":
                        jsonData.PurchCurrency = value;
                        break;
                    case "VerifyEnrollmentRequestId":
                        jsonData.VerifyEnrollmentRequestId = value;
                        break;
                    case "Xid":
                        jsonData.Xid = value;
                        break;
                    case "SessionInfo":
                        jsonData.SessionInfo = value;
                        break;
                    case "Status":
                        jsonData.Status = value;
                        break;
                    case "Cavv":
                        jsonData.Cavv = value;
                        break;
                    case "Eci":
                        jsonData.Eci = value;
                        break;
                    case "BankPacket":
                        jsonData.BankPacket = value;
                        break;
                    case "Amount":
                        jsonData.Amount = value;
                        break;
                }
            }

            using (var client = new HttpClient())
            {
                HttpResponseMessage response =
                    await client.PostAsJsonAsync("https://castle.payosfer.com/api/app/payment/complete3DSPayment",
                        jsonData);

                response.EnsureSuccessStatusCode();
            }
        }

    }
}