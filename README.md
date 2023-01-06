# Core Service

C# MVP Implementation of Core Service.

## Usage

> NOTE: WIP

```bash
$ cp docker-compose{.sample,}.yml
$ vim docker-compose.yml
# Focus on `services -> core_service -> environment -> PERSISTENCE_HOST'
$ docker network create core_service
$ docker-compose up -d
$ curl http://localhost/api/status
```
