using System.Text.RegularExpressions;
using ResumeChat.Rag.Models;

namespace ResumeChat.Rag.Pipeline;

/// <summary>
/// Appends corpus-aligned vocabulary to the query when conversational terms are detected.
/// Expansions are additive — the original query is preserved and terms are appended so the
/// embedding captures both the user's intent and the corpus vocabulary.
/// </summary>
public sealed partial class SynonymExpansionEnricher : IQueryEnricher
{
    public int Order => 10;

    // Maps a regex pattern (case-insensitive, conversational terms) to corpus-aligned expansions.
    // Patterns match the way users ask questions; expansions match how the corpus describes things.
    private static readonly (Regex Pattern, string Expansion)[] Expansions =
    [
        // Data pipeline / ETL concepts
        (BigData(), "ETL pipeline batch processing data pipeline large scale dataflow"),
        (DataPipeline(), "ETL pipeline dataflow batch processing direct-mail"),
        (Etl(), "ETL pipeline batch processing dataflow data pipeline"),
        (DataProcessing(), "ETL pipeline batch processing data pipeline dataflow"),
        (BatchProcessing(), "ETL pipeline dataflow batch processing large scale"),

        // Messaging / architecture
        (MessageBus(), "MassTransit RabbitMQ message-driven saga orchestration event-driven"),
        (Microservices(), "MassTransit distributed systems event-driven saga orchestration CQRS"),
        (EventDriven(), "MassTransit event-driven saga orchestration message-driven RabbitMQ"),
        (Cqrs(), "CQRS MassTransit vertical-slice command query separation"),

        // Database
        (Database(), "SQL Server Dapper Entity Framework database-design schema"),
        (Sql(), "SQL Server Dapper stored procedures views reporting"),
        (Orm(), "Entity Framework Dapper EF Core data access"),

        // Infrastructure / DevOps
        (Devops(), "Kubernetes Docker Helm GitOps ArgoCD CI/CD GitHub Actions"),
        (CloudInfra(), "Kubernetes Docker containers orchestration Talos Helm"),
        (Cicd(), "GitHub Actions CI/CD Docker Kubernetes Helm GitOps"),

        // Frontend
        (Frontend(), "Angular TypeScript RxJS SPA HTML5"),
        (React(), "Angular TypeScript SPA frontend web development"),

        // Performance
        (PerformanceWork(), "performance optimization SIMD zero-allocation benchmarking throughput"),
        (HighPerformance(), "performance optimization SIMD AVX2 Span zero-allocation BenchmarkDotNet"),

        // AI / ML
        (AiMl(), "AI machine learning LLM RAG embeddings Ollama semantic"),

        // Testing
        (Testing(), "testing NUnit integration-tests WebApplicationFactory Aspire test infrastructure"),

        // E-commerce / storefront
        (Ecommerce(), "KbStore KbClient ecommerce storefront product catalog inventory Angular domain-driven"),
        (Storefront(), "KbStore ecommerce storefront product catalog inventory checkout"),

        // Migration / modernization
        (LegacyMigration(), "database migration schema migration zero-downtime rename pattern dual deployment historical seeding"),
        (Modernization(), "migration refactoring architecture distributed systems event-driven MassTransit"),
        (Refactoring(), "migration refactoring vertical-slice CQRS architecture dependency-injection composition"),

        // Direct mail / Madera domain
        (DirectMail(), "direct-mail Madera Call-Trader mail file recipient campaign address normalization"),
        (MailCampaign(), "direct-mail campaign mail file recipient import processing"),
        (AddressProcessing(), "address normalization USPS CASS DPV verification scrub"),

        // ── Adjacent technology redirects ────────────────────────────────
        // Technologies Bryan doesn't use, nudged toward the nearest corpus equivalent.
        // The completion model's system prompt refuses to fabricate experience,
        // so surfacing related evidence lets it say "Bryan works in X, not Y."

        // JVM ecosystem → .NET
        (Jvm(), "csharp dotnet ASP.NET Core dependency-injection generics"),
        // Python ecosystem → .NET + data
        (Python(), "csharp dotnet Dapper data processing ETL pipeline"),
        // AWS services → homelab infra
        (Aws(), "Kubernetes Docker Helm infrastructure homelab GitOps ArgoCD"),
        // Azure managed services → self-hosted infra
        (Azure(), "Kubernetes Docker Helm ASP.NET Core infrastructure Aspire"),
        // GCP → same redirect as AWS/Azure
        (Gcp(), "Kubernetes Docker infrastructure homelab Helm"),
        // React/Vue/Svelte → Angular
        (NonAngularFrontend(), "Angular TypeScript RxJS SPA frontend web development"),
        // NoSQL → SQL Server + Qdrant
        (Nosql(), "SQL Server Qdrant database-design Dapper schema"),
        // Non-.NET ORMs → EF Core + Dapper
        (NonDotnetOrm(), "Entity Framework Dapper EF Core data access csharp"),
        // Kafka/SQS/NATS → MassTransit + RabbitMQ
        (NonRabbitMessaging(), "MassTransit RabbitMQ message-driven saga event-driven"),
        // Terraform/Pulumi/CloudFormation → GitOps
        (IaC(), "Helm GitOps ArgoCD Kubernetes Talos infrastructure"),
        // Go → Bryan has some Go (open-source evidence)
        (GoLang(), "Go open-source wyoming-rust kubernetes contributions"),
        // Rust → Bryan has some Rust
        (RustLang(), "Rust open-source wyoming-rust FastAddress performance"),
        // Node.js/Deno/Bun → .NET backend
        (NodeRuntime(), "csharp dotnet ASP.NET Core minimal-apis backend"),
        // GraphQL → REST/minimal API
        (GraphQl(), "ASP.NET Core minimal-apis endpoints REST API"),
        // Project management tools → Bryan's PM experience
        (ProjectManagementTool(), "Jira Trello GitHub Issues project management sprint planning agile scrum kanban"),
        (AgileProcess(), "sprint planning fibonacci sizing burndown velocity Jira Trello issue tracking"),
        // Weaknesses / limitations / honest assessment
        (WeaknessQuery(), "limitations tradeoffs shortcomings scope self-assessment gaps honest"),
    ];

