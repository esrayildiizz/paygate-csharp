

  
<p align="center">
  <img src="https://github.com/esrayildiizz/Example/assets/106755194/e0197438-b265-449a-a90b-cf4f526a0e01" alt="PAYGATE Image"/>
</p>

<p align="center">
<strong>PAYGATE</strong>
</p>

<p align="center">
PayGate ile tüm online ödemelerinizi tek merkezden yönetin 
</p>
<p align="center">
katma değerli servislerimiz ile ödeme giderlerinizi azaltın, cironuzu artırın ve işletmenizi büyütün.
</p>

## PAYGATE
[![Craftgate Dotnet CI](https://img.shields.io/badge/Craftgate%20Dotnet%20CI-passing-brightgreen)]()
[![nuget](https://img.shields.io/badge/nuget-v1.0.61-blue)]()
[![Gitpod ready-to-code](https://img.shields.io/badge/Gitpod-ready--to--code-blue?logo=gitpod)]()


## Requirements
- .NET Framework 4.6+
- .NET Core 1.1+
- .NET Core 2.0+

## Installation
`Install-Package  ...... `



## Usage
PayGate API'sine erişmek için öncelikle API kimlik bilgilerini (örneğin bir API anahtarı ve gizli anahtar) edinmeniz gerekir. Zaten bir Craftgate hesabınız yoksa https://paygate.io/ adresinden kaydolabilirsiniz.

API kimlik bilgilerinizi aldıktan sonra, PayGate kimlik bilgilerinizle bir örnek oluşturarak PayGate'i kullanmaya başlayabilirsiniz.


`PayGateClient _paygate = new PayGateClient("<YOUR API KEY>", "<YOUR SECRET KEY>");`


Varsayılan olarak PayGate istemcisi üretim API sunucularına bağlanır https://api.paygate.io. Test amaçlı olarak lütfen https://sandbox-api.paygate.io. kullanarak deneme alanı URL'sini kullanın.


`PayGateClient _paygate = new PayGateClient("<YOUR API KEY>", "<YOUR SECRET KEY>", "https://sandbox-api.paygate.io");`


## Examples


### Running the Examples


### Credit Card Payment Use Case

```csharp
CraftgateClient _craftgate = new CraftgateClient("<YOUR API KEY>", "<YOUR SECRET KEY>");
var request = new CreatePaymentRequest
{
    Price = new decimal(100.0),
    PaidPrice = new decimal(100.0),
    WalletPrice = new decimal(0.0),
    Installment = 1,
    ConversationId = "456d1297-908e-4bd6-a13b-4be31a6e47d5",
    Currency = Currency.Try,
    PaymentGroup = PaymentGroup.ListingOrSubscription,
    Card = new CardDto
    {
        CardHolderName = "Haluk Demir",
        CardNumber = "5258640000000001",
        ExpireYear = "2044",
        ExpireMonth = "07",
        Cvc = "000"
    },
    Items = new List<PaymentItem>
    {
        new PaymentItem
        {
            Name = "Item 1",
            Price = new decimal(30.0),
            ExternalId = "externalId-1"
        },
        new PaymentItem
        {
            Name = "Item 2",
            Price = new decimal(50.0)
        },
        new PaymentItem
        {
            Name = "Item 3",
            Price = new decimal(20.0),
            ExternalId = "externalId-3"
        }
    }
};
var response = _craftgate.Payment().CreatePayment(request);
Assert.NotNull(response);
```

### Contributions
*For all contributions to this client please see the contribution guide here.*

## License

**MIT**










