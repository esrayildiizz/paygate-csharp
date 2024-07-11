using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Abp.Authorization;
using Abp.UI;
using Microsoft.EntityFrameworkCore;
using PayosferCastle.CastleService.Dtos.Cards;
using PayosferCastle.CastleService.Dtos.Response;
using PayosferCastle.CastleService.Entities;
using PayosferCastle.CastleService.Permissions;
using PayosferCastle.CastleService.Repositories;
using static PayosferCastle.CastleService.Permissions.CastleServicePermissions;

namespace PayosferCastle.CastleService.Services
{
    public class CardService : CastleAppService, ICardService
    {
        private readonly ICardRepository _cardRepositories;

        #region Ctor
        public CardService(ICardRepository cardRepositories)
        {
            _cardRepositories = cardRepositories;
        }
        #endregion

        #region GetAllCardsAsync
        public async Task<List<CardDto>> GetAllCardsAsync()
        {
            var cards = await _cardRepositories.GetListAsync(x => x.TenantId == CurrentTenant.Id );
            var sortedCars = cards.OrderByDescending(x => x.CardHolderName).ToList();
            var responseDto = ObjectMapper.Map<List<Card>, List<CardDto>>(sortedCars);
            return responseDto;
        }
        #endregion
        
       
        #region SaveStoredCardAsync
        [AbpAuthorize(CastleServicePermissions.Cards.Create)]
        public async Task SaveCardAsync(SaveCardDto input)
        {
            var card = ObjectMapper.Map<SaveCardDto, Card>(input);
            card.TenantId = CurrentTenant.Id;
            await _cardRepositories.InsertAsync(card);
        }
        #endregion

        #region UpdateStoredCardAsync
        [AbpAuthorize(CastleServicePermissions.Cards.Edit)]
        public async Task UpdateCardAsync(UpdateCardDto input)
        {
            var cardQueryable = await _cardRepositories.GetQueryableAsync();
            var card = await cardQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id && x.Id == input.Id );
            card.CardHolderName = input.CardHolderName;
            card.CardNumber = input.CardNumber;
            card.ExpireYear = input.ExpireYear;
            card.ExpireMonth = input.ExpireMonth;
            card.Cvc = input.Cvc;
            card.TerminalId = input.TerminalId;
            card.CardAlias = input.CardAlias;
            card.CardUserKey = input.CardUserKey;
            card.CardToken = input.CardToken;
            card.SaveCard = input.SaveCard;
            card.BinNumber = input.BinNumber;
            card.LastFourDigits = input.LastFourDigits;
            card.CardHolderIdentityNumber = input.CardHolderIdentityNumber;

            await _cardRepositories.UpdateAsync(card);

        }
        #endregion

        #region DeleteStoredCardAsync
        [AbpAuthorize(CastleServicePermissions.Cards.Delete)]
        public async Task DeleteCardAsync(long id)
        {
            var cardsQueryable = await _cardRepositories.GetQueryableAsync();
            var card = await cardsQueryable.FirstOrDefaultAsync(x => x.TenantId == CurrentTenant.Id && x.Id == id);

            if (card != null)
            {
                await _cardRepositories.DeleteAsync(card);
            }
            else
            {
                throw new UserFriendlyException("Card not found");
            }
        }
        #endregion


        #region SearchStoredCardAsync
        //public Task<StoredCardListDto> SearchCardsAsync(SearchCardsDto searchStoredCardsInput)
        //{
        //    throw new NotImplementedException();
        //}
        #endregion

    }
}
