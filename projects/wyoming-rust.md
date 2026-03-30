---
project: Wyoming-Rust Voice Satellite
company: Personal
dates: 2025 – present
role: Sole Developer
tags: [rust, embedded, iot, home-assistant, cross-compilation, state-machine]
---

# Wyoming-Rust — Project Narrative

## Context

A Rust implementation of the Wyoming protocol for Home Assistant voice pipeline satellites. The Wyoming protocol allows external devices to serve as voice input/output endpoints for Home Assistant's voice assistant pipeline (wake word detection → speech-to-text → intent processing → text-to-speech → audio output).

## Why Rust

The target hardware is a Raspberry Pi Zero W v1.1 — a single-core ARM1176 with 512 MB RAM. Existing Wyoming satellite implementations are Python-based and consume significant resources on this hardware. Rust provides:
- Predictable memory usage (no garbage collector)
- Small binary size
- Zero-cost abstractions
- No runtime overhead

## Technical Design

### State Machine Architecture
Pure function state machine: `(state, input) → (new_state, actions[])`
- All state transitions are deterministic and testable without hardware
- Side effects returned as action lists, not executed inline
- This design was a deliberate architectural choice (documented in ADR-011)

### No Async Runtime
Single-threaded blocking I/O by design:
- Microphone read takes ~20ms and serves as the system clock tick
- On a single-core CPU, async runtime overhead is pure waste
- The natural pacing of audio reads eliminates the need for timers or schedulers

### Hardware Abstraction
Traits for hardware access: `AudioSource`, `AudioSink`, `Led`, `Gpio`
- Protocol logic is testable without hardware
- Multiple hardware backends can be swapped (ALSA, PulseAudio, mock)

### Protocol Implementation
- Library/binary separation: Wyoming protocol crate is reusable; satellite binary is a separate consumer
- JSON wire format: header line + data bytes + payload
- mDNS discovery for automatic Home Assistant integration
- Health check endpoints for monitoring

## Deployment

- Cross-compilation via `cross`: `arm-unknown-linux-gnueabihf` target
- Multi-arch Docker builds (ARMv7 + ARM64)
- 50 commits of active development

## Significance for Resume

- Systems programming in Rust (not just "learning Rust")
- Embedded/IoT development on resource-constrained hardware
- Architectural decisions driven by hardware constraints (no async, single-threaded)
- Cross-compilation and multi-arch deployment
- Protocol implementation with clean library/application separation
