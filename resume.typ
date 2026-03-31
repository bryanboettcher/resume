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
    linkedin.com/in/bryan-boettcher-7397b113
  ]
]

#v(2pt)
#line(length: 100%, stroke: 0.5pt)

// === SUMMARY ===
#section("Summary")

Software engineer with 25+ years of experience building and modernizing production systems. Track record of taking legacy platforms through ground-up rewrites --- most recently a direct mail platform processing 30M recipients across 10--15M unique addresses. Deep .NET ecosystem expertise from Framework 1.x through .NET 9, with hands-on performance work (SIMD, zero-allocation patterns, benchmark-driven optimization). Active open source contributor with merged PRs across 5 projects including MassTransit, Klipper, and LINSTOR. Comfortable across the full delivery stack --- from Angular frontends to Kubernetes infrastructure to custom PCB design. Seeking roles where complex technical problems need pragmatic, shipped solutions --- not committees.

// === SKILLS ===
#section("Technical Skills")

#grid(
  columns: (1fr, 1fr),
  column-gutter: 24pt,
  [
    *Languages:* C\#, TypeScript, JavaScript, C, SQL\
    *Backend:* ASP.NET Core, MassTransit, Dapper, EF Core, RabbitMQ\
    *Data:* SQL Server, PostgreSQL, MongoDB, ETL pipelines
  ],
  [
    *Cloud:* Azure (App Service, Container Apps, Service Bus, Azure SQL, Blob Storage, Communications Services, AI Services)\
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
    - Assessed legacy Node.js/Express platform and led ground-up rewrite to .NET 9, expanding from 45 features across 9 domains to 100+ features across 12 domains
    - Designed multi-source ETL pipeline processing 30M recipients and 10--15M unique addresses with USPS-certified normalization
    - Achieved \<10 sec imports for 50K-row datasets, \<100ns address lookups via CRC64 hash-based deduplication and in-memory caching
    - Built architecture that enabled team members to ship independently --- lead developer contributed 2.5x the commit volume on established patterns
    - Delivered maintainable codebase with flexible event-driven architecture, hundreds of repeatable and rerunnable SQL migrations, and full CI/CD pipeline
  ]
)

#experience(
  "Taylor Summit Consulting, LLC",
  "Software Architect",
  "2023 -- Oct 2025",
  [
    - Added urgent huntgroup calling, rule-based on-call scheduling, and facial recognition authentication to iOS-deployed mental health platform
    - Implemented real-time clinical updates via SignalR serving clinicians, patients, and emergency responders
    - Designed mobile device management platform for administrative operations of the clinical system
    - Built scheduled prescription drug pricing engine synchronizing disparate manufacturer sources for small pharmacy real-time lookups
    - Delivered across Azure stack: App Service, Container Apps, Azure SQL, Service Bus, Communications Services, AI Services
  ]
)

#experience(
  "Kansys, Inc.",
  "Software Architect",
  "2020 -- 2023",
  [
    - Promoted from Senior Developer to Architect after identifying fundamental code quality and process issues in legacy telecom billing platform
    - Established coding standards, source control practices, and modern tooling across the development team
    - Isolated legacy components as black boxes and reimplemented with modern techniques, backed by acceptance suites written against existing behavior
    - Replaced legacy Win32 C++ components with extensible C\# implementations, achieving 4--5x performance improvements over the originals
    - Encapsulated domain boundaries into injectable, cloud-first services --- eliminating on-prem hosting dependency and enabling team scale-out
    - Achieved 85% unit test coverage and 95% integration test coverage across modernized codebase
  ]
)

#experience(
  "Henry Wurst, Inc. / Mittera Creative Services",
  "Senior Developer",
  "2018 -- 2020",
  [
    - Modernized development practices --- introduced CI/CD, automated testing, coding standards, and mentored junior developers
    - Developed and open-sourced a distributed, queue-driven data processing architecture
  ]
)

#experience(
  "Service Management Group",
  "Senior Developer",
  "2016 -- 2018",
  [
    - Diagnosed and resolved performance issues, improving application response times by 80%
    - Implemented ElasticSearch-based search system and mentored junior developers through code reviews and pair programming
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
Maintains approximately 7,400 combined reputation across Stack Overflow and Software Engineering Stack Exchange, with answers characterized by working benchmark implementations rather than opinion-based recommendations --- including a top-scored answer demonstrating HashSet versus sorted array binary search performance with full BenchmarkDotNet harness, and a high-view answer on LINQ query composition viewed 190,000 times. Open source contributions consistently involve substantive code review engagement with project maintainers, iterating on feedback across multiple review cycles rather than drive-by pull requests.

]
