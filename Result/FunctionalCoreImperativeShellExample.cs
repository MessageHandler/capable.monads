using FluentAssertions;

namespace Result
{
    /// <summary>
    /// Demonstrates Functional Core / Imperative Shell architecture.
    /// 
    /// FUNCTIONAL CORE:
    /// - Pure functions with no side effects.
    /// - All dependencies are explicit parameters.
    /// - Deterministic and fully testable.
    /// 
    /// IMPERATIVE SHELL:
    /// - Handles I/O, async, and external dependencies.
    /// - Composes the core logic and orchestrates effects.
    /// - Wires everything at the application boundary.
    /// </summary>
    public class FunctionalCoreImperativeShellExample
    {
        // ============================================================
        // DOMAIN TYPES: Business language
        // ============================================================

        /// <summary>
        /// Strongly-typed domain errors. Speaks the business language.
        /// </summary>
        public abstract record DomainError
        {
            public record InsufficientInventory(int Requested, int Available) : DomainError;
            public record UnauthorizedAction(string Reason) : DomainError;
            public record BusinessRuleViolation(string Rule) : DomainError;
        }

        /// <summary>
        /// Domain events: The output of core logic.
        /// </summary>
        public abstract record DomainEvent
        {
            public record OrderPlaced(string OrderId, int Quantity) : DomainEvent;
            public record InventoryReserved(int Quantity) : DomainEvent;
        }

        /// <summary>
        /// Encapsulates all domain context in one place.
        /// Safer than scattering parameters, clearer intent.
        /// </summary>
        public class CommandContext
        {
            public ValidatedCommand Command { get; }
            public DomainEvent[] History { get; }
            public Authorized User { get; }

            public CommandContext(ValidatedCommand command, DomainEvent[] history, Authorized user)
            {
                Command = command;
                History = history;
                User = user;
            }
        }

        // ============================================================
        // FUNCTIONAL CORE: Pure business logic
        // ============================================================

        /// <summary>
        /// Domain decision maker. Encapsulates pure business rules.
        /// Core invariants:
        /// - An order must have a valid quantity
        /// - Only authorized users can place orders
        /// - All outcomes are domain events
        /// - All failures are domain errors
        /// </summary>
        public class OrderDecision
        {
            /// <summary>
            /// Execute: Pure function. No I/O, no side effects.
            /// Returns domain events on success, domain errors on failure.
            /// </summary>
            public Result<DomainEvent[], DomainError> Execute(CommandContext context)
            {
                return from _ in ValidateBusinessRules(context)
                       from events in ComputeStateChanges(context)
                       select events;
            }

            private Result<Unit, DomainError> ValidateBusinessRules(CommandContext context)
            {
                // Domain validation: part of the core, not deferred to shell
                if (context.Command.Quantity <= 0)
                    return Result<Unit, DomainError>.Failure(
                        new DomainError.BusinessRuleViolation("Orders must have positive quantity"));

                return Result<Unit, DomainError>.Success(new Unit());
            }

            private Result<DomainEvent[], DomainError> ComputeStateChanges(CommandContext context)
            {
                // Business logic: what events should be emitted?
                var events = new DomainEvent[]
                {
                    new DomainEvent.OrderPlaced(context.Command.OrderId, context.Command.Quantity),
                    new DomainEvent.InventoryReserved(context.Command.Quantity)
                };

                return Result<DomainEvent[], DomainError>.Success(events);
            }
        }

        // ============================================================
        // IMPERATIVE SHELL: Handles I/O and orchestrates effects
        // ============================================================

        /// <summary>
        /// Shell: Manages the imperative boundary.
        /// Responsible for validation, authorization, loading, persisting.
        /// Delegates domain decisions to the core.
        /// </summary>
        public class CommandHandler
        {
            private readonly ValidationService _validationService;
            private readonly AuthorizationService _authorizationService;
            private readonly EventStore _eventStore;
            private readonly OrderDecision _decision;

            // Constructor injection: explicit dependencies wired at the boundary
            public CommandHandler(
                ValidationService validationService,
                AuthorizationService authorizationService,
                EventStore eventStore)
            {
                this._validationService = validationService;
                this._authorizationService = authorizationService;
                this._eventStore = eventStore;
                this._decision = new OrderDecision(); // Core logic is instantiated directly since it has no dependencies
            }

            /// <summary>
            /// Shell entry point: Handles the command from outside.
            /// Orchestrates I/O, then delegates to the pure core.
            /// Failures short-circuit automatically; no explicit error handling needed.
            /// </summary>
            public async Task<Result<DomainEvent[], DomainError>> HandleAsync(Command command)
            {
                return await
                    (from validatedCommand in this._validationService.ValidateAsync(command)
                    from authorized in this._authorizationService.AuthorizeAsync(new User())
                    from history in this._eventStore.LoadEventsAsync(validatedCommand)
                    from events in this._decision.Execute(new CommandContext(validatedCommand, history, authorized)).ToAsync()
                    from _ in this._eventStore.PersistEventsAsync(events)
                    select events);
            }
        }

