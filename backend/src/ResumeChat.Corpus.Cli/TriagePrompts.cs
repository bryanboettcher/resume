namespace ResumeChat.Corpus.Cli;

/// <summary>
/// Per-language triage prompt templates. <<FILE_PATH>>, <<LANGUAGE>>, <<CONTENT>> are replaced at call time.
/// All share the same 4 boolean dimensions; language-specific rules precede the file content.
/// </summary>
static class TriagePrompts
{
    public const string CSharp = """
        You are a code analyst triaging C# source files for a resume portfolio.
        Your job is to make factual observations about what this file contains.

        Describe what you observe (2-3 sentences), then classify each dimension as true or false.

        Dimensions:
          has_logic          — contains branching, loops, state transitions, calculations, or non-trivial
                               control flow beyond simple property access or delegation.
                               IMPORTANT: interfaces and abstract classes with NO method bodies cannot
                               have has_logic = true. Method SIGNATURES are not logic — only method
                               IMPLEMENTATIONS with actual code in the body count. An interface that
                               declares Decode(), Process(), or Calculate() has NO logic because there
                               is no implementation in the file.
          has_domain_rules   — contains validation, business constraints, domain-specific transformations,
                               or industry-specific knowledge encoded in actual code
          has_composition    — wires together multiple services, configures DI containers, registers
                               pipelines, or orchestrates dependencies
          has_data_modeling  — defines entities/records/classes with multiple related properties,
                               relationships, constraints, or invariants

        Language-specific rules:
          - Score based on what the file CONTAINS, not what callers or implementations might do.
          - Empty classes, marker interfaces, and attribute definitions have all dimensions false.
          - Simple POCO/record types with only auto-properties are has_data_modeling only if they
            represent a meaningful domain entity (not just DTOs with 2-3 fields).
          - Extension method classes that only delegate to other methods have no logic.
          - State machines (MassTransit .During/.When/.TransitionTo, or similar) that define
            event-driven transitions ARE logic — they encode control flow as state transitions.
          - Services that validate inputs then delegate to other services have has_domain_rules
            if the validation is domain-specific (not just null checks).
          - Consistency: if your reasoning says "no logic" or "only signatures", has_logic must be false.

        File: <<FILE_PATH>>
        Language: <<LANGUAGE>>

        ```
        <<CONTENT>>
        ```

        Respond with JSON only:
        {
          "reasoning": "...",
          "has_logic": false,
          "has_domain_rules": false,
          "has_composition": false,
          "has_data_modeling": false
        }
        """;

    public const string TypeScript = """
        You are a code analyst triaging source files for a senior software engineer's resume portfolio.
        Your job is to make factual observations about what this file contains — no subjective ratings.

        First, briefly describe what you observe in the file (2-3 sentences).
        Then answer true or false for each dimension based solely on what is present in the code.

        Dimensions:
          has_logic          — contains branching, loops, state transitions, calculations, or non-trivial
                               control flow beyond simple property access or delegation
          has_domain_rules   — contains validation, business constraints, domain-specific transformations,
                               or industry-specific knowledge
          has_composition    — wires together multiple services, configures pipelines, orchestrates
                               dependencies, or sets up infrastructure
          has_data_modeling  — defines entities with relationships, constraints, invariants, or
                               non-trivial data structures

        Use lowercase true or false. Do not use subjective language or scores.

        Language-specific rules:
          - Angular module declarations (imports, declarations, providers arrays) are composition.
          - Test spec boilerplate (describe/it with no assertions or with only component creation checks)
            is not logic. Empty test files have no dimensions true.
          - Simple interfaces or type aliases with only property declarations (no methods with bodies)
            are not data_modeling unless they define domain entities with relationships or constraints.
            A generic interface like IKeyed<T> with one property is not data_modeling.
          - has_data_modeling requires multiple related fields forming a domain entity, or explicit
            constraints/invariants — not just a TypeScript interface shape.
          - Classes with methods that implement domain-specific filter/transform/validation behavior
            have has_domain_rules = true, not just has_logic.
          - Consistency: if your reasoning mentions "domain" or "business" concepts, consider
            has_domain_rules = true.

        File: <<FILE_PATH>>
        Language: <<LANGUAGE>>

        ```
        <<CONTENT>>
        ```

        Respond with JSON only:
        {
          "reasoning": "brief factual description of what you observe — 2-3 sentences",
          "has_logic": false,
          "has_domain_rules": false,
          "has_composition": false,
          "has_data_modeling": false
        }
        """;

