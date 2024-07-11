using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.Bins;
using PayosferCastle.CastleService.Entities;
using Volo.Abp.Domain.Repositories;

namespace PayosferCastle.CastleService.Services
{
    public class BinService : CastleAppService, IBinService
    {
        private readonly IRepository<BIN> _binRepository;
        public BinService(IRepository<BIN> binRepository)
        {
            this._binRepository = binRepository;
        }

        public async Task SaveBinAsync(SaveBINDto input)
        {
            var entity = ObjectMapper.Map<SaveBINDto, BIN>(input);
            await _binRepository.InsertAsync(entity);
          
        }

        public async Task UpdateBinAsync(UpdateBINDto input)
        {

            var binQueryable = await _binRepository.GetQueryableAsync();
            var bin = await binQueryable.FirstOrDefaultAsync(x => x.Id == input.Id );

            bin.BinNumber = input.BinNumber;
            bin.CardType = input.CardType;
            bin.CardAssociation= input.CardAssociation;
            bin.CardBrand = input.CardBrand;
            bin.BankName = input.BankName;
            bin.BankCode = input.BankCode;
            bin.Commercial = input.Commercial;

            await _binRepository.UpdateAsync(bin);

        }

        public async Task<BINDto> GetBinAsync(long id)
        {
            var binQueryable = await _binRepository.GetQueryableAsync();
            var entity = await binQueryable.FirstOrDefaultAsync(x => x.Id == id );
            var responseDto = ObjectMapper.Map<BIN, BINDto>(entity);
            return responseDto;
        }

        public async Task<List<BINDto>> GetAllBinListAsync()
        {
            var binList = await _binRepository.GetListAsync();
            var responseDto = ObjectMapper.Map<List<BIN>, List<BINDto>>(binList);
            return responseDto;
        }

        public async Task DeleteBinAsync(long id)
        {
            var binQueryable = await _binRepository.GetQueryableAsync();
            var entity = await binQueryable.FirstOrDefaultAsync(x => x.Id == id);
            await _binRepository.DeleteAsync(entity);
        }


    }
}
