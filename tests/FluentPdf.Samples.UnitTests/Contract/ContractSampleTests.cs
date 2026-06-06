using System.Text;
using FluentPdf.Application.Rendering;
using FluentPdf.Infrastructure.Rendering;
using FluentPdf.Kernel;
using FluentPdf.Samples.Contract;

namespace FluentPdf.Samples.UnitTests.Contract;

public sealed class ContractSampleTests
{
    private static string Decode(Result<RenderedPdf> result)
    {
        result.IsSuccess.Should().BeTrue();
        return Encoding.UTF8.GetString(result.Value.ToArray());
    }

    [Fact]
    public void The_agreement_paginates_to_a_long_multi_page_document()
    {
        var result = ContractSample.RenderWithInMemoryAdapter();

        result.IsSuccess.Should().BeTrue();
        result.Value.PageCount.Should().BeInRange(30, 40);
    }

    [Fact]
    public void The_agreement_contains_its_real_content()
    {
        var text = Decode(ContractSample.RenderWithInMemoryAdapter());

        text.Should().Contain("Senior Secured Facility Agreement");
        text.Should().Contain("Article 1. Definitions and Interpretation");
        text.Should().Contain("Article 18.");
        text.Should().Contain("IN WITNESS WHEREOF");
    }

    [Fact]
    public void The_running_footer_numbers_every_page_with_the_resolved_total()
    {
        var result = ContractSample.RenderWithInMemoryAdapter();
        var text = Decode(result);

        text.Should().Contain("Page 2 of " + result.Value.PageCount);
        text.Should().Contain("Page " + result.Value.PageCount + " of " + result.Value.PageCount);
    }
}