    public const string Yaml = """
        You are a code analyst triaging YAML files for a resume portfolio.
        Your job is to make factual observations about what this file contains.

        IMPORTANT CONTEXT: YAML files are typically configuration, not executable code.
        Most YAML files are declarative — they describe desired state, not procedural logic.

        For YAML files, the dimensions are defined as:
          has_logic          — contains EXECUTABLE logic: stored procedures, PromQL/query expressions
                               with calculations, or CI/CD pipeline scripts with conditional steps.
                               Helm template directives ({{ if }}, {{ range }}, {{ with }}) are
                               boilerplate templating and are NOT logic — they are the YAML equivalent
                               of a mail merge. Simple value lookups like {{ .Values.x }} are not logic.
                               Workflow documentation describing steps to perform is NOT logic —
                               the document itself is data, not code.
          has_domain_rules   — contains domain-specific thresholds, constraints, or business rules
                               encoded as configuration (e.g. alert thresholds, resource limits with
                               specific rationale, SLA definitions, retention policies).
          has_composition    — wires together multiple services, configures service-to-service
                               relationships, defines multi-container pods, or orchestrates deployments
                               across components. A single Deployment manifest is NOT composition
                               unless it connects multiple services.
          has_data_modeling  — defines data schemas, CRD specs with structural validation, or
                               complex nested data structures with typed fields.

        Describe what you observe (2-3 sentences), then classify.

        File: <<FILE_PATH>>
        Language: <<LANGUAGE>>

        ```
        <<CONTENT>>
        ```

        Respond with JSON only:
        {
          "reasoning": "...",
          "has_logic": false,
          "has_domain_rules": false,
          "has_composition": false,
          "has_data_modeling": false
        }
        """;

    public const string Html = """
        You are a code analyst triaging HTML template files for a resume portfolio.
        Your job is to make factual observations about what this file contains.

        IMPORTANT CONTEXT: This file is an HTML template, likely an Angular component template.
        Angular templates use declarative directives (*ngIf, *ngFor, [binding], (event), [(ngModel)])
        that are NOT logic — they are view bindings. The actual logic lives in the companion .ts file.

        For HTML templates, the dimensions are defined as:
          has_logic          — contains <script> tags with JavaScript, inline arithmetic in
                               interpolations ({{ price * qty }}), or complex ternary expressions.
                               Angular directives (*ngIf, *ngFor, *ngSwitch), property bindings
                               ([src], [ngStyle], [class.x]), event bindings ((click), (change)),
                               two-way bindings ([(ngModel)]), and pipe expressions (| date) are
                               ALL declarative and do NOT count as logic. has_logic is almost
                               always false for Angular templates.
          has_domain_rules   — contains hardcoded validation constraints in the markup itself
                               (min="0", max="100", pattern="...", required with specific rules).
                               Simply having form inputs does not qualify.
          has_composition    — wires together multiple services or configures infrastructure.
                               HTML templates do not do this. Almost always false.
          has_data_modeling  — defines data entities with relationships. HTML templates do not
                               do this. Almost always false.

        Describe what you observe (2-3 sentences), then classify each dimension.

        File: <<FILE_PATH>>
        Language: <<LANGUAGE>>

        ```
        <<CONTENT>>
        ```

        Respond with JSON only:
        {
          "reasoning": "...",
          "has_logic": false,
          "has_domain_rules": false,
          "has_composition": false,
          "has_data_modeling": false
        }
        """;

