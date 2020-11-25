# DNS over TLS Proxy
N26 Challenge to create a DNS TLS proxy 

## TODO

- Integration tests
- Check //TODO in code
- Answer questions
- Explain TCP and UDP proxy mechanism
- Explain how to build/debug in vscode container
- Check DNS mesh container
- Check K8S DNS override
- Review commits and create clean history
- Refacto DNS/TCP 2 bytes in UdpProxy
- Make generic DnsTlsClient class that would accept the request data and return the response

## Run

### Default

```bash
docker build . --tag dns-tls-proxy
docker rm -f dns-tls-proxy
docker run -d -p 5053:5053 -p 5053:5053/udp --name dns-tls-proxy dns-tls-proxy
dig @127.0.0.1 -p 5053 n26.com
dig @127.0.0.1 +tcp -p 5053 n26.com
```
### Custom config

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

### Default values

- AppConfig__LocalIp="0.0.0.0"
- AppConfig__LocalPort="5053"
- AppConfig__DnsIp="1.1.1.1"
- AppConfig__DnsPort="853"

## Technical choices

DNS/TCP => 2 first bytes are the length of the DNS question

## Build

## Questions

### 1. Imagine this proxy being deployed in an infrastructure. What would be the security concerns you would raise?

a

### 2. How would you integrate that solution in a distributed, microservices-oriented and containerized architecture?

a

### 3. What other improvements do you think would be interesting to add to the project?

a

## References

- <https://github.com/justingarfield/dns-over-tls/>
- <https://github.com/kapetan/dns>
- <https://docs.microsoft.com/en-us/dotnet/api/system.net.security.sslstream?view=netcore-3.1>
- https://github.com/briancurt/encrypted-dns-proxy
