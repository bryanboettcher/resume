using ResumeChat.Rag.Models;
using ResumeChat.Rag.Pipeline;
using Shouldly;

namespace ResumeChat.Rag.Pipeline.Tests.Pipeline;

public abstract class SynonymExpansionEnricher_EnrichAsync
{
    protected SynonymExpansionEnricher Subject = null!;
    protected ChatQuery Input = null!;
    protected ChatQuery Result = null!;

    [SetUp]
    public async Task BaseSetUp()
    {
        Subject = new SynonymExpansionEnricher();
        Arrange();
        Result = await Subject.EnrichAsync(Input, CancellationToken.None);
    }

    protected abstract void Arrange();

    // --- Scenario classes ---

    public class When_query_contains_big_data : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "big data", ProcessedMessage = "big data" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("big data");

        [Test]
        public void It_should_append_etl_expansion_terms() =>
            Result.ProcessedMessage.ShouldContain("ETL pipeline batch processing");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("big data");
    }

    public class When_query_contains_message_bus : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "message bus", ProcessedMessage = "message bus" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("message bus");

        [Test]
        public void It_should_append_masstransit_expansion_terms() =>
            Result.ProcessedMessage.ShouldContain("MassTransit RabbitMQ message-driven saga orchestration event-driven");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("message bus");
    }

    public class When_query_contains_devops : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "devops experience", ProcessedMessage = "devops experience" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("devops experience");

        [Test]
        public void It_should_append_kubernetes_docker_expansion() =>
            Result.ProcessedMessage.ShouldContain("Kubernetes Docker Helm GitOps ArgoCD CI/CD GitHub Actions");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("devops experience");
    }

    public class When_query_contains_performance : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "performance work", ProcessedMessage = "performance work" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("performance work");

        [Test]
        public void It_should_append_optimization_simd_expansion() =>
            Result.ProcessedMessage.ShouldContain("performance optimization SIMD zero-allocation benchmarking throughput");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("performance work");
    }

    public class When_query_contains_direct_mail : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "direct mail campaigns", ProcessedMessage = "direct mail campaigns" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("direct mail campaigns");

        [Test]
        public void It_should_append_madera_mail_file_expansion() =>
            Result.ProcessedMessage.ShouldContain("direct-mail Madera Call-Trader mail file recipient campaign address normalization");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("direct mail campaigns");
    }

    public class When_query_contains_multiple_matches : SynonymExpansionEnricher_EnrichAsync
    {
        // "big data ETL data pipeline" hits BigData, Etl, AND DataPipeline patterns — all expansions are appended.
        // DataPipeline requires "data pipeline" as adjacent words; "big data ETL pipeline" does not satisfy it.
        protected override void Arrange()
        {
            Input = new ChatQuery
            {
                OriginalMessage = "big data ETL data pipeline",
                ProcessedMessage = "big data ETL data pipeline"
            };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("big data ETL data pipeline");

        [Test]
        public void It_should_append_big_data_expansion() =>
            Result.ProcessedMessage.ShouldContain("ETL pipeline batch processing data pipeline large scale dataflow");

        [Test]
        public void It_should_append_etl_expansion() =>
            Result.ProcessedMessage.ShouldContain("ETL pipeline batch processing dataflow data pipeline");

        [Test]
        public void It_should_append_data_pipeline_expansion() =>
            Result.ProcessedMessage.ShouldContain("ETL pipeline dataflow batch processing direct-mail");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("big data ETL data pipeline");
    }

    public class When_query_matches_nothing : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "hello world", ProcessedMessage = "hello world" };
        }

        [Test]
        public void It_should_return_query_unchanged() =>
            Result.ProcessedMessage.ShouldBe("hello world");

        [Test]
        public void It_should_return_same_query_instance() =>
            ReferenceEquals(Result, Input).ShouldBeTrue();

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("hello world");
    }

    public class When_match_is_case_insensitive : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "BIG DATA", ProcessedMessage = "BIG DATA" };
        }

        [Test]
        public void It_should_preserve_original_casing_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("BIG DATA");

        [Test]
        public void It_should_append_same_etl_expansion_as_lowercase() =>
            Result.ProcessedMessage.ShouldContain("ETL pipeline batch processing data pipeline large scale dataflow");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("BIG DATA");
    }

    public class When_query_contains_ai : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "AI experience", ProcessedMessage = "AI experience" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("AI experience");

        [Test]
        public void It_should_append_machine_learning_llm_expansion() =>
            Result.ProcessedMessage.ShouldContain("AI machine learning LLM RAG embeddings Ollama semantic");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("AI experience");
    }

    // ── Adjacent technology redirect scenarios ───────────────────────────────

    public class When_query_mentions_java : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "does Bryan know Java", ProcessedMessage = "does Bryan know Java" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("does Bryan know Java");

        [Test]
        public void It_should_append_dotnet_redirect_terms() =>
            Result.ProcessedMessage.ShouldContain("csharp dotnet ASP.NET");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("does Bryan know Java");
    }

    public class When_query_mentions_python : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "Python experience", ProcessedMessage = "Python experience" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("Python experience");

        [Test]
        public void It_should_append_dotnet_dapper_redirect_terms() =>
            Result.ProcessedMessage.ShouldContain("csharp dotnet Dapper");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("Python experience");
    }

    public class When_query_mentions_aws : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "AWS Lambda experience", ProcessedMessage = "AWS Lambda experience" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("AWS Lambda experience");

        [Test]
        public void It_should_append_kubernetes_homelab_redirect_terms() =>
            Result.ProcessedMessage.ShouldContain("Kubernetes Docker Helm");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("AWS Lambda experience");
    }

    public class When_query_mentions_react : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "React development", ProcessedMessage = "React development" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("React development");

        [Test]
        public void It_should_append_angular_typescript_redirect_terms() =>
            Result.ProcessedMessage.ShouldContain("Angular TypeScript");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("React development");
    }

    public class When_query_mentions_kafka : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "Kafka messaging", ProcessedMessage = "Kafka messaging" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("Kafka messaging");

        [Test]
        public void It_should_append_masstransit_rabbitmq_redirect_terms() =>
            Result.ProcessedMessage.ShouldContain("MassTransit RabbitMQ");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("Kafka messaging");
    }

    public class When_query_mentions_terraform : SynonymExpansionEnricher_EnrichAsync
    {
        // "Terraform infrastructure" matches IaC() on "terraform" AND Devops() on "infrastructure".
        // Both expansions are appended.
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "Terraform infrastructure", ProcessedMessage = "Terraform infrastructure" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("Terraform infrastructure");

        [Test]
        public void It_should_append_helm_gitops_iac_redirect_terms() =>
            Result.ProcessedMessage.ShouldContain("Helm GitOps ArgoCD");

        [Test]
        public void It_should_append_devops_expansion_terms_from_infrastructure_match() =>
            Result.ProcessedMessage.ShouldContain("Kubernetes Docker Helm GitOps ArgoCD CI/CD GitHub Actions");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("Terraform infrastructure");
    }

    public class When_query_mentions_mongodb : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "MongoDB experience", ProcessedMessage = "MongoDB experience" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("MongoDB experience");

        [Test]
        public void It_should_append_sql_server_qdrant_redirect_terms() =>
            Result.ProcessedMessage.ShouldContain("SQL Server Qdrant");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("MongoDB experience");
    }

    public class When_query_mentions_rust : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "Rust systems programming", ProcessedMessage = "Rust systems programming" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("Rust systems programming");

        [Test]
        public void It_should_append_rust_opensource_redirect_terms() =>
            Result.ProcessedMessage.ShouldContain("Rust open-source wyoming-rust");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("Rust systems programming");
    }

    public class When_query_mentions_golang : SynonymExpansionEnricher_EnrichAsync
    {
        // "golang microservices" matches GoLang() on "golang" AND Microservices() on "microservices".
        // Both expansions are appended.
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "golang microservices", ProcessedMessage = "golang microservices" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("golang microservices");

        [Test]
        public void It_should_append_go_opensource_redirect_terms() =>
            Result.ProcessedMessage.ShouldContain("Go open-source");

        [Test]
        public void It_should_append_microservices_expansion_terms() =>
            Result.ProcessedMessage.ShouldContain("MassTransit distributed systems event-driven saga orchestration CQRS");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("golang microservices");
    }

    public class When_query_mentions_graphql : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "GraphQL API", ProcessedMessage = "GraphQL API" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("GraphQL API");

        [Test]
        public void It_should_append_aspnet_minimal_apis_redirect_terms() =>
            Result.ProcessedMessage.ShouldContain("ASP.NET Core minimal-apis");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("GraphQL API");
    }

    public class When_query_mentions_nodejs : SynonymExpansionEnricher_EnrichAsync
    {
        protected override void Arrange()
        {
            Input = new ChatQuery { OriginalMessage = "Node.js backend", ProcessedMessage = "Node.js backend" };
        }

        [Test]
        public void It_should_preserve_original_message_at_start() =>
            Result.ProcessedMessage.ShouldStartWith("Node.js backend");

        [Test]
        public void It_should_append_dotnet_aspnet_redirect_terms() =>
            Result.ProcessedMessage.ShouldContain("csharp dotnet ASP.NET");

        [Test]
        public void It_should_not_modify_original_message() =>
            Result.OriginalMessage.ShouldBe("Node.js backend");
    }
}
