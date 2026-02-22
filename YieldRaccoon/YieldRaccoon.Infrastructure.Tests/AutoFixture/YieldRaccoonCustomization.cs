using AutoFixture;
using YieldRaccoon.Infrastructure.Tests.AutoFixture.Builders;

namespace YieldRaccoon.Infrastructure.Tests.AutoFixture;

/// <summary>
/// AutoFixture customization that registers all YieldRaccoon specimen builders.
/// </summary>
public class YieldRaccoonCustomization : ICustomization
{
    public void Customize(IFixture fixture)
    {
        fixture.Customizations.Add(new FundIdBuilder());
        fixture.Customizations.Add(new FundHistoryRecordIdBuilder());
        fixture.Customizations.Add(new FundProfileBuilder());
        fixture.Customizations.Add(new FundHistoryRecordBuilder());
        fixture.Customizations.Add(new OrderBookIdBuilder());
    }
}
