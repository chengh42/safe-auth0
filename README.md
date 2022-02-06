# safe-auth0 [![Deploy to heroku](https://github.com/chengh42/safe-auth0/actions/workflows/deploy-heroku.yml/badge.svg)](https://github.com/chengh42/safe-auth0/actions/workflows/deploy-heroku.yml)

Demo of [SAFE Stack](https://safe-stack.github.io/) with authentication using [Auth0](https://auth0.com/).

## Prerequisites

* [.NET Core SDK](https://www.microsoft.com/net/download) 5.0 or higher
* [Node LTS](https://nodejs.org/en/download/)

## Development

```bash
# for the first time use only
dotnet tool restore

# development
dotnet run

# production
dotnet run bundle
```
