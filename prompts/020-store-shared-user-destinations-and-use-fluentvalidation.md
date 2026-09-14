Date: 2026-09-14

Purpose: Correct destination ownership and replace custom validation with an existing framework.

A few things need to be fixed.

For example, we currently get the email and slackId from appsettings. This is completely the wrong direction; we did not agree on this. There can be a "dummy" user table with an Id where this configuration can be specified. We do not need to implement Identity, but these data must be stored in the DB because n8n will need them too. Because of this, the DB design is not necessarily right either. Although putting the given channel on the alert can work, in real life it should probably come from the user table. If we want to be able to specify a channel for every individual "subscribe", it can stay this way (in that case we do not need a user table), but if not, and there is only one email or one Slack, then this can be removed from the alert. A join will tell us where to send the given alert to the user.

The other issue is validation. You have now built completely custom validation. Instead, let's use an existing framework. For example, FluentValidation (but in that case, make sure we use a version that is "free").
