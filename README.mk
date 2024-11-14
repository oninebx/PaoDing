
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

Useful scripts
insert into DbEndpoint(EndpointKey, Host, Port, Username, PasswordHash, CreatedAt, IsActive) values('dev-sales','localhost', '1433', 'sa', 'Password@Ryan', CURRENT_TIMESTAMP, true);

curl http://localhost:2375/containers/json'?filters=%7B%22status%22%3A%5B%22running%22%5D%7D'


## References
https://learn.microsoft.com/en-us/dotnet/core/extensions/workers
https://stackoverflow.com/questions/52838808/how-to-enable-expose-daemon-on-tcp-localhost2375-without-tls-on-mac




{
        "Table": [
          {
            "CustomerID": "e60d285c-a773-4dae-bc96-5f1a729da3ec",
            "FirstName": "Tom",
            "LastName": "Smith",
            "Email": "tom.smith@example.com",
            "RegistrationDate": "2024-11-14T00:00:00",
            "SYS_CHANGE_VERSION": 20,
            "SYS_CHANGE_CREATION_VERSION": 20,
            "SYS_CHANGE_OPERATION": "I",
            "SYS_CHANGE_COLUMNS": {},
            "SYS_CHANGE_CONTEXT": {},
            "CustomerID1": "e60d285c-a773-4dae-bc96-5f1a729da3ec"
          },
          {
            "CustomerID": "6400e7eb-7acd-4d50-86d1-7958613ee6a7",
            "FirstName": "Jerry",
            "LastName": "Port",
            "Email": "jerry.port@example.com",
            "RegistrationDate": "2024-11-14T00:00:00",
            "SYS_CHANGE_VERSION": 20,
            "SYS_CHANGE_CREATION_VERSION": 20,
            "SYS_CHANGE_OPERATION": "I",
            "SYS_CHANGE_COLUMNS": {},
            "SYS_CHANGE_CONTEXT": {},
            "CustomerID1": "6400e7eb-7acd-4d50-86d1-7958613ee6a7"
          },
          {
            "CustomerID": {},
            "FirstName": {},
            "LastName": {},
            "Email": {},
            "RegistrationDate": {},
            "SYS_CHANGE_VERSION": 19,
            "SYS_CHANGE_CREATION_VERSION": {},
            "SYS_CHANGE_OPERATION": "D",
            "SYS_CHANGE_COLUMNS": {},
            "SYS_CHANGE_CONTEXT": {},
            "CustomerID1": "56b317c3-c323-418c-8e25-d185926630d4"
          },
          {
            "CustomerID": {},
            "FirstName": {},
            "LastName": {},
            "Email": {},
            "RegistrationDate": {},
            "SYS_CHANGE_VERSION": 19,
            "SYS_CHANGE_CREATION_VERSION": {},
            "SYS_CHANGE_OPERATION": "D",
            "SYS_CHANGE_COLUMNS": {},
            "SYS_CHANGE_CONTEXT": {},
            "CustomerID1": "dc663293-b926-4da2-9e17-e99bbe2605ea"
          }
        ]
      }