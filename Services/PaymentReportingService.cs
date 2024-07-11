using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Iyzipay.Model;
using PayosferCastle.CastleService.Dtos.Request;
using PayosferCastle.CastleService.Dtos.Response;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Models;
using PayosferCastle.CastleService.Repositories;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;
namespace PayosferCastle.CastleService.Services
{
    public class PaymentReportingService : CastleAppService, IPaymentReportingService
    {

        private readonly IPaymentRepository _paymentRepository;
        private readonly IRefundRepository _refundRepository;
        private readonly ITerminalRepository _terminalRepository;
        private readonly IMerchantRepository _merchantRepository;
        public PaymentReportingService(IPaymentRepository _paymentRepository,
            IRefundRepository _refundRepository,
             ITerminalRepository _terminalRepository,
             IMerchantRepository merchantRepository)
        {
            this._paymentRepository = _paymentRepository;
            this._refundRepository = _refundRepository;
            this._terminalRepository = _terminalRepository;
            this._merchantRepository = merchantRepository;
        }

        //Detaylı Ödeme Sorgulama
        public async Task<ReportingPaymentDto> RetrievePaymentAsync(long input)
        {
            var response = await _paymentRepository.GetAsync(input);
            var responseDto = ObjectMapper.Map<Entities.Payment, ReportingPaymentDto>(response);
            //responseDto.StatusText = Enum.GetName(typeof(Status), responseDto.Status);
            return responseDto;
        }
        //Ödeme Arama
        public async Task<List<ReportingPaymentDto>> SearchPaymentsAsync(SearchPaymentsInput input)
        {


            var responseDto = new List<ReportingPaymentDto>();
           

            if (input.TerminalId == 0)
            {
                
                var terminalQueryable = await _terminalRepository.GetQueryableAsync();
                var terminals = terminalQueryable.Where(x => x.TenantId == CurrentTenant.Id).Select(x => x.Id).ToList();
                var paymentQueryable = await _paymentRepository.GetQueryableAsync();
                var payments = paymentQueryable.Where(x => terminals.Contains(x.Id));
                var filteredPayments = payments.Where(x => x.TranDate >= input.StartTransactionDate && x.TranDate <= input.FinishTransactionDate).ToList();

                //Hepsi Seçeneği için yapıldı 
                if (input.PaymentStatus == PaymentStatus.INIT_THREEDS)
                {
                    filteredPayments = filteredPayments.Where(x => x.PaymentStatus == (int)PaymentStatus.SUCCESS || x.PaymentStatus == (int)PaymentStatus.FAILURE).ToList();
                }
                else
                {
                    filteredPayments = filteredPayments.Where(x => x.PaymentStatus == (int)input.PaymentStatus).ToList();
                }
                

               
                responseDto = ObjectMapper.Map<List<Entities.Payment>, List<ReportingPaymentDto>>(filteredPayments);

                foreach (var item in responseDto)
                {
                    item.PaidWithStoredCard = false;  //saklanan bir kart mı?

                    item.RefundStatusText = Enum.GetName(typeof(RefundStatus), item.RefundStatus);

                    item.PaymentTypeText = Enum.GetName(typeof(PaymentType), item.PaymentType);
                    item.PaymentProviderText = Enum.GetName(typeof(PaymentProvider), item.PaymentProvider);
                    item.PaymentSourceText = Enum.GetName(typeof(PaymentSource), item.PaymentSource);
                    item.PaymentStatusText = Enum.GetName(typeof(PaymentStatus), item.PaymentStatus);
                    item.ActionText = Enum.GetName(typeof(PaymentAction), item.Action);

                    item.CardTypeText = Enum.GetName(typeof(CardType), item.CardType);
                    item.CardAssociationText = Enum.GetName(typeof(CardAssociation), item.CardAssociation);
                    //item.CardBrandText = Enum.GetName(typeof(CardBrand), item.CardBrandText);

                    var responseTerminal = await _terminalRepository.GetAsync(item.TerminalId);
                    item.TerminalName = responseTerminal.Name;
                    item.TerminalAlias = responseTerminal.Alias;
                    item.TerminalBankCode = responseTerminal.BankCode;

                    if (item.RefundStatus == (int)RefundStatus.SUCCESS)
                    {
                        var responseRefund = await _refundRepository.GetListByPaymentId(item.Id);
                        var xx = ObjectMapper.Map<List<Entities.Refund>, List<ReportingPaymentRefundDto>>(responseRefund);
                        item.Refunds = xx;
                    }
                }

            }
            else
            {
                var terminal = await _terminalRepository.FirstOrDefaultAsync(x => x.Id == input.TerminalId);
                var merchant = await _merchantRepository.FirstOrDefaultAsync(x => x.TenantId == terminal.TenantId);

                var paymentQueryable = await _paymentRepository.GetQueryableAsync();
                var payments = paymentQueryable.Where(x => x.TerminalId == input.TerminalId).ToList();
                var filteredPayments =  payments.Where(x => x.TranDate >= input.StartTransactionDate && x.TranDate <= input.FinishTransactionDate).ToList();

                //Hepsi Seçeneği için yapıldı 
                if (input.PaymentStatus == PaymentStatus.INIT_THREEDS)
                {
                    filteredPayments = filteredPayments.Where(x => x.PaymentStatus == (int)PaymentStatus.SUCCESS || x.PaymentStatus == (int)PaymentStatus.FAILURE).ToList();
                }
                else
                {
                    filteredPayments = filteredPayments.Where(x => x.PaymentStatus == (int)input.PaymentStatus).ToList();
                }


                responseDto = ObjectMapper.Map<List<Entities.Payment>, List<ReportingPaymentDto>>(filteredPayments);
                foreach (var item in responseDto)
                {
                    item.PaidWithStoredCard = false;  //saklanan bir kart mı?

                    item.RefundStatusText = Enum.GetName(typeof(RefundStatus), item.RefundStatus);

                    item.PaymentTypeText = Enum.GetName(typeof(PaymentType), item.PaymentType);
                    item.PaymentProviderText = Enum.GetName(typeof(PaymentProvider), item.PaymentProvider);
                    item.PaymentSourceText = Enum.GetName(typeof(PaymentSource), item.PaymentSource);
                    item.PaymentStatusText = Enum.GetName(typeof(PaymentStatus), item.PaymentStatus);
                    item.ActionText = Enum.GetName(typeof(PaymentAction), item.Action);

                    item.CardTypeText = Enum.GetName(typeof(CardType), item.CardType);
                    item.CardAssociationText = Enum.GetName(typeof(CardAssociation), item.CardAssociation);
                    //item.CardBrandText = Enum.GetName(typeof(CardBrand), item.CardBrandText);

                    var responseTerminal = await _terminalRepository.GetAsync(item.TerminalId);
                    item.TerminalName = responseTerminal.Name;
                    item.TerminalAlias = responseTerminal.Alias;
                    item.TerminalBankCode = responseTerminal.BankCode;

                    if (item.RefundStatus == (int)RefundStatus.SUCCESS)
                    {
                        var responseRefund = await _refundRepository.GetListByPaymentId(item.Id);
                        var xx = ObjectMapper.Map<List<Entities.Refund>, List<ReportingPaymentRefundDto>>(responseRefund);
                        item.Refunds = xx;
                    }
                }
            }

            return responseDto;
        }

 
        //Ödeme İade Sorgulama
        public async Task<ReportingPaymentRefundDto> RetrievePaymentRefundsAsync(long input)
        {
            var response = await _refundRepository.GetAsync(input);
            var responseDto = ObjectMapper.Map<Entities.Refund, ReportingPaymentRefundDto>(response);
            //responseDto.StatusText = Enum.GetName(typeof(Status), responseDto.Status);
            return responseDto;
        }

