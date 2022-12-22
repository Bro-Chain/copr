# CØPR - Cosmos Proposal Bot 

![Sonarcloud](https://github.com/Bro-Chain/copr/actions/workflows/sonarcloud.yml/badge.svg?branch=main)
![Docker Build & Publish](https://github.com/Bro-Chain/copr/actions/workflows/docker-publish.yml/badge.svg?branch=main)

## What is this?

This small project is a bot built for Discord using .Net 6. This, in turn, means that it can run on any platform where the .Net 6 runtime is available.

### Ok, why .Net?

It's was just faster for me to build it that way as I've used the .Net libraries to build Discord bots before 

## How do I run it? 

CØPR can run as a standalone executable or as a Docker container.  

### Run CØPR standalone
Update the [appsettings.json](https://github.com/Bro-Chain/copr/blob/main/CosmosProposalBot/appsettings.json) file with whatever parameters you need and start it! If you're having issues and what to find out why, we recommend updating LogLevel values to Trace/Debug first of all, as the bot will become a lot more chatty that way. 

### Run CØPR as a Docker container

The easiest way to run CØPR is to use the Docker image. You can find published images on [Docker Hub](https://hub.docker.com/r/brochain/copr), or you can build and run an image yourself locally.
For example, use the provided [docker-compose.yml](https://github.com/Bro-Chain/copr/blob/main/docker-compose.yml) in order to get going quickly. 

_**Please note**_ that you **must** set the `BotOptions__DiscordApiToken` environment variable in [docker-compose.yml](https://github.com/Bro-Chain/copr/blob/main/docker-compose.yml), or the bot will not be able to start!

### Dependencies

* A bot application registered with in the [Discord developer portal](https://discord.com/developers)
* SQL Server