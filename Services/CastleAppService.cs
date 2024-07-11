using PayosferCastle.CastleService.Localization;
using Volo.Abp.Application.Services;

namespace PayosferCastle.CastleService.Services;

/* Inherit your application services from this class.
 */
public abstract class CastleAppService : ApplicationService
{
    protected CastleAppService()
    {
        LocalizationResource = typeof(CastleServiceResource);
    }
}
