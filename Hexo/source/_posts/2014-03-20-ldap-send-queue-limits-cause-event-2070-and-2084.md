---
title: LDAP send queue limits cause event 2070 and 2084
date: 2014-03-20 00:00:00
tags:
- Exchange Server
- Active Directory
- Powershell
- C#
---

I recently worked on an issue where the domain controllers kept intentionally disconnecting the Exchange servers. The error messages that described the reason for the disconnect were rather misleading, and we ended up wasting quite a bit of time taking steps that had no chance of improving the situation. In this blog post, I'm going to document this behavior in detail, in hopes of saving anyone else who runs into this a lot of time and effort.

## The Problem ##

The behavior we observed was that Exchange would lose its connection to its config DC. Then, it would change DCs and lose connection to the new one as well. This would  repeat until it exhausted all in-site DCs, generated an event 2084, and started hitting out-of-site DCs, often returning the same error. Usually, the error we saw was a 0x51 indicating the DC was down:

~~~
Log Name:      Application
Source:        MSExchange ADAccess
Event ID:      2070
Task Category: Topology
Level:         Information

Description:
Process w3wp.exe () (PID=10860).  Exchange Active Directory Provider lost
contact with domain controller dc1.bilong.test.  Error was 0x51 (ServerDown)
(Active directory response: The LDAP server is unavailable.).  Exchange
Active Directory Provider will attempt to reconnect with this domain
controller when it is reachable.
~~~

Network traces revealed that the DC was intentionally closing the LDAP connection. Once we discovered that, we set the following registry value to 2 in order to increase the logging level on the DC:

```
HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Services\NTDS\Diagnostics\16 LDAP Interface Events
```

With that set to 2, the DC started generating a pair of Event ID 1216 events every time it disconnected Exchange. The second 1216 event it generated wasn't particularly helpful:

~~~
Log Name:      Directory Service
Source:        Microsoft-Windows-ActiveDirectory_DomainService
Event ID:      1216
Task Category: LDAP Interface
Level:         Warning
Description:
Internal event: An LDAP client connection was closed because of an error. 
 
Client IP:
192.168.0.190:8000 
 
Additional Data 
Error value:
1236 The network connection was aborted by the local system. 
Internal ID:
c0602f1
~~~

But the first one gave us something to go on:

~~~
Log Name:      Directory Service
Source:        Microsoft-Windows-ActiveDirectory_DomainService
Event ID:      1216
Task Category: LDAP Interface
Level:         Warning
Description:
Internal event: An LDAP client connection was closed because of an error. 
 
Client IP:
192.168.0.190:8000 
 
Additional Data 
Error value:
8616 The LDAP servers network send queue has filled up because the client
is not processing the results of it's requests fast enough. No more
requests will be processed until the client catches up. If the client
does not catch up then it will be disconnected. 
Internal ID:
c060561
~~~

The LDAP client, in this case, is Exchange. So this error means the Exchange server isn't processing the results of the LDAP query fast enough, right? With this information, we started focusing on the network, and we spent days pouring over network traces trying to figure out where the network bottleneck was, or if the Exchange server itself was just too slow. We also found that sometimes, the 2070 event would show a 0x33 error, indicating the same send queue problem that was usually masked by the 0x51 error:

~~~
Log Name:      Application
Source:        MSExchange ADAccess
Event ID:      2070
Task Category: Topology
Level:         Information

Description:
Process w3wp.exe () (PID=10860).  Exchange Active Directory Provider lost
contact with domain controller dc1.bilong.test.  Error was 0x33 (Busy)
(Additional information: The LDAP servers network send queue has filled
up because the client is not processing the results of it's requests fast
enough. No more requests will be processed until the client catches up.
If the client does not catch up then it will be disconnected.

Active directory response: 000021A8: LdapErr: DSID-0C06056F, comment:
The server is sending data faster than the client has been receiving.
Subsequent requests will fail until the client catches up, data 0, v1db1).
Exchange Active Directory Provider will attempt to reconnect with this
 domain controller when it is reachable.
~~~

We removed antivirus, looked at NIC settings, changed some TCP settings to try to improve performance, all to no avail. Also, we weren't able to reproduce the error using various LDAP tools. No matter what we did with Powershell, LDP, ldifde, or ADFind, the DC would not terminate the connection. It was only terminating the Exchange connections.

We eventually found out that this error had nothing to do with how fast the LDAP client was processing results, and it *is* possible to reproduce it. In fact, **you can reproduce this LDAP error at will in any Active Directory environment**, and I will show you exactly how to do it.

## LDAP Send Queue 101 ##

Here's how Active Directory's LDAP send queue limit works. The send queue limit is a per-connection limit, and is roughly 23 MB. When a DC is responding to an LDAP query, **and it receives another query over the same LDAP connection**, it first checks to see how much data it is already pushing over that connection. If that amount exceeds 23 MB, **it terminates the connection**. Otherwise, it generates the response to the second query and sends it over the same connection.

