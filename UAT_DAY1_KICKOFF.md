# UAT Day 1 Kickoff - Quick Start Guide

**Date**: 2026-07-04  
**Time**: 9:00 AM - 5:00 PM  
**Location**: [Conference room / Zoom link]

---

## Morning Session (9:00 AM - 12:00 PM)

### 1. Welcome & Overview (30 min)
**Led by: Product Owner**

- [ ] Introduce Synaptix Operator Dashboard project
- [ ] Explain UAT objectives and scope
- [ ] Review success criteria
- [ ] Q&A

### 2. Dashboard Features Overview (60 min)
**Led by: Dev Lead**

- [ ] What is the operator dashboard?
- [ ] Core functionality overview
- [ ] 21 modules walkthrough
- [ ] Key features demo
- [ ] Live walkthrough of main modules

### 3. Access & Environment Setup (30 min)
**Led by: DevOps Lead**

- [ ] Staging environment URL
- [ ] Login credentials distributed
- [ ] First-time login walkthrough
- [ ] Account access verified
- [ ] Environment health check

### 4. Tools & Reporting (30 min)
**Led by: UAT Lead**

- [ ] How to log issues (GitHub)
- [ ] Issue severity levels
- [ ] Daily standup process
- [ ] Report templates
- [ ] Q&A

---

## Afternoon Session (1:00 PM - 5:00 PM)

### 5. Environment Navigation (60 min)
**Activity: Hands-on exploration**

**All testers, follow these steps**:

```
Step 1: Access the dashboard
├─ URL: https://dashboard-staging.synaptixplay.com
├─ Email: uat-admin@synaptix.com
├─ Password: [provided in kickoff]
└─ Expected: Login page with mock mode option

Step 2: Enable mock mode (for first-time users)
├─ Click "Enable Mock Mode" button
├─ This allows testing without backend data
└─ Expected: Redirect to dashboard

Step 3: Explore home page
├─ Review dashboard stats
├─ Check system health indicators
├─ Click a few links to familiarize
└─ Expected: All elements load without error

Step 4: Open Developer Tools
├─ Press F12 (Chrome/Edge) or Cmd+Option+I (Safari)
├─ Go to Console tab
├─ Expected: No red errors (warnings OK)
```

### 6. Basic Functionality Test (60 min)
**Activity: Smoke test all modules**

**For each module below**:
1. Click the link in sidebar
2. Verify page loads (< 3 seconds)
3. Check for console errors
4. Verify data displays (if applicable)
5. Take screenshot if any issues

**Modules to smoke test**:
- [ ] Users
- [ ] Store
- [ ] Config
- [ ] Anti-Cheat
- [ ] Economy
- [ ] Notifications
- [ ] Personalization
- [ ] Installer
- [ ] Storage
- [ ] Diagnostics
- [ ] Skills
- [ ] Match History
- [ ] Event Queue
- [ ] Operations
- [ ] Content
- [ ] [Others]

### 7. Issue Reporting Practice (60 min)
**Activity: Find and report a non-critical issue**

**Assignment**: Find any minor issue and practice reporting:
- Take screenshot
- Document steps to reproduce
- Use issue template (in UAT_EXECUTION_PLAN.md)
- Assign appropriate severity (Medium/Low for practice)
- Submit to #uat-support

**Examples of practice issues**:
- Minor text typo
- Spacing inconsistency
- Missing tooltip
- Slow-loading element

---

## Evening (4:00 PM - 5:00 PM)

### 8. Q&A & Preparation for Day 2 (60 min)
**Led by: UAT Lead**

**Checklist**:
- [ ] Everyone has access to staging
- [ ] Everyone can log in successfully
- [ ] Everyone understands issue reporting
- [ ] Everyone has test assignments for Day 2
- [ ] Questions answered
- [ ] Tomorrow's plan confirmed

**Tomorrow's Focus**: Authentication & Login Flows

---

## Materials Needed

### Documents
- [ ] UAT_EXECUTION_PLAN.md (print or PDF)
- [ ] UAT_CHECKLIST.md
- [ ] DEPLOYMENT_GUIDE.md (reference)

### Tools
- [ ] Modern web browser (Chrome/Firefox/Safari/Edge)
- [ ] Developer Tools knowledge
- [ ] GitHub account (for issue reporting)
- [ ] Slack (for communication)

### Access
- [ ] Staging environment URL
- [ ] Login credentials
- [ ] GitHub project link
- [ ] Slack channel (#uat-support)

---

## Signup Sheet

| Name | Role | Signature | Time |
|------|------|-----------|------|
| | Tester #1 | | |
| | Tester #2 | | |
| | Performance | | |
| | Security | | |
| | UAT Lead | | |
| | Dev Support | | |

---

## Day 1 Success Criteria

At end of day, confirm:

- [ ] All UAT team members have staging access
- [ ] All can log in successfully
- [ ] All understand issue reporting process
- [ ] All completed smoke test of modules
- [ ] No critical blockers identified
- [ ] Daily standup confirmed for tomorrow 10 AM

---

## Day 2 Preview

**Authentication Testing**
- Login with valid credentials
- Error handling (invalid email, short password)
- Session persistence
- Forgot password flow
- Logout functionality

**Time**: 9:00 AM - 5:00 PM  
**Lead**: Tester #1  
**Expected Issues**: 2-5 (minor formatting/UX)

---

## Emergency Contacts

| Role | Name | Phone | Slack |
|------|------|-------|-------|
| UAT Lead | [TBD] | [TBD] | @uatlead |
| Dev Lead | Claude Haiku | N/A | @claude |
| DevOps | [TBD] | [TBD] | @devops |

---

## Parking Lot (Issues to Address Later)

Any questions that don't fit Day 1 scope:
- [ ] [Question/Issue]
- [ ] [Question/Issue]
- [ ] [Question/Issue]

*Will address in retrospective*

---

## Notes Section

```
[Space for team notes, observations, and feedback]

Date: 2026-07-04
Notes by: [Tester name]
──────────────────────────────────────────────────

[Notes here...]


```

---

**UAT Officially Started! 🚀**

*Next Stop: Day 2 - Authentication Testing*
