cd "..\Hexo"
hexo clean
hexo generate
Remove-Item -Recurse -Force "..\Web\wwwroot"
Copy-Item -Recurse ".\public" "..\Web\wwwroot"