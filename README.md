# Standalone MCP Health Check Server (ASP.NET Core)

[![Difficulty](https://img.shields.io/badge/difficulty-hard-critical)]()
[![Languages](https://img.shields.io/badge/languages-C%23%20%7C%20ASP.NET%20Core-informational)]()
[![Deadline](https://img.shields.io/badge/deadline-2026--01--06-critical)]()

> Build a **standalone MCP Server** that lets AI models query your cloud services’ health status over the public internet in a **standard way**—using **HTTP + SSE**, the MCP lifecycle (**Initialize** + **Handshake**), a `check_api_status` tool, and **multi-client session management**.

---

## Table of Contents

* [Standalone MCP Health Check Server (ASP.NET Core)](#standalone-mcp-health-check-server-aspnet-core)

  * [Table of Contents](#table-of-contents)
  * [Requirements](#requirements)
  * [Problem Description](#problem-description)
  * [Rules and Constraints](#rules-and-constraints)
  * [Core Scenarios](#core-scenarios)
  * [Input/Output and Examples](#inputoutput-and-examples)
  * [How to Run and Test](#how-to-run-and-test)

    * [1) Clone the Project](#1-clone-the-project)
    * [2) Build and Run](#2-build-and-run)
    * [3) (Optional) Run Tests](#3-optional-run-tests)
  * [How to Submit (PR)](#how-to-submit-pr)
  * [Evaluation Criteria](#evaluation-criteria)
  * [Timeline](#timeline)
  * [Contact](#contact)

---

## Requirements

* Allowed Language: **C#**
* Framework: **.NET 8 (recommended)** / .NET 7+ acceptable
* Recommended SDK: **.NET 8.0.x**
* Server: **ASP.NET Core**
* Protocol: **HTTP + Server-Sent Events (SSE)** (HTTPS recommended)
* OS: Any (Windows / Linux / macOS)
* Familiarity with:

  * HTTP APIs
  * SSE streaming
  * Basic async programming
  * Concurrency & session/state handling

---

## Problem Description

Your organization wants AI models (e.g., Claude) to query the health status of your cloud services over the internet in a **direct** and **standard** way.

Design and implement a **Standalone MCP Server** using **ASP.NET Core** that supports:

1. **Full MCP lifecycle** over **HTTP/SSE**, including **Initialize** and **Handshake**.
2. A **tool** named `check_api_status` that accepts a URL and reports availability.
3. **Multi-client concurrency** via **session management** (multiple clients simultaneously with isolated sessions).

> This is a backend/server challenge: implement protocol flow, SSE streaming, tool execution, and session lifecycle.

---

## Rules and Constraints

* **Protocol & lifecycle**

  * You must implement **Initialize** and **Handshake** in a way that is testable via HTTP calls.
  * SSE events must be **session-bound** and correctly routed.
  * MCP message formats and event payloads must be clearly documented (even if simplified).

* **Session management**

  * Support multiple concurrent sessions.
  * No cross-talk: events/results for one session must never appear in another.
  * Implement session cleanup (TTL/expiration) and document the policy.

* **Tool execution**

  * `check_api_status` must return a clear status (e.g., `UP` / `DOWN`) with useful details.
  * Handle timeouts, DNS failures, TLS errors, invalid URLs—without crashing.

* **Security notes (must address in solution)**

  * Protect against SSRF by default (recommended approaches: allowlist domains, block private/loopback ranges, configurable policies).
  * Use sane request timeouts and size limits.

* **Dependencies**

  * Prefer the .NET standard library. If you add packages, justify them in docs.

---

## Core Scenarios

### 1) Initialize (HTTP)

A client starts a new MCP session.

**Expected server responsibilities:**

* Create and store a new `session_id`.
* Return protocol info and capabilities.
* Return the list of available tools (must include `check_api_status`).
* Provide the SSE stream URL for that session.

### 2) Handshake (HTTP + SSE)

The client completes handshake and attaches to an SSE stream tied to `session_id`.

**Expected server responsibilities:**

* Validate `session_id`.
* Open/maintain the SSE connection.
* Emit handshake/ready events (or equivalent) to confirm the session is active.

### 3) Tool Invocation: `check_api_status`

The client requests tool execution with input:

* `name`: `check_api_status`
* `input`: `{ "url": "https://example.com/health" }`

**Expected server responsibilities:**

* Perform an HTTP probe.
* Report:

  * status (`UP`/`DOWN`)
  * HTTP status code (if reachable)
  * latency (ms)
  * timestamp
  * error details (if failed)

### 4) Multiple Clients / Sessions

Your server must support many clients concurrently.

**Expected server responsibilities:**

* Correct routing of SSE events and tool results per session.
* Thread-safe session storage.
* Cleanup of expired sessions.

---

## Input/Output and Examples

### Tool Definition (Required)

**Tool name:** `check_api_status`

**Default behavior:**

* Default probe timeout: **3000ms** (must be configurable).

**Expected input JSON:**

```json
{
  "url": "https://example.com/health"
}
```

**Suggested success output JSON:**

```json
{
  "url": "https://example.com/health",
  "status": "UP",
  "http_status": 200,
  "latency_ms": 87,
  "checked_at": "2025-12-30T12:00:00Z"
}
```

**Suggested failure output JSON:**

```json
{
  "url": "https://example.com/health",
  "status": "DOWN",
  "error": "Timeout after 3000ms",
  "checked_at": "2025-12-30T12:00:00Z"
}
```

---

## How to Run and Test

### 1) Clone the Project

```bash
git clone https://github.com/dotin-challenge/MCP-Health-Check-Server.git
cd MCP-Health-Check-Server
```

### 2) Build and Run

```bash
dotnet restore
dotnet build
dotnet run --project src/McpHealthServer
```

### 3) (Optional) Run Tests

```bash
dotnet test
```

**Minimum expected tests:**

* Initialize returns `session_id` + tools
* SSE stream binds to `session_id` and emits handshake/ready
* `check_api_status` returns correct `UP/DOWN`
* Two sessions in parallel don’t mix responses

---

## How to Submit (PR)

1. **Fork** the repository.

2. Create a new branch:

   ```bash
   git checkout -b solution/<username>
   ```

3. Place your solution here:

   ```text
   solutions/C#/<username>/
     ├── src/...
     ├── tests/...
     └── README.md (optional: architecture notes)
   ```

4. Open a Pull Request titled:

   ```text
   [Solution] Standalone MCP Health Check Server - <username>
   ```

---

## Evaluation Criteria

| Criteria                                            | Weight  |
| --------------------------------------------------- | ------- |
| Correctness (end-to-end flow + tool works)          | 35%     |
| Code Quality (structure, clarity, SOLID)            | 15%     |
| MCP Lifecycle Compliance (Initialize/Handshake/SSE) | 15%     |
| Concurrency & Session Management                    | 15%     |
| Observability & Security (logging, SSRF)            | 5%      |
| Documentation (clear endpoints + run guide)         | 5%      |
| **Deliverable Quality (runnable, clean repo)**      | **10%** |

---

## Timeline

* Start: `2025-12-30`
* PR Submission Deadline: `2026-01-06`

---

## Contact

* Use **GitHub Issues** in this repository for questions and clarifications.
