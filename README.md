# IFPA Companion For Discord
 
Discord Bot that provides information about Pinball Rankings via the [IFPA](https://www.ifpapinball.com)

To add IFPA Discord Companion to your server, use the following link:

https://discord.com/api/oauth2/authorize?client_id=932451797749075998&permissions=84992&scope=bot

To run this project, you'll need an [IFPA API Key](https://www.ifpapinball.com/api/request_api_key.php) and a [Discord Bot Token](https://github.com/reactiflux/discord-irc/wiki/Creating-a-discord-bot-&-getting-a-token) , both placed in the appsettings.json file.

### Docker Build Instructions

```
cd /IFPACompanionDiscord/IFPACompanionDiscord
docker build --tag ifpa-discord-image .
docker save -o ifpa-discord-image.tar ifpa-discord-image
```
- Copy tar file to docker host
- Delete existing Container and Image
- Load new Image from tar file
- Create new Container and start
