---
title: Moving from TechNet blogs to Jekyll on Windows Azure
date: 2014-01-01 00:00:00
tags:
- Blog
- Azure
- Jekyll
- Technet
---

{% asset_img ruby01.jpg Image %}
For nearly a decade, I've been occasionally blogging on the [Exchange Team Blog](http://blogs.technet.com/b/exchange/), and later on my [personal TechNet blog](http://blogs.technet.com/b/bill_long/). Those platforms are stable, easy to use, and perfectly acceptable. But they're not much fun. I want something I can tweak, break, and put back together again.

Now that cloud hosting has become so cheap (free web sites on [Windows Azure](http://www.windowsazure.com/en-us/)!) and managing/updating a web site has become so easy (deployment from [GitHub](https://github.com/) or a local Git repository!), I've decided to try blogging on a platform that is basically the complete opposite of every other major blogging platform.

It's called [Jekyll](http://jekyllrb.com/), and it's the platform used for [GitHub Pages](http://pages.github.com/). What makes it so different is that your blog is a **static** site - it's just html and css files sitting on disk, which are served up to the browser as-is. No controllers, no server-side view engine, and no database. To add a new blog post, you literally just drop a text file in a folder, and run Jekyll to update the html files. Done.

A complex content management system with an underlying database, such as Wordpress, is more user-friendly as a hosted solution. However, when you're running the site yourself, all that complexity can make for a lot of extra work. Being able to manage my blog posts by just altering text files in a folder is pretty amazing.

Did I mention it also has code highlighting for practically every language under the sun, including Powershell? Now when I post a script that is a hundred lines long, it might actually be somewhat readable.

{% codeblock lang:powershell %}
if ($ExchangeServer -eq "")
{
    # Choose a PF server
    $pfdbs = Get-PublicFolderDatabase
    if ($pfdbs.Length -ne $null)
    {
        $ExchangeServer = $pfdbs[0].Server.Name
    }
{% endcodeblock %}

Alright, I've gushed about Jekyll enough. If you're interested in a different kind of blogging platform, go check it out. Otherwise, stay tuned for more Exchange-related posts.
