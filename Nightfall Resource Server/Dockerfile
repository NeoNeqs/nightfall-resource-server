FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Nightfall Resource Server/Nightfall Resource Server.csproj", "Nightfall Resource Server/"]
RUN dotnet restore "Nightfall Resource Server/Nightfall Resource Server.csproj"
COPY . .
WORKDIR "/src/Nightfall Resource Server"
RUN dotnet build "Nightfall Resource Server.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Nightfall Resource Server.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app
EXPOSE 5000
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Nightfall Resource Server.dll"]
