FROM mcr.microsoft.com/dotnet/sdk:5.0 as build

# Install node
RUN curl -sL https://deb.nodesource.com/setup_16.x | bash
RUN apt-get update && apt-get install -y nodejs

WORKDIR /workspace
COPY . .
RUN dotnet tool restore
RUN npm install

RUN dotnet publish src/Server/Server.fsproj -o "deploy"
RUN dotnet fable --cwd src/Client -o output -s
RUN npm run build


# EXPOSE 8085 used for local testing
# EXPOSE $PORT is used for production in heroku
FROM mcr.microsoft.com/dotnet/aspnet:5.0-alpine
COPY --from=build /workspace/deploy /app
WORKDIR /app
EXPOSE 8085
ENTRYPOINT [ "dotnet", "Server.dll" ]
