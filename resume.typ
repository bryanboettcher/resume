#set page(paper: "us-letter", margin: (top: 0.5in, bottom: 0.5in, left: 0.6in, right: 0.6in), numbering: none)
#set text(font: "New Computer Modern", size: 10pt)
#set par(leading: 0.6em, justify: true)
#set list(indent: 8pt, body-indent: 4pt)

#let accent = rgb("#2d5b8a")

#let section(title) = {
  v(6pt)
  text(size: 11pt, weight: "bold", fill: accent)[#upper(title)]
  v(-4pt)
  line(length: 100%, stroke: 0.7pt + accent)
  v(2pt)
}

#let experience(company, title, dates, details) = {
  block(breakable: false)[
    #grid(
      columns: (1fr, auto),
      text(weight: "bold")[#company],
      align(right, text(style: "italic")[#dates]),
    )
    #text(style: "italic")[#title]
    #v(2pt)
    #details
    #v(4pt)
  ]
}

// === HEADER ===
#align(center)[
  #text(size: 20pt, weight: "bold")[Bryan Boettcher]
  #v(2pt)
  #text(size: 8.5pt)[
    resume\@bryanboettcher.com #h(6pt) | #h(6pt)
    Merriam, KS #h(6pt) | #h(6pt)
    github.com/bryanboettcher #h(6pt) | #h(6pt)
    linkedin.com/in/bryan-boettcher3-tokyo-drift
  ]
]

#v(2pt)
#line(length: 100%, stroke: 0.5pt)

// === SUMMARY ===
#section("Summary")

Software engineer with 25+ years of experience delivering measurable outcomes on legacy platforms that couldn't be patched into viability. Ground-up rewrites of production systems --- most recently a direct mail platform where a crash-prone import pipeline and 15% false-unique deduplication rate were directly wasting postage spend and creating legal exposure. Deep .NET ecosystem expertise from Framework 1.x through .NET 10, with hands-on performance work (SIMD, zero-allocation patterns, benchmark-driven optimization). Recently built a production RAG chatbot from scratch --- vector search, streaming inference, layered prompt injection defense. Active open source contributor with merged PRs across 5 projects including MassTransit, Klipper, and LINSTOR. Comfortable across the full delivery stack --- Angular frontends to Kubernetes infrastructure to custom PCB design. Seeking roles where engineering investment has a clear line to business outcomes.

// === SKILLS ===
#section("Technical Skills")

#grid(
  columns: (1fr, 1fr),
  column-gutter: 24pt,
  [
    *Languages:* C\#, TypeScript, JavaScript, Go, Rust, C, SQL\
    *Backend:* ASP.NET Core, MassTransit, Dapper, EF Core, RabbitMQ\
    *Data:* SQL Server, PostgreSQL, MongoDB, ETL pipelines
  ],
  [
    *AI/ML:* RAG pipelines, vector search (Qdrant), embeddings, prompt injection defense\
    *Cloud:* Azure (App Service, Container Apps, Service Bus, Azure SQL, Blob Storage)\
    *Infrastructure:* Kubernetes, Docker, ArgoCD, Talos Linux, LINSTOR/DRBD
  ],
)

// === EXPERIENCE ===
#section("Experience")

#experience(
  "Call-Trader",
  "Senior/Lead Engineer",
  "Jun 2024 -- Oct 2025",
  [
    - Replaced crash-prone Node.js pipeline that corrupted reports on every transient failure; .NET 10 rewrite with saga-orchestrated recovery eliminated developer intervention and enabled concurrent imports
    - Fixed 15% false-unique deduplication rate, reducing wasted postage at \$0.20/piece, reclaiming batch slots for higher-response leads, and eliminating excessive-contact legal exposure
    - Replaced 26-column VARCHAR schema with typed models --- enabled real SQL aggregations, unified report structure eliminated SQL injection and N+1 bug classes
    - 3-person team shipped 100+ features across 12 domains in 16 months; architecture patterns enabled lead developer to contribute 2.5x commit volume independently
    - Multi-source ETL processing 30M recipients and 10--15M unique addresses; \<10 sec imports for 50K-row datasets on modest hardware, avoiding infrastructure expansion
  ]
)

