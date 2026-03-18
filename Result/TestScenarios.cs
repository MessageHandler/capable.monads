using FluentAssertions;

namespace Result
{
    public class TestScenarios
    {
        public Task<Result<Success, Problem>> HandleCommand(
            Command command,
            Func<Command, Result<ValidatedCommand, Problem>> validate,
            Func<User, Result<Authorized, Problem>> authorize,
            Func<ValidatedCommand, Task<Result<Event[], Problem>>> load,
            Func<Event[], ValidatedCommand, Authorized, AggregateRoot> action,
            Func<AggregateRoot, Task<Result<Event[], Problem>>> persist)
        {
            return
                from validatedCommand in validate(command)
                from authorized in authorize(new User())
                from history in load(validatedCommand)
                let aggregateRoot = action(history, validatedCommand, authorized)
                from emitted in persist(aggregateRoot)
                select new Success
                {                    
                    Emitted = emitted
                };
        }

        [Fact]
        public async Task CanSucceed()
        {
            var result = await HandleCommand(new PlaceOrder(),
                request => new ValidatedCommand(),
                command => new Authorized(),
                async command => new Event[0],
                (events, command, authorized) => new Booking(),
                async (booking) => new Event[0]);

            result.Fold(
                p => p.Should().BeNull(),
                s => s.Should().BeOfType<Success>()
            );
        }

        [Fact]
        public async Task CanFailOnValidation()
        {
            var result = await HandleCommand(new PlaceOrder(),
                request => new Problem(),
                command => new Authorized(),
                async command => new Event[0],
                (events, command, authorized) => new Booking(),
                async (booking) => new Event[0]);

            result.Fold(
               p => p.Should().BeOfType<Problem>(),
               s => s.Should().BeNull()
           );
        }

        [Fact]
        public async Task CanFailOnAuthorization()
        {
            var result = await HandleCommand(new PlaceOrder(),
                request => new ValidatedCommand(),
                command => new Problem(),
                async command => new Event[0],
                (events, command, authorized) => new Booking(),
                async ( booking) => new Event[0]);

            result.Fold(
               p => p.Should().BeOfType<Problem>(),
               s => s.Should().BeNull()
           );
        }

        [Fact]
        public async Task CanFailOnLoading()
        {
            var result = await HandleCommand(new PlaceOrder(),
                request => new ValidatedCommand(),
                command => new Authorized(),
                async command => new Problem(),
                (events, command, authorized) => new Booking(),
                async (booking) => new Event[0]);

            result.Fold(
               p => p.Should().BeOfType<Problem>(),
               s => s.Should().BeNull()
           );
        }

        [Fact]
        public async Task CanFailOnPersistance()
        {
            var result = await HandleCommand(new PlaceOrder(),
                request => new ValidatedCommand(),
                command => new Authorized(),
                async command => new Event[0],
                (events, command, authorized) => new Booking(),
                async (booking) => new Problem());

            result.Fold(
               p => p.Should().BeOfType<Problem>(),
               s => s.Should().BeNull()
           );
        }       
    }

    public record PlaceOrder : Command { }
    public record Command { }
    public record ValidatedCommand { }
    public record Problem { }
    public record User { }
    public record Authorized { }
    public record Event { }
    public record Booking : AggregateRoot { }
    public record AggregateRoot { }
    public record Success
    {
        public required Event[] Emitted { get; init; }
    }
}