Think about that for a minute - it has to receive another LDAP query *over the same LDAP connection* while it's responding to other queries. You can do that? Yep. As noted in the wldap32 documentation on [MSDN](http://msdn.microsoft.com/en-us/library/ms806997.aspx):

> The rules for multithreaded applications do not depend on whether each thread shares a connection or creates its own connection. One thread will not block while another thread is making a synchronous call over the same connection. By sharing a connection between threads, an application can save on system resources. However, multiple connections give faster overall throughput.

Until now, I had always thought of LDAP as a protocol where you send one request and wait for the response before sending your next request over that connection. As it turns out, you can have multiple different threads all submitting different requests over the same connection at the same time. The API does the work of lining up the requests and responses and getting the right responses back to the right threads, and LDAP has no problem with this - at least, not until you hit the send queue limit.

This is why we could never reproduce this issue with other LDAP tools. Every single one of those tools issues one request and waits for the response, and in that case, it is impossible to get disconnected due to the send queue limit.

## The Solution ##

In the case of Exchange, we share the config DC connection between multiple threads. One thread would kick off a complete topology rediscovery, which involves querying for all the virtual directories in the environment. In this particular environment, there were thousands of virtual directories, and the properties on the OWA virtual directories can be relatively large. The DC would generate a response containing a page of virtual directory objects (we were using a page size of 1,000), and due to the number of properties on those objects, this response exceeded the 23 MB limit.

By itself, that wasn't enough to cause a problem. The problem happened when some other thread came along and used the same LDAP connection to ask for something else - maybe it just needed to read a property from a server object. When that second query hit the DC while the DC was still sending us the response to the virtual directory query, the DC killed the connection due to the send queue limit.

So, how can you avoid this? As a user of software, there's not much you can do except delete objects until the LDAP response is small enough to be under the send queue limit, or reduce the MaxPageSize in the Active Directory LDAP policies to force *everything* to use a smaller page size.

As a developer of software, there are a few approaches you can take to avoid this problem. One is to not submit multiple queries at the same time over a single connection; either wait for the previous query to return, or open a new connection. Another approach is to reduce the page size used by your query so that the response size doesn't exceed the send queue limit. That's the approach we're taking here, and the page size used for topology rediscovery is being reduced in Exchange so that the LDAP response to the virtual directory query doesn't exceed the send queue limit in large environments.

Note that this update to Exchange will fix one very specific scenario where you're hitting this error due to the size of the virtual directory query in an environment with hundreds of CAS servers. Depending on your environment, there may be other ways to produce this error that are unrelated to the virtual directories.

## Let's Break It On Purpose ##

After I thought I understood what was happening, I wanted to prove it by writing some code that would intentionally hit the send queue limit and cause the DC to disconnect it. This turned out to be fairly easy to do, and the tool is written in such a way that you can use it to reproduce a send queue error in any environment, even without Exchange. Note that causing a send queue error doesn't actually break anything - it just makes the DC close that particular LDAP connection to that particular application.

In order to produce a send queue error, you need a bunch of big objects. In my lab, I used a Powershell script to create 500 user objects and filled those user objects with multiple megabytes of totally bogus proxyAddress values. Here's the script:

{% gist 9657992 %}

If you run this script, you'll end up with some objects that look like this:

{% asset_img sshot-5.png Image %}

Lovely, isn't it? I needed a way to make these user objects really big, and stuffing a bunch of meaningless data into the proxyAddresses attribute seemed like a good way to do it.

Now that you have enough big objects that you can easily exceed the send queue limit by querying for them, all you need is a tool that will query for them on one thread while another thread performs other queries on the same LDAP connection. To accomplish that, I wrote some C# code and called it LdapSendQueueTest. Find the code on GitHub here: [https://github.com/bill-long/LdapSendQueueTest](https://github.com/bill-long/LdapSendQueueTest).

Once you compile it, you can use it to query those big objects and reproduce the send queue error:

{% asset_img sshot-6.png Image %}

In this example, 1 is the number of threads to spawn (not counting the main thread, which hammers the DC with tiny queries), and 50 is the page size. Apparently I went a little overboard with the amount of data I'm putting in proxyAddresses, because with these objects, the error reproduces even with just 1 thread and a relatively small page size of 50 or even 30. The only way I can get the tool to complete against these test users is to make the page size truly tiny - about 15 or less.

In any real world scenario, you can probably get away with a larger page size, because your objects probably aren't as big as the monsters created by this Powershell script. The tool lets you point to whatever container and filter you want, so you can always just test it against a set of real objects and see.

## Conclusion ##

The bottom line is this: When you see this error from Active Directory telling you the client isn't keeping up, the error doesn't really mean what it says. If you take a closer look at what the application is doing, you may find that it's sharing an LDAP connection between threads while simultaneously asking for a relatively large set of data. If that's what the application is doing, you can reduce the MaxPageSize in the LDAP policies, which will affect *all* software in your environment, or you can delete some objects or delete some properties from those objects to try to get the size of that particular query down. Ideally, you want the software that's performing the big query to be updated to use a more appropriate page size, but that isn't always possible.
