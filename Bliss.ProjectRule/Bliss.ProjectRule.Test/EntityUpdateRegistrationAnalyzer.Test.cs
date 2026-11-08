using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<Bliss.ProjectRule.Analyzer.EntityUpdateRegistrationAnalyzer, Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace TestProject1;

public class EntityUpdateRegistrationAnalyzerTests
{
    [Test]
    public async Task GameplayWorldRegistrationIsLimitedToGameplayEntityBridge()
    {
        var sourceCode = @"
namespace Bliss
{
    public class Entity
    {
        protected virtual void RegisterToSystem()
        {
        }
    }

    public class GameplayWorld
    {
        public void Register(Entity entity)
        {
        }
    }

    public class GamePlayEntity : Entity
    {
        protected override void RegisterToSystem()
        {
            var gameplayWorld = new GameplayWorld();
            gameplayWorld.Register(this);
        }
    }

    public class UnapprovedCaller
    {
        public void Register(GameplayWorld gameplayWorld, Entity entity)
        {
            {|#0:gameplayWorld.Register|}(entity);
        }
    }
}
";

        var test = new Verify
        {
            TestCode = sourceCode,
        };
        test.ExpectedDiagnostics.Add(
            new Microsoft.CodeAnalysis.Testing.DiagnosticResult(Bliss.ProjectRule.Analyzer.DiagnosticIds.ENTITY_UPDATE_REGISTRATION_RULE_ID, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0));

        await test.RunAsync();
    }

    [Test]
    public async Task GameplayWorldUpdaterRegistrationIsLimitedToGameplayWorldBridge()
    {
        var sourceCode = @"
namespace Bliss
{
    public class Entity
    {
    }

    public class GameplayWorldUpdater
    {
        public void Add(Entity entity)
        {
        }
    }

    public class GameplayWorld
    {
        private readonly GameplayWorldUpdater updater = new GameplayWorldUpdater();

        public void Register(Entity entity)
        {
            updater.Add(entity);
        }
    }

    public class UnapprovedCaller
    {
        public void Register(GameplayWorldUpdater updater, Entity entity)
        {
            {|#0:updater.Add|}(entity);
        }
    }
}
";

        var test = new Verify
        {
            TestCode = sourceCode,
        };
        test.ExpectedDiagnostics.Add(
            new Microsoft.CodeAnalysis.Testing.DiagnosticResult(Bliss.ProjectRule.Analyzer.DiagnosticIds.ENTITY_UPDATE_REGISTRATION_RULE_ID, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0));

        await test.RunAsync();
    }

    [Test]
    public async Task EntityRegistrationIsLimitedToLogicSceneAndOverrideBaseCalls()
    {
        var sourceCode = @"
namespace Bliss
{
    public class Entity
    {
        public object LogicScene
        {
            set
            {
                RegisterToSystem();
                var otherEntity = new Entity();
                {|#1:otherEntity.RegisterToSystem|}();
            }
        }

        protected virtual void RegisterToSystem()
        {
        }
    }

    public class DerivedEntity : Entity
    {
        protected override void RegisterToSystem()
        {
            base.RegisterToSystem();
        }

        public void RegisterAgain()
        {
            {|#0:RegisterToSystem|}();
        }
    }
}
";

        var test = new Verify
        {
            TestCode = sourceCode,
        };
        test.ExpectedDiagnostics.Add(
            new Microsoft.CodeAnalysis.Testing.DiagnosticResult(Bliss.ProjectRule.Analyzer.DiagnosticIds.ENTITY_UPDATE_REGISTRATION_RULE_ID, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0));
        test.ExpectedDiagnostics.Add(
            new Microsoft.CodeAnalysis.Testing.DiagnosticResult(Bliss.ProjectRule.Analyzer.DiagnosticIds.ENTITY_UPDATE_REGISTRATION_RULE_ID, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(1));

        await test.RunAsync();
    }

    [Test]
    public async Task EntitySystemRegistrationIsRestrictedToEntityRegistrationBridge()
    {
        var sourceCode = @"
namespace Bliss
{
    public class EntitySystem
    {
        public void RegisterSystem(Entity entity)
        {
        }
    }

    public class Entity
    {
        private readonly EntitySystem entitySystem = new EntitySystem();

        public object LogicScene
        {
            set
            {
                RegisterToSystem();
            }
        }

        protected virtual void RegisterToSystem()
        {
            entitySystem.RegisterSystem(this);
        }
    }

    public class UnapprovedCaller
    {
        public void Register(EntitySystem entitySystem, Entity entity)
        {
            {|#0:entitySystem.RegisterSystem|}(entity);
        }
    }
}
";

        var test = new Verify
        {
            TestCode = sourceCode,
        };
        test.ExpectedDiagnostics.Add(
            new Microsoft.CodeAnalysis.Testing.DiagnosticResult(Bliss.ProjectRule.Analyzer.DiagnosticIds.ENTITY_UPDATE_REGISTRATION_RULE_ID, Microsoft.CodeAnalysis.DiagnosticSeverity.Error)
                .WithLocation(0));

        await test.RunAsync();
    }
}
