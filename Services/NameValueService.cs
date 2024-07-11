using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.Request;
using PayosferCastle.CastleService.Dtos.Response;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Repositories;

namespace PayosferCastle.CastleService.Services
{
    public class NameValueService : CastleAppService, INameValueService
    {
        private readonly INameValueRepository nameValueRepository;
        public NameValueService(INameValueRepository nameValueRepository)
        {
            this.nameValueRepository = nameValueRepository;
        }

        public async Task<NameValueDto> SaveNameValueAsync(NameValueSaveInput input)
        {
            var entity = ObjectMapper.Map<NameValueSaveInput, NameValue>(input);

            var response = await nameValueRepository.InsertAsync(entity);

            var responseDto = ObjectMapper.Map<NameValue, NameValueDto>(response);

            return responseDto;
        }
        public async Task<List<NameValueDto>> GetNameValueListBySourceObjectAsync(string sourceObject)
        {
            var response = await nameValueRepository.GetQueryableAsync();

            var nameValueList = await response.Where(x => x.SourceObject == sourceObject).OrderByDescending(x => x.Id).ToListAsync();

            var responseDto = ObjectMapper.Map<List<NameValue>, List<NameValueDto>>(nameValueList);

            return responseDto;
        }
        public async Task<List<NameValueDto>> GetNameValueListByControlRootAsync(string controlRoot)
        {
            var response = await nameValueRepository.GetQueryableAsync();

            var nameValueList = await response.Where(x => x.ControlRoot == controlRoot).OrderByDescending(x => x.Id).ToListAsync();

            var responseDto = ObjectMapper.Map<List<NameValue>, List<NameValueDto>>(nameValueList);

            return responseDto;
        }
        public async Task<NameValueDto> GetNameValueByParameterNameAsync(string parameterName)
        {
            var response = await nameValueRepository.GetQueryableAsync();
            var nameValue = response.Where(x => x.ParameterName == parameterName).FirstOrDefault();
            var responseDto = ObjectMapper.Map<NameValue, NameValueDto>(nameValue);
            return responseDto;
        }

        public async Task<List<NameValueDto>> GetNameByControlRootAsync(string controlRoot)
        {
            var response = await nameValueRepository.GetQueryableAsync();
            var nameValues = response.Where(x => x.ControlRoot == controlRoot).ToList();
            var responseDto = ObjectMapper.Map<List<NameValue> ,List<NameValueDto>>( nameValues);
            
            return responseDto;
        }

    }
}
