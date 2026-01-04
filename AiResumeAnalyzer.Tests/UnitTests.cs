using AiResumeAnalyzer.Api.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AiResumeAnalyzer.Tests
{
    public class UnitTests
    {
        private readonly ServiceProvider _serviceProvider;

        public UnitTests()
        {
            var services = new ServiceCollection();
            services.AddScoped<TextInputExtractor>();
            services.AddScoped<UploadFileExtractor>();
            services.AddScoped<Analyzer>();
            services.AddScoped<ITextInputExtractor>(provider =>
                provider.GetRequiredService<TextInputExtractor>()
            );
            services.AddScoped<IUploadFileExtractor>(provider =>
                provider.GetRequiredService<UploadFileExtractor>()
            );
            services.AddScoped<IAnalyzer>(provider => provider.GetRequiredService<Analyzer>());

            _serviceProvider = services.BuildServiceProvider();
        }

        [Fact]
        public void Services_AreRegisteredCorrectly()
        {
            // Arrange
            var textExtractor = _serviceProvider.GetService<ITextInputExtractor>();
            var uploadExtractor = _serviceProvider.GetService<IUploadFileExtractor>();
            var analyzer = _serviceProvider.GetService<IAnalyzer>();

            // Assert
            Assert.NotNull(textExtractor);
            Assert.NotNull(uploadExtractor);
            Assert.NotNull(analyzer);
        }
    }
}
