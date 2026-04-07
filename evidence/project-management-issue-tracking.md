---
title: Project Management and Issue Tracking
tags: [project-management, jira, trello, github, agile, scrum, sprint-planning, kanban, ticketing]
---

## Project Management Methodology

Bryan has managed development workflows across multiple issue tracking platforms throughout his career, with direct involvement in workflow design, sprint planning, and requirements documentation.

## Jira — Veriship/Ticket Solutions (Architect, 2011–2013)

As the architect at Veriship, Bryan worked directly with the PM/Scrum Master to design the Jira workflow: issue types, transition rules, sizing conventions, and sprint scoping criteria. The team used Fibonacci sizing with a hard rule that no task larger than a 3 could be scheduled into a sprint — anything larger was decomposed further before acceptance.

Bryan co-ran planning meetings and was responsible for thorough requirements documentation. Tickets were verbose by design: each included acceptance criteria, context, and edge cases before entering a sprint. The philosophy was that a well-scoped small task eliminates ambiguity and reduces mid-sprint surprises.

The result was measurable: sprint burndown charts showed near-perfect adherence to the projected guideline. The remaining-work line tracked the ideal burndown closely, with a characteristic stair-step pattern — work accumulating over weekends and catching up during the week. This predictability enabled reliable long-term forecasting for the business, turning sprint velocity into an actual planning tool rather than a retrospective metric.

## Jira — Kansys (Architect, 2020–2023)

Used Jira for project tracking during the legacy telecom billing platform modernization. Managed the transition from ad-hoc development practices to structured workflows as part of establishing coding standards and modern tooling across the team.

## Trello + GitHub Issues — Madera/Call-Trader (Lead Engineer, 2024–2025)

Operated a dual-board approach: Trello guided epics and high-level feature planning, while GitHub Issues tracked individual implementation tasks. This split reflected the team structure — business stakeholders followed progress in Trello, while developers worked from GitHub Issues linked to branches and PRs.

GitHub Issues demonstrate structured task decomposition. The "Make MailFiles work" epic was broken into granular verification tasks, each written in Arrange/Act/Assert format:

- Issue #1: Full architectural overview documenting 4 database schemas, state machine transitions, and behavioral specifications for the MailFile lifecycle
- Issues #2–#10: Individual verification tasks for each state transition (Create, Kneading→Shaking, Shaking→Baking, Baking→Complete), each with explicit setup conditions, actions, and expected outcomes
- Issue #11: Cleanup pass after all transitions verified

This pattern — architectural documentation as the epic, decomposed verification tasks as the implementation work — was repeated across multiple features in the platform.

## Cross-Tool Experience

Bryan has used Jira, Trello, and GitHub Issues across different organizations and team structures. The common thread is not the specific tool but the approach: verbose requirements, small task sizes, measurable velocity, and documentation that outlives the sprint. The tooling is interchangeable; the discipline is portable.
