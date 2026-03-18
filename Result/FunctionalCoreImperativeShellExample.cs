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
        // FUNCTIONAL CORE: Pure business logic
        // ============================================================

        /// <summary>
        /// Domain object: Encapsulates a pure decision.
        /// Takes all required inputs as method parameters.
        /// Decide() is a pure function with no side effects.
        /// </summary>
        public class Decision
        {
            /// <summary>
            /// Pure execution: No I/O, no side effects.
            /// Returns a Result representing the outcome of the decision.
            /// </summary>
            public Result<Success, Problem> Decide(
                ValidatedCommand validatedCommand,
                Event[] history,
                Authorized authorized)
            {
                if (validatedCommand is null || history is null || authorized is null)
                {
                    return Result<Success, Problem>.Failure(new Problem());
                }

                // add pure business logic here
                
                var emitted = new Event[0];

                return Result<Success, Problem>.Success(new Success
                {
                    Emitted = emitted
                });
            }
        }

        // ============================================================
        // IMPERATIVE SHELL: Handles I/O and orchestrates effects
        // ============================================================

        /// <summary>
        /// Shell: Manages the imperative boundary.
        /// Responsible for validation, authorization, loading, persisting.
        /// </summary>
        public class CommandHandler
        {
            private readonly ValidationService? _validationService;
            private readonly AuthorizationService? _authorizationService;
            private readonly EventStore? _eventStore;

            // Constructor injection: explicit dependencies wired at the boundary
            public CommandHandler(
                ValidationService validationService,
                AuthorizationService authorizationService,
                EventStore eventStore)
            {
                this._validationService = validationService;
                this._authorizationService = authorizationService;
                this._eventStore = eventStore;
            }

            /// <summary>
            /// Shell entry point: Handles the command from outside.
            /// Orchestrates I/O using LINQ query syntax, then delegates to the pure core.
            /// Failures short-circuit automatically; no explicit error handling needed.
            /// </summary>
            public async Task<Result<Success, Problem>> HandleAsync(Command command)
            {
                if (command is null || this._validationService is null || this._authorizationService is null || this._eventStore is null)
                {
                    return Result<Success, Problem>.Failure(new Problem());
                }

                return await
                    (from validatedCommand in this._validationService.ValidateAsync(command)
                    from authorized in this._authorizationService.AuthorizeAsync(new User())
                    from history in this._eventStore.LoadEventsAsync(validatedCommand)
                    let decision = new Decision()
                    from success in decision.Decide(validatedCommand, history, authorized).ToAsync()
                    from _ in this._eventStore.PersistEventsAsync(success.Emitted)
                    select success);
            }
        }

        // ============================================================
        // SHELL DEPENDENCIES: Isolated I/O interfaces
        // ============================================================

        public class ValidationService
        {
            public virtual async Task<Result<ValidatedCommand, Problem>> ValidateAsync(Command command)
            {
                // Could be I/O, external API, database query, etc.
                return await Task.FromResult(Result<ValidatedCommand, Problem>.Success(new ValidatedCommand()));
            }
        }

        public class AuthorizationService
        {
            public async Task<Result<Authorized, Problem>> AuthorizeAsync(User user)
            {
                // Could be I/O, external API, database query, etc.
                return await Task.FromResult(Result<Authorized, Problem>.Success(new Authorized()));
            }
        }

        public class EventStore
        {
            public async Task<Result<Event[], Problem>> LoadEventsAsync(ValidatedCommand command)
            {
                // I/O: database query
                return await Task.FromResult(Result<Event[], Problem>.Success(new Event[0]));
            }

            public async Task<Result<Unit, Problem>> PersistEventsAsync(Event[] events)
            {
                // I/O: database write
                return await Task.FromResult(Result<Unit, Problem>.Success(new Unit()));
            }
        }

        public readonly struct Unit { }

        // ============================================================
        // TESTS: Show the separation
        // ============================================================

        [Fact]
        public void FunctionalCore_IsPure_AndFullyTestable()
        {
            // No I/O, no mocking, no async—just pure logic
            var decision = new Decision();

            var result = decision.Decide(
                new ValidatedCommand(),
                new Event[0],
                new Authorized());

            result.IsSuccess.Should().BeTrue();
            result.Fold(_ => false, s => s is Success).Should().BeTrue();
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

        // ============================================================
        // TEST DOUBLE: Custom shell dependency for testing
        // ============================================================

        private class FailingValidationService : ValidationService
        {
            public override async Task<Result<ValidatedCommand, Problem>> ValidateAsync(Command command)
            {
                return await Task.FromResult(Result<ValidatedCommand, Problem>.Failure(new Problem()));
            }
        }
    }
}
