// using AsyncHandler.EventSourcing.Events;
// using AsyncHandler.EventSourcing.Repositories;

// namespace AsyncHandler.EventSourcing;

// public class TestAggregate(Guid sourceid) : AggregateRoot<TId>(sourceid)
// {
//     protected override void Apply(SourcedEvent e)
//     {
//         throw new NotImplementedException();
//     }
// }
// public class Init(IEventSource<TestAggregate,Guid> service)
// {
//     public async void InitAggregate()
//     {
//         var TestAggregate = new TestAggregate(1);
//         await service.CreateOrRestore(Guid.NewGuid());
//     }
// }

// public abstract class Typed<TId> where TId : notnull // that allows only long or Guid
// {

// }