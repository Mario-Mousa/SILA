<!--
═══════════ HANDOFF ═══════════════════════════════════
Task:        T1.4 | Owner: P4 (QA & Business Analyst) | Status: DONE
What exists: Literature Review, Academic Grading Criteria Breakdown, and DEPI Supervisor Feedback Log.
What is left: // TODO(T4.4): Incorporate finalized supervisor review notes and citations into final graduation documentation.
Depends on:  DEPI Full Stack .NET Track Guidelines
Used by:     Team Lead (P1), Documentation Owner (P2), Academic Evaluators
How to test: Review against DEPI graduation rubric; verify alignment with academic B2B supply chain literature.
Notes:       Maintains an active ledger for instructor feedback items and milestone approvals.
═══════════════════════════════════════════════════════
-->

# SILA (صِلة) — Literature Review & Academic Evaluation Framework

> **Document:** Academic Literature Review, DEPI Grading Criteria & Supervisor Feedback  
> **Task:** T1.4 | **Owner:** P4 (QA & Business Analyst)  
> **Program:** Digital Egypt Pioneers Initiative (DEPI) — Full Stack .NET Track  
> **Academic Supervisor:** DEPI Course Instructor / Mentor ("the Eng.")  
> **Last Updated:** 29 October 2026  

---

## 1. Academic Literature Review & Problem Space

### 1.1 The Egyptian Apparel Supply Chain Context
The Egyptian ready-made garment (RMG) sector represents a cornerstone of national manufacturing and export revenue. However, Small and Medium Enterprises (SMEs) — specifically independent brand designers and specialized sewing workshops — operate in an information vacuum:
- **Informal Brokerage:** Discovery of certified fabric mills and assembly factories relies predominantly on word-of-mouth and unverified social media directories.
- **Price Opacity:** Volatility in raw yarn and textile prices creates friction during quoting, leading to unrecorded negotiations that are vulnerable to mid-production disputes.
- **Supply Instability:** Without centralized inventory visibility, small clothing brands frequently commit to production runs only to discover that necessary fabric yardage is out of stock.

### 1.2 Comparison with Existing Platforms

| Platform / Approach | Primary Focus | Limitations Addressed by SILA |
|---|---|---|
| **Alibaba / Global Sources** | Large-scale international B2B export | High Minimum Order Quantities (MOQs); prohibitive shipping/import duties; lack of local currency support (EGP); disconnected from Egyptian domestic suppliers. |
| **Local Social Media (FB / Instagram)** | Informal marketing and networking | Zero transactional guarantees; no inventory reservation; untracked price negotiation; no formal account verification. |
| **Generic ERP Systems (Odoo / SAP)** | Internal enterprise resource management | Intended for a single organization; high implementation costs; prohibitive complexity for local Egyptian SME brand owners. |
| **SILA (صِلة)** | **Local Egyptian B2B Fashion Supply Chain** | Specialized for local fashion SMEs; dual isolated workflows (custom manufacturing vs. fabric purchasing); atomic stock reservation; structured negotiation in EGP. |

---

## 2. DEPI Track Grading Criteria & Compliance Matrix

The project is structured to directly fulfill the evaluation rubrics established by DEPI for the Full Stack .NET Graduation Project:

| Evaluation Dimension | Weight | Required Academic Artifacts | SILA Project Compliance / Implementation |
|---|---|---|---|
| **1. Planning & Management** | 15% | Agile methodology, sprint breakdowns, task assignments, Gantt chart, risk management. | Documented in `docs/1-planning/planning-and-management.md`; GitHub Projects Kanban board; 4 sprint phases; measurable KPIs. |
| **2. Requirements & Analysis** | 20% | Functional & Non-Functional requirements, Use Cases, business rules, working decisions. | Formally documented in `docs/2-requirements/system-requirements.md`; Decisions D-01..D-12; Rules BR-01..BR-11; Given-When-Then criteria. |
| **3. System Design & Architecture** | 25% | ERD, Schema, Class diagrams, State diagrams, Sequence diagrams, Activity diagrams, DFDs, Component/Deployment models. | Authored across `docs/3-design/*`; Mermaid models for all required UML and architectural diagrams; 3NF normalized schema with snapshot denormalization. |
| **4. Implementation & Quality** | 30% | Clean C# code, ASP.NET Core MVC, EF Core Migrations, Identity auth, server-side validation, atomic transactions. | Implemented in `src/SILA.Web/`; Thin controllers; Decoupled service layer; RowVersion concurrency token; Zero hardcoded strings. |
| **5. Testing & Verification** | 10% | Comprehensive test plan, concurrency testing, authorization negative tests, usability results. | Formally planned in Phase 4 (T4.1); Defect log and QA reports mapped to acceptance criteria. |

---

## 3. Supervisor ("the Eng.") Feedback & Action Log

This section records direct feedback, evaluation comments, and required refinements provided by the DEPI instructor:

| Log ID | Date | Instructor Feedback / Observation | Action Taken / Architectural Resolution | Status |
|---|---|---|---|---|
| **FB-01** | 25 Oct 2026 | Advised team to eliminate SMS OTP verification due to telecommunications service costs and balance constraints. | Adopted **Decision D-04**: Use SMTP email confirmation exclusively for account registration; in-app notifications for all operational alerts. | **Resolved** |
| **FB-02** | 26 Oct 2026 | Questioned whether material stock should be deducted at checkout or at supplier confirmation. | Adopted **Decision D-05 & Rule BR-03**: Stock is validated at checkout but deducted **atomically only when the Supplier confirms** the order in a single transaction. | **Resolved** |
| **FB-03** | 27 Oct 2026 | Clarified that factories must be able to negotiate price via counter-offers, but fabric suppliers sell at fixed catalog prices. | Adopted **Decision D-03**: Factories support counter-offers (`CounterOffered` state); raw materials feature strictly fixed catalog unit prices. | **Resolved** |
| **FB-04** | 27 Oct 2026 | Warned against project scope creep (online payments, shipping integrations, production stage tracking). | Updated **Section 3.3 (Out of Scope)**: Explicitly banned payment gateways, logistics tracking, and shop-floor scheduling to preserve 5-week focus. | **Resolved** |
| **FB-05** | 28 Oct 2026 | Requested formal Gantt chart and explicit agile sprint definitions matching real dates (28 Oct – 4 Dec). | Generated Mermaid Gantt chart and comprehensive Agile methodology in `docs/1-planning/planning-and-management.md`. | **Resolved** |

