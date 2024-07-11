using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.LinkQr;

using PayosferCastle.CastleService.Repositories;
using Volo.Abp;

namespace PayosferCastle.CastleService.Services
{
    public class LinkQRService : CastleAppService, ILinkQRService
    {
        private readonly ILinkQRRepository _linkQrRepository;
        public LinkQRService(ILinkQRRepository linQrRepository)
        {
            this._linkQrRepository = linQrRepository;
        }
        public async Task<List<LinkQRDto>> GetAllLinkQRtoListAsync()
        {
            var linkQrList = await _linkQrRepository.GetListAsync(x => x.TenantId == CurrentTenant.Id);
            var responseDto = ObjectMapper.Map<List<LinkQR>, List<LinkQRDto>>(linkQrList);
            return responseDto;
        }
        public async Task<LinkQRDto> GetLinkQRAsync(long linkId)  
        {
            var linkQrQueryable= await _linkQrRepository.GetQueryableAsync();
            var linkQr = await linkQrQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id && x.Id == linkId);
            var responseDto = ObjectMapper.Map<LinkQR, LinkQRDto>(linkQr);
            return responseDto;
        }  
     public async Task CreateLinkQRAsync(CreateLinkQrDto input)
        {
            var aes = Aes.Create();
            
            var responseDto = new LinkQRDto();
            var entity = ObjectMapper.Map<CreateLinkQrDto, LinkQR>(input);
            entity.TenantId = CurrentTenant.Id;
            entity.TerminalId = input.TerminalId;
            var uri = Guid.NewGuid().ToString().Replace("-","");

            if(await Exists(uri) != true){
                entity.Url = uri;
                entity.QrCodeUrl = "http://localhost:4200//link-qr-payment/" + uri;
                await _linkQrRepository.InsertAsync(entity);
            }
            else
            {
                throw new UserFriendlyException("Bir hata oluştu");
            }     

        }
     

        public async Task UpdateLinkQRAsync(UpdateLinkQrDto input) 
        {
            
            var linkQrQueryable = await _linkQrRepository.GetQueryableAsync();
            var linkQr =await linkQrQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id && x.Id == input.Id.Value);
            
            linkQr.Status = input.Status;
            linkQr.Amount = input.Amount;
            linkQr.Currency = input.Currency;
            linkQr.RRN = input.RRN;
            linkQr.Token = input.Token;
            linkQr.EnabledInstallments = input.EnabledInstallments;
            linkQr.Channel = input.Channel;
            linkQr.TerminalId = input.TerminalId;

        }

        public async Task DeleteLinkQRAsync(long linkId)
        {
            var tempRes = await _linkQrRepository.GetQueryableAsync();
            var filtered = tempRes.Any(x => x.TenantId == CurrentTenant.Id && x.Id == linkId );
            if(filtered)
            {
                await _linkQrRepository.DeleteAsync(linkId);
            }
            else
            {
                throw new UserFriendlyException("LinkQr not found");
            }
            
         
        }

        [AllowAnonymous]
        public async Task<string> CheckQR(string guid)
        {
            var linkQrQueryable = await _linkQrRepository.GetQueryableAsync();
            //var linkQr = linkQrQueryable.Where(x => x.Url.ToString().Split("/",
            //                StringSplitOptions.None).Last() == guid).FirstOrDefaultAsync();
            var linkQr = await linkQrQueryable.Where(x => x.Url == guid).FirstOrDefaultAsync();

           
            if (linkQr == null)
            {
                return "";
            }
            else
            {
                return linkQr.QrCodeUrl;
            }

        }

        #region Private Methods
      
        private async Task<bool> Exists(string uri)
        {
            var response = await _linkQrRepository.GetListAsync(x => x.Url == uri );
            return response.Count > 0;
        }
        #endregion

    }
}