    public ValueTask<ChatQuery> EnrichAsync(ChatQuery query, CancellationToken cancellationToken = default)
    {
        var message = query.ProcessedMessage;
        var appended = new List<string>();

        foreach (var (pattern, expansion) in Expansions)
        {
            if (pattern.IsMatch(message))
                appended.Add(expansion);
        }

        if (appended.Count == 0)
            return new(query);

        var enriched = query with
        {
            ProcessedMessage = $"{message} {string.Join(' ', appended)}"
        };

        return new(enriched);
    }

    // ── Data pipeline / ETL ──────────────────────────────────────────────────

    [GeneratedRegex(@"\bbig\s*data\b", RegexOptions.IgnoreCase)]
    private static partial Regex BigData();

    [GeneratedRegex(@"\bdata\s*pipelines?\b", RegexOptions.IgnoreCase)]
    private static partial Regex DataPipeline();

    [GeneratedRegex(@"\bETL\b", RegexOptions.IgnoreCase)]
    private static partial Regex Etl();

    [GeneratedRegex(@"\bdata\s*processing\b", RegexOptions.IgnoreCase)]
    private static partial Regex DataProcessing();

    [GeneratedRegex(@"\bbatch\s*processing\b", RegexOptions.IgnoreCase)]
    private static partial Regex BatchProcessing();

    // ── Messaging / architecture ─────────────────────────────────────────────

    [GeneratedRegex(@"\b(message\s*bus|messaging|message\s*queue|event\s*bus)\b", RegexOptions.IgnoreCase)]
    private static partial Regex MessageBus();

