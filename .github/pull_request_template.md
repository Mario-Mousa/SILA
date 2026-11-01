## Pull Request Details

### Task Reference
Closes #<issue_number>
Task ID: T<phase>.<number> (e.g. T1.2, T2.1)
Owner: P<1|2|3|4>

---

### What Was Done
- [ ] Implemented feature / task requirements
- [ ] Maintained all XML doc comments and HANDOFF comment blocks
- [ ] Preserved all existing comments and business rules
- [ ] Zero build warnings / errors

---

### How to Test
1. Run `dotnet ef database update --project src/SILA.Web` (if schema changes exist)
2. Run `dotnet run --project src/SILA.Web`
3. Steps to verify:
   - ...

---

### Review Checklist (For Reviewer)
- [ ] Business logic resides in Services layer, not in Controllers or Views
- [ ] Record-level ownership (IOwnershipService) checked where appropriate
- [ ] No hardcoded statuses (Enums used exclusively)
- [ ] No migrations created unless PR is by Database Owner (P2)

