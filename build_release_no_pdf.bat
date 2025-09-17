@echo off
dotnet publish -c Release -p:SkipPdf=true
pause
