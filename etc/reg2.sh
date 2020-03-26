echo Make sure APPID is set!
pause
az group deployment create --resource-group "BlenderBot" --template-file "deploymentTemplates\template-with-preexisting-rg.json" --parameters appId="%APPID%" appSecret="%PASSWD%" botId="blenderbot" newWebAppName="blenderbot" newAppServicePlanName="blenderbot_app" appServicePlanLocation="northeurope" --name "blenderbot"
az webapp deployment source config-zip --resource-group "BlenderBot" --name "blenderbot" --src ..\bot.zip
