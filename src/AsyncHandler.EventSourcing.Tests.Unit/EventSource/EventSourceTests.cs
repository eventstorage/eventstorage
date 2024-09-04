using System.Linq.Expressions;
using AsyncHandler.EventSourcing.Configuration;
using AsyncHandler.EventSourcing.Repositories;
using FluentAssertions;
using Moq;
using Moq.Language.Flow;

namespace AsyncHandler.EventSourcing.Tests.Unit;

public class EventSourceTests
{
    private readonly Mock<IRepository<AggregateRoot>> _mockRepo = new();
    private readonly IReturnsThrows<IRepository<AggregateRoot>,Task<AggregateRoot>> _createOrRestoreSetup;
    private readonly IReturnsThrows<IRepository<AggregateRoot>,Task<AggregateRoot>> _createOrRestoreSetup1;
    private readonly IReturnsThrows<IRepository<AggregateRoot>,Task<AggregateRoot>> _createOrRestoreSetup2;
    public EventSourceTests()
    {
        _createOrRestoreSetup = _mockRepo.Setup(x => x.AzureSqlClient.CreateOrRestore(It.IsAny<long>()));
        _createOrRestoreSetup1 = _mockRepo.Setup(x => x.PostgreSqlClient.CreateOrRestore(It.IsAny<long>()));
        _createOrRestoreSetup2 = _mockRepo.Setup(x => x.SqlServerClient.CreateOrRestore(It.IsAny<long>()));
    }
    public EventSource<AggregateRoot> BuildSut(EventSources source) =>
        new (_mockRepo.Object, source);
    
    [Theory]
    [InlineData(EventSources.AzureSql)]
    [InlineData(EventSources.PostgresSql)]
    [InlineData(EventSources.SqlServer)]
    public async Task GivenSourceToCreate_ShouldInvokeResponsibleClient(EventSources source)
    {
        // given
        _createOrRestoreSetup.ReturnsAsync(It.IsAny<AggregateRoot>());
        _createOrRestoreSetup1.ReturnsAsync(It.IsAny<AggregateRoot>());
        _createOrRestoreSetup2.ReturnsAsync(It.IsAny<AggregateRoot>());

        // when
        var sut = BuildSut(source);
        await sut.CreateOrRestore(It.IsAny<long>());

        // then
        Expression<Func<IRepository<AggregateRoot>,Task<AggregateRoot>>> exp = source switch
        {
            EventSources.AzureSql => x => x.AzureSqlClient.CreateOrRestore(It.IsAny<long>()),
            EventSources.PostgresSql => x => x.PostgreSqlClient.CreateOrRestore(It.IsAny<long>()),
            EventSources.SqlServer => x => x.SqlServerClient.CreateOrRestore(It.IsAny<long>()),
            _ => x => x.AzureSqlClient.CreateOrRestore(It.IsAny<long>()),
        };
        
        _mockRepo.Verify(exp, Times.Once());
    }
    [Fact]
    public async Task WhenCreateOrRestore_ShouldReturnExpectedAggregate()
    {
        // given
        var sourceId = 1;
        var expected = new OrderAggregate(sourceId);
        _createOrRestoreSetup.ReturnsAsync(expected);

        // when
        var sut = BuildSut(It.IsAny<EventSources>());
        var aggregate = await sut.CreateOrRestore(It.IsAny<long>());

        // then
        Assert.Equal(expected, aggregate);
        aggregate.SourceId.Should().Be(expected.SourceId);
    }
}