@echo off
set PHPDIR=..\..\apache\php\
"%PHPDIR%\php.exe" monitor-iis-webpage-check.php %1
