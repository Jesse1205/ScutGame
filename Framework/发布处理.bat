@echo off
xcopy /y lib\*.dll ..\..\Release\6.7.10.0\Lib\
xcopy /y ZyGames.Framework\bin\ZyGames.Framework.* ..\..\Release\6.7.10.0\Lib\
xcopy /y ZyGames.Framework.Common\bin\ZyGames.Framework.Common.* ..\..\Release\6.7.10.0\Lib\

ECHO ������ϣ�& PAUSE