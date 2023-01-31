# Core Service

C# MVP Implementation of Core Service.

## Usage

> NOTE: WIP

```bash
$ cp build/docker-compose.yaml docker-compose.yaml
$ mkdir /path/to/a/directory
$ vim .env
# PERSISTENCE__HOST=/path/to/a/directory
$ docker network create core_service
$ docker-compose up -d
$ curl -vvv http://localhost:80/
```
