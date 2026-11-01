---
name: Schema Change Request
about: Request a database schema modification (assigned strictly to Database Owner P2)
title: '[Schema Change] <Table or Entity>'
labels: ['schema-change']
assignees: ''
---

### Requesting Task & Developer
- **Task ID**: T_._
- **Requested by**: P_

### Proposed Change
- **Table / Entity**: 
- **Columns to Add / Modify / Delete**:
- **Data Types & Constraints** (e.g. `decimal(18,2)`, max lengths, foreign keys, indexes):

### Business Justification
<!-- Why is this change necessary for your service or workflow? -->

### Action for Database Owner (P2)
- [ ] Review request against PROJECT_CONTEXT.md rules
- [ ] Update Entity classes and DbContext
- [ ] Create EF Core Migration
- [ ] Verify clean update and notify team

