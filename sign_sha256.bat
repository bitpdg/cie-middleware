@echo off
%~d0
cd %~p0
if EXIST sign_sha256_local.bat (
    sign_sha256_local.bat %1
)
ELSE (
    "C:\Program Files (x86)\Microsoft SDKs\ClickOnce\SignTool\signtool.exe" sign /fd sha256 /sha1 e0402d01f6292e55540e018df14130234ec08306 /tr http://sha256timestamp.ws.symantec.com/sha256/timestamp /td sha256 %1
)
