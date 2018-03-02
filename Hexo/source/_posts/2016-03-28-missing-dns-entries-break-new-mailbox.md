title: Missing DNS entries break New-Mailbox
date: 2016-03-28 11:03:17
thumbnail: ruby.jpg
tags:
- Exchange Server
- Active Directory
- DNS
---
{% asset_img ruby.jpg Picture of Ruby %}

It's interesting how fairly obvious settings can break things in very non-obvious ways.

We recently had a case where the customer was not able to create new mailboxes on Exchange 2007. This had worked fine prior to applying some updates. After the updates, the **New-Mailbox** cmdlet began failing with error indicating that an address generator DLL was missing.

That error was a little misleading. The application log showed a very different error:

```
ID: 2030
Level: Error
Source: MSExchangeSA
Message: Unable to find the e-mail address 'smtp:someone@contoso.com    SMTP:someone@contoso.com    ' in the directory. Error '80072020'.
```

That error code is **ERROR_DS_OPERATIONS_ERROR** - not very specific. After a lot of tracing, we eventually found that when we created the new mailbox, Exchange was generating the new email addresses and then searching to see if they exist. The expected result is that the search returns 0 results, so we know the new addresses are unique. But in this case, wldap32 was returning code 1, **LDAP_OPERATIONS_ERROR**.

We used `psexec -i -s ldp.exe` to launch **ldp** as localsystem on the Exchange server, and then connected to the DCs. Choosing to bind as **Current logged-on user** showed that we bound as the computer account, as expected, and the searches worked fine. Then, some additional tracing revealed that we were not connecting to the DCs by name - we were connecting to the domain name, **contoso.com** in this example.

When we used **ldp** to connect to the domain name, something interesting happened - we were no longer able to bind as the computer account. The bind would succeed, but would return `NT AUTHORITY\Anonymous Logon`. Attempting to search while in that state produced:

```
***Searching...
ldap_search_s(ld, "(null)", 2, "(proxyAddresses=smtp:user@contoso.com)", attrList,  0, &msg)
Error: Search: Operations Error. <1>
Server error: 000004DC: LdapErr: DSID-0C0906E8, comment: In order to perform this operation a successful bind must be completed on the connection., data 0, v1db1
Error 0x4DC The operation being requested was not performed because the user has not been authenticated.
Result <1>: 000004DC: LdapErr: DSID-0C0906E8, comment: In order to perform this operation a successful bind must be completed on the connection., data 0, v1db1
Getting 0 entries:
```

That was exactly what we were looking for! Operations error, code 1, which is **LDAP_OPERATIONS_ERROR**. At this point, we turned our attention to understanding why we could not authenticate to the domain name, when authenticating to the server name worked fine. After all, connecting to the domain name just connected us to one of the DCs that we had already tested directly - we could see that by observing the **dnsHostName** value. So why would the name we used to connect matter?

The Active Directory engineeers eventually discovered that the **_sites** container, **_tcp** container, and other related DNS entries were all missing. Dynamic DNS had been disabled in this environment. Once it was enabled, everything worked.

The moral of this story is to be careful when you disable a setting that is, at face value, a simple and obvious thing. The effects can ripple out in very unexpected ways.