---
title: Delegated setup fails in Exchange 2013
date: 2014-02-04 00:00:00
tags:
- Exchange Server
- Powershell
---

In Exchange 2013, the built-in **Delegated Setup** role group allows users to install new Exchange 2013 servers after those servers have been provisioned with the **/NewProvisionedServer** switch. However, you may find that even after provisioning the server, when a member of **Delegated Setup** attempts to install the server, it fails. The setup log from the delegated setup attempt shows:

```
[11/07/2013 21:11:33.0015] [1] Failed [Rule:GlobalServerInstall] [Message:You must be a member of the 'Organization Management' role group or a member of the 'Enterprise Admins' group to continue.]

[11/07/2013 21:11:33.0031] [1] Failed [Rule:DelegatedBridgeheadFirstInstall] [Message:You must use an account that's a member of the Organization Management role group to install or upgrade the first Mailbox server role in the topology.]

[11/07/2013 21:11:33.0031] [1] Failed [Rule:DelegatedCafeFirstInstall] [Message:You must use an account that's a member of the Organization Management role group to install the first Client Access server role in the topology.]

[11/07/2013 21:11:33.0031] [1] Failed [Rule:DelegatedFrontendTransportFirstInstall] [Message:You must use an account that's a member of the Organization Management role group to install the first Client Access server role in the topology.]

[11/07/2013 21:11:33.0031] [1] Failed [Rule:DelegatedMailboxFirstInstall] [Message:You must use an account that's a member of the Organization Management role group to install or upgrade the first Mailbox server role in the topology.]

[11/07/2013 21:11:33.0031] [1] Failed [Rule:DelegatedClientAccessFirstInstall] [Message:You must use an account that's a member of the Organization Management role group to install or upgrade the first Client Access server role in the topology.]

[11/07/2013 21:11:33.0031] [1] Failed [Rule:DelegatedUnifiedMessagingFirstInstall] [Message:You must use an account that's a member of the Organization Management role group to install the first Mailbox server role in the topology.]
```

This occurs if legacy Exchange administrative group objects exist from when Exchange 2003 was still present in the organization. Unfortunately, setup does not handle this gracefully in the delegated setup scenario.

To fix the problem, you could delete the legacy administrative groups, but we don't recommend this. Instead, a safer approach is to simply add an explicit deny for the **Delegated Setup** group on the legacy administrative groups. This prevents setup from seeing those admin groups, and it proceeds as normal. After setup is finished, you can remove the explicit deny to put the permissions back in their normal state.

Setting the explicit deny is fairly easy to do in ADSI Edit, but I've also written a simple script to make this easier when you have a lot of legacy admin groups. The script takes no parameters. Run it once to add the Deny, and run it again to remove the Deny:

{% asset_img sshot-4.png Image %}

[Download the script](https://gist.github.com/bill-long/8810381/raw/de92cc9ac0178f998547c214734ad5daa3800f0f/Fix-DelegatedSetup.ps1) (You may need to right click->save as)

{% gist 8810381 %}
