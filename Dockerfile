# build image
FROM microsoft/aspnetcore-build:2.1 as build
WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish --output /out/ --configuration Release

# runtime image
FROM microsoft/aspnetcore:2.1
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT [ "dotnet", "Hub.dll" ]

EXPOSE 80
