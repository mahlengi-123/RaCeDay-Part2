FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["RaCeDay Part 2.csproj", "./"]
RUN dotnet restore "RaCeDay Part 2.csproj"
COPY . .
RUN dotnet publish "RaCeDay Part 2.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads/profiles && chown -R "$APP_UID" /app/wwwroot/uploads
USER $APP_UID
ENTRYPOINT ["dotnet", "RaCeDay Part 2.dll"]
