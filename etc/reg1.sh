echo Bot creation script 1
cd ..\blenderbot-py
7za a ..\bot.zip .
set PASSWD=AtLeastSixteenCharacters_66
az ad app create --display-name "blenderbot" --password "%PASSWD%" --available-to-other-tenants
echo Now locate app id and execute SET APPID=....
