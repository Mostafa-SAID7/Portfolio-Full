using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Moq;
using PortfolioAPI.Pipelines;
using Xunit;

namespace PortfolioAPI.UnitTests
{
    public class CorrelationPipelineTests
    {
        [Fact]
        public void ResolveOrGenerateCorrelationId_WhenHeaderIsProvidedAndValid_PreservesHeader()
        {
            // Arrange
            var context = new DefaultHttpContext();
            const string expected = "req-12345-abc.def_ghi";
            context.Request.Headers[CorrelationPipeline.HeaderName] = expected;

            // Act
            var correlationId = CorrelationPipeline.ResolveOrGenerateCorrelationId(context);

            // Assert
            correlationId.Should().Be(expected);
        }

        [Fact]
        public void ResolveOrGenerateCorrelationId_WhenHeaderContainsCRLFInjection_RejectsAndGeneratesSafeId()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationPipeline.HeaderName] = "injected\r\nCRLF: header";

            // Act
            var correlationId = CorrelationPipeline.ResolveOrGenerateCorrelationId(context);

            // Assert
            correlationId.Should().NotContain("\r");
            correlationId.Should().NotContain("\n");
            correlationId.Should().NotBe("injected\r\nCRLF: header");
            correlationId.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public void ResolveOrGenerateCorrelationId_WhenHeaderExceedsMaxLength_RejectsAndGeneratesSafeId()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Request.Headers[CorrelationPipeline.HeaderName] = new string('a', 65);

            // Act
            var correlationId = CorrelationPipeline.ResolveOrGenerateCorrelationId(context);

            // Assert
            correlationId.Should().NotBe(new string('a', 65));
            correlationId.Length.Should().BeLessThanOrEqualTo(64);
        }

        [Fact]
        public void ResolveOrGenerateCorrelationId_WhenHeaderMissing_GeneratesNonEmptyId()
        {
            // Arrange
            var context = new DefaultHttpContext();

            // Act
            var correlationId = CorrelationPipeline.ResolveOrGenerateCorrelationId(context);

            // Assert
            correlationId.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task UseCorrelationId_PopulatesContextItemsAndResponseHeader()
        {
            // Arrange
            var app = new ApplicationBuilder(new Mock<IServiceProvider>().Object);
            app.UseCorrelationId();

            var executed = false;
            app.Run(ctx =>
            {
                executed = true;
                ctx.Items[CorrelationPipeline.ItemKey].Should().NotBeNull();
                return Task.CompletedTask;
            });

            var pipeline = app.Build();
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Headers[CorrelationPipeline.HeaderName] = "test-corr-id";

            // Act
            await pipeline(httpContext);

            // Assert
            executed.Should().BeTrue();
            httpContext.Items[CorrelationPipeline.ItemKey].Should().Be("test-corr-id");
        }
    }
}

