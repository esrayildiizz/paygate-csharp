using System;
using System.Threading.Tasks;
using PayosferCastle.CastleService.Dtos.Bins;
using PayosferCastle.CastleService.Dtos.Request;
using PayosferCastle.CastleService.Dtos.Response;

namespace PayosferCastle.CastleService.Services
{
    public class MembersService : CastleAppService, IMembersService
    {
        public Task<MemberDto> CreateMemberAsync(CreateMemberInput input)
        {
            throw new NotImplementedException();
        }

        public Task<BINDto> GetBINsAsync(BINDto input)
        {
            throw new NotImplementedException();
        }

        public Task<MemberDto> RetrieveMemberAsync(long id)
        {
            throw new NotImplementedException();
        }

        //public Task<MemberListDto> SearchMembersAsync(SearchMembersInput input)
        //{
        //    throw new NotImplementedException();
        //}

        public Task<MemberDto> UpdateMemberAsync(long id, UpdateMemberInput input)
        {
            throw new NotImplementedException();
        }
    }
}