    public const string Sql = """
        You are a code analyst triaging SQL files for a resume portfolio.
        Your job is to make factual observations about what this file contains.

        Describe what you observe (2-3 sentences), then classify each dimension as true or false.

        Dimensions:
          has_logic          — contains procedural logic: IF/ELSE, WHILE loops, CASE expressions
                               with calculations, CTEs with recursive logic, or complex WHERE
                               predicates with multiple conditions. Simple IF EXISTS guards before
                               CREATE/DROP are boilerplate and are NOT logic.
          has_domain_rules   — contains CHECK constraints encoding business rules, computed columns
                               with domain formulas, or stored procedures with business validation
          has_composition    — SQL files rarely have composition. Only true if the file orchestrates
                               across multiple schemas or coordinates multi-step ETL processes.
          has_data_modeling  — CREATE TABLE/VIEW with columns, constraints, relationships (foreign
                               keys), or indexes. This is the primary signal for SQL files.

        Language-specific rules:
          - CREATE TABLE with constraints and relationships is data_modeling.
          - Stored procedures/functions with loops, branching, or calculations are logic.
          - Simple INSERT/SELECT/DROP statements are neither logic nor modeling.
          - IF EXISTS before CREATE/DROP is boilerplate, not logic.

        File: <<FILE_PATH>>
        Language: <<LANGUAGE>>

        ```
        <<CONTENT>>
        ```

        Respond with JSON only:
        {
          "reasoning": "...",
          "has_logic": false,
          "has_domain_rules": false,
          "has_composition": false,
          "has_data_modeling": false
        }
        """;

    public const string Markdown = """
        You are a code analyst triaging Markdown documentation files for a resume portfolio.
        Your job is to make factual observations about what this file contains.

        IMPORTANT: Markdown files are DOCUMENTATION. They describe systems, they are not the systems
        themselves. All dimensions should almost always be false for documentation files.

        For Markdown files, the dimensions are defined as:
          has_logic          — the file IS an executable script or notebook with runnable code as its
                               primary content. Shell commands shown as examples in a runbook are NOT
                               logic — they are documentation of commands to run. Code snippets shown
                               for illustration are NOT logic. has_logic is almost never true for .md files.
          has_domain_rules   — the file contains a specification or reference table that encodes actual
                               business rules (e.g. a pricing table, a regulatory compliance checklist
                               with specific thresholds). Descriptions of how a system works are NOT
                               domain rules.
          has_composition    — almost never true for markdown. Documentation that describes architecture
                               is NOT composition — it describes composition that exists elsewhere.
          has_data_modeling  — almost never true for markdown. Documentation that describes a data model
                               is NOT data modeling — it describes models defined elsewhere.

        Describe what you observe (2-3 sentences), then classify.

        File: <<FILE_PATH>>
        Language: <<LANGUAGE>>

        ```
        <<CONTENT>>
        ```

        Respond with JSON only:
        {
          "reasoning": "...",
          "has_logic": false,
          "has_domain_rules": false,
          "has_composition": false,
          "has_data_modeling": false
        }
        """;

    public const string Default = """
        You are a code analyst triaging source files for a resume portfolio.
        Your job is to make factual observations about what this file contains.

        Describe what you observe (2-3 sentences), then classify each dimension as true or false.

        Dimensions:
          has_logic          — contains branching, loops, state transitions, calculations, or non-trivial
                               control flow beyond simple property access or delegation. Configuration
                               files and static data do NOT have logic.
          has_domain_rules   — contains validation, business constraints, domain-specific transformations,
                               or industry-specific knowledge encoded in the file
          has_composition    — wires together multiple services, configures pipelines, orchestrates
                               dependencies, or sets up infrastructure
          has_data_modeling  — defines entities with relationships, constraints, invariants, or
                               non-trivial data structures

        File: <<FILE_PATH>>
        Language: <<LANGUAGE>>

        ```
        <<CONTENT>>
        ```

        Respond with JSON only:
        {
          "reasoning": "...",
          "has_logic": false,
          "has_domain_rules": false,
          "has_composition": false,
          "has_data_modeling": false
        }
        """;

    public const string FullAnalysis = """
        You are a code analyst producing structured metadata about source files for a senior software engineer's resume portfolio.
        Analyze this file and produce detailed metadata that captures what makes it interesting from an engineering perspective.

        File: <<FILE_PATH>>
        Language: <<LANGUAGE>>

        ```
        <<CONTENT>>
        ```

        Respond with JSON only:
        {
          "purpose": "1-2 sentence description of what this file does",
          "domain_concepts": ["list", "of", "domain", "concepts"],
          "patterns": [
            {"name": "pattern name", "notes": "how it's applied here"}
          ],
          "notable_techniques": ["specific techniques worth highlighting"],
          "frameworks": ["frameworks and libraries used"],
          "interactions": ["other components/services this interacts with"],
          "complexity": "low" | "medium" | "high",
          "resume_keywords": ["keywords for resume matching — e.g. MassTransit, saga, SIMD, EF Core, Kubernetes"]
        }
        """;
}
