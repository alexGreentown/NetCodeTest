# NetCodeTest

Multiplayer Lobby & Gameplay Demo (Unity 6 + NGO)
Overview

A small multiplayer Unity project demonstrating a clean, SOLID-based architecture for lobby management, scene switching, and synchronized gameplay using Netcode for GameObjects (NGO).

The project focuses on correctness, extensibility, and production-style structure rather than visuals.

Tech Stack

Unity: 6.0+

Networking: Netcode for GameObjects (NGO)

Input: New Input System

Async: UniTask

Tweening: DOTween

Architecture: SOLID + Dependency Injection (custom lightweight DI)

## Controls
C = change player color
F = create object
E = grab object
T = throw object
V = delete object

## Dependency Injection

Project uses a lightweight custom DI container.

- Composition Root initializes services
- MonoBehaviours depend on interfaces, not implementations

This keeps the architecture modular, testable and easy to extend.


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
