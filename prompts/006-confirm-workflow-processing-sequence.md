Date: 2026-09-14

Purpose: Confirm n8n ownership of the event-processing and notification-delivery sequence.

Yes, I had something like this in mind:

Schedule
   ↓
Fetch RSS
   ↓
Normalize item
   ↓
Insert event / deduplicate
   ↓
Was event new?
   ↓ yes
Load active alert rules
   ↓
Evaluate conditions
   ↓
For each matching alert
   ↓
Create delivery if not exists
   ↓
Email / Slack
   ↓
Update delivery status
