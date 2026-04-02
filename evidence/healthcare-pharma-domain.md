---
title: Healthcare & Pharmaceutical Domain Expertise
tags: [healthcare, pharma, ncpdp, dscsa, epcis, gs1, fda, dea, pmp, hipaa, mdm, pki, clinical, telehealth, edi, onnx, azure]
related:
  - projects/taylor-summit.md
  - evidence/cloud-azure-experience.md
  - evidence/ai-driven-development.md
  - evidence/dotnet-csharp-expertise.md
  - evidence/distributed-systems-architecture.md
category: evidence
contact: resume@bryanboettcher.com
---

# Healthcare & Pharmaceutical Domain — Evidence Portfolio

## Philosophy

Bryan's healthcare work at Taylor Summit spans the full pharmaceutical and clinical care spectrum — from drug supply chain traceability through pharmacy dispensing to behavioral health patient care. This isn't surface-level CRUD over a medical database; it involves implementing regulatory protocols from specification documents, building PKI infrastructure for device security, and integrating AI into clinical workflows in a compliance-sensitive context.

---

## Evidence: NCPDP Binary Protocol Implementation (Pharmacy Claims Adjudication)

**Project:** Pharmacy Management Platform for a bulk pharmaceutical provider (Taylor Summit Consulting)

Implemented the complete NCPDP (National Council for Prescription Drug Programs) Telecommunications Standard — the binary protocol pharmacies use to submit claims to insurance payers in real-time.

### Technical Details
- **Raw byte-level segment building** with standardized field IDs: `D2` (RxReferenceNumber), `D7` (ProductDispensedId), `E7` (QuantityDispensed)
- **Coordinate of Benefit (COB)** segment handling for multi-payer adjudication
- **Drug Utilization Review (DUR)** segments for drug interaction checking
- **Compound medication** claim handling (multi-ingredient prescriptions)
- **Prior authorization** flows for restricted medications
- **TLS socket communication** with NDC Health claim switches via `NdcClaimBroker`

### Why This Matters
NCPDP Telecommunications is a proprietary binary protocol — not REST, not SOAP, not gRPC. Implementing it from specification requires understanding pharmacy billing domain concepts (DAW codes, therapeutic substitution, coordination of benefits) alongside low-level binary protocol engineering. Few developers outside the pharmacy software industry have this expertise.

---

## Evidence: DSCSA/EPCIS Drug Supply Chain Compliance

**Projects:** EPCIS Generator, Pharmacy Management Platform (Taylor Summit Consulting)

### Drug Supply Chain Security Act (DSCSA) Compliance
Generated EPCIS (Electronic Product Code Information Services) XML documents for FDA-mandated drug product traceability. The DSCSA requires that every pharmaceutical product be serialized and tracked through the supply chain — from manufacturer to dispenser.

### GS1 Standards Implementation
- **SGTIN/SSCC/SGLN URN generation** — Global Trade Item Numbers for individual drug packages
- **Event types:** ObjectEvent (commissioning), AggregationEvent (packaging), ObserveEvent (shipping)
- **Business transaction types:** `desadv` (dispatch advice), `po` (purchase order)
- **TraceLink integration** — TraceLink-specific EPCIS extensions for pharmaceutical serialization
- **GS1 DataMatrix barcode parsing** (`TaylorSummit.BarCodeProcessing`) — extracting GTIN, lot number, expiration date, and serial number from pharmaceutical product barcodes

---

## Evidence: SureScripts E-Prescribing Integration

**Project:** Pharmacy Management Platform (Taylor Summit Consulting)

Full NCPDP SCRIPT XML message parsing for electronic prescriptions:
- Hundreds of data model classes implementing the SureScripts/NCPDP SCRIPT specification
- **Veterinary prescriber support** (a non-trivial extension of the human prescribing model)
- **Transfer requests** between pharmacies
- **Controlled substance prescribing** with DEA validation

---

## Evidence: Prescription Monitoring Program (PMP) Reporting

**Project:** Pharmacy Management Platform (Taylor Summit Consulting)

PMP reporting for DEA-tracked controlled substances (Schedule II–V). State-mandated reporting that tracks every dispensing of opioids, benzodiazepines, stimulants, and other controlled substances. Implementation includes:
- Controlled substance classification and scheduling
- Dispensing event reporting with prescriber/patient/pharmacist identification
- State-specific reporting format compliance

---

## Evidence: Pharmaceutical Pricing Engine

**Project:** Pharmacy Platform — legacy system (Taylor Summit Consulting)

