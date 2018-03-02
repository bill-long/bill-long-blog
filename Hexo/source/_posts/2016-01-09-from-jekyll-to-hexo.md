title: From Jekyll to Hexo
date: 2016-01-09 09:24:58
thumbnail: ruby.jpg
tags:
- Blog
- Azure
- Jekyll
- Hexo
---
{% asset_img ruby.jpg Picture of Ruby %}

Two years ago, I dove into the wonderful world of static blog generators when I left my TechNet blog behind and started using [Jekyll](http://jekyllrb.com/) to generate an Azure web site. With my newfound freedom from complex content management systems, I raved about Jekyll in a blog post. But once the honeymoon was over, some cracks started to appear in the relationship. 

Jekyll does not officially support Windows, so you have to jump through some hoops to get it up and running. This didn't seem so bad at first, but I'm one of those people who is constantly tinkering with my PC, buying new hardware, and upgrading things, so I end up doing a clean install of my OS several times a year.

Back in the day, a clean install of Windows might sound daunting, but these days, it only takes minutes. The Windows install itself is pretty fast, and I have several [Boxstarter](http://boxstarter.org/) scripts that use [Chocolatey](https://chocolatey.org/) to install all the software I use. This means getting back up and running is fairly painless - except for Jekyll.

It seemed like every time I got a clean install, that fresh new clean OS feeling was soon soured by errors from Jekyll. The hoops I had to jump through to get it up and running would change slightly each time due to changes in [Ruby](https://www.ruby-lang.org) or problems with gems. For a while, I dealt with this issue by blogging from one of my Ubuntu VMs.

Finally, I started shopping around for something not based on Jekyll and preferably with no Ruby dependency at all. There are [a lot of options](https://www.staticgen.com/), but for now, I've settled on [Hexo](https://hexo.io).

Hexo is powered by [Node.js](https://nodejs.org/), and since I'm a big fan of JavaScript and a big fan of npm, this seems like a natural fit. Maybe this will be enough motivation to continue the series I left off with, or at least to write a new technical post of some kind.