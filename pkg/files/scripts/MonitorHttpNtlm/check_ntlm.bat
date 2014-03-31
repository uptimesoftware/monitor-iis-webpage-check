@echo off
set PHPDIR=..\..\apache\php\
"%PHPDIR%\php.exe" ..\..\plugins\scripts\monitor-iis-webpage-check\monitor-iis-webpage-check.php %1
