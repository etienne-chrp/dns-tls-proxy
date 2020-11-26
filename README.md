# DNS over TLS Proxy

- [DNS over TLS Proxy](#dns-over-tls-proxy)
  - [TODO](#todo)
  - [Description](#description)
  - [Build](#build)
    - [Docker image](#docker-image)
    - [Dotnet](#dotnet)
  - [Run](#run)
    - [Docker](#docker)
      - [Default](#default)
      - [Custom config](#custom-config)
    - [Dotnet](#dotnet-1)
    - [Default config](#default-config)
  - [Test](#test)
    - [Linux](#linux)
    - [Windows](#windows)
  - [Technical consideratons](#technical-consideratons)
    - [DNS TCP message length](#dns-tcp-message-length)
    - [Byte array Endian](#byte-array-endian)
  - [VS Code remote container](#vs-code-remote-container)
  - [Questions](#questions)
    - [1. Imagine this proxy being deployed in an infrastructure. What would be the security concerns you would raise?](#1-imagine-this-proxy-being-deployed-in-an-infrastructure-what-would-be-the-security-concerns-you-would-raise)
    - [2. How would you integrate that solution in a distributed, microservices-oriented and containerized architecture?](#2-how-would-you-integrate-that-solution-in-a-distributed-microservices-oriented-and-containerized-architecture)
    - [3. What other improvements do you think would be interesting to add to the project?](#3-what-other-improvements-do-you-think-would-be-interesting-to-add-to-the-project)
  - [References](#references)

## TODO

- Answer questions
- Integration tests
- Check DNS mesh container
- Check K8S DNS override

## Description

Most applications are not nativelly supporting DNS over TLS, the goal of this project is to create a proxy that will forward proxy unsecured DNS request to a secured DNS server. 

```
+-----------+ DNS Query +-----------+ DNS Query +-----------+
|           |---------->|           |---------->|           |
|  Client   | 53tcp/udp |   Proxy   |   853tcp  |  DNS TLS  |
|           |<----------|           |<----------|  Server   |           
+-----------+           +-----------+    TLS    +-----------+
```

## Build

### Docker image

```bash
docker build . --tag dns-tls-proxy
```

### Dotnet

```bash
dotnet build
```

## Run

### Docker

#### Default

```bash
docker rm -f dns-tls-proxy
docker run -d -p 5053:5053 -p 5053:5053/udp --name dns-tls-proxy dns-tls-proxy
```
#### Custom config

```bash
docker run -d \
    -p 6053:6053 \
    -p 6053:6053/udp \
    --name dns-tls-proxy \
    --env AppConfig__LocalPort="6053" \
    --env AppConfig__DnsIp="8.8.8.8" \
    --env AppConfig__DnsPort="853" \
    dns-tls-proxy
```
### Dotnet

```bash
dotnet run
```
### Default config

- AppConfig__LocalIp="0.0.0.0"
- AppConfig__LocalPort="5053"
- AppConfig__DnsIp="1.1.1.1"
- AppConfig__DnsPort="853"

## Test

### Linux

```bash
dig @127.0.0.1 -p 5053 n26.com
dig @127.0.0.1 +tcp -p 5053 n26.com
```

### Windows

```
nslookup -port=5053 n26.com 127.0.0.1
nslookup -set=vc -port=5053 n26.com 127.0.0.1 
```

## Technical consideratons

### DNS TCP message length

> Messages sent over TCP connections use server port 53 (decimal). The message is prefixed with a two byte length field which gives the message

<https://tools.ietf.org/html/rfc1035#section-4.2.2>

### Byte array Endian

In order to convert to an integer the 2 bytes of the DNS message length you need to verify the way it is stored.

```C#
BitConverter.IsLittleEndian
```

> Different computer architectures store data using different byte orders.
> "Little-endian" means the most significant byte is on the right end of a word.
  
<https://docs.microsoft.com/en-us/dotnet/api/system.bitconverter.islittleendian>

## VS Code remote container

The project is containing the configuration files to be opened in a VS Code remote container. All you need is to install the *Remote - Containers* extension: `ms-vscode-remote.remote-containers`.

## Questions

### 1. Imagine this proxy being deployed in an infrastructure. What would be the security concerns you would raise?

a

### 2. How would you integrate that solution in a distributed, microservices-oriented and containerized architecture?

a

### 3. What other improvements do you think would be interesting to add to the project?

- Integration tests <https://wright-development.github.io/post/using-docker-for-net-core/>
- 

## References

- <https://github.com/justingarfield/dns-over-tls/>
- <https://github.com/kapetan/dns>
- <https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netcore-3.1>
- <https://github.com/briancurt/encrypted-dns-proxy>