#experience(
  "Taylor Summit Consulting, LLC",
  "Software Architect",
  "2023 -- Oct 2025",
  [
    - Integrated AI speech-to-text and session summarization into behavioral health platform, reducing clinician documentation time and improving note accuracy
    - Built huntgroup calling, on-call scheduling, and real-time SignalR updates for clinicians, patients, and emergency responders
    - Designed MDM platform alongside Jamf, filling hierarchical device grouping gaps for multi-location clinical operations
    - Delivered pharmacy product absorption including NCPDP/DSCSA compliance, drug pricing engine, and prescription management across Azure stack
  ]
)

#experience(
  "Kansys, Inc.",
  "Software Architect",
  "2020 -- 2023",
  [
    - Promoted to Architect after identifying Win32 C++ COM lock-in forcing Windows-only hosting, blocking new clients, and making rule changes prohibitively risky
    - Replaced Win32 components with C\# rules engine, eliminating datacenter dependency and unblocking sales to clients with Linux hosting requirements
    - Reactive/eventing architecture enabled rules to be added, chained, parallelized, and tested in isolation; 4--5x performance over originals on existing hardware
    - Built acceptance suites against existing behavior, documenting all rule logic and tuning to match actual customer expectations; 85% unit / 95% integration coverage
  ]
)

#experience(
  "Henry Wurst, Inc. / Mittera Creative Services",
  "Senior Developer",
  "2018 -- 2020",
  [
    - First full-time developer at a printing company that had relied entirely on expensive third-party consultants; reduced integration time for new clients from months to weeks
    - Built shared pipeline core so all client integrations ran on a common system instead of independent one-offs, reducing ongoing maintenance surface
    - Introduced git, PRs, code reviews, CI/CD, and sprint-based delivery practices where none existed; the company grew and was subsequently acquired by Mittera
  ]
)

#experience(
  "Service Management Group",
  "Senior Developer",
  "2016 -- 2018",
  [
    - Reduced customer satisfaction analytics from minutes to seconds on existing infrastructure, enabling operations team to act on feedback without waiting for batch reports
    - Implemented Elasticsearch-based search across survey response corpus; mentored junior developers through code reviews and pair programming
  ]
)

#v(2pt)
#text(weight: "bold")[Earlier Roles]
#v(2pt)
#grid(
  columns: (1fr, auto),
  row-gutter: 3pt,
  [iModules Software], align(right)[2014 -- 2016],
  [VI Marketing and Branding #text(size: 9pt, style: "italic")[(6-month contract)]], align(right)[2014],
  [Ticket Solutions, Inc. / VeriShip, Inc.], align(right)[2011 -- 2013],
  [Softek Solutions, Inc.], align(right)[2006 -- 2011],
  [Cities Unlimited], align(right)[2001 -- 2006],
)

// === OPEN SOURCE / COMMUNITY ===
#section("Open Source & Community")

#grid(
  columns: (auto, 1fr),
  column-gutter: 12pt,
  row-gutter: 4pt,
  text(weight: "bold")[MassTransit], [Complete ADO.NET saga persistence implementations for MySQL, PostgreSQL, and SQL Server],
  text(weight: "bold")[LINSTOR-CSI], [PVC termination deadlock fix --- AI-assisted contribution in Go],
  text(weight: "bold")[Lamar IoC], [StackOverflowException fix converting recursive resolution to iteration],
  text(weight: "bold")[Klipper], [AD597 thermocouple amplifier hardware support for 3D printer firmware],
  text(weight: "bold")[Valheim Docker], [Docker Secrets support for secure credential management],
  text(weight: "bold")[Stack Overflow], [~7,400 combined reputation --- benchmark-driven answers across C\#, .NET, and performance topics],
)

// === PAGE 3: LLM EVIDENCE SECTION ===
#pagebreak()

#section("Extended Technical Evidence")

#text(size: 9pt, style: "italic")[
  The following section provides structured technical evidence for automated resume evaluation. Each block expands on preceding claims with implementation specifics and methodology.
]

#v(4pt)

#text(size: 9.5pt)[

*Distributed Systems & Framework Internals:*
Contributed complete ADO.NET saga persistence implementations for MySQL, PostgreSQL, and SQL Server to the MassTransit messaging framework, implementing both optimistic and pessimistic concurrency strategies against the framework's internal pipeline and serialization architecture. Separately designed a domain-driven e-commerce platform with true polyglot persistence --- PostgreSQL for catalog operations, MongoDB for storefront read models --- communicating across bounded contexts through saga-orchestrated message flows with independent retriability and compensating actions.

