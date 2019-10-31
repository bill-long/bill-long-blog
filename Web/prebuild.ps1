cd "..\Hexo"
yarn
hexo clean
hexo generate
if (Test-Path "..\Web\wwwroot") {
  Remove-Item -Recurse -Force "..\Web\wwwroot"
}
Copy-Item -Recurse ".\public" "..\Web\wwwroot"