---
title: Hardware Design & Embedded Systems
tags: [kicad, embedded, rust, iot, pcb-design, rp2040, home-automation, 3d-printing, cross-compilation, alsa, state-machine]
related:
  - projects/mpc-ups-hardware.md
  - projects/wyoming-rust.md
  - projects/cloud-orca-slicer.md
  - projects/homelab-infrastructure.md
  - evidence/infrastructure-devops.md
  - links/github-repos.md
category: evidence
contact: resume@bryanboettcher.com
---

# Hardware Design & Embedded Systems — Evidence Portfolio

## Philosophy

Bryan's engineering extends below the application layer into hardware design and embedded systems programming. His projects in this space solve real infrastructure problems (UPS for homelab nodes, voice satellites for home automation) and involve genuine hardware design work (KiCAD schematics, SPICE simulation, cross-compiled firmware).

---

## Evidence: MPC-UPS — Custom UPS Hardware Design

**Local path:** ~/src/bryanboettcher/mpc-ups/
**Status:** Active — schematic design phase

### Purpose
A custom multi-protocol battery management and UPS system designed specifically for the Minisforum MS-A2 nodes in Bryan's homelab Kubernetes cluster. This is not a software project — it's a ground-up hardware design using KiCAD 8.

### Design Architecture
- **Power conversion:** LT8228 buck-boost converter with dual-path design (direct passthrough + battery fallback)
- **Seamless switchover:** Ideal diode circuit for uninterrupted power during battery transitions
- **Sensing:** INA226 high-side current/voltage monitoring + NTC thermal sensing
- **MCU:** RP2040 (dual-core ARM Cortex-M0+) with USB HID interface for host communication
- **Input range:** 21–29V (accommodating PSU variation)
- **Level shifting:** Between RP2040 3.3V logic and INA226/LT8228 signal levels

### Design Methodology
The project follows a 10-session modular design approach:
1. LT8228 Power Stage
2. Passthrough / Ideal Diode (architecture decision point)
3. INA226 Current/Voltage Sensing
4. Thermal Sensing
5. RP2040 MCU Core
6. MCU Power Supply
7. Level Shifting
8. USB HID Interface
9. Connectors & Mechanical
10. Integration & Layout Review

### Artifacts
- KiCAD schematic files (`mpc-ups.kicad_sch`, `passthrough.kicad_sch`, `simulator.kicad_sch`)
- LTSPICE/KiCAD simulation files for power stage validation
- Python-based design calculation scripts

### Skills Demonstrated
- Analog circuit design (buck-boost converters, ideal diode circuits, current sensing)
- Digital circuit design (RP2040, USB HID, I2C bus, level shifting)
- SPICE simulation for power stage validation
- KiCAD proficiency (schematic capture, simulation integration)
- Systems thinking — the UPS is designed specifically for the constraints of the homelab hardware it protects

---

## Evidence: Wyoming-Rust — Embedded Voice Satellite

**Repository:** https://github.com/bryanboettcher/wyoming-rust
**Local path:** ~/src/bryanboettcher/wyoming-rust/
**Status:** Active — core implementation

### Purpose
A Rust implementation of the Wyoming protocol for Home Assistant voice pipeline satellites, targeting the Raspberry Pi Zero W v1.1 (ARM1176, 512 MB RAM — one of the most resource-constrained Linux-capable boards available).

### Technical Design
- **Language:** Rust (no async/tokio — single-threaded blocking I/O by design)
- **Architecture:** Pure function state machine: `(state, input) → (new_state, actions[])`
- **Hardware abstraction:** Traits for `AudioSource`, `AudioSink`, `Led`, `Gpio` — separating hardware access from protocol logic
- **Audio:** ALSA backend for microphone input and speaker output
- **Protocol:** Wyoming JSON wire format (header line + data bytes + payload)
- **Cross-compilation:** `arm-unknown-linux-gnueabihf` target via `cross`
- **Docker:** Multi-arch builds (ARMv7 + ARM64) for deployment

### Design Decisions (documented in ADR-011)
- **Single-threaded blocking I/O:** Mic read takes ~20ms and serves as the system clock tick. No need for async complexity on a single-core CPU.
- **Library + binary separation:** Protocol crate is a reusable library; satellite binary is a separate consumer. Enables future reuse of the Wyoming protocol implementation.
- **Pure function state machine:** All state transitions are deterministic and testable without hardware. Side effects are returned as action lists, not executed inline.
- **No tokio:** Deliberate choice for a resource-constrained target. Tokio's runtime overhead is unnecessary on a single-core system where the audio read naturally paces execution.

### Scale
- 50 commits of active development
- Multi-arch Docker builds with automated publishing
- Health check endpoints and mDNS discovery for Home Assistant integration
- LED driver implementations for status indication
- VAD (Voice Activity Detection) mode configuration

---

## Evidence: 3D Printing Tooling

### PostProcessor — GCode Post-Processing
**Repository:** https://github.com/bryanboettcher/PostProcessor
**Language:** C#

A GCode post processor designed for extensibility. GCode is the machine language for CNC/3D printers, and post-processors modify the generated toolpaths after slicing. This demonstrates understanding of:
- CNC/3D printing toolchain (CAD → slicer → post-processor → printer)
- Machine control languages
- Text processing pipelines

### Cloud-Orca — Web-Based 3D Printer Slicer
**Local path:** ~/src/bryanboettcher/cloud-orca/
**Tech:** .NET 9 backend + Angular 19 frontend + Three.js for 3D visualization

A web-based 3D printer slicer wrapping existing engines (CuraEngine first, OrcaSlicer planned) with a modern UI:
- **Adapter pattern:** Pluggable slicer engine backends
- **3D visualization:** Three.js for model preview in the browser
- **API:** `/api/slice`, `/api/printers`, `/api/health`
- CuraEngine built from source inside Docker container

### Klipper Contribution
**URL:** https://github.com/Klipper3d/klipper/pull/3164 (Merged August 2020)

Added AD597 thermocouple amplifier support to the Klipper 3D printer firmware (11.4K stars). This is a hardware interface contribution — understanding the electrical characteristics of the AD597 amplifier IC and implementing the corresponding ADC temperature conversion in the firmware.

### Printer Modifications
**Repository:** https://github.com/bryanboettcher/gdsolar-PrinterMods (fork)

Physical printer modifications — 3D printed parts for modifying printer hardware.

---

## Evidence: Environmental Monitoring
**Repository:** https://github.com/bryanboettcher/envirowatch
**Language:** C++

An environmental monitoring project suggesting sensor integration and data collection firmware.

---

## Summary

Bryan's hardware/embedded work demonstrates:
- **Analog design:** Power conversion, current sensing, thermal monitoring
- **Digital design:** Microcontroller systems, USB interfaces, bus protocols (I2C, SPI)
- **Simulation:** SPICE for power stage validation before fabrication
- **Embedded Rust:** Resource-constrained programming without async runtime overhead
- **Cross-compilation:** Building for ARM targets from x86 development machines
- **Hardware/software co-design:** The UPS hardware is designed for his specific software infrastructure; the voice satellite firmware is designed for specific hardware constraints
- **Manufacturing toolchain:** Understanding the full CNC/3D printing pipeline from CAD to GCode to physical part
