# Example minimal api

## Simple

```sh
dotnet run
```

## Minimal with auth/rate limiting/caching

```sh
dotnet run
curl -i http://localhost:{port}/info
dotnet user-jwts create --role "user" --role "admin"
curl -i -H "Authorization: Bearer {token}" https://localhost:{port}/hello
curl -i -H "Authorization: Bearer {token}" https://localhost:{port}/auth/test
curl -i -H "Authorization: Bearer {token}" https://localhost:{port}/auth/admin
```

## todo

Example of output cache and response cache