    [GeneratedRegex(@"\b(microservices?|distributed\s*systems?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Microservices();

    [GeneratedRegex(@"\bevent[\s-]*driven\b", RegexOptions.IgnoreCase)]
    private static partial Regex EventDriven();

    [GeneratedRegex(@"\bCQRS\b", RegexOptions.IgnoreCase)]
    private static partial Regex Cqrs();

    // ── Database ─────────────────────────────────────────────────────────────

    [GeneratedRegex(@"\b(database|DB)\s*(design|work|experience)?\b", RegexOptions.IgnoreCase)]
    private static partial Regex Database();

    [GeneratedRegex(@"\bSQL\b", RegexOptions.IgnoreCase)]
    private static partial Regex Sql();

    [GeneratedRegex(@"\b(ORM|entity\s*framework|EF\s*Core|dapper)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Orm();

    // ── Infrastructure / DevOps ──────────────────────────────────────────────

    [GeneratedRegex(@"\b(devops|infrastructure|deployment)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Devops();

    [GeneratedRegex(@"\b(cloud|kubernetes|k8s|docker|containers?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex CloudInfra();

    [GeneratedRegex(@"\b(CI/?CD|continuous\s*(integration|delivery|deployment)|build\s*pipeline)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Cicd();

    // ── Frontend ─────────────────────────────────────────────────────────────

    [GeneratedRegex(@"\b(frontend|front[\s-]*end|UI|user\s*interface)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Frontend();

    [GeneratedRegex(@"\b(react|angular|vue|SPA)\b", RegexOptions.IgnoreCase)]
    private static partial Regex React();

    // ── Performance ──────────────────────────────────────────────────────────

    [GeneratedRegex(@"\b(performance|optimization|optimizing|latency|throughput)\b", RegexOptions.IgnoreCase)]
    private static partial Regex PerformanceWork();

    [GeneratedRegex(@"\b(high[\s-]*performance|zero[\s-]*allocation|SIMD|low[\s-]*latency)\b", RegexOptions.IgnoreCase)]
    private static partial Regex HighPerformance();

    // ── AI / ML ──────────────────────────────────────────────────────────────

    [GeneratedRegex(@"\b(AI|artificial\s*intelligence|machine\s*learning|ML|LLM|RAG)\b", RegexOptions.IgnoreCase)]
    private static partial Regex AiMl();

    // ── Testing ──────────────────────────────────────────────────────────────

    [GeneratedRegex(@"\b(testing|test\s*strategy|unit\s*tests?|integration\s*tests?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Testing();

    // ── E-commerce / storefront ─────────────────────────────────────────────

    [GeneratedRegex(@"\b(e[\s-]*commerce|online\s*store|shopping\s*cart|checkout)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Ecommerce();

    [GeneratedRegex(@"\b(storefront|product\s*catalog|inventory\s*management)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Storefront();

    // ── Migration / modernization ─────────────────────────────────────────────

    [GeneratedRegex(@"\b(legacy|migration|migrating|migrate)\b", RegexOptions.IgnoreCase)]
    private static partial Regex LegacyMigration();

    [GeneratedRegex(@"\b(moderniz(e|ing|ation)|rewrit(e|ing)|greenfield)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Modernization();

    [GeneratedRegex(@"\b(refactor(ing)?|tech[\s-]*debt|code\s*quality)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Refactoring();

    // ── Direct mail / Madera domain ──────────────────────────────────────────

    [GeneratedRegex(@"\b(direct\s*mail|mailing|bulk\s*mail)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DirectMail();

    [GeneratedRegex(@"\b(mail\s*campaign|campaign\s*management|recipient)\b", RegexOptions.IgnoreCase)]
    private static partial Regex MailCampaign();

    [GeneratedRegex(@"\b(address\s*(processing|verification|normalization|matching)|CASS|USPS)\b", RegexOptions.IgnoreCase)]
    private static partial Regex AddressProcessing();

    // ── Adjacent technology redirects ────────────────────────────────────────

    [GeneratedRegex(@"\b(java|kotlin|spring\s*boot?|gradle|JVM|maven)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Jvm();

    [GeneratedRegex(@"\b(python|django|flask|fastapi|pandas|numpy)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Python();

    [GeneratedRegex(@"\b(AWS|amazon|lambda|S3|dynamo\s*DB|ECS|fargate|cloud\s*formation|CDK)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Aws();

    [GeneratedRegex(@"\b(azure|cosmos\s*DB|azure\s*functions?|AKS|app\s*service)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Azure();

    [GeneratedRegex(@"\b(GCP|google\s*cloud|cloud\s*run|bigquery|firestore)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Gcp();

    [GeneratedRegex(@"\b(react|next\.?js|vue|svelte|nuxt|remix|solid\.?js)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NonAngularFrontend();

    [GeneratedRegex(@"\b(mongo\s*DB|cassandra|redis|couch\s*DB|dynamo|neo4j|NoSQL)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Nosql();

    [GeneratedRegex(@"\b(hibernate|SQLAlchemy|prisma|sequelize|typeorm|drizzle|knex)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NonDotnetOrm();

    [GeneratedRegex(@"\b(kafka|SQS|SNS|NATS|pulsar|event\s*hubs?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NonRabbitMessaging();

    [GeneratedRegex(@"\b(terraform|pulumi|cloud\s*formation|CDK|ansible|chef|puppet)\b", RegexOptions.IgnoreCase)]
    private static partial Regex IaC();

    [GeneratedRegex(@"\b(golang|go\s+lang(uage)?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex GoLang();

    [GeneratedRegex(@"\b(rust|cargo|tokio|actix)\b", RegexOptions.IgnoreCase)]
    private static partial Regex RustLang();

    [GeneratedRegex(@"\b(node\.?js|deno|bun|express\.?js|nestjs|koa)\b", RegexOptions.IgnoreCase)]
    private static partial Regex NodeRuntime();

    [GeneratedRegex(@"\b(graphql|apollo|hasura|relay)\b", RegexOptions.IgnoreCase)]
    private static partial Regex GraphQl();

    // ── Project management / ticketing ───────────────────────────────────────

    [GeneratedRegex(@"\b(jira|trello|asana|linear|shortcut|monday|azure\s*devops|youtrack|clickup|basecamp)\b", RegexOptions.IgnoreCase)]
    private static partial Regex ProjectManagementTool();

    [GeneratedRegex(@"\b(agile|scrum|kanban|sprint|standup|retro(spective)?|backlog|story\s*points?|velocity|burndown|project\s*management|issue\s*track(ing|er)|ticketing)\b", RegexOptions.IgnoreCase)]
    private static partial Regex AgileProcess();

    // ── Self-assessment / limitations ────────────────────────────────────────

    [GeneratedRegex(@"\b(weakness(es)?|shortcoming|limitation|downside|flaw|gap|honest|candid|concern|risk|worry|sycophant|biased?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex WeaknessQuery();
}
