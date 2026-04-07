---
title: MPC-UPS Custom UPS Hardware
tags: [kicad, hardware, embedded, rp2040, power-electronics, pcb-design, spice, usb-hid, i2c, analog-design]
related:
  - evidence/hardware-embedded-systems.md
  - projects/homelab-infrastructure.md
category: project
contact: resume@bryanboettcher.com
---

# MPC-UPS — Project Narrative

## Context

A custom battery management and UPS system designed specifically for the Minisforum MS-A2 nodes in Bryan's homelab Kubernetes cluster. Commercial UPS units are oversized, expensive, and don't integrate with Kubernetes for graceful shutdown signaling. This project designs a purpose-built solution.

## Design

### Power Architecture
- **LT8228 buck-boost converter:** Handles the bidirectional power path between PSU input and battery
- **Ideal diode passthrough:** Seamless, uninterrupted power switching between direct PSU and battery power
- **Input range:** 21–29V (accommodating PSU variation and battery charge states)

### Sensing & Control
- **INA226:** High-precision current/voltage monitoring (I2C bus) for battery state tracking
- **NTC thermistors:** Thermal monitoring for battery safety
- **RP2040 MCU:** Dual-core ARM Cortex-M0+ running the BMS algorithm and USB HID communication

### Host Communication
- **USB HID:** Standard interface allowing the Kubernetes node to query UPS state and receive shutdown signals
- No special drivers needed — USB HID is natively supported by Linux

### Design Tools
- **KiCAD 8:** Schematic capture and PCB layout
- **SPICE simulation:** Power stage validation before fabrication
- **Python:** Design calculations and component selection scripts

## Status

Schematic design phase with 10 modular design sessions planned. Core schematics (power stage, passthrough, MCU) in progress. Simulation files for power stage validation created.

## Significance for Resume

- Demonstrates engineering capability below the software stack
- Analog + digital + firmware + host integration in a single project
- Real problem solving (commercial UPS doesn't fit the use case)
- Design-for-manufacturing methodology (modular sessions, SPICE validation before fabrication)
