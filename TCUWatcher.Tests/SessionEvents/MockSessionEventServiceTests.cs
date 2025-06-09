using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using AutoMapper;
using TCUWatcher.Application.SessionEvents;
using TCUWatcher.Application.SessionEvents.DTOs;
using TCUWatcher.Domain.Services;
using TCUWatcher.Domain.Repositories;
using TCUWatcher.Domain.Entities;
using TCUWatcher.Domain.Common;

namespace TCUWatcher.Test.SessionEvents
{
    public class MockSessionEventServiceTests
    {
        [Fact]
        public async Task CreateWithUploadAsync_Should_Call_SyncService()
        {
            // Arrange
            var mockRepo = new Mock<ISessionEventRepository>();
            var mockStorage = new Mock<IStorageService>();
            var mockLogger = new Mock<ILogger<SessionEventService>>();
            var mockMapper = new Mock<IMapper>();
            var mockSyncService = new Mock<ISessionSyncService>();

            var dummyDto = new CreateSessionEventWithUploadDto
            {
                Title = "Teste",
                StorageKey = "arquivo.mp4"
            };

            mockMapper.Setup(m => m.Map<SessionEventDto>(It.IsAny<SessionEvent>())).Returns(new SessionEventDto { Title = "Teste" });

            var service = new SessionEventService(
                mockRepo.Object,
                mockStorage.Object,
                mockLogger.Object,
                mockMapper.Object,
                mockSyncService.Object
            );

            // Act
            var result = await service.CreateWithUploadAsync(dummyDto);

            // Assert
            mockSyncService.Verify(s => s.SyncAsync(default), Times.Once);
        }
    }
}
