using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PayosferCastle.CastleService.BankModels;
using PayosferCastle.CastleService.Business;
using PayosferCastle.CastleService.Dtos.Installment;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Models;
using PayosferCastle.CastleService.Repositories;

namespace PayosferCastle.CastleService.Services
{
    public class InstallmentService : CastleAppService, IInstallmentService
    {
        //private readonly IPaymentRepository paymentRepository;
        //public AuthService(IPaymentRepository paymentRepository)
        //{
        //    this.paymentRepository = paymentRepository;
        //}

        private readonly IBINRepository _bINRepository;
        private readonly ICommissionManagementRepository _commissionManagementRepository;
        private readonly ITerminalRepository _terminalRepository;
        private readonly ISettingRepository _settingsRepository;
        private readonly IBankClientFactory _bankClientFactory;
        private readonly IBankPOSAuthFactory _bankPOSAuthFactory;


        public InstallmentService(IBINRepository bINRepository, 
            ICommissionManagementRepository commissionManagementRepository, 
            ITerminalRepository terminalRepository, 
            IBankClientFactory bankClientFactory,
            IBankPOSAuthFactory bankPOSAuthFactory)
        {
            _bINRepository = bINRepository;
            _commissionManagementRepository = commissionManagementRepository;
            _terminalRepository = terminalRepository;
            _bankClientFactory = bankClientFactory;
            _bankPOSAuthFactory = bankPOSAuthFactory;
        }

        public async Task<List<InstallmentDto>> SearchInstallmentsAsync(SearchInstallmentDto input)
        {
            List<InstallmentDto> responseDto = new List<InstallmentDto>();

            if (!string.IsNullOrWhiteSpace(input.BinNumber))
            {
                var bINNumber = input.BinNumber;
                var ammount = input.Amount;
                var responseBIN = await _bINRepository.GetQueryableAsync();
                BIN? bIN = responseBIN.Where(x => x.BinNumber == bINNumber).FirstOrDefault();

                input.DistinctCardBrandsWithLowestCommissions = false; //TODO:Daha düşük komisyonlu kart var mı kısmı daha sonra yapılacak.


                var result = await DoAllInstallmentQueryAsync(input, (int)bIN.BankCode);

                if (result.Confirm == true)
                {
                    var response = result.InstallmentList.ToList();

                    responseDto = ObjectMapper.Map<List<AllInstallment>, List<InstallmentDto>>(response);
                }
                else
                {
                    Console.WriteLine("İşleme devam edilemiyor.");
                    return responseDto;
                }
            }

            return responseDto;
        }

        #region  Private Methods - Yardımcı Methodlar 
        private async Task<BankAllInstallmentQueryResponse> DoAllInstallmentQueryAsync(SearchInstallmentDto input, int bankCode)
        {
            var bankPos = _bankPOSAuthFactory.Create(bankCode);


            var bankPOSAuth = new BankPOSAuth
            {
                BankCode = bankPos.GetBankCode(),
                MerchantID = bankPos.GetMerchantID(),
                MerchantUser = bankPos.GetMerchantUser(),
                MerchantPassword = bankPos.GetMerchantPassword(),
                MerchantStorekey = bankPos.GetMerchantStorekey(),
                TestPlatform = bankPos.GetTestPlatform()
            };

            var bINNumber = input.CardNumber.Substring(0, 8);
            var responseBIN = await _bINRepository.GetQueryableAsync();
            BIN? bIN = responseBIN.Where(x => x.BinNumber == bINNumber).FirstOrDefault();

            BankAllInstallmentQueryRequest saleRequest = new BankAllInstallmentQueryRequest
            {
                Amount = (decimal)input.Amount,
                CardNumber = input.CardNumber,
                BinNumber = input.BinNumber,
                EFTCode= bIN.BankCode,
                Installment = (int)input.Installment,
                Currency = input.Currency,
                DistinctCardBrandsWithLowestCommissions = input.DistinctCardBrandsWithLowestCommissions,
            };


            var bankClient = _bankClientFactory.Create(bankCode);

            BankAllInstallmentQueryResponse result = null;

            result = bankClient.AllInstallmentQuery(saleRequest, bankPOSAuth);

            if (result.Confirm == true)
            {
                return result;
            }

            result.Confirm = false;
            return result;

        }
        #endregion
    }
}
