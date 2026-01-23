# Unity Multiplayer Architecture Demo (Netcode for GameObjects)

This repository contains a technical prototype demonstrating a clean and modular
multiplayer architecture using **Unity Netcode for GameObjects (NGO)**.

The project is intentionally focused on **architecture and code structure** rather
than game content or visuals. It is meant to showcase how multiplayer gameplay
systems can be designed in a scalable, maintainable way for small-to-mid sized games.

---

## Goals of This Project

- Demonstrate a **server-authoritative multiplayer model**
- Separate **gameplay logic** from **networking concerns**
- Show clean usage of `NetworkBehaviour`, RPCs, and ownership checks

---

## Architecture Overview

**Key principles used:**
- Server-authoritative gameplay decisions
- Ownership-aware input handling
- Explicit separation of responsibilities between components
- Minimal coupling between networking and gameplay logic

The code avoids monolithic behaviours and instead favors small, focused components
that are easier to reason about and extend.

---

## What This Project Demonstrates

- Correct usage of Unity Netcode for GameObjects
- Client/server responsibility separation
- Safe RPC patterns
- Modular gameplay code suitable for multiplayer environments
- Readable and maintainable C# structure

This is not a complete game, but a technical sandbox intended to demonstrate
multiplayer architecture patterns.

---



## Movement Architecture

This project can use

1. A server authoritative movement architecture with:

Client-side prediction

Server reconciliation

Unreliable input streaming

Fixed-tick simulation

The server is the single source of truth.
Clients predict movement locally to hide latency, but the server always decides the final position.

High-Level Flow
Client Input >> Client Prediction (SimulatePrediction) >> InputTick >> Server (Unreliable RPC) >> Server Simulation (SimulateServer) >> Authoritative StateTick >> Client Reconciliation

2. An owner authoritative movement architecture with:
NetworkTransform
Component Based DI to switch modes. 

To switch modes must change Authority Mode in NetworkTransform component on PlayerPrefab

---

## Tech Stack

- Unity (Netcode for GameObjects)
- C#
- Unity Transport
- New Input System
- UniTask
- Git

---

## Notes

This project was built as an architectural exercise and learning prototype.
It is intentionally lightweight and focused on code quality rather than features.

## Controls
C = change player color
F = create object
E = grab object
T = throw object
V = delete object

