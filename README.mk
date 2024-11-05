
## Installtion
### OS X
Configure the Docker daemon
"hosts": [
    "tcp://0.0.0.0:2375",
    "unix:///var/run/docker.sock"
  ]
Enable TLS
$ socat -d TCP-LISTEN:2376,range=127.0.0.1/32,reuseaddr,fork UNIX:/var/run/docker.sock

$ curl localhost:2376/version

## References
https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
https://stackoverflow.com/questions/52838808/how-to-enable-expose-daemon-on-tcp-localhost2375-without-tls-on-mac