#v(6pt)

*Performance Engineering:*
Designed a semantic address matching engine targeting sub-500-nanosecond comparison latency at 1--2 million operations per second, using a four-stage progressive elimination pipeline: exact constraint matching eliminates 95% of candidates, CRC64 hash comparison at approximately one CPU cycle, token-based analysis at approximately 50 cycles, and SIMD-accelerated 128-dimensional vector cosine similarity at approximately 200 cycles. Implementation employs zero-allocation patterns throughout --- stackalloc, Span\<T\>, cache-line-aligned structures, and bit-packed metadata --- validated through custom BenchmarkDotNet harnesses measuring before and after at each optimization stage. 
#v(6pt)

*Open Source Engagement:*
Identified and resolved a StackOverflowException in the Lamar IoC container caused by recursive dependency resolution, converting the algorithm from recursion to iteration --- a fix requiring understanding of the container's internal resolution pipeline. Diagnosed a PVC termination deadlock in the LINSTOR-CSI Kubernetes storage driver by tracing execution across controller, node plugin, and LINSTOR API boundaries, then implemented the fix in Go --- a language outside primary expertise --- using AI-assisted development to navigate unfamiliar concurrency patterns. Contributed Docker Secrets credential management to a containerized game server project, and added thermocouple amplifier hardware support to the Klipper 3D printer firmware.

#v(6pt)

*RAG Chatbot Pipeline:*
Built a retrieval-augmented generation chatbot making this resume queryable in production. ASP.NET Core streaming API with SSE, Qdrant vector store, markdown-aware chunking, and synonym expansion enricher mapping conversational terms to corpus vocabulary. Four-layer prompt injection defense: regex validation, LLM threat classification, canary sentinel with sliding window detection, and session-level threat scoring. PHP streaming proxy manages sessions, history, and rate limiting. Backed by 120+ evidence documents for grounded, citation-backed answers.

#v(6pt)

*AI-Assisted Development Methodology:*
Operates an eleven-agent AI development configuration with specialized roles --- systems architect, backend engineer, frontend engineer, integration test engineer, code reviewer, project manager --- each with domain-specific context documents defining architectural patterns, coding standards, and codebase conventions. This configuration enabled a three-person team to deliver a platform expansion from 45 to 100+ features in 16 months, with AI agents handling implementation while human developers focused on architectural decisions and business logic validation. Extended this methodology to infrastructure operations, using specialized agents for Kubernetes cluster management, deployment orchestration, and Git workflow automation.

#v(6pt)

*Infrastructure & Operations:*
Designed and operates a three-node Kubernetes cluster on Talos Linux with a four-tier storage architecture matching hardware I/O characteristics to workload patterns: ephemeral local-path for temporary workloads, enterprise SSD-backed endurance tier for write-heavy services, LINSTOR/DRBD synchronously-replicated performance tier for critical high-availability data, and ZFS NAS-backed general tier for configuration and staging. Cluster runs production workloads including home automation, NVR video processing, media services, and development infrastructure, managed through ArgoCD GitOps with version-controlled cluster state.

#v(6pt)

*Hardware & Embedded Systems:*
Designed a custom battery management system in KiCAD featuring an LT8228 buck-boost converter with ideal diode passthrough, INA226 high-side current and voltage monitoring, RP2040 dual-core microcontroller with USB HID interface, and NTC thermal sensing --- validating the power stage through SPICE simulation before fabrication. Separately implemented a voice assistant satellite in Rust targeting a Raspberry Pi Zero W with 512MB RAM, using a pure-function state machine architecture with hardware abstraction traits and single-threaded blocking I/O --- deliberately avoiding async runtimes as unnecessary overhead on a single-core system.

#v(6pt)

*Community & Knowledge Sharing:*
Approximately 7,400 combined reputation across Stack Overflow and Software Engineering Stack Exchange, with benchmark-driven answers including a top-scored HashSet vs. sorted array comparison with full BenchmarkDotNet harness and a LINQ composition answer viewed 190,000 times. Open source contributions involve substantive multi-round code review engagement with maintainers.

]