        //Ödeme İade Arama
        public async Task<List<ReportingPaymentRefundDto>> SearchPaymentRefundsAsync(SearchPaymentRefundsInput input)
        {
            var response = await _refundRepository.GetListByFilter(input.StartTransactionDate.Value, input.FinishTransactionDate.Value);
            var responseDto = ObjectMapper.Map<List<Entities.Refund>, List<ReportingPaymentRefundDto>>(response);

            foreach ( var item in responseDto )
            {
                item.StatusText = Enum.GetName(typeof(RefundStatus), item.Status);
                item.RefundStatus = Enum.GetName(typeof(RefundType), item.RefundType);
                item.RefundDestinationTypeText = Enum.GetName(typeof(RefundDestinationType), item.RefundDestinationType);
                item.RefundActionTypeText = Enum.GetName(typeof(RefundActionType), item.RefundActionType);
            }
            return responseDto;
        }


        //Bu 3 servis kullanılmayacak.
        //Ödeme Kırılımdan İade Sorgulama
        //public async Task<ReportingPaymentTransactionRefundListDto> RetrievePaymentTransactionRefundsAsync(long paymentId, long paymentTransactionId)
        //{
        //    throw new NotImplementedException();
        //}
        ////Detaylı Ödeme Kırılımı Sorgulama
        //public async Task<ReportingPaymentTransactionListDto> RetrievePaymentTransactionsAsync(long input)
        //{
        //    throw new NotImplementedException();
        //}
        ////Ödeme Kırılımdan İade Arama
        //public async Task<ReportingPaymentTransactionRefundListDto> SearchPaymentTransactionRefundsAsync(SearchPaymentTransactionRefundsInput input)
        //{
        //    throw new NotImplementedException();
        //}
    }
}
