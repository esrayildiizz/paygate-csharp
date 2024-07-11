using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.TerminalCommissions;
using PayosferCastle.CastleService.Dtos.Terminals;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Models;
using PayosferCastle.CastleService.Repositories;


namespace PayosferCastle.CastleService.Services
{
    public class TerminalService : CastleAppService, ITerminalService
    { 
  
        public readonly  ICommissionInstallmentRepository _commissionInstallmentRepository;
        private readonly ITerminalRepository _terminalRepository;
        private readonly ICommissionManagementRepository _commissionManagementRepository;
        
        public TerminalService(
            ITerminalRepository terminalRepository, 
            ICommissionManagementRepository commissionManagementRepository, 
            ICommissionInstallmentRepository commissionInstallmentRepository)
        {
        
            this._terminalRepository = terminalRepository;
            this._commissionManagementRepository = commissionManagementRepository;
            _commissionInstallmentRepository = commissionInstallmentRepository;
        }
        
        #region Commissions
        
        public async Task UpdateCommissionAsync(CreateOrEditCommissionInputDto input)
        {
            var commissionQueryable = await _commissionManagementRepository.GetQueryableAsync();
            var commission = commissionQueryable.Include(x => x.Installments).FirstOrDefault(x => x.TenantId == CurrentTenant.Id && x.Id == input.Id.Value);

            commission.Name = input.Name;
            commission.BankCode = input.BankCode;
            commission.IsCommission = input.IsCommission;
            commission.IsBlockDay = input.IsBlockDay;
            commission.IsForeignCard = input.IsForeignCard;
            commission.IsInterestRate = input.IsInterestRate;
            commission.IsInstallment = input.IsInstallment;


            var newInstallments = ObjectMapper.Map<List<CommissionInstallmentDto>, List<CommissionInstallment>>(input.Installments);

            // Silinmiş kayıtlar var ise
            foreach (var existingItem in commission.Installments.ToList())
            {
                if (!newInstallments.Any(i => i.Id == existingItem.Id))
                {
                    await _commissionInstallmentRepository.DeleteAsync(existingItem);
                }
            }
            
            foreach (var updatedItem in newInstallments)
            {
                var existingItem = commission.Installments.SingleOrDefault(i => i.Id == updatedItem.Id);

                if (existingItem != null)
                {
                    // Update existing item      
                    
                    existingItem.Code = updatedItem.Code;
                    existingItem.Status = updatedItem.Status;
                    existingItem.OnUsDebitCardRate = updatedItem.OnUsDebitCardRate;
                    existingItem.OnUsCreditCardRate = updatedItem.OnUsCreditCardRate;
                    existingItem.NotOnUsDebitCardRate = updatedItem.NotOnUsDebitCardRate;
                    existingItem.NotOnUsCreditCardRate = updatedItem.NotOnUsCreditCardRate;
                    existingItem.ForeignCardRate = updatedItem.ForeignCardRate;
                    existingItem.BlockageDayCount = updatedItem.BlockageDayCount;
                    existingItem.InterestRate = updatedItem.InterestRate;
                    existingItem.TenantId = CurrentTenant.Id;
                    existingItem.LastModificationTime = DateTime.Now;
                    existingItem.LastModifierId = CurrentUser.Id;
                   
                }
                else
                {
                    // New item, add it
                    updatedItem.TenantId = CurrentTenant.Id;
                    updatedItem.CommissionId = commission.Id;
                    await _commissionInstallmentRepository.InsertAsync(updatedItem);
                }
            }
 
        }
        public async Task<CommissionDto> GetCommissionAsync(long id)
        {
            var commissionQueryable = await _commissionManagementRepository.GetQueryableAsync();
            var commission = commissionQueryable.FirstOrDefault(x => x.TenantId == CurrentTenant.Id && x.TerminalId == id);
            var responseDto = ObjectMapper.Map<Commission, CommissionDto>(commission);
            
            var installments = await _commissionInstallmentRepository.GetListAsync(x => x.CommissionId == id);
            var tempRes = ObjectMapper.Map<List<CommissionInstallment>, List<CommissionInstallmentDto>>(installments);
            responseDto.Installments = tempRes;

            return responseDto;
        }
       
        #endregion

