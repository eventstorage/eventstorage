// using System.Linq.Expressions;
// using EventStorage.AggregateRoot;
// using EventStorage.Configurations;
// using EventStorage.Repositories;
// using EventStorage.Repositories.PostgreSql;
// using EventStorage.Repositories.SqlServer;
// using EventStorage.Unit.Tests.AggregateRoot;
// using FluentAssertions;
// using Moq;
// using Moq.Language.Flow;

// namespace EventStorage.Unit.Tests.EventStorage;

// public class EventStorageTests
// {
//     private static readonly Mock<PostgreSqlClient<EventSource<long>>> _mockPostgres = new();
//     private static readonly Mock<SqlServerClient<EventSource<long>>> _mockMssql = new();
//     private readonly IReturnsThrows<PostgreSqlClient<EventSource<long>>, Task<EventSource<long>>> _postgresSetup;
//     private readonly IReturnsThrows<SqlServerClient<EventSource<long>>, Task<EventSource<long>>> _mssqlSetup;
//     public EventStorageTests()
//     {
//         _postgresSetup = _mockPostgres.Setup(x => x.CreateOrRestore(It.IsAny<string>()));
//         _mssqlSetup = _mockMssql.Setup(x => x.CreateOrRestore(It.IsAny<string>()));
//     }
//     static IEventStorage<EventSource<long>> BuildSut(EventStore source) => source switch
//     {
//         EventStore.PostgresSql => _mockPostgres.Object,
//         _ => _mockMssql.Object
//     };

//     [Theory]
//     [InlineData(EventStore.AzureSql)]
//     [InlineData(EventStore.PostgresSql)]
//     [InlineData(EventStore.SqlServer)]
//     public async Task GivenSourceToCreate_ShouldInvokeResponsibleClient(EventStore source)
//     {
//         // given
//         _postgresSetup.ReturnsAsync(It.IsAny<EventSource<long>>());
//         _mssqlSetup.ReturnsAsync(It.IsAny<EventSource<long>>());

//         // when
//         var sut = BuildSut(source);
//         await sut.CreateOrRestore(It.IsAny<string>());

//         // then
//         Expression<Func<PostgreSqlClient<EventSource<long>>, SqlServerClient<EventSource<long>>,
//             Task<EventSource<long>>>> exp = source switch
//         {
//             EventStore.AzureSql => (p, s) => p.CreateOrRestore(It.IsAny<string>()),
//             EventStore.PostgresSql => (p, s) => p.CreateOrRestore(It.IsAny<string>()),
//             EventStore.SqlServer => (p, s) => p.CreateOrRestore(It.IsAny<string>()),
//             _ => (p, s) => p.CreateOrRestore(It.IsAny<string>()),
//         };

//         _mockPostgres.Verify(x => exp, Times.Once());
//         _mockPostgres.Verify(x => x.CreateOrRestore(It.IsAny<string>()), Times.Once());
//     }
//     [Fact]
//     public async Task WhenCreateOrRestore_ShouldReturnExpectedAggregate()
//     {
//         // given
//         var expected = new OrderAggregate();
//         _createOrRestoreSetup.ReturnsAsync(expected);
//         _createOrRestoreSetup1.ReturnsAsync(expected);

//         // when
//         var sut = BuildSut(It.IsAny<EventStore>());
//         var aggregate = await sut.CreateOrRestore(It.IsAny<string>());

//         // then
//         Assert.Equal(expected, aggregate);
//         aggregate.SourceId.Should().Be(expected.SourceId);
//     }
// }