# IFPA Companion For Discord
 
Discord Bot that provides information about Pinball Rankings via the [IFPA](https://www.ifpapinball.com)

To add IFPA Discord Companion to your server, use the following link:

https://discord.com/api/oauth2/authorize?client_id=932451797749075998&permissions=84992&scope=bot

To run this project, you'll need an [IFPA API Key](https://www.ifpapinball.com/api/request_api_key.php) and a [Discord Bot Token](https://github.com/reactiflux/discord-irc/wiki/Creating-a-discord-bot-&-getting-a-token) , both placed in the appsettings.json file.

# Synology NAS Deployment for IFPA Companion Discord Bot

## Prerequisites
- Container Manager installed via Synology Package Center.
- SSH access to NAS.
- GHCR Personal Access Token (PAT) with `read:packages` scope.

## One-time Setup
```bash
docker login ghcr.io -u edgiardina --password-stdin

## Deploying Bot + Watchtower
```bash
copy docker-compose.yml
docker compose up -d