        #region Terminal
        public async Task SaveTerminal(TerminalSaveInputDto input)
        {
            var entity = ObjectMapper.Map<TerminalSaveInputDto, Terminal>(input);
            entity.TenantId = CurrentTenant.Id;
            
           
            
            var commissionEntity = new Commission();
            commissionEntity.TenantId = CurrentTenant.Id;
            commissionEntity.Name = "Default";
            commissionEntity.IsCommission = false;
            commissionEntity.IsInstallment = false;
            commissionEntity.IsInterestRate = false;
            commissionEntity.IsForeignCard = false;
            commissionEntity.IsBlockDay = false;
            commissionEntity.BankCode =  input.BankCode;
            
            var commissionInstallment = new CommissionInstallment();
            commissionInstallment.TenantId = CurrentTenant.Id;
            commissionInstallment.Code = "1";
            commissionInstallment.InterestRate = 1;
            commissionInstallment.BlockageDayCount = 1;
            commissionInstallment.ForeignCardRate = 1;

            commissionInstallment.OnUsCreditCardRate = 1;
            commissionInstallment.OnUsDebitCardRate = 1;
            commissionInstallment.NotOnUsCreditCardRate = 1;
            commissionInstallment.NotOnUsDebitCardRate = 1;
            
            var terminalEntity = await _terminalRepository.InsertAsync(entity,true);
            commissionEntity.TerminalId  = terminalEntity.Id;
            var commissionId = await _commissionManagementRepository.InsertAsync(commissionEntity,true);
            terminalEntity.CommissionId = commissionId.Id;
            commissionInstallment.CommissionId = commissionId.Id;
            var commissionInstallmentId = await _commissionInstallmentRepository.InsertAsync(commissionInstallment, true);
            await _terminalRepository.UpdateAsync(terminalEntity);

        }
        public async Task UpdateTerminal(TerminalUpdateInputDto input)
        {
            var terminalQueryable = await _terminalRepository.GetQueryableAsync();
            var terminal =await terminalQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id && x.Id == input.Id.Value );
         
            terminal.Status = input.Status;
            terminal.BankTerminalId = input.BankTerminalId;
            terminal.Bank = input.Bank;
            terminal.BankCode = input.BankCode;
            terminal.Name = input.Name.IsNullOrEmpty() ? terminal.Name : input.Name;
            terminal.Alias = input.Alias.IsNullOrEmpty() ? terminal.Alias : input.Alias;
            terminal.CastlePilotStatus = input.CastlePilotStatus;
            terminal.ClientId = input.ClientId;
            terminal.ThreeDSecureKey = input.ThreeDSecureKey;
            terminal.TerminalUserName = input.TerminalUserName;
            terminal.TerminalPassword = input.TerminalPassword;
            terminal.Currency = input.Currency;
            terminal.MultiCurrency = input.MultiCurrency;
            terminal.Installment = input.Installment;
            terminal.NonCVV = input.NonCVV;
            terminal.Mandatory3DS = input.Mandatory3DS;
            terminal.OrderNo = input.OrderNo;
            terminal.Mode = input.Mode;
            terminal.HostName = input.HostName;
            terminal.Path = input.Path;
            terminal.Port = input.Port;
            terminal.ThreeDSPath = input.ThreeDSPath;
            terminal.SupportedCardAssociations = input.SupportedCardAssociations;
            // terminal.PROVAUTUserName = input.PROVAUTUserName;
            // terminal.PROVAUTPassword = input.PROVAUTPassword;
            // terminal.PROVRFNUserName = input.PROVRFNUserName;
            // terminal.PROVRFNPassword = input.PROVRFNPassword;
            terminal.PosnetId = input.PosnetId;
            terminal.ThreeDTerminalId = input.ThreeDTerminalId;
            terminal.ThreeDPosnetId = input.ThreeDPosnetId;
            terminal.ThreeDSPath = input.ThreeDSPath;
            // terminal.IsNewPosnet = input.IsNewPosnet;
            terminal.MarketNumber = input.MarketNumber;

            await _terminalRepository.UpdateAsync(terminal);
  
        }
        public async Task DeleteTerminal(long id)
        {
            var tempRes = await _terminalRepository.GetQueryableAsync();
            var filtered = tempRes.Any(x => x.TenantId == CurrentTenant.Id && x.Id == id);
            if(filtered)
            {
                await _terminalRepository.DeleteAsync(id);
            }
            else
            {
                throw new UserFriendlyException("Terminal not found");
            }
        }
        public async Task<List<TerminalDto>> GetAllTerminals()
        {
            var terminalList = await _terminalRepository.GetListAsync(x => x.TenantId == CurrentTenant.Id );
            var responseDto = ObjectMapper.Map<List<Terminal>, List<TerminalDto>>(terminalList);
            return responseDto;
        }
        public async Task<TerminalDto> GetTerminal(long terminalId)
        {
            var terminalQueryable = await _terminalRepository.GetQueryableAsync();
            var response = terminalQueryable.FirstOrDefault(x => x.TenantId == CurrentTenant.Id && x.Id == terminalId );
            var responseDto = ObjectMapper.Map<Terminal, TerminalDto>(response);
            return responseDto;
        }
        public async Task UpdateTerminalOrderAsync(long id, int orderId)
        {
            var terminalQueryable = await _terminalRepository.GetQueryableAsync();
            var terminal = await terminalQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id && x.Id == id);
            terminal.OrderNo = orderId; 
            await _terminalRepository.UpdateAsync(terminal);
  
        }
        public async Task UpdateTerminalListOrderAsync(IList<OrderItems> input)
        {
            foreach (var item in input)
            {
                var terminalQueryable = await _terminalRepository.GetQueryableAsync();
                var terminal =await  terminalQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id && x.Id == item.Id);
                terminal.OrderNo = item.OrderNo;
                await _terminalRepository.UpdateAsync(terminal);
            }
        }
        
        #endregion
        
    }
}