        // ============================================================
        // SHELL DEPENDENCIES: Isolated I/O interfaces
        // ============================================================

        public class ValidationService
        {
            public virtual async Task<Result<ValidatedCommand, DomainError>> ValidateAsync(Command command)
            {
                // Could be I/O, external API, database query, etc.
                return await Task.FromResult(Result<ValidatedCommand, DomainError>.Success(new ValidatedCommand()));
            }
        }

        public class AuthorizationService
        {
            public async Task<Result<Authorized, DomainError>> AuthorizeAsync(User user)
            {
                // Could be I/O, external API, database query, etc.
                return await Task.FromResult(Result<Authorized, DomainError>.Success(new Authorized()));
            }
        }

        public class EventStore
        {
            public async Task<Result<DomainEvent[], DomainError>> LoadEventsAsync(ValidatedCommand command)
            {
                // I/O: database query
                return await Task.FromResult(Result<DomainEvent[], DomainError>.Success(new DomainEvent[0]));
            }

            public async Task<Result<Unit, DomainError>> PersistEventsAsync(DomainEvent[] events)
            {
                // I/O: database write
                return await Task.FromResult(Result<Unit, DomainError>.Success(new Unit()));
            }
        }

        public readonly struct Unit { }

        // ============================================================
        // TESTS: Show the separation and domain-driven approach
        // ============================================================

        [Fact]
        public void FunctionalCore_Validates_BusinessRules()
        {
            // Domain validation happens in the core, not the shell
            var decision = new OrderDecision();
            var invalidContext = new CommandContext(
                new ValidatedCommand { Quantity = 0 },
                new DomainEvent[0],
                new Authorized());

            var result = decision.Execute(invalidContext);

            result.IsFailure.Should().BeTrue();
            result.Fold(
                error => error is DomainError.BusinessRuleViolation,
                _ => false).Should().BeTrue();
        }

        [Fact]
        public void FunctionalCore_IsPure_AndFullyTestable()
        {
            // No I/O, no mocking, no async—just pure logic
            var decision = new OrderDecision();
            var context = new CommandContext(
                new ValidatedCommand { OrderId = "123", Quantity = 5 },
                new DomainEvent[0],
                new Authorized());

            var result = decision.Execute(context);

            result.IsSuccess.Should().BeTrue();
            result.Fold(
                _ => false,
                events => events.OfType<DomainEvent.OrderPlaced>().Any()).Should().BeTrue();
        }

        [Fact]
        public async Task ImperativeShell_OrchestratesEffects_AndDelegatesToCoreLogic()
        {
            // Shell: wired with explicit dependencies
            var shell = new CommandHandler(
                new ValidationService(),
                new AuthorizationService(),
                new EventStore());

            var result = await shell.HandleAsync(new PlaceOrder());

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ImperativeShell_ShortCircuits_OnValidationFailure()
        {
            // Custom ValidationService that always fails
            var failingValidation = new FailingValidationService();
            var shell = new CommandHandler(
                failingValidation,
                new AuthorizationService(),
                new EventStore());

            var result = await shell.HandleAsync(new PlaceOrder());

            result.IsFailure.Should().BeTrue();
        }

        [Fact]
        public void FunctionalCore_ReturnsStronglyTypedDomainErrors()
        {
            // Strongly-typed errors enable meaningful handling
            var decision = new OrderDecision();
            var invalidContext = new CommandContext(
                new ValidatedCommand { Quantity = -5 },
                new DomainEvent[0],
                new Authorized());

            var result = decision.Execute(invalidContext);

            result.Fold(
                error => error is DomainError.BusinessRuleViolation bv && bv.Rule.Contains("positive"),
                _ => false).Should().BeTrue();
        }

        // ============================================================
        // TEST DOUBLE: Custom shell dependency for testing
        // ============================================================

        private class FailingValidationService : ValidationService
        {
            public override async Task<Result<ValidatedCommand, DomainError>> ValidateAsync(Command command)
            {
                return await Task.FromResult(Result<ValidatedCommand, DomainError>.Failure(
                    new DomainError.BusinessRuleViolation("Validation failed")));
            }
        }

        // ============================================================
        // HELPER TYPES: Supporting test stubs
        // ============================================================

        public class ValidatedCommand
        {
            public string OrderId { get; set; } = "order-001";
            public int Quantity { get; set; } = 1;
        }

        public class Authorized { }
        public class User { }
        public abstract class Command { }
        public class PlaceOrder : Command { }
        public class Problem { }
    }
}
