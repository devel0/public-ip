# public ip

## description

Simple webapi that returns the public ip of the connecting client

![](./doc/dia.svg)

## quickstart

```sh
dotnet build -c Release
```

- copy `src/bin/Release/net9.0/publish` folder to some `/srv/app/public-ip`

- setup a nginx conf `/etc/nginx/conf.d/util.somedomain.conf` to proxy this service replacing
  - `somedomain` with the domain name ( to enable https cert [see here][2] )
  - `UTILSERVERIP` with the ip where this service run ( see [setup systemd][1] )

```nginx
server {
  root /var/www/html;

  server_name util.somedomain;
  access_log /var/log/nginx/util.access.log;
  error_log /var/log/nginx/util.error.log notice;  

  location /ip/ {
    rewrite /ip/(.*) /$1 break;
    include /etc/nginx/mime.types;
    
    proxy_set_header Host $host;
    proxy_pass http://UTILSERVERIP:5001;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
  }

  listen 443 ssl;
  ssl_certificate /etc/letsencrypt/live/somedomain.com/fullchain.pem;
  ssl_certificate_key /etc/letsencrypt/live/somedomain.com/privkey.pem;
  include /etc/letsencrypt/options-ssl-nginx.conf;
  ssl_dhparam /etc/letsencrypt/ssl-dhparams.pem;
}

server {
  if ($host = util.somedomain.com) {
    return 301 https://$host$request_uri;
  }

  server_name util.somedomain.com;
  listen 80;
  return 404;
}
```

connect to `https://util.somedomain/ip` to get public ip of connecting client.

## deploy samples

### server



### publish with script

following and example of a `publish.sh` script to publish the app to a server

```sh
#!/bin/bash

exdir=$(dirname $(readlink -f "$BASH_SOURCE"))

cd "$exdir"

dotnet publish -c Release

rsync -arvx --delete "$exdir"/src/bin/Release/net9.0/publish/ main-apps:/srv/app/public-ip/bin/

ssh main-apps service public-ip restart
```

### service

following and example for the [systemd service][1] `/etc/systemd/system/public-ip.service`

```conf
[Unit]
Description=Public ip utility
After=network.target
StartLimitIntervalSec=0

[Service]
Type=simple
Restart=always
RestartSec=10
User=user
Group=user
SyslogIdentifier=public-ip
KillSignal=SIGINT
EnvironmentFile=/root/security/public-ip.env
ExecStart=/srv/app/public-ip/bin/PublicIp

[Install]
WantedBy=multi-user.target
```

where the `/root/security/public-ip.env` could contains

```sh
DOTNET_ROOT=/opt/dotnet
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://UTILSERVERIP:5001
```

## how this project was built

```sh
mkdir public-ip

dotnet new sln
dotnet new gitignore
dotnet new webapi -o src -n PublicIp -f net9.0 --no-openapi --no-https
dotnet sln add src
```

[1]: https://github.com/devel0/knowledge/blob/d17acddf694682e76fd5c66e4221c9ec4f66e75d/doc/systemd-service.md
[2]: https://github.com/devel0/knowledge/blob/8d23e89bc1bcc1e2e658eb4e5f4f36b38ec9d13b/doc/letsencrypt-acme-dns.md