Multi-tier drug pricing with:
- **AWP (Average Wholesale Price)** and **WAC (Wholesale Acquisition Cost)** base pricing
- Brand vs. generic tiered markup calculation
- Contract-based pricing with three markup types: MULTIPLIER, PERCENTAGE, DOLLAR AMOUNT
- Claim repricing jobs for pharmacy benefit management

---

## Evidence: MediSpan Drug Database Integration

**Project:** MediSpan (Taylor Summit Consulting)

Automated import pipeline for Wolters Kluwer MediSpan drug reference database — the industry-standard drug information database used by pharmacies nationwide:
- **25+ import targets:** MF2Ndc (National Drug Codes), MF2Name, MF2Lab, MF2Gpr, MF2Prc (pricing), IpmVal, IpmSum (drug interactions), IpmLink, and more
- **Template method pattern:** `MediSpanTarget` base class with `CreateDataTable`/`PopulateRow`/`GetDataColumns` overrides per data file type
- **Pipe-delimited file parsing** for MF2/IPM data feeds
- **Transactional truncate-and-reload** with rollback support
- **CRC64 hash verification** — only modified files uploaded to Azure Blob Storage
- .NET 9, Docker-containerized, `SqlBulkCopy` for bulk loading

---

## Evidence: AI-Powered Clinical Workflows

**Project:** Clinical Care Platform for a regional mental health provider (Taylor Summit Consulting)

### Real-Time Clinical Transcription
Azure Cognitive Services Speech with SignalR streaming for live speech-to-text during behavioral health counseling sessions. Bulk transcript storage via SQL Table-Valued Parameters.

### AI Session Summarization
OpenAI-powered clinical note generation with iterative prompt engineering (4 prompt iterations visible in codebase):
- Incorporates patient demographics (age bracket, gender)
- Identifies treatment modalities used during the session
- Extracts coping skills discussed
- Generates structured clinical notes suitable for medical record keeping

### Facial Recognition for Patient Identity
`FaceAiSharp.Bundle` with ONNX Runtime for on-device ML inference:
- Face detection and landmark extraction
- Embedding vector generation
- Confidence scoring via dot product comparison
- Single-face enforcement for security
- No cloud dependency for inference — runs on-device

### Why This Matters
AI in healthcare isn't the same as AI in a chatbot. The clinical context imposes constraints: HIPAA compliance, medical record accuracy requirements, audit trail needs, and the sensitivity of behavioral health data. Bryan built these features in that context, not in a sandbox.

---

## Evidence: EDI (Electronic Data Interchange) Processing

**Project:** Pharmacy Platform — legacy system (Taylor Summit Consulting)

AWS Lambda function for processing healthcare EDI files (X12 transactions):
- **837** — Healthcare claims
- **835** — Claims payment/remittance advice
- **270/271** — Eligibility inquiry/response
- ReadyToRun publishing for Lambda cold-start optimization
- S3/SFTP source file retrieval

---

## Evidence: Drug Recall Management

**Project:** Pharmacy Platform — legacy system (Taylor Summit Consulting)

Full recall communication system:
- Lot tracking for affected products
- Contact management for notification targets
- Affected location profile identification
- S3 file management for recall documentation
- Multi-channel notification delivery

---

## Evidence: PKI Certificate Chain Management

**Project:** MDM (Taylor Summit Consulting)

Built a complete PKI infrastructure for the Apple MDM server:
- **Self-signed Certificate Authority** generation
- **Device identity certificate** generation (CA → device cert chain)
- **Apple Push Certificate Request** generation
- **Apple MDM Vendor Certificate Request** generation
- **DEP Server Token** generation and decryption
- **PEM/PKCS8** private key importers
- **PKCS7 MIME** content type support for signed profiles
- **PKCS12** identity certificate embedding in enrollment profiles

This is deep security/cryptography work — building the trust infrastructure that secures every device communication in the MDM system.

---

## Summary

Bryan's healthcare domain expertise includes:
- **Regulatory protocol implementation:** NCPDP Telecommunications (binary), NCPDP SCRIPT (XML), GS1/EPCIS, DSCSA, PMP — implemented from specification, not consumed from libraries
- **Pharmaceutical domain knowledge:** NDC codes, drug pricing (AWP/WAC), drug interactions, controlled substance scheduling, supply chain serialization
- **Clinical AI integration:** Real-time transcription, session summarization, facial recognition — in a HIPAA-sensitive context
- **PKI/Security engineering:** Full certificate chain management for MDM device security
- **Multi-regulatory compliance:** FDA (DSCSA), DEA (PMP), HIPAA (patient data), state pharmacy boards
