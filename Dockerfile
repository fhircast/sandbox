# build image
FROM microsoft/dotnet:2.1-sdk as build
WORKDIR /app

COPY *.csproj .
RUN dotnet restore

COPY . .
RUN dotnet publish --output /out/ --configuration Release

# runtime image
FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app
COPY --from=build /out .
ENTRYPOINT [ "dotnet", "Hub.dll" ]

EXPOSE 80
