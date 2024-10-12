using System.Linq.Expressions;
using EventStorage.AggregateRoot;
using EventStorage.Configurations;
using EventStorage.Repositories;
using EventStorage.Unit.Tests.AggregateRoot;
using FluentAssertions;
using Moq;
using Moq.Language.Flow;

namespace EventStorage.Unit.Tests.EventStorage;

public class EventSourceTests
{
    private readonly Mock<IRepository<EventSource<long>>> _mockRepo = new();
    private readonly IReturnsThrows<IRepository<EventSource<long>>, Task<EventSource<long>>> _createOrRestoreSetup;
    private readonly IReturnsThrows<IRepository<EventSource<long>>, Task<EventSource<long>>> _createOrRestoreSetup1;
    public EventSourceTests()
    {
        _createOrRestoreSetup = _mockRepo.Setup(x => x.SqlServerClient.CreateOrRestore(It.IsAny<string>()));
        _createOrRestoreSetup1 = _mockRepo.Setup(x => x.PostgreSqlClient.CreateOrRestore(It.IsAny<string>()));
    }
    public EventStorage<EventSource<long>> BuildSut(EventSources source) =>
        new(_mockRepo.Object, source);

    [Theory]
    [InlineData(EventSources.AzureSql)]
    [InlineData(EventSources.PostgresSql)]
    [InlineData(EventSources.SqlServer)]
    public async Task GivenSourceToCreate_ShouldInvokeResponsibleClient(EventSources source)
    {
        // given
        _createOrRestoreSetup.ReturnsAsync(It.IsAny<EventSource<long>>());
        _createOrRestoreSetup1.ReturnsAsync(It.IsAny<EventSource<long>>());

        // when
        var sut = BuildSut(source);
        await sut.CreateOrRestore(It.IsAny<string>());

        // then
        Expression<Func<IRepository<EventSource<long>>, Task<EventSource<long>>>> exp = source switch
        {
            EventSources.AzureSql => x => x.SqlServerClient.CreateOrRestore(It.IsAny<string>()),
            EventSources.PostgresSql => x => x.PostgreSqlClient.CreateOrRestore(It.IsAny<string>()),
            EventSources.SqlServer => x => x.SqlServerClient.CreateOrRestore(It.IsAny<string>()),
            _ => x => x.SqlServerClient.CreateOrRestore(It.IsAny<string>()),
        };

        _mockRepo.Verify(exp, Times.Once());
    }
    [Fact]
    public async Task WhenCreateOrRestore_ShouldReturnExpectedAggregate()
    {
        // given
        var expected = new OrderAggregate();
        _createOrRestoreSetup.ReturnsAsync(expected);
        _createOrRestoreSetup1.ReturnsAsync(expected);

        // when
        var sut = BuildSut(It.IsAny<EventSources>());
        var aggregate = await sut.CreateOrRestore(It.IsAny<string>());

        // then
        Assert.Equal(expected, aggregate);
        aggregate.SourceId.Should().Be(expected.SourceId);
    }
}